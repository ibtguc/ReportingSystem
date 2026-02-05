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
Phase 4 (complete): Upward Flow - Suggested actions, resource requests, support requests with status tracking
Phase 5 (complete): Workflow & Tagging - Comments, confirmation tags, threaded discussions, workflow admin pages
Phase 6 (complete): Downward Flow - Feedback, recommendations, decisions with acknowledgment tracking

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

### Key Models (Phase 4)
- **SuggestedAction**: title, description, justification, expected outcome, timeline, category (ActionCategory constants), priority (ActionPriority constants), status (ActionStatus: Submitted→UnderReview→Approved/Rejected/Implemented/Deferred), ReviewedById FK
- **ResourceRequest**: title, description, quantity (string), justification, category (ResourceCategory constants), urgency (ResourceUrgency constants), EstimatedCost/ApprovedAmount/Currency, status (ResourceStatus constants), ReviewedById FK, FulfilledAt
- **SupportRequest**: title, description, current situation, desired outcome, category (SupportCategory constants), urgency (SupportUrgency constants), status (SupportStatus: Submitted→Acknowledged→InProgress→Resolved/Closed), AssignedToId/AcknowledgedById/ResolvedById FKs, Resolution field, IsOpen computed
- All three models link to Report via ReportId FK with cascade delete
- Admin pages at `/Admin/UpwardFlow/{SuggestedActions,ResourceRequests,SupportRequests}/Index`
- Report Fill page includes inline entry forms when template has IncludeSuggestedActions/IncludeNeededResources/IncludeNeededSupport enabled
- Report View page displays submitted upward flow items with status badges

### Key Models (Phase 5)
- **Comment**: threaded discussions on reports with @mentions support, Status (Active/Edited/Deleted/Hidden), ParentCommentId for replies, AuthorId FK, SectionReference optional, MentionedUserIdsJson for @mention tracking, IsReply/IsDeleted computed properties
- **ConfirmationTag**: originator tags another user to confirm/verify report section, Status (Pending/Confirmed/RevisionRequested/Declined/Expired/Cancelled), RequestedById/TaggedUserId FKs, SectionReference optional, Message/Response fields, ReminderSentAt for reminder tracking, IsPending/IsConfirmed/DaysSinceRequested computed properties
- Admin pages at `/Admin/Workflow/{Comments,Confirmations}/Index` with filtering, status management, and reminder functionality
- Report View page includes Comments section (threaded with replies) and Confirmation Tags section (request/respond workflow)
- Navigation updated with Workflow dropdown menu
- SeedData.cs updated with Phase 5 sample data (comments and confirmation tags)

### Key Models (Phase 6)
- **Feedback**: management responses to reports with Category (PositiveRecognition/Concern/Observation/Question/General), Visibility (Private/TeamWide/DepartmentWide/OrganizationWide), Status (Active/Resolved/Archived), threading via ParentFeedbackId, acknowledgment tracking (IsAcknowledged/AcknowledgedAt/AcknowledgmentResponse), RequiresAcknowledgment/IsPendingAcknowledgment computed properties
- **Recommendation**: guidance/directives with Category (ProcessChange/SkillDevelopment/PerformanceImprovement/Compliance/StrategicAlignment/ResourceOptimization/General), Priority (Critical/High/Medium/Low), TargetScope (Individual/Team/Department/OrganizationWide), Status (Draft/Issued/Acknowledged/InProgress/Completed/Cancelled), optional ReportId/TargetOrgUnitId/TargetUserId, DueDate/EffectiveDate, CascadeToSubUnits, IsOverdue/DaysUntilDue computed
- **Decision**: formal responses to upward flow with RequestType (SuggestedAction/ResourceRequest/SupportRequest), Outcome (Pending/Approved/ApprovedWithMods/PartiallyApproved/Deferred/Rejected/Referred), optional links to SuggestedActionId/ResourceRequestId/SupportRequestId, ApprovedAmount/Currency, Conditions/Modifications, acknowledgment tracking, IsPositive/IsPendingAcknowledgment computed
- Admin pages at `/Admin/Downward/{Feedback,Recommendations,Decisions}/Index` with filtering, status management, and acknowledgment tracking
- Report View page displays Feedback (threaded), Recommendations (table), and Decisions (list) sections
- Navigation updated with Downward Flow dropdown menu
- SeedData.cs updated with Phase 6 sample data

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

## Session Handoff

### Current Status
**Phase 6 complete** (of 9 phases) - Downward Flow (Feedback, Recommendations, Decisions) fully implemented.

### Project Statistics
| Category | Count |
|----------|-------|
| Total .cs/.cshtml files | ~120 |
| Model classes | 20 |
| Razor Pages (.cshtml) | 52 |
| Services | 5 |
| Admin page sections | 11 |

### Implemented Models (20)
| Model | Phase | Purpose |
|-------|-------|---------|
| `User` | 1,2 | Users with roles, org unit assignment, MagicLinks |
| `MagicLink` | 1 | Passwordless authentication tokens |
| `Notification` | 1 | In-app notifications with types/priorities |
| `DatabaseBackup` | 1 | Backup records with file paths |
| `OrganizationalUnit` | 2 | Self-referential hierarchy (6 levels) |
| `Delegation` | 2 | Temporary authority transfer |
| `ReportTemplate` | 3 | Template definitions with versioning |
| `ReportField` | 3 | Field definitions (8 types) with validation |
| `ReportPeriod` | 3 | Time periods with deadlines |
| `Report` | 3 | Report instances with status workflow |
| `ReportFieldValue` | 3 | Actual data entries per field |
| `Attachment` | 3 | File uploads linked to reports |
| `SuggestedAction` | 4 | Process improvements attached to reports |
| `ResourceRequest` | 4 | Budget/equipment/personnel requests |
| `SupportRequest` | 4 | Management/technical assistance requests |
| `Comment` | 5 | Threaded discussions with @mentions |
| `ConfirmationTag` | 5 | User verification requests on report sections |
| `Feedback` | 6 | Management responses with categories/visibility |
| `Recommendation` | 6 | Guidance/directives with target scope |
| `Decision` | 6 | Formal responses to upward flow requests |

### Implemented Admin Pages (11 sections, 52 pages)
| Section | Pages | Purpose |
|---------|-------|---------|
| `/Admin/Backup` | 4 | Create, restore, delete, WAL checkpoint |
| `/Admin/Delegations` | 2 | Index with revoke, Create |
| `/Admin/OrgUnits` | 5 | CRUD with recursive tree view |
| `/Admin/Periods` | 3 | Index with Open/Close, Create, Edit |
| `/Admin/Reports` | 4 | Index, Create, Fill (with upward flow), View (with all flow sections) |
| `/Admin/Templates` | 5 | CRUD with inline field/assignment management |
| `/Admin/Users` | 5 | Full CRUD with org unit/role dropdowns |
| `/Admin/UpwardFlow/SuggestedActions` | 1 | Index with filtering and status management |
| `/Admin/UpwardFlow/ResourceRequests` | 1 | Index with cost tracking and status management |
| `/Admin/UpwardFlow/SupportRequests` | 1 | Index with assignment and status management |
| `/Admin/Workflow/Comments` | 1 | Index with moderation and status management |
| `/Admin/Workflow/Confirmations` | 1 | Index with reminder sending and status management |
| `/Admin/Downward/Feedback` | 1 | Index with category/visibility filtering |
| `/Admin/Downward/Recommendations` | 1 | Index with priority/scope filtering |
| `/Admin/Downward/Decisions` | 1 | Index with outcome/request type filtering |
| `/Admin/Dashboard` | 1 | Quick-access buttons for all sections |

### Seed Data (seed.sql)
| Entity | Count | Notes |
|--------|-------|-------|
| OrganizationalUnits | 36 | GUC hierarchy: 1 Root, 2 Campuses, 8 Faculties, 14 Depts, 6 Sectors, 5 Teams |
| Users | 60 | 5 Executives, 3 Admins, 13 Dept Heads, 9 Team Mgrs, 6 Reviewers, 20 Originators, 3 Auditors, 1 Inactive |
| Delegations | 6 | 3 Active, 1 Upcoming, 1 Past, 1 Revoked |
| ReportTemplates | 5 | Monthly Dept, Weekly Team, Quarterly Academic, Annual Executive, IT Infrastructure |
| ReportFields | 22 | Across 3 templates with various field types |
| ReportPeriods | 9 | Mix of Upcoming, Open, Closed statuses |
| Reports | 4 | 2 Approved, 1 Draft, 1 Submitted |
| ReportFieldValues | 18 | Sample data entries |
| SuggestedActions | 6 | 3 Approved, 1 Implemented, 1 Under Review, 1 Submitted |
| ResourceRequests | 7 | 3 Approved, 1 Fulfilled, 1 Partially Approved, 2 Submitted |
| SupportRequests | 6 | 2 Resolved, 1 Closed, 1 In Progress, 1 Acknowledged, 1 Submitted |
| Notifications | 5 | Welcome and delegation notifications |
| Comments | 8 | Threaded discussions with @mentions and replies |
| ConfirmationTags | 6 | 3 Confirmed, 1 Pending, 1 RevisionRequested, 1 other |
| Feedbacks | 6 | 2 PositiveRecognition, 2 Concern, 1 Question, 1 Observation |
| Recommendations | 5 | 1 InProgress, 2 Acknowledged, 2 Issued |
| Decisions | 6 | 2 Approved, 1 ApprovedWithMods, 1 PartiallyApproved, 1 Deferred, 1 other |

### Database State
- Schema includes all Phase 1-6 entities (21 tables)
- Uses SQLite for dev (`db/reporting.db`), SQL Server for prod
- **Important**: Delete `db/reporting.db` before running if schema changed (EnsureCreatedAsync won't migrate)
- **seed.sql**: Complete SQL seed script with all Phase 1-6 data (org units, users, templates, reports, upward flow, workflow, downward flow)
- **SeedData.cs**: C# seeding for minimal data (includes Phase 5+6 sample data)

### Remaining Phases (3)
| Phase | Name | Key Deliverables |
|-------|------|------------------|
| 7 | Aggregation | Aggregation engine, drill-down, AuditLog, data lineage |
| 8 | Dashboards & Export | Role-based dashboards, charts, PDF/Excel export |
| 9 | Polish | Enhanced notifications, preferences, responsive design, performance |

### Next Phase: Phase 7 - Aggregation & Drill-Down
Implement data aggregation and drill-down capabilities:

1. **Aggregation Engine** - Configurable rules per field
   - Numeric: Sum, Average, Weighted Average, Min, Max, Count, Percentage
   - Textual: Concatenate, select representative, manual synthesis
   - Custom formulas for calculated aggregates

2. **Manager Amendment Layer**
   - Annotate/add context to aggregated data
   - Distinguish original vs. amended values
   - Aggregate upward flow items at each hierarchy level
   - Auto-generate executive summaries with key metrics

3. **Drill-Down Navigation**
   - Navigate from any summary value to contributing source reports
   - Data lineage: originator, original value, amendment history, aggregation path
   - Visual hierarchy navigation (tree view, breadcrumb)

4. **AuditLog Model** - Track all data changes
   - User, timestamp, before/after values
   - Full change history for compliance

Admin pages:
- `/Admin/Aggregation/Rules` - Configure aggregation rules per field
- `/Admin/Aggregation/Summary` - View aggregated data with drill-down
- `/Admin/AuditLog/Index` - View all data changes

### Key Files to Reference
| File | Pattern/Purpose |
|------|-----------------|
| `Models/Report.cs` | Status workflow with constants, computed properties |
| `Models/SuggestedAction.cs` | Category/Priority/Status constants pattern |
| `Models/ResourceRequest.cs` | Cost tracking, status workflow |
| `Models/SupportRequest.cs` | Assignment tracking, IsOpen computed property |
| `Models/Comment.cs` | Threaded comments with ParentCommentId, @mentions |
| `Models/ConfirmationTag.cs` | User tagging with confirmation workflow |
| `Models/Feedback.cs` | Management feedback with categories, visibility, threading |
| `Models/Recommendation.cs` | Directives with target scope, priority, due dates |
| `Models/Decision.cs` | Responses to upward flow with outcomes, acknowledgment |
| `Pages/Admin/Reports/Fill.cshtml` | Inline upward flow entry forms |
| `Pages/Admin/Reports/Fill.cshtml.cs` | Handler methods for add/remove upward flow |
| `Pages/Admin/Reports/View.cshtml` | Display all flow sections (upward, workflow, downward) |
| `Pages/Admin/Reports/View.cshtml.cs` | Handler methods for comments/confirmations |
| `Pages/Admin/UpwardFlow/*/Index.cshtml` | Status management with dropdown menus |
| `Pages/Admin/Workflow/*/Index.cshtml` | Comment moderation and confirmation management |
| `Pages/Admin/Downward/*/Index.cshtml` | Feedback, recommendations, decisions management |
| `Data/ApplicationDbContext.cs` | Entity config with indexes, relationships |
| `Data/SeedData.cs` | C# seeding with Phase 5+6 workflow and downward flow data |

### Build Commands
```bash
# Build
dotnet build --no-restore /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# Run
dotnet run --project /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj

# If packages missing, restore from local cache
dotnet restore --source /home/user/ReportingSystem/local-packages/ /home/user/ReportingSystem/ReportingSystem/ReportingSystem.csproj
```

### Git Branch
`claude/create-reporting-system-2FPWa` - all work committed and pushed

