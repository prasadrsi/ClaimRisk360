# CLAIMRISK 360 — High-Level Design Document

| Field | Value |
|-------|-------|
| **Project** | ClaimRisk 360 — End-to-End Claim Risk Intelligence Platform |
| **Version** | 1.0 |
| **Target** | .NET 10 / ASP.NET Core Razor Pages |
| **Database** | SQLite via Entity Framework Core |
| **Auth** | Microsoft Entra ID (Azure AD) + OIDC |
| **Real-time** | SignalR |

---

## 1. Executive Summary

ClaimRisk 360 is an end-to-end healthcare claim fraud detection and risk intelligence platform. It combines rule-based fraud detection, AI/ML risk scoring, pattern analysis, network (collusion) graph analysis, digital fingerprinting, and straight-through processing (STP) into a single web application with role-based access control, real-time notifications, and full audit trails.

### Key Capabilities

| Capability | Description |
|-----------|-------------|
| Claim Ingestion & Validation | Schema, business rule, and reference data validation at upload |
| AI Risk Scoring | Isolation Forest + GNN-based scoring with SHAP explainability |
| Rule Engine | 7 configurable fraud detection rules (duplicates, thresholds, blacklists, timing, eligibility) |
| Pattern Analysis | 6 statistical/behavioral anomaly detectors |
| Digital Risk | 5 device/network signal types (VPN, geo mismatch, bot, rapid submission, device reuse) |
| STP (Auto-Decisioning) | Auto-approve (score ? 25), auto-reject (score ? 85), route grey-zone to human review |
| Case Management | Assign, review, escalate, approve/reject with mandatory justification |
| Provider Profiling | Risk scoring by peer benchmarking, flag rate, volume, collusion links |
| Network/Collusion Analysis | Graph visualization of provider-patient-pharmacy fraud rings |
| Document Management | Upload, view, and audit supporting documents with rendered content |
| Reporting & Analytics | Fraud savings, false positive trends, productivity metrics |
| Audit & Compliance | Immutable audit trail for every action; HIPAA/GDPR considerations |
| Ethics & Governance | Bias audits, privacy framework, governance reporting |
| Real-time Notifications | SignalR-powered toast notifications, badge updates, auto-refresh |
| RBAC | 6 roles, 21 granular permissions, session-based role switching |

---

## 2. Architecture Overview

### 2.1 Layered Architecture

```
????????????????????????????????????????????????????????
?                   PRESENTATION LAYER                  ?
?  Razor Pages (.cshtml) + Bootstrap 5 + Bootstrap Icons?
?  SignalR Client (notifications.js)                    ?
????????????????????????????????????????????????????????
?                  BUSINESS LOGIC LAYER                 ?
?  Services: FraudDetection, ClaimValidation, Audit,   ?
?  CaseManagement, RuleEngine, PatternAnalysis,        ?
?  DigitalRisk, ClaimApproval, ProviderProfile,        ?
?  Document, Role, Notification                        ?
????????????????????????????????????????????????????????
?                    DATA ACCESS LAYER                  ?
?  Repositories: Claim, Audit, Document, User          ?
?  AppDbContext (EF Core) + ReferenceDataRepository    ?
????????????????????????????????????????????????????????
?                    INFRASTRUCTURE                     ?
?  SQLite Database ? SignalR Hub ? Azure AD (OIDC)     ?
?  Microsoft Graph ? Static Assets                      ?
????????????????????????????????????????????????????????
```

### 2.2 Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Razor Pages, Bootstrap 5.3, Bootstrap Icons 1.11, Inter font |
| Backend | ASP.NET Core 10 (.NET 10), C# 14 |
| Database | SQLite via Microsoft.EntityFrameworkCore.Sqlite |
| Authentication | Microsoft Identity Web (Entra ID / Azure AD OIDC) |
| Real-time | ASP.NET Core SignalR |
| Hosting | Azure App Service (Linux/Windows) |

### 2.3 Component Diagram

```
                    ???????????????
                    ?  Azure AD   ?
                    ?  (Entra ID) ?
                    ???????????????
                           ? OIDC
    ???????????????????????????????????????????????
    ?                 ASP.NET Core                  ?
    ?  ??????????????????????????????????????????  ?
    ?  ?         Razor Pages (18 pages)         ?  ?
    ?  ??????????????????????????????????????????  ?
    ?  ?        11 Business Services            ?  ?
    ?  ??????????????????????????????????????????  ?
    ?  ?     5 Repositories + AppDbContext       ?  ?
    ?  ??????????????????????????????????????????  ?
    ?  ?   SQLite DB  ?  SignalR NotificationHub ?  ?
    ?  ??????????????????????????????????????????  ?
    ????????????????????????????????????????????????
                           ?
              ???????????????????????????
              ?            ?            ?
         Browser 1    Browser 2    Browser N
         (SignalR)    (SignalR)    (SignalR)
```

---

## 3. Data Model

### 3.1 Entity Relationship Summary

| Entity | Key | Relationships |
|--------|-----|--------------|
| **Claim** | ClaimId (string) | Has many: Documents, AuditEntries, StpDecisions, DigitalRiskSignals |
| **FraudRing** | RingId (string) | Has many: GraphNodes, GraphEdges |
| **GraphNode** | GraphNodeId (int, auto) | FK ? FraudRing.RingId |
| **GraphEdge** | GraphEdgeId (int, auto) | FK ? FraudRing.RingId |
| **AuditEntry** | AuditId (string) | Optional FK ? CaseReview.CaseId |
| **CaseReview** | CaseId (string) | Has many: AuditEntry (History) |
| **ClaimDocument** | DocumentId (string) | FK ? Claim.ClaimId (logical) |
| **AppUser** | UserId (string) | FK ? AppRole.RoleId (logical) |
| **AppRole** | RoleId (string) | Owns: RolePermissions (21 booleans) |
| **DigitalRiskSignal** | SignalId (string) | FK ? Claim.ClaimId (logical) |
| **StpDecision** | StpDecisionId (int, auto) | FK ? Claim.ClaimId (logical) |
| **RuleCheckResult** | RuleId (string) | Computed on-the-fly (not persisted) |
| **ProviderProfile** | ProviderId (string) | Computed on-the-fly (not persisted) |
| **ClaimPattern** | PatternId (string) | Computed on-the-fly (not persisted) |

### 3.2 Database Details

- **Engine:** SQLite (file: `claimrisk360.db` in ContentRootPath)
- **ORM:** Entity Framework Core 10 with `AppDbContext`
- **14 DbSet tables** with indexes on frequently queried columns
- **JSON columns:** `Claim.RiskReasons`, `ProviderProfile.RiskIndicators` (via EF value converters)
- **Owned type:** `AppRole.Permissions` ? stored as 21 boolean columns in AppRoles table
- **Seeding:** `DatabaseSeeder` + static seed methods in services, idempotent on startup
- **Schema migration:** Auto-detect stale schema and recreate (dev/demo mode)

---

## 4. Service Layer Design

### 4.1 Business Services (11 total)

| Service | Responsibility | Lifetime |
|---------|---------------|----------|
| `FraudDetectionService` | Risk scoring, explainability, dashboard stats, fraud ring queries | Scoped |
| `ClaimValidationService` | 14 claim upload validation rules (required fields, dates, reference data) | Scoped |
| `AuditService` | Immutable audit trail logging | Scoped |
| `DocumentService` | Document CRUD + audit logging | Scoped |
| `CaseManagementService` | Case lifecycle: create, assign, review, escalate, resolve | Scoped |
| `RuleEngineService` | 7 fraud detection rules (duplicates, thresholds, blacklists, timing, eligibility) | Scoped |
| `PatternAnalysisService` | 6 statistical anomaly patterns (frequency, amount, timing, geographic, behavioral) | Scoped |
| `DigitalRiskService` | 5 device/network signal types + STP decisions | Scoped |
| `ClaimApprovalService` | STP auto-approval/rejection + manual approval with mandatory comment | Scoped |
| `ProviderProfileService` | Provider risk scoring (flag rate × 0.4 + deviation × 0.3 + volume × 3) | Scoped |
| `RoleService` | RBAC: current user, role, permissions, user switching | Scoped |
| `NotificationService` | SignalR broadcast: toasts, badge updates, data refresh signals | Singleton |

### 4.2 Data Repositories (5 total)

| Repository | Backing | Lifetime |
|-----------|---------|----------|
| `ClaimRepository` | AppDbContext (Claims, FraudRings with Include) | Scoped |
| `AuditRepository` | AppDbContext (AuditEntries) | Scoped |
| `DocumentRepository` | AppDbContext (ClaimDocuments) | Scoped |
| `UserRepository` | AppDbContext (AppUsers, AppRoles with owned Permissions) | Scoped |
| `ReferenceDataRepository` | In-memory (static lookup data, no DB) | Singleton |

---

## 5. Page Inventory

### 5.1 All Pages (18)

| Page | Route | Purpose | Key Permission |
|------|-------|---------|----------------|
| Index | `/` | Home dashboard with role-based section cards | All |
| Dashboard | `/Dashboard` | KPIs, charts, fraud trends, recent high-risk | CanViewDashboard |
| ClaimUpload | `/ClaimUpload` | Submit new claim with documents + validation | CanSubmitClaim |
| Claims | `/Claims` | Claims inbox with risk/approval filters, approve/reject modal | CanViewClaims |
| Explainability | `/Explainability` | AI explanation: SHAP features, risk reasons, documents | CanReviewClaim |
| RuleEngine | `/RuleEngine` | Fraud alerts: triggered rules by category/severity | CanViewFraudAlerts |
| PatternAnalysis | `/PatternAnalysis` | Behavioral anomaly patterns | CanViewPatterns |
| DigitalRisk | `/DigitalRisk` | Device fingerprinting, STP decisions, ML model info | CanViewMlModels |
| CaseManagement | `/CaseManagement` | Investigation cases: assign, review, decide | CanManageCases |
| FraudRings | `/FraudRings` | Network graph visualization of collusion rings | CanViewFraudRings |
| ProviderProfiling | `/ProviderProfiling` | Provider risk profiles, peer benchmarking | CanViewProviderProfiles |
| Reports | `/Reports` | Analytics: fraud savings, false positives, productivity | CanViewReports |
| AuditTrail | `/AuditTrail` | Immutable action history | CanViewAuditTrail |
| EthicsReport | `/EthicsReport` | Bias audits, governance framework | CanViewEthicsReport |
| UserManagement | `/UserManagement` | Users, roles, permissions admin + role switching | CanManageUsers |
| DocumentViewer | `/DocumentViewer` | Rendered document content with metadata sidebar | CanViewClaims |
| RulesDocumentation | `/RulesDocumentation` | Complete rules reference (43+ rules across 8 categories) | All |
| Privacy | `/Privacy` | Data retention, PII, HIPAA/GDPR | All |

---

## 6. Role-Based Access Control

### 6.1 Roles (6)

| Role | Badge | Target User |
|------|-------|-------------|
| **Administrator** | ?? bg-danger | IT admins with full system control |
| **Investigator** | ?? bg-warning | Fraud analysts performing investigations |
| **Approver** | ?? bg-success | Claims managers making approve/reject decisions |
| **Auditor** | ?? bg-info | Compliance officers with read-only access |
| **Claim Processor** | ?? bg-primary | Front-line staff submitting claims |
| **Viewer** | ? bg-secondary | Executives with dashboard-only access |

### 6.2 Permission Matrix

| Permission | Admin | Investigator | Approver | Auditor | Processor | Viewer |
|-----------|:-----:|:------------:|:--------:|:-------:|:---------:|:------:|
| CanViewDashboard | ? | ? | ? | ? | | ? |
| CanSubmitClaim | ? | | | | ? | |
| CanViewClaims | ? | ? | ? | ? | ? | ? |
| CanReviewClaim | ? | ? | ? | ? | | |
| CanViewFraudAlerts | ? | ? | ? | ? | | |
| CanViewPatterns | ? | ? | | ? | | |
| CanViewMlModels | ? | ? | | ? | | |
| CanManageCases | ? | ? | ? | | | |
| CanApproveClaim | ? | | ? | | | |
| CanRejectClaim | ? | | ? | | | |
| CanEscalateClaim | ? | ? | ? | | | |
| CanViewFraudRings | ? | ? | | ? | | |
| CanViewProviderProfiles | ? | ? | ? | ? | | |
| CanViewReports | ? | ? | ? | ? | | |
| CanExportReports | ? | | ? | ? | | |
| CanViewAuditTrail | ? | ? | ? | ? | | |
| CanViewEthicsReport | ? | ? | | ? | | |
| CanManageUsers | ? | | | | | |
| CanManageRoles | ? | | | | | |
| CanConfigureSystem | ? | | | | | |

---

## 7. Evaluation Rules Summary

### 7.1 Rule Categories (43+ rules)

| Category | Count | Service | Persistence |
|----------|-------|---------|-------------|
| Claim Validation | 14 | ClaimValidationService | Not persisted (runtime) |
| Fraud Detection (Rule Engine) | 7 | RuleEngineService | Not persisted (runtime) |
| Pattern Analysis | 6 | PatternAnalysisService | Not persisted (runtime) |
| STP Auto-Decisioning | 3 | ClaimApprovalService + DigitalRiskService | Persisted (Claims, StpDecisions) |
| Manual Approval | 5 | ClaimApprovalService + CaseManagementService | Persisted (Claims, CaseReviews) |
| AI Features (SHAP) | 8 | FraudDetectionService | Not persisted (runtime) |
| Digital Risk Signals | 5 | DigitalRiskService | Persisted (DigitalRiskSignals) |
| Provider Risk Indicators | 5 | ProviderProfileService | Not persisted (runtime) |

### 7.2 Key Thresholds

| Threshold | Value | Used By |
|-----------|-------|---------|
| Auto-approve risk score | ? 25 | STP, ClaimApprovalService |
| Auto-reject risk score | ? 85 | STP, ClaimApprovalService |
| Digital risk flag reject | ? 2 Critical/High signals | STP |
| Amount threshold (fraud alert) | > $10,000 | RuleEngineService |
| Amount threshold (critical) | > $25,000 | RuleEngineService |
| Amount warning (validation) | > $50,000 | ClaimValidationService |
| Patient frequency threshold | > 5 claims | RuleEngineService |
| Patient frequency (critical) | > 8 claims | RuleEngineService |
| Duplicate window | 3 days | RuleEngineService |
| Patient frequency (pattern) | ? 4 claims | PatternAnalysisService |
| Provider frequency (pattern) | ? 6 claims | PatternAnalysisService |
| Amount anomaly multiplier | > 2.5× average | PatternAnalysisService |
| Near-policy-limit | > $12,000 | PatternAnalysisService |
| Provider deviation alert | > 40% from peers | ProviderProfileService |
| Provider flag rate alert | > 40% | ProviderProfileService |
| Provider volume alert | > 8 claims | ProviderProfileService |
| Stale claim cutoff | > 1 year old | ClaimValidationService |

---

## 8. Real-Time Notification System

### 8.1 Architecture

```
Services ??? NotificationService ??? SignalR Hub ??? All Connected Browsers
                                       ?
                              ???????????????????
                              ?        ?        ?
                          Toast    Badge     Refresh
                         Notification  Update    Bar
```

### 8.2 SignalR Events

| Event | Payload | Trigger | Client Action |
|-------|---------|---------|---------------|
| `ReceiveNotification` | title, message, type, claimId | Approve/Reject/Escalate/Role switch | Toast notification + sound |
| `BadgeUpdate` | pendingCount (int) | Claim approve/reject | Update bell badge in header |
| `DataRefresh` | area, entityId | Any data mutation | "Refresh Now" bar on affected pages, auto-refresh 15s |

---

## 9. Authentication & Security

| Aspect | Implementation |
|--------|---------------|
| **Identity Provider** | Microsoft Entra ID (Azure AD) via OIDC |
| **Library** | Microsoft.Identity.Web 3.14 |
| **Token Cache** | In-memory |
| **Authorization** | All pages require authentication (FallbackPolicy = DefaultPolicy) |
| **RBAC** | Application-level via RoleService + RolePermissions model |
| **Audit** | Every significant action logged to AuditEntries table |
| **HTTPS** | Enforced via HSTS + redirect middleware |
| **Cookie Policy** | SameSite=Unspecified with compatibility handling |

---

## 10. Deployment Architecture

```
????????????????????????????????????????????
?            Azure App Service              ?
?  ??????????????????????????????????????  ?
?  ?   ASP.NET Core 10 Application      ?  ?
?  ?   ???????????? ?????????????????? ?  ?
?  ?   ? Razor    ? ? SignalR Hub    ? ?  ?
?  ?   ? Pages    ? ? /hubs/notif.  ? ?  ?
?  ?   ???????????? ?????????????????? ?  ?
?  ?   ???????????????????????????????? ?  ?
?  ?   ? SQLite: claimrisk360.db     ? ?  ?
?  ?   ? (ContentRootPath)           ? ?  ?
?  ?   ???????????????????????????????? ?  ?
?  ??????????????????????????????????????  ?
?                    ?                      ?
?           ??????????????????             ?
?           ? Azure AD OIDC  ?             ?
?           ??????????????????             ?
????????????????????????????????????????????
```

---

## 11. Seed Data & Demo Mode

| Data | Count | Source |
|------|-------|--------|
| Roles | 6 | DatabaseSeeder |
| Users | 10 | DatabaseSeeder |
| Claims | 50 | DatabaseSeeder |
| Fraud Rings | 2 (with nodes/edges) | DatabaseSeeder |
| Audit Entries | 40 | DatabaseSeeder |
| Documents | 30 (with rendered HTML content) | DatabaseSeeder |
| Case Reviews | Up to 12 (flagged claims) | CaseManagementService.SeedCases |
| Digital Risk Signals | Variable (from 25 claims) | DigitalRiskService.SeedDigitalData |
| STP Decisions | 50 (one per claim) | DigitalRiskService.SeedDigitalData |

Startup flow: `EnsureCreated ? Schema Check ? Seed ? SeedCases ? SeedDigitalData ? ApplyAutoApprovals`

---

## 12. Non-Functional Requirements

| NFR | Target |
|-----|--------|
| **Performance** | All pages render < 500ms (SQLite indexed queries) |
| **Scalability** | Single-instance (SQLite). For scale-out: migrate to SQL Server/PostgreSQL |
| **Availability** | Azure App Service SLA (99.95%) |
| **Security** | Azure AD + HTTPS + RBAC + audit trail |
| **Compliance** | HIPAA-aware (audit logging, PII handling), GDPR-aware (privacy page) |
| **Accessibility** | Bootstrap 5 responsive, mobile sidebar (offcanvas) |
| **Browser Support** | Modern browsers (Chrome, Edge, Firefox, Safari) |

---

*Document generated from codebase analysis. All rules, thresholds, and architecture reflect the current implementation.*
