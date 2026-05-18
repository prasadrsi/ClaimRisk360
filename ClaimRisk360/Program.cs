using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.EntityFrameworkCore;
using ClaimRisk360.Data;
using ClaimRisk360.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ') ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ');

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
            .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
            .AddInMemoryTokenCaches()
            .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
            .AddInMemoryTokenCaches();

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new OpenIdConnectEvents();
    var existingRedirect = options.Events.OnRedirectToIdentityProvider;
    options.Events.OnRedirectToIdentityProvider = async context =>
    {
        if (existingRedirect != null)
            await existingRedirect(context);

        context.ProtocolMessage.RedirectUri = context.ProtocolMessage.RedirectUri?.Replace("http://", "https://");
    };
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.HandleSameSiteCookieCompatibility();
});

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

// Add in-memory caching for performance optimization
builder.Services.AddMemoryCache();

// EF Core + SQLite
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "claimrisk360.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Data Layer � Repositories
builder.Services.AddScoped<ClaimRepository>();
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<DocumentRepository>();
builder.Services.AddSingleton<ReferenceDataRepository>();
builder.Services.AddScoped<UserRepository>();

// Caching utility
builder.Services.AddScoped<CacheHelper>();

// Business Logic Layer � Services
builder.Services.AddScoped<FraudDetectionService>();
builder.Services.AddScoped<ClaimValidationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<CaseManagementService>();
builder.Services.AddScoped<RuleEngineService>();
builder.Services.AddScoped<ProviderProfileService>();
builder.Services.AddScoped<PatternAnalysisService>();
builder.Services.AddScoped<DigitalRiskService>();
builder.Services.AddScoped<ClaimApprovalService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCookiePolicy();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();
app.MapHub<ClaimRisk360.Hubs.NotificationHub>("/hubs/notifications");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Recreate database if schema is outdated (no migrations � dev/demo mode)
        if (db.Database.EnsureCreated())
        {
            logger.LogInformation("Database created at {Path}", dbPath);
        }
        else
        {
            // Verify schema is current by checking for a recently added column
            try
            {
                _ = db.ClaimDocuments.Select(d => d.Content).FirstOrDefault();
            }
            catch
            {
                logger.LogWarning("Schema outdated � recreating database at {Path}", dbPath);
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
        }

        DatabaseSeeder.Seed(db);
        CaseManagementService.SeedCases(db);
        DigitalRiskService.SeedDigitalData(db);

        var approvalService = scope.ServiceProvider.GetRequiredService<ClaimApprovalService>();
        approvalService.ApplyAutoApprovals();

        logger.LogInformation("Database seeding complete");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database at {Path}", dbPath);
        throw;
    }
}

app.Run();

