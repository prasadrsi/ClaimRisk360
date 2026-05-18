using ClaimRisk360.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimRisk360.Data;

/// <summary>
/// Seeds the SQLite database with sample data (equivalent to the JSON seed files).
/// Only runs if the database is empty.
/// </summary>
public static class DatabaseSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Claims.Any()) return; // Already seeded

        SeedRolesAndUsers(db);
        SeedClaims(db);
        SeedFraudRings(db);
        SeedAuditEntries(db);
        db.SaveChanges();

        SeedDocuments(db);
        db.SaveChanges();
    }

    private static void SeedRolesAndUsers(AppDbContext db)
    {
        var roles = new List<AppRole>
        {
            new() { RoleId = "admin", RoleName = "Administrator",
                Description = "Full system access including user management, configuration, and all operational features",
                BadgeClass = "bg-danger", IconClass = "bi-shield-lock",
                Permissions = AllPermissions() },
            new() { RoleId = "investigator", RoleName = "Investigator",
                Description = "Fraud investigation, case management, claim review, and network analysis",
                BadgeClass = "bg-warning text-dark", IconClass = "bi-search",
                Permissions = new() { CanViewDashboard=true, CanViewClaims=true, CanReviewClaim=true,
                    CanViewFraudAlerts=true, CanViewPatterns=true, CanViewMlModels=true,
                    CanManageCases=true, CanEscalateClaim=true, CanViewFraudRings=true,
                    CanViewProviderProfiles=true, CanViewReports=true, CanViewAuditTrail=true, CanViewEthicsReport=true } },
            new() { RoleId = "approver", RoleName = "Approver",
                Description = "Authorized to approve, reject, or escalate claims after investigation review",
                BadgeClass = "bg-success", IconClass = "bi-check-circle",
                Permissions = new() { CanViewDashboard=true, CanViewClaims=true, CanReviewClaim=true,
                    CanViewFraudAlerts=true, CanManageCases=true, CanApproveClaim=true, CanRejectClaim=true,
                    CanEscalateClaim=true, CanViewProviderProfiles=true, CanViewReports=true,
                    CanExportReports=true, CanViewAuditTrail=true } },
            new() { RoleId = "auditor", RoleName = "Auditor",
                Description = "Read-only access to audit trails, reports, ethics, and compliance data",
                BadgeClass = "bg-info", IconClass = "bi-journal-check",
                Permissions = new() { CanViewDashboard=true, CanViewClaims=true, CanReviewClaim=true,
                    CanViewFraudAlerts=true, CanViewPatterns=true, CanViewMlModels=true,
                    CanViewFraudRings=true, CanViewProviderProfiles=true, CanViewReports=true,
                    CanExportReports=true, CanViewAuditTrail=true, CanViewEthicsReport=true } },
            new() { RoleId = "claimprocessor", RoleName = "Claim Processor",
                Description = "Submit and view claims, limited fraud view, no investigation or admin access",
                BadgeClass = "bg-primary", IconClass = "bi-inbox",
                Permissions = new() { CanSubmitClaim=true, CanViewClaims=true } },
            new() { RoleId = "viewer", RoleName = "Viewer",
                Description = "Read-only access to dashboard and claims. No actions permitted",
                BadgeClass = "bg-secondary", IconClass = "bi-eye",
                Permissions = new() { CanViewDashboard=true, CanViewClaims=true } },
        };
        db.AppRoles.AddRange(roles);

        var users = new List<AppUser>
        {
            new() { UserId = "USR-001", DisplayName = "Sarah Chen", Email = "sarah.chen@claimrisk360.com", RoleId = "investigator", Department = "Fraud Investigation", IsActive = true },
            new() { UserId = "USR-002", DisplayName = "James Rivera", Email = "james.rivera@claimrisk360.com", RoleId = "approver", Department = "Claims Management", IsActive = true },
            new() { UserId = "USR-003", DisplayName = "Priya Sharma", Email = "priya.sharma@claimrisk360.com", RoleId = "investigator", Department = "Fraud Investigation", IsActive = true },
            new() { UserId = "USR-004", DisplayName = "Marcus Johnson", Email = "marcus.johnson@claimrisk360.com", RoleId = "admin", Department = "IT Administration", IsActive = true },
            new() { UserId = "USR-005", DisplayName = "Emily Foster", Email = "emily.foster@claimrisk360.com", RoleId = "auditor", Department = "Compliance", IsActive = true },
            new() { UserId = "USR-006", DisplayName = "David Kim", Email = "david.kim@claimrisk360.com", RoleId = "claimprocessor", Department = "Claims Processing", IsActive = true },
            new() { UserId = "USR-007", DisplayName = "Lisa Wang", Email = "lisa.wang@claimrisk360.com", RoleId = "approver", Department = "Claims Management", IsActive = true },
            new() { UserId = "USR-008", DisplayName = "Robert Taylor", Email = "robert.taylor@claimrisk360.com", RoleId = "viewer", Department = "Executive Office", IsActive = true },
            new() { UserId = "USR-009", DisplayName = "Ana Lopez", Email = "ana.lopez@claimrisk360.com", RoleId = "claimprocessor", Department = "Claims Processing", IsActive = false },
            new() { UserId = "USR-010", DisplayName = "Tom Baker", Email = "tom.baker@claimrisk360.com", RoleId = "investigator", Department = "Fraud Investigation", IsActive = true },
        };
        db.AppUsers.AddRange(users);
    }

    private static RolePermissions AllPermissions() => new()
    {
        CanViewDashboard = true, CanSubmitClaim = true, CanViewClaims = true, CanReviewClaim = true,
        CanViewFraudAlerts = true, CanViewPatterns = true, CanViewMlModels = true,
        CanManageCases = true, CanApproveClaim = true, CanRejectClaim = true, CanEscalateClaim = true,
        CanViewFraudRings = true, CanViewProviderProfiles = true,
        CanViewReports = true, CanExportReports = true,
        CanViewAuditTrail = true, CanViewEthicsReport = true,
        CanManageUsers = true, CanManageRoles = true, CanConfigureSystem = true
    };

    private static void SeedClaims(AppDbContext db)
    {
        var patients = new[] { "John Smith", "Maria Garcia", "David Lee", "Sarah Johnson", "James Brown",
            "Linda Martinez", "Michael Wilson", "Jennifer Anderson", "Robert Thomas", "Patricia Jackson",
            "William Harris", "Elizabeth Clark", "Richard Lewis", "Barbara Walker", "Joseph Hall" };
        var patientIds = patients.Select((_, i) => $"PAT-{1000 + i}").ToArray();
        var providers = new[] { "Dr. Emily Chen", "Metro Health Clinic", "Valley Medical Group", "Dr. Richard Park",
            "Sunrise Hospital", "Dr. Amanda Foster", "Pacific Labs", "CityPharm Pharmacy", "Dr. Kevin Wright" };
        var providerIds = providers.Select((_, i) => $"PRV-{100 + i}").ToArray();
        var specialties = new[] { "Cardiology", "Orthopedics", "General Practice", "Neurology", "Emergency", "Radiology", "Pathology", "Pharmacy", "Internal Medicine" };
        var diagCodes = new[] { "I25.1", "M54.5", "J06.9", "E11.9", "K21.0", "L30.9", "G43.9", "S82.0", "I10", "J18.9" };
        var procCodes = new[] { "99213", "99214", "99215", "99223", "99232", "99291", "36415", "71046", "80053", "85025" };
        var locations = new[] { "New York, NY", "Los Angeles, CA", "Chicago, IL", "Houston, TX", "Phoenix, AZ", "Philadelphia, PA", "San Antonio, TX", "San Diego, CA" };
        var fraudTypes = new[] { "Legitimate", "Legitimate", "Legitimate", "Provider Fraud", "Patient Fraud", "Pharmacy Fraud", "Collusion", "Legitimate" };

        var claims = new List<Claim>();
        for (int i = 0; i < 50; i++)
        {
            var pi = Random.Shared.Next(patients.Length);
            var pri = Random.Shared.Next(providers.Length);
            var score = i switch { < 8 => Random.Shared.Next(75, 98), < 20 => Random.Shared.Next(35, 74), _ => Random.Shared.Next(5, 34) };
            var fraudType = score > 70 ? fraudTypes[Random.Shared.Next(3, fraudTypes.Length)] : (score > 30 ? fraudTypes[Random.Shared.Next(fraudTypes.Length)] : "Legitimate");

            var reasons = new List<string>();
            if (score > 70) reasons.AddRange(["Unusual billing pattern", "Amount exceeds peer average"]);
            if (score > 50) reasons.Add("Frequency anomaly detected");
            if (score > 30) reasons.Add("Minor deviation from norm");

            claims.Add(new Claim
            {
                ClaimId = $"CLM-{2024000 + i}",
                PatientName = patients[pi],
                PatientId = patientIds[pi],
                ProviderName = providers[pri],
                ProviderId = providerIds[pri],
                Specialty = specialties[pri],
                DiagnosisCode = diagCodes[Random.Shared.Next(diagCodes.Length)],
                ProcedureCode = procCodes[Random.Shared.Next(procCodes.Length)],
                Amount = Math.Round((decimal)(Random.Shared.NextDouble() * 15000 + 200), 2),
                SubmissionDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90)),
                Location = locations[Random.Shared.Next(locations.Length)],
                FraudRiskScore = score,
                RiskReasons = reasons,
                FraudType = fraudType,
                Status = "Pending"
            });
        }
        db.Claims.AddRange(claims);
    }

    private static void SeedFraudRings(AppDbContext db)
    {
        var ring1 = new FraudRing
        {
            RingId = "RING-001", Label = "Provider-Patient Collusion Network",
            ClaimCount = 14, TotalAmount = 87500, RiskScore = 92,
            Nodes =
            [
                new() { Id = "P1", FraudRingId = "RING-001", Label = "Dr. Emily Chen", Type = "Doctor", X = 300, Y = 200 },
                new() { Id = "P2", FraudRingId = "RING-001", Label = "John Smith", Type = "Patient", X = 100, Y = 100 },
                new() { Id = "P3", FraudRingId = "RING-001", Label = "Maria Garcia", Type = "Patient", X = 500, Y = 100 },
                new() { Id = "P4", FraudRingId = "RING-001", Label = "CityPharm", Type = "Pharmacy", X = 300, Y = 400 },
                new() { Id = "P5", FraudRingId = "RING-001", Label = "Pacific Labs", Type = "Hospital", X = 100, Y = 350 },
            ],
            Edges =
            [
                new() { FraudRingId = "RING-001", From = "P1", To = "P2", Relationship = "Treated", Weight = 8 },
                new() { FraudRingId = "RING-001", From = "P1", To = "P3", Relationship = "Treated", Weight = 6 },
                new() { FraudRingId = "RING-001", From = "P2", To = "P4", Relationship = "Prescription", Weight = 5 },
                new() { FraudRingId = "RING-001", From = "P3", To = "P4", Relationship = "Prescription", Weight = 4 },
                new() { FraudRingId = "RING-001", From = "P1", To = "P5", Relationship = "Referral", Weight = 7 },
            ]
        };

        var ring2 = new FraudRing
        {
            RingId = "RING-002", Label = "Pharmacy Kickback Ring",
            ClaimCount = 9, TotalAmount = 42300, RiskScore = 85,
            Nodes =
            [
                new() { Id = "Q1", FraudRingId = "RING-002", Label = "Dr. Richard Park", Type = "Doctor", X = 300, Y = 200 },
                new() { Id = "Q2", FraudRingId = "RING-002", Label = "David Lee", Type = "Patient", X = 100, Y = 100 },
                new() { Id = "Q3", FraudRingId = "RING-002", Label = "CityPharm", Type = "Pharmacy", X = 500, Y = 300 },
            ],
            Edges =
            [
                new() { FraudRingId = "RING-002", From = "Q1", To = "Q2", Relationship = "Treated", Weight = 5 },
                new() { FraudRingId = "RING-002", From = "Q1", To = "Q3", Relationship = "Referral", Weight = 9 },
                new() { FraudRingId = "RING-002", From = "Q2", To = "Q3", Relationship = "Filled Rx", Weight = 5 },
            ]
        };

        db.FraudRings.AddRange(ring1, ring2);
    }

    private static void SeedAuditEntries(AppDbContext db)
    {
        var actions = new[] { "Claim Submitted", "Risk Scored", "Rule Check", "Case Created", "Document Uploaded", "Decision Made" };
        var performers = new[] { "System", "Sarah Chen", "James Rivera", "Priya Sharma", "Marcus Johnson" };
        var categories = new[] { "Submission", "Analysis", "Rule Engine", "Case Management", "Documents", "Decision" };

        for (int i = 0; i < 40; i++)
        {
            db.AuditEntries.Add(new AuditEntry
            {
                AuditId = $"AUD-{i + 1:D5}",
                ClaimId = $"CLM-{2024000 + Random.Shared.Next(0, 50)}",
                Action = actions[Random.Shared.Next(actions.Length)],
                PerformedBy = performers[Random.Shared.Next(performers.Length)],
                Timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60)).AddHours(Random.Shared.Next(0, 12)),
                Details = $"Automated processing step {i + 1}",
                Category = categories[Random.Shared.Next(categories.Length)]
            });
        }
    }

    private static void SeedDocuments(AppDbContext db)
    {
        var docTypes = new[] { "Medical Report", "Invoice", "Lab Result", "Prescription", "ID Proof", "Insurance Card" };
        var uploaders = new[] { "System", "Sarah Chen", "David Kim" };
        var claims = db.Claims.ToList();

        for (int i = 0; i < 30; i++)
        {
            var claimIdx = Random.Shared.Next(0, Math.Min(25, claims.Count));
            var claim = claims[claimIdx];
            var docType = docTypes[Random.Shared.Next(docTypes.Length)];
            db.ClaimDocuments.Add(new ClaimDocument
            {
                DocumentId = $"DOC-{i + 1:D5}",
                ClaimId = claim.ClaimId,
                FileName = $"{docType.ToLower().Replace(" ", "-")}-{claimIdx + 1}.pdf",
                DocumentType = docType,
                FileSizeBytes = Random.Shared.Next(50_000, 5_000_000),
                UploadedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                UploadedBy = uploaders[Random.Shared.Next(uploaders.Length)],
                Version = 1,
                Status = "Verified",
                Content = GenerateDocumentContent(docType, claim)
            });
        }
    }

    private static string GenerateDocumentContent(string docType, Claim claim)
    {
        var date = claim.SubmissionDate.ToString("MMMM dd, yyyy");
        var refNo = $"REF-{Random.Shared.Next(100000, 999999)}";

        return docType switch
        {
            "Medical Report" => $"""
                <div class="doc-header">MEDICAL REPORT</div>
                <table class="doc-table">
                    <tr><td class="doc-label">Report Reference</td><td>{refNo}</td></tr>
                    <tr><td class="doc-label">Patient Name</td><td>{claim.PatientName}</td></tr>
                    <tr><td class="doc-label">Patient ID</td><td>{claim.PatientId}</td></tr>
                    <tr><td class="doc-label">Date of Service</td><td>{date}</td></tr>
                    <tr><td class="doc-label">Attending Physician</td><td>{claim.ProviderName}</td></tr>
                    <tr><td class="doc-label">Facility</td><td>{claim.Location}</td></tr>
                    <tr><td class="doc-label">Diagnosis Code</td><td>{claim.DiagnosisCode}</td></tr>
                    <tr><td class="doc-label">Procedure Code</td><td>{claim.ProcedureCode}</td></tr>
                </table>
                <div class="doc-section">Clinical Notes</div>
                <p>Patient presented with symptoms consistent with diagnosis {claim.DiagnosisCode}. 
                Physical examination performed. Procedure {claim.ProcedureCode} was administered per standard protocol. 
                Patient tolerated procedure well. Follow-up recommended in 2 weeks.</p>
                <div class="doc-section">Assessment &amp; Plan</div>
                <p>Continue current treatment plan. Monitor for complications. 
                Referral to {claim.Specialty} specialist if symptoms persist.</p>
                <div class="doc-footer">
                    <strong>Signed:</strong> {claim.ProviderName}, MD<br/>
                    <strong>Date:</strong> {date}<br/>
                    <em>This document is confidential and intended for authorized use only.</em>
                </div>
                """,

            "Invoice" => $"""
                <div class="doc-header">HEALTHCARE INVOICE</div>
                <table class="doc-table">
                    <tr><td class="doc-label">Invoice Number</td><td>INV-{Random.Shared.Next(10000, 99999)}</td></tr>
                    <tr><td class="doc-label">Claim Reference</td><td>{claim.ClaimId}</td></tr>
                    <tr><td class="doc-label">Date Issued</td><td>{date}</td></tr>
                    <tr><td class="doc-label">Provider</td><td>{claim.ProviderName}</td></tr>
                    <tr><td class="doc-label">Provider ID</td><td>{claim.ProviderId}</td></tr>
                    <tr><td class="doc-label">Patient</td><td>{claim.PatientName}</td></tr>
                </table>
                <div class="doc-section">Line Items</div>
                <table class="doc-table">
                    <tr class="doc-table-header"><td>Description</td><td>Code</td><td>Qty</td><td>Amount</td></tr>
                    <tr><td>Consultation — {claim.Specialty}</td><td>{claim.ProcedureCode}</td><td>1</td><td>${claim.Amount * 0.3m:N2}</td></tr>
                    <tr><td>Diagnostic Assessment ({claim.DiagnosisCode})</td><td>{claim.DiagnosisCode}</td><td>1</td><td>${claim.Amount * 0.25m:N2}</td></tr>
                    <tr><td>Procedure / Treatment</td><td>{claim.ProcedureCode}</td><td>1</td><td>${claim.Amount * 0.35m:N2}</td></tr>
                    <tr><td>Administrative &amp; Facility Fee</td><td>ADMIN</td><td>1</td><td>${claim.Amount * 0.1m:N2}</td></tr>
                    <tr class="doc-table-header"><td colspan="3"><strong>Total</strong></td><td><strong>${claim.Amount:N2}</strong></td></tr>
                </table>
                <div class="doc-section">Payment Terms</div>
                <p>Net 30 days. Payable to {claim.ProviderName}. Tax ID: XX-XXX{Random.Shared.Next(1000, 9999)}</p>
                <div class="doc-footer">
                    <em>This invoice is subject to audit and verification.</em>
                </div>
                """,

            "Lab Result" => $"""
                <div class="doc-header">LABORATORY RESULTS</div>
                <table class="doc-table">
                    <tr><td class="doc-label">Lab Reference</td><td>LAB-{Random.Shared.Next(100000, 999999)}</td></tr>
                    <tr><td class="doc-label">Patient</td><td>{claim.PatientName} ({claim.PatientId})</td></tr>
                    <tr><td class="doc-label">Ordering Physician</td><td>{claim.ProviderName}</td></tr>
                    <tr><td class="doc-label">Collection Date</td><td>{date}</td></tr>
                    <tr><td class="doc-label">Report Date</td><td>{claim.SubmissionDate.AddDays(2):MMMM dd, yyyy}</td></tr>
                </table>
                <div class="doc-section">Test Results</div>
                <table class="doc-table">
                    <tr class="doc-table-header"><td>Test</td><td>Result</td><td>Reference Range</td><td>Flag</td></tr>
                    <tr><td>Complete Blood Count (CBC)</td><td>{Random.Shared.Next(38, 52)}%</td><td>38.0–50.0%</td><td>Normal</td></tr>
                    <tr><td>Hemoglobin</td><td>{12.0 + Random.Shared.NextDouble() * 6:F1} g/dL</td><td>12.0–17.5 g/dL</td><td>Normal</td></tr>
                    <tr><td>White Blood Cell</td><td>{4.0 + Random.Shared.NextDouble() * 8:F1} K/uL</td><td>4.5–11.0 K/uL</td><td>Normal</td></tr>
                    <tr><td>Glucose, Fasting</td><td>{70 + Random.Shared.Next(0, 60)} mg/dL</td><td>70–100 mg/dL</td><td>{(Random.Shared.NextDouble() > 0.7 ? "<span class='text-danger'>HIGH</span>" : "Normal")}</td></tr>
                    <tr><td>Creatinine</td><td>{0.6 + Random.Shared.NextDouble() * 0.8:F2} mg/dL</td><td>0.6–1.2 mg/dL</td><td>Normal</td></tr>
                </table>
                <div class="doc-footer">
                    <strong>Pathologist:</strong> Dr. Lab Director<br/>
                    <em>Results verified electronically. This report is for medical use only.</em>
                </div>
                """,

            "Prescription" => $"""
                <div class="doc-header">PRESCRIPTION</div>
                <table class="doc-table">
                    <tr><td class="doc-label">Rx Number</td><td>RX-{Random.Shared.Next(100000, 999999)}</td></tr>
                    <tr><td class="doc-label">Date</td><td>{date}</td></tr>
                    <tr><td class="doc-label">Patient</td><td>{claim.PatientName}</td></tr>
                    <tr><td class="doc-label">DOB</td><td>{DateTime.Today.AddYears(-Random.Shared.Next(25, 75)):MM/dd/yyyy}</td></tr>
                    <tr><td class="doc-label">Prescriber</td><td>{claim.ProviderName}</td></tr>
                    <tr><td class="doc-label">DEA Number</td><td>AB{Random.Shared.Next(1000000, 9999999)}</td></tr>
                </table>
                <div class="doc-section">Medication</div>
                <table class="doc-table">
                    <tr class="doc-table-header"><td>Drug</td><td>Strength</td><td>Qty</td><td>Directions</td></tr>
                    <tr><td>Amoxicillin</td><td>500mg</td><td>30</td><td>Take 1 capsule three times daily</td></tr>
                </table>
                <p><strong>Refills:</strong> 2 &nbsp; <strong>DAW:</strong> No</p>
                <div class="doc-footer">
                    <strong>Signature:</strong> {claim.ProviderName}, MD<br/>
                    <em>Valid for 12 months from date of issue.</em>
                </div>
                """,

            "ID Proof" => $"""
                <div class="doc-header">IDENTITY VERIFICATION</div>
                <table class="doc-table">
                    <tr><td class="doc-label">Document Type</td><td>Government-Issued Photo ID</td></tr>
                    <tr><td class="doc-label">Full Name</td><td>{claim.PatientName}</td></tr>
                    <tr><td class="doc-label">ID Number</td><td>***-**-{Random.Shared.Next(1000, 9999)}</td></tr>
                    <tr><td class="doc-label">Date of Birth</td><td>{DateTime.Today.AddYears(-Random.Shared.Next(25, 75)):MM/dd/yyyy}</td></tr>
                    <tr><td class="doc-label">Address</td><td>{claim.Location}</td></tr>
                    <tr><td class="doc-label">Verified</td><td><span class="text-success">? Identity Confirmed</span></td></tr>
                </table>
                <div class="doc-footer">
                    <em>Copy retained per HIPAA compliance requirements. Original returned to patient.</em>
                </div>
                """,

            "Insurance Card" => $"""
                <div class="doc-header">INSURANCE CARD — COPY</div>
                <table class="doc-table">
                    <tr><td class="doc-label">Insurance Plan</td><td>HealthGuard PPO</td></tr>
                    <tr><td class="doc-label">Member Name</td><td>{claim.PatientName}</td></tr>
                    <tr><td class="doc-label">Member ID</td><td>{claim.PatientId}</td></tr>
                    <tr><td class="doc-label">Group Number</td><td>GRP-{Random.Shared.Next(10000, 99999)}</td></tr>
                    <tr><td class="doc-label">Plan Type</td><td>PPO — Standard</td></tr>
                    <tr><td class="doc-label">Effective Date</td><td>{DateTime.Today.AddYears(-Random.Shared.Next(1, 5)):MM/dd/yyyy}</td></tr>
                    <tr><td class="doc-label">Copay (Office)</td><td>$25.00</td></tr>
                    <tr><td class="doc-label">Copay (Specialist)</td><td>$50.00</td></tr>
                    <tr><td class="doc-label">Deductible</td><td>$1,500.00</td></tr>
                </table>
                <div class="doc-footer">
                    <em>For verification purposes only. Contact insurer for benefit details.</em>
                </div>
                """,

            _ => $"""
                <div class="doc-header">DOCUMENT</div>
                <p>Document for claim {claim.ClaimId}, patient {claim.PatientName}.</p>
                <p>Uploaded on {date}.</p>
                """
        };
    }
}
