# ORS — Organizational Reporting System

## Project Overview

ASP.NET Core 8.0 Razor Pages web application for hierarchical organizational reporting with bi-directional communication flows, structured meeting management, confidentiality controls, and full audit trail.

**Current Status**: All 11 Phases COMPLETE + Custom user/committee hierarchy seeded + Report visibility rules implemented + Seeded reports (28) with status histories and source links

## Tech Stack

- **Framework**: ASP.NET Core 8.0, Razor Pages (NOT MVC)
- **ORM**: EF Core 8.0 with SQLite (dev) / SQL Server (prod), `EnsureCreatedAsync()` (no migrations)
- **Auth**: Cookie-based with magic link passwordless login (15-min token, 30-day sliding session)
- **Email**: Microsoft Graph API (disabled by default in dev)
- **Frontend**: Bootstrap 5.3.3, Bootstrap Icons (CDN), jQuery + jQuery Validation
- **Data Protection**: Keys persisted to `/keys` folder

## Quick Start

```bash
dotnet build ReportingSystem/ReportingSystem.csproj
dotnet run --project ReportingSystem/ReportingSystem.csproj
# Dev URL: http://localhost:5296
```

**Important**: After schema changes (new models/DbSets), delete `ReportingSystem/db/reporting.db` before running — `EnsureCreatedAsync()` won't add new tables to an existing database.

---

## Project Structure

```
ReportingSystem/
├── Data/
│   ├── ApplicationDbContext.cs      # EF Core DbContext, 24 DbSets
│   ├── SeedData.cs                  # Placeholder (unused)
│   ├── UserSeeder.cs                # ACTIVE — Seeds 57 users, 22 committees, 80+ memberships
│   ├── ReportSeeder.cs              # ACTIVE — Seeds 28 reports, status histories, approvals, source links
│   ├── OrganizationSeeder.cs        # Old seeder for ~155 users (commented out in Program.cs)
│   └── DemoDataSeeder.cs            # Old seeder for 30 reports, 15 directives (commented out, references old schema)
├── Filters/
│   └── AutomaticBackupFilter.cs     # Pre-POST/PUT/DELETE backup trigger
├── Models/ (23 files, 25 classes)
│   ├── User.cs                      # User + MagicLink + SystemRole enum
│   ├── Committee.cs                 # Committee + HierarchyLevel enum
│   ├── CommitteeMembership.cs       # Membership + CommitteeRole enum
│   ├── ShadowAssignment.cs          # Shadow/backup assignments
│   ├── Report.cs                    # Report + ReportApproval + ReportType/ReportStatus enums
│   ├── ReportTemplate.cs            # Configurable report templates
│   ├── Attachment.cs                # File attachments for reports
│   ├── ReportStatusHistory.cs       # Audit trail for status transitions
│   ├── ReportSourceLink.cs          # Summary→Source report links
│   ├── Directive.cs                 # Directive + DirectiveType/Priority/Status enums
│   ├── DirectiveStatusHistory.cs    # Audit trail for directive status transitions
│   ├── Meeting.cs                   # Meeting + MeetingType/MeetingStatus/RecurrencePattern enums
│   ├── MeetingAgendaItem.cs         # Agenda items with presenter + linked reports
│   ├── MeetingAttendee.cs           # Attendees with RSVP + minutes confirmation
│   ├── MeetingDecision.cs           # Formal decisions + DecisionType enum
│   ├── ActionItem.cs                # Trackable action items + ActionItemStatus enum
│   ├── ConfidentialityMarking.cs    # Confidentiality markings with hierarchy-based access
│   ├── AccessGrant.cs               # Explicit access grants for confidential items
│   ├── AuditLog.cs                  # Append-only audit log + AuditActionType enum
│   ├── Notification.cs              # In-app notifications + NotificationType enum
│   ├── DatabaseBackup.cs            # Backup records
│   ├── KnowledgeCategory.cs         # Hierarchical categories
│   └── KnowledgeArticle.cs          # Articles with source linking, tags, view count
├── Pages/ (59 .cshtml files across 21 folders)
│   ├── Admin/                       # Dashboard, Users CRUD, Organization tree, Committees CRUD, Templates CRUD, Knowledge admin, Backup, AuditLog
│   ├── Auth/                        # Login, Verify, Logout
│   ├── Reports/                     # Index (hierarchical committee filter), Create, Details, Edit, CreateSummary, DrillDown
│   ├── Directives/                  # Index, Create, Details, Track
│   ├── Meetings/                    # Index, Create, Details, Minutes, ActionItems
│   ├── Confidentiality/             # Mark, AccessGrants
│   ├── Search/                      # Index (unified search)
│   ├── Archives/                    # Index (archived items)
│   ├── Dashboard/                   # Index (role-specific: Chairman/Office/Head/Personal)
│   ├── Notifications/               # Index (notification center)
│   ├── Knowledge/                   # Index, Article
│   ├── Analytics/                   # Index (Chart.js dashboard)
│   ├── Shared/_Layout.cshtml        # Nav: Dashboard, Organization, Reports, Directives, Meetings, Knowledge, Analytics, Search, Administration
│   └── Index.cshtml                 # Landing → redirects to Dashboard or Login
├── Services/ (16 files)
│   ├── ReportService.cs             # Reports, visibility, approval workflow, summarization, drill-down
│   ├── DirectiveService.cs          # Directives, status transitions, propagation, overdue
│   ├── MeetingService.cs            # Meetings, agenda, attendees, RSVP, minutes, decisions, action items
│   ├── DashboardService.cs          # Role-specific dashboards (Chairman/Office/Head/Personal)
│   ├── ConfidentialityService.cs    # Confidentiality marking, access control, impact preview
│   ├── SearchService.cs             # Unified full-text search across all content types
│   ├── AnalyticsService.cs          # Overview, trends, compliance, committee metrics
│   ├── KnowledgeBaseService.cs      # Article CRUD, bulk indexing, category management
│   ├── AuditService.cs              # Append-only audit logging, query/export
│   ├── OrganizationService.cs       # Committee/membership/shadow CRUD
│   ├── NotificationService.cs       # In-app notifications CRUD + event helpers
│   ├── ReportTemplateService.cs     # Template CRUD, committee-scoped lookup
│   ├── MagicLinkService.cs          # Token gen/verify/cleanup
│   ├── EmailService.cs              # Microsoft Graph email sending
│   ├── DatabaseBackupService.cs     # Backup create/restore/delete/WAL
│   └── DailyBackupHostedService.cs  # Background 12h backup scheduler
├── ApiEndpoints.cs                  # REST API extension method: /api/* endpoints
├── Program.cs                       # App configuration & DI
└── wwwroot/                         # Static files (Bootstrap, jQuery, uploads/)
```

---

## Current Organizational Hierarchy (UserSeeder.cs)

### Users (57 total)

| # | Role | Name | Email | Notes |
|---|------|------|-------|-------|
| 1 | SystemAdmin | System Administrator | admin@org.edu | |
| 2 | Chairman | AM | am@org.edu | |
| 3 | ChairmanOffice (Rank 1) | Ahmed Mansour | ahmed.mansour@org.edu | Also co-heads Marketing & Outreach |
| 4 | ChairmanOffice (Rank 2) | Moustafa Fouad | moustafa.fouad@org.edu | |
| 5 | ChairmanOffice (Rank 3) | Marwa El Serafy | marwa.elserafy@org.edu | |
| 6 | ChairmanOffice (Rank 4) | Samia El Ashiry | samia.elashiry@org.edu | Also co-heads Marketing & Outreach |
| 7-13 | CommitteeUser | 7 L0 Heads | various | See below |
| 14-20 | CommitteeUser | 7 Named L2 Heads | various | See below |
| 21-27 | CommitteeUser | 7 Generated L2 Heads | various | |
| 28-57 | CommitteeUser | 30 Generated L2 Members | various | 2 per L2 committee |

### Committee Tree (22 committees)

```
Top Level Committee (L0) — Heads: Mohamed Ibrahim, Radwa Selim, Ghadir Nassar, Engy Galal, Karim Salme, Sherine Khalil, Sherine Salamony
├── Academic Quality & Accreditation (L1) — Head: Ghadir Nassar
│   ├── Curriculum (L2) — Head: Hanan Mostafa — Members: Amira Soliman, Bassem Youssef
│   ├── Probation & Mentoring (L2) — Head: Tarek Abdel Fattah — Members: Fatma El Zahraa, Omar Hashem
│   └── Teaching and Evaluation Standard (L2) — Head: Noha El Sayed — Members: Yasmin Abdel Rahman, Khaled Mostafa
├── Student Activities (L1) — Head: Sherine Salamony
│   ├── Music (L2) — Head: Ohoud Khadr — Members: Nadia Fawzy, Wael Abdel Meguid
│   ├── Theater (L2) — Head: Layla Hassan — Members: Rania Samir, Hazem Ashraf
│   ├── Sports (L2) — Head: Yehia Razzaz — Members: Tamer El Naggar, Dina Raafat
│   └── AWG (L2) — Head: Yehia Razzaz — Members: Sahar El Gendy, Hassan Mahmoud
├── Admission (L1) — Head: Sherine Khalil
│   ├── Marketing & Outreach (L2) — Co-Heads: Ahmed Mansour, Samia El Ashiry — Members: Lamia Youssef, Mahmoud Farouk
│   ├── Admission Services (L2) — Head: Ramy Shawky — Members: Heba Abdel Aziz, Nermeen Sami
│   └── Admission Office (L2) — Head: Sherine Khalil — Members: Waleed Tantawy, Rana El Kholy
├── Campus Administration (L1) — Head: Mohamed Ibrahim
│   ├── Facility Management (L2) — Co-Heads: Amr Baibars, Gen. Ibrahim Khalil — Members: Mostafa Ragab, Samiha Adel
│   ├── Security (L2) — Head: Hossam Badawy — Members: Abdallah Ramzy, Yasser Galal
│   └── Agriculture (L2) — Head: Mona Farid — Members: Nashwa Ahmed, Adel Ismail
└── HR (L1) — Head: Radwa Selim
    ├── Recruitment (L2) — Head: Dalia El Mainouny — Members: Mohamed Abdel Wahab, Hala Mahmoud
    ├── Compensation & Benefits (L2) — Head: Ayman Rahmou — Members: Essam Hamdy, Iman El Batouty
    └── Personnel (L2) — Head: Salma Ibrahim — Members: Sherif Naguib, Amany Lotfy
```

### L1 Members = L2 Heads Pattern
Each L1 committee's members are the heads of its L2 sub-committees:
- AQA members: Hanan Mostafa, Tarek Abdel Fattah, Noha El Sayed (heads of Curriculum, Probation, Teaching)
- Student Activities members: Ohoud Khadr, Layla Hassan, Yehia Razzaz (heads of Music, Theater, Sports/AWG)
- Admission members: Ahmed Mansour, Samia El Ashiry, Ramy Shawky (heads of Marketing, Adm Services; Sherine Khalil also heads Adm Office)
- Campus Admin members: Amr Baibars, Ibrahim Khalil, Hossam Badawy, Mona Farid (heads of Facility, Security, Agriculture)
- HR members: Dalia El Mainouny, Ayman Rahmou, Salma Ibrahim (heads of Recruitment, Compensation, Personnel)

---

## Seeded Reports (ReportSeeder.cs — 28 total)

### By Committee Level
| Level | Committee | Reports | Types | Statuses |
|-------|-----------|---------|-------|----------|
| L0 | Top Level Committee | r27, r28 | 2 ExecSummary | Submitted, Summarized |
| L1 | AQA | r22 | 1 Summary | Approved |
| L1 | Student Activities | r23 | 1 Summary | Approved |
| L1 | Admission | r24 | 1 Summary | Submitted |
| L1 | Campus Administration | r25 | 1 Summary | Approved |
| L1 | HR | r26 | 1 Summary | Submitted |
| L2 | Curriculum | r1, r2 | 2 Detailed | Approved, Submitted |
| L2 | Probation | r3 | 1 Detailed | Approved |
| L2 | Teaching | r4, r5 | 2 Detailed | Approved, Draft |
| L2 | Music | r6 | 1 Detailed | Approved (SkipApprovals) |
| L2 | Theater | r7 | 1 Detailed | Submitted |
| L2 | Sports | r8 | 1 Detailed | Approved |
| L2 | AWG | r9 | 1 Detailed | FeedbackRequested |
| L2 | Marketing & Outreach | r10 | 1 Detailed | Approved |
| L2 | Admission Services | r11 | 1 Detailed | Approved |
| L2 | Admission Office | r12 | 1 Detailed | Submitted |
| L2 | Facility Management | r13 | 1 Detailed | Approved (SkipApprovals) |
| L2 | Security | r14, r15 | 2 Detailed | Approved (SkipApprovals), Draft |
| L2 | Agriculture | r16 | 1 Detailed | Submitted |
| L2 | Recruitment | r17, r18 | 2 Detailed | Approved (SkipApprovals), Submitted |
| L2 | Compensation | r19 | 1 Detailed | FeedbackRequested |
| L2 | Personnel | r20, r21 | 2 Detailed | Approved, Approved (SkipApprovals+Confidential) |

### Totals
- **By type**: Detailed: 21, Summary: 5, ExecutiveSummary: 2
- **By status**: Draft: 3, Submitted: 6, FeedbackRequested: 2, Approved: 15, Summarized: 1
- **SkipApprovals**: 5 (r6, r13, r14, r17, r21)
- **Confidential**: 1 (r21)

### ReportSourceLinks (Summary→Sources)
- r22 (AQA Summary) → r1, r3, r4
- r23 (Student Activities) → r6, r8
- r24 (Admission) → r10, r11
- r25 (Campus Admin) → r13, r14
- r26 (HR) → r17, r20
- r27 (Institutional Perf) → r22, r23, r25

### ReportSeeder Helper Methods
- `R(title, type, status, author, committee, daysAgo, body, ..., skipApprovals)` — creates report
- `SH(report, from, to, by, daysAgo, comment)` — adds status history entry
- `Approve(report, by, daysAgo, comment)` — adds ReportApproval record
- `UByEmail(email)` / `Comm(namePart)` / `D(daysAgo)` — lookup helpers

---

## Report Lifecycle — Collective Approval

### Status Flow
```
With approvals:  Draft → Submitted → FeedbackRequested ⇄ Submitted → Approved → Summarized
Skip approvals:  Draft → Approved → Summarized  (SkipApprovals=true)
```

### ReportStatus Enum (5 values)
`Draft`, `Submitted`, `FeedbackRequested`, `Approved`, `Summarized`

### SkipApprovals Flag
- `Report.SkipApprovals` (bool, default false) — when true, `SubmitReportAsync` transitions directly to `Approved`
- No member approvals needed, no head finalization, no feedback cycle
- UI: checkbox on Create/Edit forms, "Approvals Skipped" badge on Details, "Submit & Approve" button
- Dashboard queries exclude SkipApprovals reports from pending/finalizable/awaiting lists

### Collective Approval Rules (when SkipApprovals=false)
1. Author submits report → status becomes `Submitted`
2. All non-author committee members can approve (records `ReportApproval`)
3. Any committee head can request feedback → status becomes `FeedbackRequested`
4. Author revises and re-submits → back to `Submitted`
5. When ALL non-author members approve → status automatically becomes `Approved`
6. After 3 days, committee head can **finalize** (force-approve) even without all member approvals
7. Approved reports can be included in summaries → `Summarized` when included

### ReportApproval Model
```csharp
public class ReportApproval { int Id, int ReportId, int UserId, DateTime ApprovedAt, string? Comments }
```

### Report Visibility Rules (ReportService)
- **Author** always sees own reports (including drafts)
- **Drafts** are private to the author only
- **Chairman / ChairmanOffice / SystemAdmin** see all non-draft reports
- **Committee members** see non-draft reports in their committees
- **Committee heads** additionally see reports in descendant committees (via `GetDescendantCommitteeIdsAsync`)
- Implemented in: `GetReportsAsync(userId, ...)`, `CanUserViewReportAsync(userId, report)`, `GetVisibleCommitteeIdsAsync(userId)`, `GetVisibleCommitteesAsync(userId)`
- Applied at: Reports/Index, Reports/Details, Reports/DrillDown, Archives/Index, /api/reports, /api/reports/{id}

### Reports/Index — Hierarchical Committee Filter
- Committee dropdown shows nested hierarchy (indented with `└` for children)
- Selecting a parent committee **includes all sub-committee reports** in results
- `GetReportsAsync` accepts optional `committeeIds` list to filter by multiple committees
- "Includes sub-committees" hint shown below dropdown when a committee is selected
- Built via `BuildHierarchicalOptions()` and `IsDescendantOf()` helper methods in `Index.cshtml.cs`

### Summarization
- Summaries can include both Detailed reports AND other Summary reports as sources
- `CreateSummary` page restricted to committee heads only
- `GetSummarizableReportsAsync` returns Approved reports of type Detailed or Summary

---

## Admin/Organization Page

- **Committee Hierarchy Tree**: Expanded by default (all nodes visible on page load)
- Toggle button "Expand/Collapse All" collapses/expands the full tree
- Each node shows: hierarchy level badge, committee name, sector tag, head(s), member count, sub count
- Highlight feature: `?Highlight=<committeeId>` scrolls to and pulses a specific committee node
- Stats cards: Active Committees, Active Users, Active Memberships, Shadow Assignments
- Chairman & Office section shown at top with CO rank table

---

## Key Enums

- `SystemRole`: SystemAdmin, Chairman, ChairmanOffice, CommitteeUser
- `HierarchyLevel`: TopLevel(0), Directors(1), Functions(2), Processes(3), Tasks(4)
- `CommitteeRole`: Head, Member
- `ReportType`: Detailed, Summary, ExecutiveSummary
- `ReportStatus`: Draft, Submitted, FeedbackRequested, Approved, Summarized
- `DirectiveType`: Instruction, Approval, CorrectiveAction, Feedback, InformationNotice
- `DirectivePriority`: Normal, High, Urgent
- `DirectiveStatus`: Issued, Delivered, Acknowledged, InProgress, Implemented, Verified, Closed
- `MeetingType`: Regular, Emergency, Annual, SpecialSession
- `MeetingStatus`: Scheduled, InProgress, MinutesEntry, MinutesReview, Finalized, Cancelled
- `RsvpStatus`: Pending, Accepted, Declined, Tentative
- `ConfirmationStatus`: Pending, Confirmed, RevisionRequested, Abstained
- `DecisionType`: Approval, Direction, Resolution, Deferral
- `ActionItemStatus`: Assigned, InProgress, Completed, Verified
- `ConfidentialItemType`: Report, Directive, Meeting
- `AuditActionType`: Login, Logout, Create, Update, Delete, StatusChange, AccessGranted, AccessDenied, AccessRevoked, ConfidentialityMarked, ConfidentialityUnmarked, RoleChanged, MembershipAdded, MembershipRemoved, SearchPerformed, Export, MeetingStarted, MinutesSubmitted, MinutesConfirmed, MinutesFinalized, DirectiveForwarded, DirectiveAcknowledged, ReportApproved

---

## Services (with key methods)

| Service | Key Methods |
|---------|------------|
| `ReportService` | **GetReportsAsync(userId, committeeId?, authorId?, status?, reportType?, committeeIds?)**, **GetVisibleCommitteeIdsAsync**, **GetVisibleCommitteesAsync**, **CanUserViewReportAsync**, GetReportByIdAsync, CreateReportAsync, UpdateReportAsync, SubmitReportAsync, **ApproveByMemberAsync**, **FinalizeByHeadAsync**, **RequestFeedbackAsync**, **GetPendingApproversAsync**, **CanHeadFinalizeAsync**, CreateSummaryAsync, GetSummarizableReportsAsync, GetDrillDownTreeAsync, GetSummarizationDepthAsync, CanUserReviewReportAsync, IsUserHeadOfCommitteeAsync, GetReportStatsAsync, GetDescendantCommitteeIdsAsync |
| `DirectiveService` | GetDirectivesAsync, CreateDirectiveAsync, ForwardDirectiveAsync, MarkDeliveredAsync, AcknowledgeAsync, StartProgressAsync, MarkImplementedAsync, VerifyAsync, CloseAsync, GetPropagationTreeAsync, GetOverdueDirectivesAsync, CanUserIssueDirectivesAsync |
| `MeetingService` | GetMeetingsAsync, CreateMeetingAsync, StartMeetingAsync, BeginMinutesEntryAsync, SubmitMinutesAsync, TryFinalizeMinutesAsync, AddAttendeeAsync, UpdateRsvpAsync, UpdateConfirmationAsync, AddAgendaItemAsync, AddDecisionAsync, CreateActionItemAsync, GetMeetingStatsAsync |
| `DashboardService` | GetChairmanDashboardAsync, GetOfficeDashboardAsync, **GetCommitteeHeadDashboardAsync**(userId) — includes FinalizableReports + PendingReports, **GetPersonalDashboardAsync**(userId) — includes ReportsAwaitingMyApproval |
| `ConfidentialityService` | MarkAsConfidentialAsync, RemoveConfidentialMarkingAsync, CanUserAccessConfidentialItemAsync, GetAccessImpactPreviewAsync, GrantAccessAsync, RevokeAccessAsync, FilterAccessibleReportsAsync |
| `AuditService` | LogAsync, LogStatusChangeAsync, GetAuditLogsAsync, ExportToCsvAsync |
| `SearchService` | SearchAsync(SearchQuery, userId) → SearchResults { Items, TotalCount } |
| `AnalyticsService` | GetOrganizationAnalyticsAsync, GetMonthlyTrendsAsync, GetCommitteeMetricsAsync, GetComplianceMetricsAsync |
| `KnowledgeBaseService` | GetArticlesAsync, BulkIndexContentAsync, GetTopLevelCategoriesAsync |
| `OrganizationService` | GetAllCommitteesAsync, GetHierarchyTreeAsync, GetOrganizationStatsAsync, GetAvailableUsersAsync |
| `NotificationService` | CreateNotificationAsync, NotifyReportSubmittedAsync, NotifyReportStatusChangedAsync, NotifyDirectiveIssuedAsync, NotifyMeetingInvitationAsync |
| `ReportTemplateService` | GetTemplatesAsync, GetTemplatesForCommitteeAsync, SeedDefaultTemplatesAsync |

---

## Program.cs — Active Seeding

Two seeders are active:
```csharp
await UserSeeder.SeedAdminUsersAsync(context);   // 57 users, 22 committees, 80+ memberships
await ReportSeeder.SeedAsync(context);            // 28 reports, status histories, approvals, source links
```

Commented out (inactive):
```csharp
// await SeedData.InitializeAsync(context);           // old placeholder
// await OrganizationSeeder.SeedAsync(context);       // old 155-user seeder
// await templateService.SeedDefaultTemplatesAsync();  // report templates
// await knowledgeService.SeedDefaultCategoriesAsync(); // KB categories
// await DemoDataSeeder.SeedAsync(context);            // old demo data (references old users/committees)
```

---

## Authorization

- All folders under `/Admin/*`, `/Reports/*`, `/Directives/*`, `/Meetings/*`, `/Confidentiality/*`, `/Search/*`, `/Archives/*`, `/Dashboard/*`, `/Notifications/*`, `/Knowledge/*`, `/Analytics/*`, and `/api/*` → `[Authorize]`
- `/Admin/Backup/*` → `SystemAdminOnly` policy
- `/Auth/*` and `/` → `[AllowAnonymous]`
- Report visibility: controlled by `ReportService.CanUserViewReportAsync` and `GetVisibleCommitteeIdsAsync`
- Report approval: committee members (non-author) approve; head can finalize after 3 days
- Directive issuing: Chairman/ChairmanOffice/SystemAdmin or committee heads
- Meeting scheduling: Chairman/ChairmanOffice/SystemAdmin or committee heads
- Confidentiality: hierarchy-based + Chairman's Office rank-based access

---

## REST API Endpoints

| Route | Method | Auth | Notes |
|-------|--------|------|-------|
| `/api/reports` | GET | Required | Visibility-filtered, excludes drafts. Params: committeeId, status, page, pageSize |
| `/api/reports/{id}` | GET | Required | Visibility-checked via CanUserViewReportAsync |
| `/api/directives` | GET | Required | Params: committeeId, status, page, pageSize |
| `/api/meetings` | GET | Required | Params: committeeId, status, page, pageSize |
| `/api/committees` | GET | Required | All active committees |
| `/api/search?q=` | GET | Required | Unified search. Params: q, page, pageSize |
| `/api/analytics/overview` | GET | Required | Organization analytics |
| `/api/analytics/trends` | GET | Required | 12-month trends |
| `/api/analytics/committees` | GET | Required | Per-committee metrics |
| `/api/analytics/compliance` | GET | Required | Compliance rates |
| `/api/knowledge/articles` | GET | Required | Params: categoryId, search, page, pageSize |
| `/api/knowledge/categories` | GET | Required | Category hierarchy |

---

## Development Patterns

- **File-scoped namespaces**: `namespace X;` (not `namespace X { }`)
- **Nullable reference types**: Enabled
- **Async/await**: All database operations
- **[BindProperty]**: For Razor Pages form binding, `[BindProperty(SupportsGet = true)]` for query params
- **TempData**: Flash messages (`TempData["SuccessMessage"]`, `TempData["ErrorMessage"]`)
- **Include()**: Eager loading for navigation properties
- **Int PKs**: `int` auto-increment primary keys (not GUIDs)
- **ModelState.Remove()**: For navigation properties in POST handlers (`"Report.Author"`, `"Report.Committee"`)
- **Self-referential FKs**: Committee.ParentCommitteeId, Report.OriginalReportId, Directive.ParentDirectiveId
- **DTOs after service classes**: DrillDownNode in ReportService.cs, DashboardData classes in DashboardService.cs, SearchResults in SearchService.cs
- **Rich text editing**: Quill v2.0.2 via CDN
- **Chart.js**: Version 4.4.1 via CDN for Analytics
- **Seeder helpers**: `U()` for users, `H()` for head memberships, `M()` for member memberships, `C()` for committees; `R()` for reports, `SH()` for status history, `Approve()` for approvals

## Common Gotchas (have caused build errors)

- **User.Name** (NOT FullName) — User model property is `Name`
- **CommitteeMembership** has NO `IsActive` property — use `cm.EffectiveTo == null` for active members
- **Notification.UserId** is `string` (not int) — legacy pattern from Phase 1
- **DateTime?** fields (UpdatedAt) need `?? fallback` when assigned to DateTime properties
- **ReportStatus** has 5 values: Draft, Submitted, FeedbackRequested, Approved, Summarized (NO UnderReview, NO Revised, NO Archived)
- **SearchService.SearchAsync** takes `(SearchQuery query, int userId)` — SearchResults has `.Items` and `.TotalCount`
- **_ViewImports.cshtml** needs `@using ReportingSystem.Models` for enum types in Razor views
- **ApiEndpoints.cs** extension method in `namespace ReportingSystem;` — Program.cs needs explicit `using ReportingSystem;`
- **DemoDataSeeder** references OLD users/committees (h.elsayed, n.kamel, etc.) — cannot use without updating user/committee references
- **New page folders** need `AuthorizeFolder` in Program.cs conventions
- **Services** registered as `AddScoped<>` in Program.cs
- **Must delete** `ReportingSystem/db/reporting.db` after schema changes
- **GetReportsAsync** now takes optional `committeeIds` list for multi-committee filtering (used by Reports/Index for sub-committee inclusion)

## NuGet Offline Restore (Sandboxed Environment)

If `dotnet restore` fails with NU1301/proxy 401:
1. Download packages via curl: `curl -sO https://api.nuget.org/v3-flatcontainer/{id}/{ver}/{id}.{ver}.nupkg`
2. `dotnet nuget disable source nuget.org`
3. `dotnet nuget add source /tmp/nuget-packages --name local-packages`
4. `dotnet restore` then `dotnet build --no-restore`

## Git

- **Branch**: `claude/ors-development-continued-1LlZI`
- **Push command**: `git push -u origin claude/ors-development-continued-1LlZI`

---

## Session Handoff Prompt

Use this prompt when starting a new Claude Code session to resume work on this project:

```
You are continuing development of the ORS (Organizational Reporting System) — an ASP.NET Core 8.0 Razor Pages web app for hierarchical organizational reporting.

ALL 11 PHASES ARE COMPLETE. The system is fully functional with seeded data.

CRITICAL CONTEXT:
- Branch: claude/ors-development-continued-1LlZI
- This is Razor Pages (NOT MVC). Pages live in /Pages/, not /Controllers/.
- EF Core 8.0 with SQLite (dev). No migrations — uses EnsureCreatedAsync().
- IMPORTANT: After adding new models/DbSets, delete `ReportingSystem/db/reporting.db` — EnsureCreatedAsync() won't alter existing DBs.
- File-scoped namespaces: `namespace X;`
- Int auto-increment PKs (NOT GUIDs)
- All DB ops are async/await
- [BindProperty] for form binding, TempData for flash messages

ACTIVE SEEDERS (both run on startup):
- UserSeeder.SeedAdminUsersAsync — 57 users, 22 committees, 80+ memberships
- ReportSeeder.SeedAsync — 28 reports with status histories, approvals, and source links
- DemoDataSeeder exists but is COMMENTED OUT (references old users — NOT compatible)

REPORT LIFECYCLE (Collective Approval — NOT the old linear workflow):
- Status flow: Draft → Submitted → FeedbackRequested ⇄ Submitted → Approved → Summarized
- SkipApprovals flag: when true, submit goes directly Draft → Approved (bypasses approval cycle)
- ReportStatus enum has ONLY 5 values: Draft, Submitted, FeedbackRequested, Approved, Summarized
- NO UnderReview, NO Revised, NO Archived statuses (those were removed)
- ReportApproval model tracks per-member approvals
- All non-author committee members must approve (or head finalizes after 3 days)
- Summaries can include both Detailed AND Summary reports as sources

REPORT VISIBILITY (implemented in ReportService):
- Author sees own reports (including drafts)
- Drafts private to author only
- Chairman/CO/Admin see all non-draft reports
- Committee members see non-draft reports in their committees
- Committee heads also see descendant committee reports
- Applied at: Reports/Index, Details, DrillDown, Archives/Index, API endpoints

REPORTS PAGE FILTER:
- Committee dropdown shows hierarchical nesting (indented with └)
- Selecting a committee includes all sub-committee reports (via committeeIds list parameter)
- GetReportsAsync accepts optional committeeIds list for multi-committee filtering

ADMIN/ORGANIZATION:
- Committee hierarchy tree expanded by default
- Toggle button collapses/expands all
- Highlight feature: ?Highlight=<id> scrolls to and pulses a committee node

CURRENT USERS & COMMITTEES (UserSeeder.cs — active seeder):
- 57 users: 1 admin, 1 Chairman (AM), 4 ChairmanOffice, 7 L0 heads, 14 L2 heads, 30 L2 members
- 22 committees: 1 L0 (Top Level), 5 L1 (AQA, Student Activities, Admission, Campus Admin, HR), 16 L2
- L1 members = L2 heads pattern (L2 heads are members of their parent L1 committee)
- Notable: Yehia Razzaz heads both Sports and AWG; Marketing & Outreach and Facility Management have co-heads
- DemoDataSeeder.cs exists but references OLD users/committees — NOT compatible with current UserSeeder

SEEDED REPORTS (ReportSeeder.cs — 28 total):
- 21 L2 Detailed, 5 L1 Summary, 2 L0 ExecutiveSummary
- By status: Draft(3), Submitted(6), FeedbackRequested(2), Approved(15), Summarized(1)
- SkipApprovals: 5 reports (r6, r13, r14, r17, r21)
- Confidential: 1 (r21)
- Source links: L1 summaries link to their L2 detailed reports, L0 exec summary links to L1 summaries

COMMON GOTCHAS:
- User.Name (NOT FullName)
- CommitteeMembership has NO IsActive — use EffectiveTo == null
- Notification.UserId is string (not int)
- DateTime? fields need ?? fallback for DateTime properties
- SearchService.SearchAsync takes (SearchQuery query, int userId)
- _ViewImports.cshtml needs @using ReportingSystem.Models for enums
- ApiEndpoints.cs in namespace ReportingSystem; — Program.cs needs using ReportingSystem;
- DTOs defined after service classes in same file
- New page folders need AuthorizeFolder in Program.cs
- GetReportsAsync has optional committeeIds parameter for multi-committee filtering
- Must delete ReportingSystem/db/reporting.db after schema changes

ENVIRONMENT SETUP (sandboxed — NuGet proxy auth may fail):
  If dotnet restore fails with NU1301/proxy 401:
  1. curl -sO https://api.nuget.org/v3-flatcontainer/{id}/{ver}/{id}.{ver}.nupkg
  2. dotnet nuget disable source nuget.org && dotnet nuget add source /tmp/nuget-packages --name local-packages
  3. dotnet restore && dotnet build --no-restore

Read claude.md for full project structure, committee tree, service methods, API endpoints, and patterns.
```
