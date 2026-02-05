# ReportingSystem - HORS (Hierarchical Organizational Reporting System)

## Project Overview

**Purpose**: ASP.NET Core 8.0 Razor Pages enterprise web application for hierarchical organizational reporting with bi-directional communication flows:
- **Upward Flow**: Reports, suggested actions, resource requests, support requests from subordinates to management
- **Downward Flow**: Feedback, recommendations, decisions from management to subordinates
- **Workflow**: Comments, confirmation tags, threaded discussions for collaboration
- **Analytics**: Aggregation, dashboards, charts, export, ad-hoc reporting

**Target Users**: Higher education institutions (GUC - German University in Cairo used as reference) with organizational hierarchy from Root → Campuses → Faculties → Departments → Sectors → Teams.

---

## Tech Stack

| Layer | Technology | Notes |
|-------|------------|-------|
| Framework | ASP.NET Core 8.0 | Razor Pages (not MVC) |
| Database | EF Core 8.0 | SQLite (dev) / SQL Server (prod) |
| Auth | Cookie-based | Magic link passwordless login (15-min token, 30-day sliding cookie) |
| Email | Microsoft Graph API | Disabled in dev mode (emails logged to console) |
| Frontend | Bootstrap 5.3.3 | CDN for CSS, bundled JS + Bootstrap Icons |
| Charts | Chart.js 4.4.1 | CDN, line/bar/doughnut charts |
| Validation | jQuery Validation | Bundled with jQuery |
| Data Protection | ASP.NET Core DP | Keys persisted to `/keys` folder |

---

## Build & Run

```bash
# Working directory
cd /home/user/ReportingSystem

# Restore (local NuGet feed - NuGet.org blocked by proxy)
dotnet restore --source /home/user/ReportingSystem/local-packages/ \
  /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# Build
dotnet build --no-restore /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# Run
dotnet run --project /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# Dev URLs: http://localhost:5296 / https://localhost:7155
```

**Important**: Delete `db/reporting.db` before running if schema changed (EnsureCreatedAsync doesn't migrate).

---

## Project Structure

```
ReportingSystem/
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext (25 DbSets, all relationships)
│   ├── SeedData.cs                # C# seeding for minimal dev data
│   └── UserSeeder.cs              # Admin user creation on startup
├── Filters/
│   └── AutomaticBackupFilter.cs   # Pre-POST/PUT/DELETE backup trigger
├── Models/                        # 25 domain entities
│   ├── User.cs, MagicLink.cs      # Auth (Phase 1)
│   ├── OrganizationalUnit.cs, Delegation.cs  # Org hierarchy (Phase 2)
│   ├── ReportTemplate.cs, ReportField.cs, ReportPeriod.cs  # Templates (Phase 3)
│   ├── Report.cs, ReportFieldValue.cs, Attachment.cs       # Reports (Phase 3)
│   ├── SuggestedAction.cs, ResourceRequest.cs, SupportRequest.cs  # Upward (Phase 4)
│   ├── Comment.cs, ConfirmationTag.cs                      # Workflow (Phase 5)
│   ├── Feedback.cs, Recommendation.cs, Decision.cs         # Downward (Phase 6)
│   ├── AggregationRule.cs, AggregatedValue.cs, ManagerAmendment.cs  # Aggregation (Phase 7)
│   ├── AuditLog.cs, Notification.cs, DatabaseBackup.cs     # Infrastructure
│   └── SavedReport.cs             # Ad-hoc reports (Phase 8)
├── Pages/
│   ├── Admin/                     # Requires authentication
│   │   ├── Aggregation/           # Rules/, Summary/
│   │   ├── AuditLog/              # Index
│   │   ├── Backup/                # Index, Create, Restore, Delete
│   │   ├── Dashboards/            # Executive, Manager, Reviewer, Originator
│   │   ├── Delegations/           # Index, Create
│   │   ├── Downward/              # Feedback/, Recommendations/, Decisions/
│   │   ├── Export/                # Download, Print
│   │   ├── OrgUnits/              # Index, Create, Edit, Delete, Details
│   │   ├── Periods/               # Index, Create, Edit
│   │   ├── ReportBuilder/         # Index, SavedReports
│   │   ├── Reports/               # Index, Create, Fill, View
│   │   ├── Templates/             # Index, Create, Edit, Delete, Details
│   │   ├── UpwardFlow/            # SuggestedActions/, ResourceRequests/, SupportRequests/
│   │   ├── Users/                 # Index, Create, Edit, Delete, Details
│   │   ├── Workflow/              # Comments/, Confirmations/
│   │   └── Dashboard.cshtml       # Main dashboard with quick actions
│   ├── Auth/                      # Anonymous access
│   │   ├── Login.cshtml           # Email entry
│   │   ├── Verify.cshtml          # Magic link validation
│   │   └── Logout.cshtml          # Sign out
│   └── Shared/
│       ├── _Layout.cshtml         # Main layout with nav, Bootstrap, Chart.js
│       └── _ValidationScriptsPartial.cshtml
├── Services/
│   ├── MagicLinkService.cs        # Token generation/validation
│   ├── EmailService.cs            # Microsoft Graph API emails
│   ├── NotificationService.cs     # In-app notifications CRUD
│   ├── DatabaseBackupService.cs   # Create/restore/delete backups
│   ├── DailyBackupHostedService.cs  # Automatic backups at 2:00/14:00 UTC
│   ├── DashboardService.cs        # KPI queries, chart data
│   └── ExportService.cs           # CSV/Excel/PDF export
├── wwwroot/
│   ├── css/site.css               # Custom styles
│   ├── js/site.js, filter-state.js  # Filter persistence
│   └── lib/                       # Bootstrap, jQuery (bundled)
├── Program.cs                     # DI setup, auth config, database init
├── appsettings.json               # Production config template
├── appsettings.Development.json   # SQLite connection string
├── seed.sql                       # SQL seed script (full sample data)
└── DEMO_GUIDE.md                  # Comprehensive demo walkthrough
```

---

## Architecture Patterns

| Pattern | Implementation |
|---------|----------------|
| **Razor Pages** | All pages use `[BindProperty]` for form binding, PageModel pattern |
| **EF Core** | Eager loading with `.Include()`, async operations, nullable refs |
| **Auth** | Cookie auth with folder-level policies (`/Admin` requires auth) |
| **Flash Messages** | `TempData["SuccessMessage"]` / `TempData["ErrorMessage"]` |
| **Modals** | Bootstrap modals for confirmations and inline forms |
| **Filter Persistence** | `filter-state.js` saves to URL query params + sessionStorage |
| **Status Constants** | String constants with `DisplayName()` and `BadgeClass()` helpers |
| **File-scoped namespaces** | `namespace X;` syntax (not `namespace X { }`) |
| **Computed Properties** | `[NotMapped]` for IsOpen, IsOverdue, DisplayValue, etc. |

---

## Database Schema

### Connection Strings
- **Dev**: `Data Source=db/reporting.db` (SQLite)
- **Prod**: SQL Server (configure in appsettings.json)

### Tables (25 total)

| Table | Phase | Key Fields |
|-------|-------|------------|
| `Users` | 1 | Email (unique), Role, OrganizationalUnitId, IsActive |
| `MagicLinks` | 1 | Token (unique), UserId, ExpiresAt, IsUsed |
| `Notifications` | 1 | UserId, Type, Priority, IsRead, Link |
| `DatabaseBackups` | 1 | Name, FileName, FilePath, FileSizeBytes, Type |
| `OrganizationalUnits` | 2 | Name, Code (unique), ParentId (self-ref), Level, IsActive |
| `Delegations` | 2 | DelegatorId, DelegateId, Scope, StartDate, EndDate, IsRevoked |
| `ReportTemplates` | 3 | Name, Schedule, Version, IncludeSuggestedActions/Resources/Support |
| `ReportTemplateAssignments` | 3 | ReportTemplateId, AssignmentType, TargetId, IncludeSubUnits |
| `ReportFields` | 3 | ReportTemplateId, Type, Section, Label, OptionsJson, Formula |
| `ReportPeriods` | 3 | ReportTemplateId, StartDate, EndDate, SubmissionDeadline, Status |
| `Reports` | 3 | TemplateId+PeriodId+SubmittedById (unique), Status, ReviewComments |
| `ReportFieldValues` | 3 | ReportId+FieldId (unique), Value, NumericValue |
| `Attachments` | 3 | ReportId, FileName, ContentType, StoragePath |
| `SuggestedActions` | 4 | ReportId, Title, Category, Priority, Status, ReviewedById |
| `ResourceRequests` | 4 | ReportId, Title, Category, Urgency, EstimatedCost, Status |
| `SupportRequests` | 4 | ReportId, Title, Category, Urgency, AssignedToId, Status |
| `Comments` | 5 | ReportId, ParentCommentId, AuthorId, Content, Status |
| `ConfirmationTags` | 5 | ReportId, RequestedById, TaggedUserId, Status, Response |
| `Feedbacks` | 6 | ReportId, AuthorId, Category, Visibility, Status, ParentFeedbackId |
| `Recommendations` | 6 | ReportId, IssuedById, TargetScope, Category, Priority, Status |
| `Decisions` | 6 | ReportId, DecidedById, RequestType, Outcome, ApprovedAmount |
| `AggregationRules` | 7 | ReportFieldId, Method, Priority, CustomFormula, IsActive |
| `AggregatedValues` | 7 | RuleId+PeriodId+OrgUnitId (unique), Value, Status |
| `ManagerAmendments` | 7 | AggregatedValueId, AmendedById, AmendmentType, AmendedValue |
| `AuditLogs` | 7 | UserId, Action, EntityType, EntityId, OldValue, NewValue, Timestamp |
| `SavedReports` | 8 | CreatedById+Name (unique), ReportType, FilterConfiguration |

---

## Services (7)

| Service | File | Purpose |
|---------|------|---------|
| `MagicLinkService` | Services/MagicLinkService.cs | Generate tokens, validate links, cleanup expired |
| `EmailService` | Services/EmailService.cs | Send via Microsoft Graph (disabled in dev) |
| `NotificationService` | Services/NotificationService.cs | Create, mark read, get unread count |
| `DatabaseBackupService` | Services/DatabaseBackupService.cs | Create/restore/delete backups, WAL checkpoint |
| `DailyBackupHostedService` | Services/DailyBackupHostedService.cs | Background service for 2:00/14:00 UTC backups |
| `DashboardService` | Services/DashboardService.cs | KPI queries, chart data for all 4 role dashboards |
| `ExportService` | Services/ExportService.cs | CSV/Excel/print-HTML export for reports, upward flow, audit |

---

## Models Reference (25)

### Phase 1: Infrastructure

**User** (`Models/User.cs`)
```csharp
Id, Email (unique), Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt
// Nav: OrganizationalUnit, MagicLinks, SubmittedReports, ReviewedReports
```

**MagicLink** (`Models/MagicLink.cs`)
```csharp
Id, UserId, Token (unique), ExpiresAt, IsUsed, CreatedAt
```

**Notification** (`Models/Notification.cs`)
```csharp
Id, UserId, Type, Title, Message, Priority, Link, IsRead, CreatedAt, ReadAt
```

**DatabaseBackup** (`Models/DatabaseBackup.cs`)
```csharp
Id, Name, Description, FileName, FilePath, FileSizeBytes, Type, CreatedAt, CreatedBy
```

### Phase 2: Organization

**OrganizationalUnit** (`Models/OrganizationalUnit.cs`)
```csharp
Id, Name, Code (unique), Description, ParentId (self-ref), Level (enum 0-5), IsActive
// Nav: Parent, Children, Users
// Computed: FullPath, Depth, IsLeaf
```

**Delegation** (`Models/Delegation.cs`)
```csharp
Id, DelegatorId, DelegateId, Scope, StartDate, EndDate, IsRevoked, Reason, RevokedAt
// Scope: Full, ReportingOnly, ApprovalOnly
// Computed: IsCurrentlyEffective
```

### Phase 3: Reporting

**ReportTemplate** (`Models/ReportTemplate.cs`)
```csharp
Id, Name, Description, Schedule, Version, IsActive, CreatedById, CreatedAt
IncludeSuggestedActions, IncludeNeededResources, IncludeNeededSupport
AutoSaveIntervalMinutes, MaxAttachments, MaxAttachmentSizeMb
// Nav: Fields, Periods, Reports, Assignments, CreatedBy
```

**ReportField** (`Models/ReportField.cs`)
```csharp
Id, ReportTemplateId, FieldKey, Label, Description, Type (enum 0-7), Section
SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, ValidationRegex
OptionsJson, DefaultValue, Placeholder, Formula, VisibilityConditionJson
PrePopulateFromPrevious, HelpText
// Types: Text, TextArea, Numeric, Date, Dropdown, Checkbox, RichText, TableGrid
```

**ReportPeriod** (`Models/ReportPeriod.cs`)
```csharp
Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays
Status (enum: Upcoming/Open/Closed/Archived), Notes
// Computed: IsOpen, IsOverdue, IsFullyClosed, DaysUntilDeadline
```

**Report** (`Models/Report.cs`)
```csharp
Id, ReportTemplateId, ReportPeriodId, SubmittedById, Status, AssignedReviewerId
CreatedAt, SubmittedAt, ReviewedAt, ReviewComments, AmendmentCount, IsLocked
// Status: Draft, Submitted, UnderReview, Approved, Rejected, RevisionRequested
// Computed: IsEditable, IsPendingReview, StatusDisplayName
// Nav: FieldValues, Attachments, SuggestedActions, ResourceRequests, SupportRequests
//      Comments, ConfirmationTags, Feedbacks, Recommendations, Decisions
```

**ReportFieldValue** (`Models/ReportFieldValue.cs`)
```csharp
Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, ModifiedAt
```

**Attachment** (`Models/Attachment.cs`)
```csharp
Id, ReportId, ReportFieldId, FileName, OriginalFileName, ContentType
StoragePath, FileSizeBytes, UploadedById, UploadedAt
// Computed: FileSizeDisplay
```

### Phase 4: Upward Flow

**SuggestedAction** (`Models/SuggestedAction.cs`)
```csharp
Id, ReportId, Title, Description, Justification, ExpectedOutcome, Timeline
Category, Priority, Status, ReviewedById, ReviewComments, CreatedAt, UpdatedAt
// Category: Process, Policy, Training, Technology, Communication, Resource, Other
// Priority: Critical, High, Medium, Low
// Status: Submitted, UnderReview, Approved, Rejected, Implemented, Deferred
```

**ResourceRequest** (`Models/ResourceRequest.cs`)
```csharp
Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency
EstimatedCost, ApprovedAmount, Currency, Status, ReviewedById, FulfilledAt
// Category: Budget, Equipment, Personnel, Space, Software, Materials, Other
// Urgency: Critical, High, Medium, Low
// Status: Submitted, UnderReview, Approved, PartiallyApproved, Rejected, Fulfilled
```

**SupportRequest** (`Models/SupportRequest.cs`)
```csharp
Id, ReportId, Title, Description, CurrentSituation, DesiredOutcome
Category, Urgency, Status, AssignedToId, AcknowledgedById, ResolvedById
AcknowledgedAt, ResolvedAt, Resolution
// Category: Management, Technical, Administrative, Training, Policy, Other
// Urgency: Critical, High, Medium, Low
// Status: Submitted, Acknowledged, InProgress, Resolved, Closed
// Computed: IsOpen
```

### Phase 5: Workflow

**Comment** (`Models/Comment.cs`)
```csharp
Id, ReportId, ParentCommentId, AuthorId, Content, SectionReference, ReportFieldId
MentionedUserIdsJson, Status, CreatedAt, EditedAt
// Status: Active, Edited, Deleted, Hidden
// Computed: IsReply, IsDeleted
// Nav: Replies
```

**ConfirmationTag** (`Models/ConfirmationTag.cs`)
```csharp
Id, ReportId, RequestedById, TaggedUserId, ReportFieldId, SectionReference
Message, Status, Response, CreatedAt, RespondedAt, ReminderSentAt
// Status: Pending, Confirmed, RevisionRequested, Declined, Expired, Cancelled
// Computed: IsPending, IsConfirmed, DaysSinceRequested
```

### Phase 6: Downward Flow

**Feedback** (`Models/Feedback.cs`)
```csharp
Id, ReportId, AuthorId, ParentFeedbackId, ReportFieldId, SectionReference
Category, Visibility, Status, Subject, Content
RequiresAcknowledgment, IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse
// Category: PositiveRecognition, Concern, Observation, Question, General
// Visibility: Private, TeamWide, DepartmentWide, OrganizationWide
// Status: Active, Resolved, Archived
// Computed: IsPendingAcknowledgment
// Nav: Replies
```

**Recommendation** (`Models/Recommendation.cs`)
```csharp
Id, ReportId, IssuedById, TargetOrgUnitId, TargetUserId, TargetScope
Category, Priority, Status, Title, Description, Rationale, ActionItems
DueDate, EffectiveDate, CascadeToSubUnits
IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse
// TargetScope: Individual, Team, Department, OrganizationWide
// Category: ProcessChange, SkillDevelopment, PerformanceImprovement, etc.
// Status: Draft, Issued, Acknowledged, InProgress, Completed, Cancelled
// Computed: IsOverdue, DaysUntilDue
```

**Decision** (`Models/Decision.cs`)
```csharp
Id, ReportId, DecidedById, RequestType, SuggestedActionId, ResourceRequestId
SupportRequestId, Outcome, Title, Justification, Conditions, Modifications
ApprovedAmount, Currency, EffectiveDate
IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse
// RequestType: SuggestedAction, ResourceRequest, SupportRequest
// Outcome: Pending, Approved, ApprovedWithMods, PartiallyApproved, Deferred, Rejected, Referred
// Computed: IsPositive, IsPendingAcknowledgment, RelatedRequestTitle
```

### Phase 7: Aggregation & Audit

**AggregationRule** (`Models/AggregationRule.cs`)
```csharp
Id, ReportFieldId, Method, Priority, WeightFieldKey, CustomFormula
TextAggregationMode, DecimalPrecision, AutoAggregate, IsActive, Description
// Method: Sum, Average, WeightedAverage, Min, Max, Count, Percentage, Custom
//         Concatenate, SelectFirst, SelectLast, SelectMostCommon, ManualSynthesis
```

**AggregatedValue** (`Models/AggregatedValue.cs`)
```csharp
Id, AggregationRuleId, ReportPeriodId, OrganizationalUnitId
NumericValue, TextValue, Status, SourceReportIdsJson, MissingSourcesJson
SourceReportCount, ComputedAt, ComputedById, HasAmendment
// Status: Pending, Current, Stale, Error, ManualOverride
// Computed: DisplayValue
// Nav: Amendments
```

**ManagerAmendment** (`Models/ManagerAmendment.cs`)
```csharp
Id, AggregatedValueId, AmendedById, AmendmentType, AmendedValue
Annotation, Justification, ExecutiveSummary, Visibility
ApprovalStatus, ApprovedById, ApprovedAt, IsActive
// AmendmentType: Annotation, Correction, ExecutiveSummary, ContextualNote, Highlight, Warning
// ApprovalStatus: Pending, Approved, Rejected
```

**AuditLog** (`Models/AuditLog.cs`)
```csharp
Id, UserId, Action, EntityType, EntityId, ReportId, OrganizationalUnitId
OldValue, NewValue, OldEntityJson, NewEntityJson
Timestamp, IpAddress, UserAgent, CorrelationId
// Action: Create, Update, Delete, View, Export, Login, Logout, Submit, Approve, Reject, etc.
// Computed: ChangeSummary
```

### Phase 8: Export & Reports

**SavedReport** (`Models/SavedReport.cs`)
```csharp
Id, Name, Description, ReportType, FilterConfiguration (JSON)
SelectedColumns, SortConfiguration, GroupingConfiguration
IsPublic, IsPinnedToDashboard, CreatedById, CreatedAt, ModifiedAt
LastRunAt, RunCount, DefaultExportFormat
// ReportType: reports, suggested_actions, resource_requests, support_requests,
//             audit_log, aggregation, users, feedback, recommendations
```

---

## Admin Pages Reference (17 sections, ~75 pages)

### Authentication (`/Auth`)
- `Login.cshtml` - Email entry form, magic link request
- `Verify.cshtml` - Token validation, cookie creation
- `Logout.cshtml` - Sign out with redirect

### Dashboard (`/Admin`)
- `Dashboard.cshtml` - Quick action buttons for all sections

### Role-Based Dashboards (`/Admin/Dashboards`)
- `Executive.cshtml` - Org-wide KPIs, charts, upward/downward flow summary
- `Manager.cshtml` - Team stats, pending approvals, team upward flow
- `Reviewer.cshtml` - Review queue, workload distribution, performance
- `Originator.cshtml` - My reports, upward flow status, feedback received

### User & Organization
- `/Admin/Users/` - Index, Create, Edit, Delete, Details (5 pages)
- `/Admin/OrgUnits/` - Index (tree view), Create, Edit, Delete, Details (5 pages)
- `/Admin/Delegations/` - Index (with revoke), Create (2 pages)

### Reporting
- `/Admin/Templates/` - Index, Create, Edit, Delete, Details (5 pages)
- `/Admin/Periods/` - Index (with Open/Close), Create, Edit (3 pages)
- `/Admin/Reports/` - Index, Create, Fill (with upward flow), View (4 pages)

### Upward Flow (`/Admin/UpwardFlow`)
- `SuggestedActions/Index.cshtml` - Status management, filtering
- `ResourceRequests/Index.cshtml` - Cost tracking, status management
- `SupportRequests/Index.cshtml` - Assignment, status management

### Workflow (`/Admin/Workflow`)
- `Comments/Index.cshtml` - Moderation, status management
- `Confirmations/Index.cshtml` - Reminder sending, status management

### Downward Flow (`/Admin/Downward`)
- `Feedback/Index.cshtml` - Category/visibility filtering
- `Recommendations/Index.cshtml` - Priority/scope filtering
- `Decisions/Index.cshtml` - Outcome/request type filtering

### Aggregation (`/Admin/Aggregation`)
- `Rules/Index.cshtml` - Create/configure rules per field
- `Summary/Index.cshtml` - View aggregates with drill-down

### Audit & Export
- `/Admin/AuditLog/Index.cshtml` - Filtering, statistics, CSV export
- `/Admin/Export/Download.cshtml` - CSV/Excel file downloads
- `/Admin/Export/Print.cshtml` - Print-friendly HTML for PDF

### Report Builder (`/Admin/ReportBuilder`)
- `Index.cshtml` - Build, run, export, save ad-hoc reports
- `SavedReports.cshtml` - Manage saved report configurations

### Backup (`/Admin/Backup`)
- Index, Create, Restore, Delete (4 pages)

---

## Seed Data Summary

| Entity | Count | Notes |
|--------|-------|-------|
| OrganizationalUnits | 36 | GUC → 2 Campuses → 8 Faculties → 14 Depts → 6 Sectors → 5 Teams |
| Users | 60 | 5 Execs, 3 Admins, 13 DeptHeads, 9 TeamMgrs, 6 Reviewers, 20 Originators, 3 Auditors |
| Delegations | 6 | 3 Active, 1 Upcoming, 1 Past, 1 Revoked |
| ReportTemplates | 5 | Monthly Dept, Weekly Team, Quarterly Academic, Annual Executive, IT Infra |
| ReportFields | 22 | Various types across templates |
| ReportPeriods | 9 | Mix of Upcoming, Open, Closed |
| Reports | 4 | 2 Approved, 1 Draft, 1 Submitted |
| SuggestedActions | 6 | Various statuses |
| ResourceRequests | 7 | With cost tracking |
| SupportRequests | 6 | Various statuses |
| Comments | 8 | Threaded with replies |
| ConfirmationTags | 6 | Various statuses |
| Feedbacks | 6 | Different categories |
| Recommendations | 5 | Different statuses |
| Decisions | 6 | Different outcomes |

---

## Status Constants Reference

All status classes use string constants with `DisplayName()` and `BadgeClass()` static methods:

| Class | Values |
|-------|--------|
| `SystemRoles` | Administrator, ReportOriginator, ReportReviewer, TeamManager, DepartmentHead, Executive, Auditor |
| `ReportStatus` | Draft, Submitted, UnderReview, Approved, Rejected, RevisionRequested |
| `ActionStatus` | Submitted, UnderReview, Approved, Rejected, Implemented, Deferred |
| `ActionCategory` | Process, Policy, Training, Technology, Communication, Resource, Other |
| `ActionPriority` | Critical, High, Medium, Low |
| `ResourceStatus` | Submitted, UnderReview, Approved, PartiallyApproved, Rejected, Fulfilled |
| `ResourceCategory` | Budget, Equipment, Personnel, Space, Software, Materials, Other |
| `ResourceUrgency` | Critical, High, Medium, Low |
| `SupportStatus` | Submitted, Acknowledged, InProgress, Resolved, Closed |
| `SupportCategory` | Management, Technical, Administrative, Training, Policy, Other |
| `SupportUrgency` | Critical, High, Medium, Low |
| `ConfirmationStatus` | Pending, Confirmed, RevisionRequested, Declined, Expired, Cancelled |
| `FeedbackCategory` | PositiveRecognition, Concern, Observation, Question, General |
| `FeedbackVisibility` | Private, TeamWide, DepartmentWide, OrganizationWide |
| `FeedbackStatus` | Active, Resolved, Archived |
| `RecommendationCategory` | ProcessChange, SkillDevelopment, PerformanceImprovement, Compliance, etc. |
| `RecommendationPriority` | Critical, High, Medium, Low |
| `RecommendationScope` | Individual, Team, Department, OrganizationWide |
| `RecommendationStatus` | Draft, Issued, Acknowledged, InProgress, Completed, Cancelled |
| `DecisionRequestType` | SuggestedAction, ResourceRequest, SupportRequest |
| `DecisionOutcome` | Pending, Approved, ApprovedWithMods, PartiallyApproved, Deferred, Rejected, Referred |
| `AggregationMethod` | Sum, Average, WeightedAverage, Min, Max, Count, Percentage, Custom, Concatenate, etc. |
| `AggregationStatus` | Pending, Current, Stale, Error, ManualOverride |
| `AmendmentType` | Annotation, Correction, ExecutiveSummary, ContextualNote, Highlight, Warning |

---

## Implementation Status

| Phase | Name | Status | Key Features |
|-------|------|--------|--------------|
| 1 | Infrastructure | **Complete** | Auth, backup, notifications, layout |
| 2 | Organization | **Complete** | Org units, roles, delegations |
| 3 | Reporting | **Complete** | Templates, fields, periods, reports, review |
| 4 | Upward Flow | **Complete** | Suggested actions, resources, support |
| 5 | Workflow | **Complete** | Comments, confirmation tags |
| 6 | Downward Flow | **Complete** | Feedback, recommendations, decisions |
| 7 | Aggregation | **Complete** | Rules, values, amendments, audit log |
| 8a | Dashboard KPIs | **Complete** | Role-based dashboards with KPI cards |
| 8b | Charts | **Complete** | Chart.js line/bar/doughnut charts |
| 8c | Export | **Complete** | CSV/Excel/PDF export service |
| 8d | Report Builder | **Complete** | Ad-hoc reports with save/share |
| 9 | Polish | **Pending** | Enhanced notifications, responsive, performance |

---

## Session Handoff

### Current Status
**Phase 8 complete** (8 of 9 phases) - All core features implemented including dashboards, charts, export, and ad-hoc report builder.

### Git Branch
`claude/analyze-reporting-system-HhdJs` - All work committed and pushed

### Latest Commit
`e748e27` - Add Phase 8c/8d: Export Capabilities & Ad-hoc Report Builder

### What's Working
- Full authentication flow with magic link login
- Complete organizational hierarchy management
- Report template creation with 8 field types
- Full report lifecycle (draft → submit → review → approve/reject)
- Upward flow (suggested actions, resources, support) with status tracking
- Workflow features (comments, confirmation tags)
- Downward flow (feedback, recommendations, decisions)
- Aggregation engine with manager amendments
- Comprehensive audit logging
- Role-based dashboards with Chart.js visualizations
- Export to CSV/Excel/PDF for reports, upward flow, audit log
- Ad-hoc report builder with save/share configurations

### Key Test Users
| Email | Role | Purpose |
|-------|------|---------|
| admin@reporting.com | Administrator | Full system access |
| exec1@guc.edu.eg | Executive | Executive dashboard |
| depthead_cs@guc.edu.eg | DepartmentHead | Department reports |
| team_web@guc.edu.eg | TeamManager | Team management |
| reviewer1@guc.edu.eg | ReportReviewer | Review queue |
| originator1@guc.edu.eg | ReportOriginator | Report submission |

### Next Steps (Phase 9)
1. **Enhanced Notifications**
   - Deadline reminders
   - Approval/rejection notifications
   - @mention alerts
   - Feedback/decision notifications

2. **Notification Preferences**
   - Per-user settings by type and channel
   - Daily/weekly digest option

3. **Polish**
   - Responsive design for mobile/tablet
   - Contextual help and tooltips
   - Performance optimization

### Key Files for Reference
| Purpose | File |
|---------|------|
| Database schema | `Data/ApplicationDbContext.cs` |
| All models | `Models/*.cs` (25 files) |
| Dashboard queries | `Services/DashboardService.cs` |
| Export logic | `Services/ExportService.cs` |
| Report creation | `Pages/Admin/Reports/Fill.cshtml` |
| Report viewing | `Pages/Admin/Reports/View.cshtml` |
| Report builder | `Pages/Admin/ReportBuilder/Index.cshtml` |
| Seed data | `seed.sql` (SQL), `Data/SeedData.cs` (C#) |
| Demo guide | `DEMO_GUIDE.md` |

### Build Commands
```bash
# If schema changed, delete database first
rm -f ReportingSystem/db/reporting.db

# Restore, build, run
dotnet restore --source /home/user/ReportingSystem/local-packages/ \
  /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj
dotnet build --no-restore /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj
dotnet run --project /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# Access at http://localhost:5296
```

### NuGet Note
NuGet.org is blocked by environment proxy. All packages are in `/home/user/ReportingSystem/local-packages/`. Always use `--source` flag for restore.

---

## Quick Reference

### Adding a New Model
1. Create `Models/NewModel.cs` with properties
2. Add `DbSet<NewModel>` to `ApplicationDbContext.cs`
3. Add entity configuration in `OnModelCreating()`
4. Delete `db/reporting.db` and restart

### Adding a New Page
1. Create `Pages/Admin/NewSection/Index.cshtml`
2. Create `Pages/Admin/NewSection/Index.cshtml.cs`
3. Add navigation link in `_Layout.cshtml`
4. Use `[BindProperty]` for form properties

### Status Constants Pattern
```csharp
public static class MyStatus
{
    public const string Draft = "draft";
    public const string Active = "active";

    public static string DisplayName(string status) => status switch
    {
        Draft => "Draft",
        Active => "Active",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Draft => "bg-secondary",
        Active => "bg-success",
        _ => "bg-secondary"
    };
}
```

### Common EF Core Patterns
```csharp
// Eager loading
await _context.Reports
    .Include(r => r.ReportTemplate)
    .Include(r => r.SubmittedBy)
    .ThenInclude(u => u.OrganizationalUnit)
    .ToListAsync();

// Filtering with nullable
if (statusFilter != null)
    query = query.Where(r => r.Status == statusFilter);
```
