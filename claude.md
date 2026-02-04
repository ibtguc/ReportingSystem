# ReportingSystem - HORS (Hierarchical Organizational Reporting System)

## Project Overview

ASP.NET Core 8.0 Razor Pages web application for hierarchical organizational reporting with bi-directional communication flows (upward reports/suggestions/resources/support and downward feedback/recommendations/decisions).

## Tech Stack

- **Framework**: ASP.NET Core 8.0, Razor Pages
- **Database**: EF Core 8.0 with SQLite (dev) / SQL Server (prod)
- **Auth**: Cookie-based with magic link passwordless login (15-min token expiry, 30-day sliding cookie)
- **Email**: Microsoft Graph API (disabled in dev mode)
- **Frontend**: Bootstrap 5.3.3, Bootstrap Icons (CDN), jQuery + jQuery Validation (bundled)
- **Data Protection**: Keys persisted to `/keys` folder

## Build & Run

```bash
# Restore (uses local NuGet feed due to environment proxy)
dotnet restore --source /home/user/ReportingSystem/local-packages/ \
  --source /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# Build
dotnet build --no-restore /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# Run (dev mode)
dotnet run --project /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj
```

Dev URL: http://localhost:5296 / https://localhost:7155

## Project Structure

```
ReportingSystem/
├── Data/                  # EF Core DbContext, seed data, user seeder
├── Filters/               # AutomaticBackupFilter (pre-POST/PUT/DELETE backups)
├── Models/                # Domain entities (User, MagicLink, OrganizationalUnit, Delegation, Report*, Attachment, Notification, DatabaseBackup)
├── Pages/
│   ├── Admin/             # Requires authentication
│   │   ├── Backup/        # Database backup management (create/restore/delete/WAL)
│   │   ├── Delegations/   # Delegation CRUD (Index, Create) with revoke
│   │   ├── OrgUnits/      # Org unit CRUD (Index, Create, Edit, Delete) with tree view
│   │   ├── Periods/       # Report period management (Index, Create, Edit)
│   │   ├── Reports/       # Report lifecycle (Index, Create, Fill, View) with review
│   │   ├── Templates/     # Template management (Index, Create, Edit, Details, Delete)
│   │   ├── Users/         # User CRUD (Index, Create, Edit, Details, Delete)
│   │   └── Dashboard.cshtml
│   ├── Auth/              # Anonymous access (Login, Verify, Logout)
│   └── Shared/            # _Layout, _ValidationScriptsPartial
├── Services/              # MagicLink, Email, Notification, Backup, DailyBackupHostedService
├── wwwroot/
│   ├── css/site.css
│   ├── js/                # site.js, filter-state.js (URL + sessionStorage persistence)
│   └── lib/               # Bootstrap, jQuery, jQuery Validation (bundled)
├── Program.cs             # DI setup, auth config, database init
├── appsettings.json       # Production config template
└── appsettings.Development.json  # SQLite config
```

## Architecture Patterns

- **Razor Pages with `[BindProperty]`**: All form-bound properties use `[BindProperty]` attribute
- **EF Core with eager loading**: Use `.Include()` for navigation properties
- **Policy-based authorization**: `"AdministratorOnly"` policy, folder-level auth (`/Admin` requires auth, `/Auth` is anonymous)
- **Modal dialogs**: Bootstrap modals for confirmations and AJAX operations
- **TempData flash messages**: Success/error messages via `TempData["SuccessMessage"]` / `TempData["ErrorMessage"]`
- **Filter state**: `filter-state.js` persists filters in URL query params + sessionStorage
- **Async/await**: All database operations are async
- **Nullable reference types**: Enabled project-wide
- **File-scoped namespaces**: Use `namespace X;` syntax (not `namespace X { }`)

## Database

- **Dev**: SQLite at `db/reporting.db` (auto-created via `EnsureCreatedAsync`)
- **Prod**: SQL Server (configure in appsettings.json)
- **Seeded users**: admin@reporting.com, admin1@reporting.com, admin2@reporting.com (all Administrators)
- **SQL seed script**: `seed.sql` at project root - seeds full org hierarchy, 60 users, delegations, templates, fields, periods, sample reports, notifications
- **Org hierarchy**: GUC > Campuses > Faculties > Departments > Sectors > Teams (6 levels via OrgUnitLevel enum)
- **Known limitation**: `TimeSpan` properties cannot be used in `ORDER BY` with SQLite provider

## Key Services

| Service | Purpose |
|---------|---------|
| `MagicLinkService` | Generate/validate magic link tokens |
| `EmailService` | Send emails via Microsoft Graph API |
| `NotificationService` | In-app notifications CRUD |
| `DatabaseBackupService` | Create/restore/delete backups, WAL checkpoint |
| `DailyBackupHostedService` | Automatic backups at 2:00 and 14:00 UTC |
| `AutomaticBackupFilter` | Trigger backup before POST/PUT/DELETE requests |

## Domain Implementation Phases (SRS-based)

Phase 1 (complete): Infrastructure - Auth, backup, notifications, admin pages, layout
Phase 2 (complete): Organization & User Hierarchy - Org units, user roles, delegations, SQL seed script
Phase 3 (complete): Report Templates & Report Entry - Templates, fields, periods, reports, review workflow

### Key Models (Phase 2)
- **OrganizationalUnit**: self-referential hierarchy with OrgUnitLevel enum (Root=0 → Team=5), Parent/Children nav props, DeleteBehavior.Restrict
- **User** (extended): OrganizationalUnitId FK (SetNull on delete), JobTitle, Role using SystemRoles string constants
- **SystemRoles**: Administrator, ReportOriginator, ReportReviewer, TeamManager, DepartmentHead, Executive, Auditor (string constants with DisplayName helper)
- **Delegation**: DelegatorId/DelegateId FKs (Restrict), StartDate/EndDate, Scope (Full/ReportingOnly/ApprovalOnly), IsCurrentlyEffective computed property
- **DelegationScope**: Full, ReportingOnly, ApprovalOnly (string constants with DisplayName helper)

### Key Models (Phase 3)
- **ReportTemplate**: name, description, schedule (ReportSchedule constants), version, standard sections (SuggestedActions/NeededResources/NeededSupport), auto-save interval, attachment settings
- **ReportTemplateAssignment**: assigns template to OrgUnit/Role/Individual (TemplateAssignmentType constants), IncludeSubUnits flag
- **ReportField**: FieldType enum (Text=0→TableGrid=7), section/order, validation (min/max/regex), OptionsJson for dropdowns, Formula for calculated fields, VisibilityConditionJson, PrePopulateFromPrevious
- **ReportPeriod**: StartDate/EndDate, SubmissionDeadline, GracePeriodDays, PeriodStatus enum (Upcoming/Open/Closed/Archived), IsOpen/IsOverdue/IsFullyClosed computed
- **Report**: ReportTemplateId+ReportPeriodId+SubmittedById (unique composite), ReportStatus constants (Draft→Submitted→UnderReview→Approved/Rejected/Amended), IsLocked, review workflow
- **ReportFieldValue**: ReportId+ReportFieldId (unique), Value as string, NumericValue for aggregation, WasPrePopulated
- **Attachment**: file storage with FileName/OriginalFileName/ContentType/StoragePath, linked to Report and optionally ReportField

### Phase 4: Upward Flow (Suggestions, Resources, Support)
- **SuggestedAction** model: title, description, justification, expected outcome, timeline, category (Process Improvement, Cost Reduction, Quality Enhancement, Innovation, Risk Mitigation), priority (Critical/High/Medium/Low), status tracking
- **ResourceRequest** model: type, description, quantity, justification, urgency, category (Budget, Equipment, Software, Personnel, Materials, Facilities, Training), estimated cost, status tracking
- **SupportRequest** model: type, description, current situation, desired outcome, urgency, category (Management Intervention, Cross-Dept Coordination, Technical Assistance, Training, Conflict Resolution, Policy Clarification), status tracking
- Each linked to a Report, with file attachments
- Pages: Entry forms for each type, list views with filtering, status management

### Phase 5: Workflow & Tagging
- **WorkflowInstance** model: report lifecycle (Draft → Submitted → Under Review → Approved/Rejected/Amended), configurable approval chain
- **ConfirmationTag** model: originator tags another user to confirm/verify report section, status (Pending, Confirmed, RevisionRequested, Declined), with timestamps
- **Comment** model: threaded discussions on report sections, @mentions support
- Route submitted reports to direct manager automatically
- Submission deadline enforcement with grace periods
- Reminder notifications (3 days, 1 day, same day before deadline)
- Lock reports after final approval

### Phase 6: Downward Flow (Feedback, Recommendations, Decisions)
- **Feedback** model: category (Positive Recognition, Concern, Observation, Question, General), visibility (Private, Team-wide, Department-wide), threading support, acknowledgment tracking
- **Recommendation** model: title, description, rationale, timeline, priority, category (Process Change, Skill Development, Performance Improvement, Compliance, Strategic Alignment), target scope (Individual/Team/Department/Org-wide), status tracking, cascade through hierarchy
- **Decision** model: type, outcome (Approved, Approved with Modifications, Partially Approved, Deferred, Rejected, Referred), justification, effective date, conditions, linked to originating request (SuggestedAction/ResourceRequest/SupportRequest), audit trail, cascade with acknowledgment tracking
- Pages: Response forms for each type, linking to source requests

### Phase 7: Aggregation & Drill-Down
- Aggregation engine: configurable rules per field (Sum, Average, Weighted Average, Min, Max, Count, Percentage, Custom Formula)
- Textual aggregation: concatenate, select representative, manual synthesis
- Manager amendment layer: annotate/add context to aggregated data, distinguish original vs. amended
- Aggregate upward flow items (suggestions, resources, support) at each hierarchy level
- Auto-generate executive summaries with key metrics
- **Drill-down**: navigate from any summary value to contributing source reports
- **Data lineage**: originator, original value, amendment history, aggregation path
- **AuditLog** model: all data changes with user, timestamp, before/after values
- Visual hierarchy navigation (tree view, breadcrumb)

### Phase 8: Dashboards & Export
- Role-based dashboards with KPIs and visualizations
- Submission status, pending actions, trends, alerts, recent activity
- Request status dashboard (open suggestions, pending resource requests, unresolved support)
- Chart visualizations (bar, line, pie, gauge)
- Export: PDF/Word/Excel reports with customizable layouts
- Ad-hoc report builder for custom queries

### Phase 9: Notifications & Polish
- Enhanced notification system: deadline reminders, approval notifications, tag/mention alerts, feedback/decision notifications
- Notification preferences per user (by type and channel)
- Notification digest (daily/weekly summary)
- Responsive design polish for mobile/tablet
- Contextual help and tooltips
- Performance optimization

## Reference Project

Infrastructure was replicated from `/ref-only-example/SchedulingSystem/` with namespace/branding changes. Domain-specific scheduling code was NOT replicated.

## NuGet Environment Note

NuGet.org is blocked by the environment proxy. Packages are downloaded via Python script to `/home/user/ReportingSystem/local-packages/` and restored from that local source. Use `--source /home/user/ReportingSystem/local-packages/` flag with `dotnet restore`.
