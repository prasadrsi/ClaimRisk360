using ClaimRisk360.Api.Authentication;
using ClaimRisk360.Api.Hubs;
using ClaimRisk360.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();


// Register application services
builder.Services.AddSingleton<ClaimValidationService>();
builder.Services.AddSingleton<ClaimRuleEvaluationService>();
builder.Services.AddSingleton<DocumentValidationService>();
builder.Services.AddSingleton<AzureFoundryAgentService>();
builder.Services.AddSingleton<ClaimReviewNotifier>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// API Key authentication
app.UseApiKeyAuthentication();

app.MapControllers();
app.MapHub<ClaimReviewHub>("/hubs/claim-review");

// Health check endpoint (no auth required)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
