using System.Text.Json;
using ClaimRisk360.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimRisk360.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<FraudRing> FraudRings => Set<FraudRing>();
    public DbSet<GraphNode> GraphNodes => Set<GraphNode>();
    public DbSet<GraphEdge> GraphEdges => Set<GraphEdge>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<CaseReview> CaseReviews => Set<CaseReview>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<RuleCheckResult> RuleCheckResults => Set<RuleCheckResult>();
    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();
    public DbSet<ClaimPattern> ClaimPatterns => Set<ClaimPattern>();
    public DbSet<DigitalRiskSignal> DigitalRiskSignals => Set<DigitalRiskSignal>();
    public DbSet<StpDecision> StpDecisions => Set<StpDecision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- JSON conversions for List<string> ---
        var jsonOptions = new JsonSerializerOptions();

        modelBuilder.Entity<Claim>(e =>
        {
            e.Property(c => c.RiskReasons)
             .HasConversion(
                 v => JsonSerializer.Serialize(v, jsonOptions),
                 v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());
        });

        modelBuilder.Entity<ProviderProfile>(e =>
        {
            e.Property(p => p.RiskIndicators)
             .HasConversion(
                 v => JsonSerializer.Serialize(v, jsonOptions),
                 v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());
        });

        // --- Owned type: RolePermissions stored as columns in AppRoles table ---
        modelBuilder.Entity<AppRole>()
            .OwnsOne(r => r.Permissions);

        // --- FraudRing -> GraphNode/GraphEdge ---
        modelBuilder.Entity<GraphNode>()
            .HasOne<FraudRing>()
            .WithMany(r => r.Nodes)
            .HasForeignKey(n => n.FraudRingId);

        modelBuilder.Entity<GraphEdge>()
            .HasOne<FraudRing>()
            .WithMany(r => r.Edges)
            .HasForeignKey(e => e.FraudRingId);

        // --- CaseReview -> AuditEntry (History) ---
        modelBuilder.Entity<AuditEntry>()
            .HasOne<CaseReview>()
            .WithMany(c => c.History)
            .HasForeignKey(a => a.CaseReviewId)
            .IsRequired(false);

        // --- Indexes (Existing) ---
        modelBuilder.Entity<Claim>().HasIndex(c => c.FraudRiskScore);
        modelBuilder.Entity<Claim>().HasIndex(c => c.ApprovalStatus);
        modelBuilder.Entity<Claim>().HasIndex(c => c.ProviderId);
        modelBuilder.Entity<AuditEntry>().HasIndex(a => a.ClaimId);
        modelBuilder.Entity<AuditEntry>().HasIndex(a => a.Timestamp);
        modelBuilder.Entity<CaseReview>().HasIndex(c => c.ClaimId);
        modelBuilder.Entity<CaseReview>().HasIndex(c => c.Status);
        modelBuilder.Entity<ClaimDocument>().HasIndex(d => d.ClaimId);
        modelBuilder.Entity<DigitalRiskSignal>().HasIndex(s => s.ClaimId);
        modelBuilder.Entity<StpDecision>().HasIndex(s => s.ClaimId);

        // --- Indexes (New - for Performance) ---
        // Pattern analysis and duplicate detection
        modelBuilder.Entity<Claim>().HasIndex(c => c.PatientId);
        modelBuilder.Entity<Claim>().HasIndex(c => c.DiagnosisCode);
        // Composite index for duplicate detection
        modelBuilder.Entity<Claim>().HasIndex(c => new { c.ProviderId, c.SubmissionDate });
        modelBuilder.Entity<Claim>().HasIndex(c => new { c.PatientId, c.SubmissionDate });

        // Audit and case review
        modelBuilder.Entity<AuditEntry>().HasIndex(a => a.CaseReviewId);

        // Fraud rings
        modelBuilder.Entity<FraudRing>().HasIndex(r => r.RingId);
    }
}
