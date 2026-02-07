# ORS — Organizational Reporting System

## Project Overview

ASP.NET Core 8.0 Razor Pages web application for hierarchical organizational reporting with bi-directional communication flows, structured meeting management, confidentiality controls, and full audit trail.

**Current Status**: Phase 1 Infrastructure Complete — Phase 2 (Organization & Hierarchy) next

## Tech Stack

- **Framework**: ASP.NET Core 8.0, Razor Pages (not MVC)
- **Database**: EF Core 8.0 with SQLite (dev) / SQL Server (prod)
- **Auth**: Cookie-based with magic link passwordless login (15-min token, 30-day session)
- **Email**: Microsoft Graph API (disabled by default in dev)
- **Frontend**: Bootstrap 5.3.3, Bootstrap Icons (CDN), jQuery + jQuery Validation
- **Data Protection**: Keys persisted to `/keys` folder

## Quick Start

```bash
dotnet build ReportingSystem/ReportingSystem.csproj
dotnet run --project ReportingSystem/ReportingSystem.csproj
# Dev URL: http://localhost:5296
```

## Project Structure

```
ReportingSystem/
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext
│   ├── SeedData.cs                # Placeholder for domain seeding
│   └── UserSeeder.cs              # Seeds 3 admin users
├── Filters/
│   └── AutomaticBackupFilter.cs   # Pre-POST/PUT/DELETE backup trigger
├── Models/
│   ├── User.cs                    # User + MagicLink models
│   ├── Notification.cs            # In-app notifications
│   └── DatabaseBackup.cs          # Backup records
├── Pages/
│   ├── Admin/                     # [Authorize] - requires login
│   │   ├── Backup/Index           # Backup management UI
│   │   ├── Users/                 # CRUD for users
│   │   └── Dashboard              # Admin home (placeholder)
│   ├── Auth/                      # [AllowAnonymous]
│   │   ├── Login                  # Magic link request
│   │   ├── Verify                 # Token verification
│   │   └── Logout                 # Sign out
│   ├── Shared/_Layout.cshtml      # Main layout with nav
│   ├── Index.cshtml               # Public landing page
│   └── Error.cshtml               # Error page
├── Services/
│   ├── MagicLinkService.cs        # Token generation/verification
│   ├── EmailService.cs            # Microsoft Graph email
│   ├── NotificationService.cs     # In-app notifications CRUD
│   ├── DatabaseBackupService.cs   # Backup create/restore/delete
│   └── DailyBackupHostedService.cs # Background backup scheduler
├── wwwroot/                       # Static files (Bootstrap, jQuery)
├── Program.cs                     # App configuration & DI
├── appsettings.json               # Production config
└── appsettings.Development.json   # Dev config (SQLite)
```

## SRS Reference

Full requirements: `ORS_Full_SRS.docx`
Test data: `ORS_Test_Data.md` (~155 users, 5 sectors, 4 hierarchy levels)

## Organizational Hierarchy Model

The ORS models a multi-level organizational structure:

```
Chairman/CEO
  └── Chairman's Office (4 members, rank-based confidentiality, shared resources)
        └── Top Level Committee (L0) — General Secretaries + shadows
              └── Directors (L1) — Sector/Directorate heads
                    └── Functions (L2) — Department/Function heads
                          └── Processes (L3) — Process owners/team leads
                                └── Tasks (L4) — Task executors
```

**Key rules**:
- Tree topology: each committee (except L0) has exactly one parent
- Multi-headed: committees can have multiple heads
- Shared membership: users can belong to multiple committees across branches
- Shadow/backup: L0 members have designated shadows who inherit access (except confidential items)
- Chairman's Office sits outside the operational hierarchy, bridges Chairman ↔ L0

## Communication Flows

**Upward**: Reports originate at Tasks (L4), each level reviews/summarizes/amends, Chairman's Office produces executive summaries for Chairman. Chairman can drill down to any raw report.

**Downward**: Chairman issues directives → Chairman's Office contextualizes → L0 propagates downward. Each level acknowledges, implements, reports status upward.

**Lateral**: Within-committee knowledge sharing via meetings with structured agendas, minutes, and confirmation workflows.

**Local Decisions**: Committees at any level can make decisions within delegated authority; all decisions documented and included in upward reports.

---

## Implementation Roadmap

### Phase 1: Infrastructure Foundation [COMPLETE]
- [x] ASP.NET Core 8.0 project setup with Razor Pages
- [x] EF Core with SQLite (dev) / SQL Server (prod)
- [x] Cookie-based magic link authentication
- [x] Models: User, MagicLink, Notification, DatabaseBackup
- [x] Services: MagicLinkService, EmailService, NotificationService, DatabaseBackupService, DailyBackupHostedService
- [x] Admin pages: Dashboard (placeholder), Users CRUD, Backup management
- [x] Auth pages: Login, Verify, Logout
- [x] AutomaticBackupFilter, Data Protection keys
- [x] 3 seeded admin users

### Phase 2: Organization & Hierarchy Model [NEXT]
**Goal**: Model the complete organizational structure from SRS Section 3 & 4.1

**Models to create**:
- `Committee` — Id (int), Name, HierarchyLevel (enum: TopLevel/Directors/Functions/Processes/Tasks), ParentCommitteeId (self-ref FK), Description, IsActive, CreatedAt
- `CommitteeMembership` — Id, UserId (FK→User), CommitteeId (FK→Committee), Role (enum: Head/Member), EffectiveFrom, EffectiveTo
- `ShadowAssignment` — Id, PrincipalUserId, ShadowUserId, CommitteeId, IsActive, EffectiveFrom, EffectiveTo

**User model changes**:
- Add Title (string), Phone (string), SystemRole (enum: SystemAdmin/Chairman/ChairmanOffice/CommitteeUser)
- Add ChairmanOfficeRank (int?, nullable — only for Chairman's Office members)
- Keep existing Email, Name, IsActive, CreatedAt, LastLoginAt

**Pages to create**:
- `/Admin/Organization/Index` — Visual org tree (expandable hierarchy)
- `/Admin/Organization/Committees/Create|Edit|Details|Delete` — Committee CRUD
- `/Admin/Organization/Members` — Membership management (assign users to committees with roles)
- `/Admin/Organization/Shadows` — Shadow/backup assignment management

**Seed data**: Import ORS_Test_Data.md (Chairman, Office, L0-L3 committees, ~155 users, cross-memberships)

**Key constraints**:
- Enforce tree topology (no circular refs, L0 has no parent)
- Support multi-headed committees
- Support cross-committee memberships
- Shadow access inheritance (Phase 7 enforces confidentiality exceptions)

### Phase 3: Report Lifecycle
**Goal**: Core report submission and lifecycle management (SRS 4.2.1, 4.2.2)

**Models to create**:
- `Report` — Id, Title, ReportType (enum: Detailed/Summary/ExecutiveSummary), Status (enum: Draft/Submitted/UnderReview/FeedbackRequested/Revised/Summarized/Approved/Archived), AuthorId (FK→User), CommitteeId (FK→Committee), BodyContent (rich text), SuggestedAction, NeededResources, NeededSupport, SpecialRemarks, IsConfidential, SubmittedAt, Version (int), CreatedAt
- `Attachment` — Id, ReportId (FK), FileName, StoragePath, ContentType, FileSizeBytes, UploadedAt, UploadedById (FK→User)
- `ReportStatusHistory` — Id, ReportId (FK), OldStatus, NewStatus, ChangedById (FK→User), ChangedAt, Comments

**Pages to create**:
- `/Reports/Create` — Report form with rich text editor, optional sections, file attachments
- `/Reports/Index` — Report list with filtering (by committee, status, date, author)
- `/Reports/Details/{id}` — Full report view with attachments and status history
- `/Reports/Edit/{id}` — Edit draft or revise after feedback
- Committee-scoped report views (see reports from your committee and subcommittees)

**Key behaviors**:
- Only committee members can submit reports to their committee
- Status transitions governed by role (only heads can move Submitted → Under Review)
- Feedback loop: reviewer sends back with comments, author revises (new version, preserves original)
- Notification on submission to parent committee head(s)

### Phase 4: Progressive Summarization & Drill-Down
**Goal**: Summary chains and Chairman drill-down (SRS 4.2.3)

**Models to create**:
- `ReportSourceLink` — Id, SummaryReportId (FK→Report), SourceReportId (FK→Report) — many-to-many linking summaries to their sources

**Pages to create**:
- Summary creation form (pre-linked to source reports, supports annotations/amendments)
- Drill-down view: expandable chain from executive summary → L0 summary → ... → raw report
- Summary chain visualization (depth indicator, lineage view)
- Chairman dedicated drill-down interface

**Key behaviors**:
- Summaries explicitly link to source report(s) via ReportSourceLink
- Annotations/amendments clearly distinguished from original content
- Summarization depth badge on each item
- Chairman has universal drill-down access

### Phase 5: Directives & Feedback
**Goal**: Top-down communication with propagation tracking (SRS 4.3)

**Models to create**:
- `Directive` — Id, Title, DirectiveType (enum: Instruction/Approval/CorrectiveAction/Feedback/InformationNotice), Priority (enum: Normal/High/Urgent), Status (enum: Issued/Delivered/Acknowledged/InProgress/Implemented/Verified/Closed), IssuerId (FK→User), TargetCommitteeId (FK), TargetUserId (FK), RelatedReportId (FK), ParentDirectiveId (self-ref FK for propagation), BodyContent, Deadline, CreatedAt
- `DirectiveStatusHistory` — tracks each status transition

**Pages to create**:
- `/Directives/Create` — Directive form (optionally linked to report)
- `/Directives/Index` — Directive list with status pipeline view
- `/Directives/Details/{id}` — Full directive with propagation tree
- `/Directives/Track` — Overdue directives dashboard

**Key behaviors**:
- Chairman → Chairman's Office → L0 → ... automatic routing
- Chairman's Office can annotate before forwarding
- Propagation: committee head creates derived directive linked to parent
- Auto-mark "Delivered" on view, explicit "Acknowledged" by target
- Overdue tracking with notifications

### Phase 6: Meeting Management
**Goal**: Full meeting lifecycle with structured minutes and confirmation (SRS 4.4)

**Models to create**:
- `Meeting` — Id, Title, ScheduledAt, Duration, Location, HostCommitteeId (FK), ModeratorId (FK→User), MinutesStatus (enum: Pending/Submitted/UnderReview/Finalized), MinutesContent, IsRecurring, RecurrencePattern, CreatedAt
- `MeetingAgendaItem` — Id, MeetingId (FK), Order, TopicTitle, Description, AllocatedMinutes, PresenterId (FK→User)
- `MeetingAttendee` — Id, MeetingId (FK), UserId (FK), RsvpStatus (enum: Pending/Accepted/Declined/Tentative), ConfirmationStatus (enum: Pending/Confirmed/RevisionRequested/Abstained), RsvpComment, ConfirmationComment
- `MeetingDecision` — Id, MeetingId (FK), AgendaItemId (FK), DecisionText, DecisionType (enum: Approval/Direction/Resolution/Deferral)
- `ActionItem` — Id, MeetingDecisionId (FK), DirectiveId (FK, optional), Title, Description, AssignedToId (FK→User), Status (enum: Assigned/InProgress/Completed/Verified), Deadline, CompletedAt

**Pages to create**:
- `/Meetings/Schedule` — Meeting scheduler with agenda builder
- `/Meetings/Index` — Meeting list (upcoming, past)
- `/Meetings/Details/{id}` — Meeting view with agenda, attendees, minutes
- `/Meetings/Minutes/{id}` — Minutes entry/editing (per agenda item)
- `/Meetings/Confirm/{id}` — Attendee confirmation interface
- `/ActionItems/Index` — Consolidated action items dashboard

**Key behaviors**:
- Attendees from multiple committees (including Chairman's Office)
- RSVP workflow, agenda with linked documents/reports
- Minutes: per-agenda-item notes, decisions, action items
- Confirmation: all attendees must Confirm or Abstain before finalization
- Finalized minutes become immutable
- Decisions generate tracked action items

### Phase 7: Confidentiality & Access Control
**Goal**: Hierarchy-based access control and confidentiality marking (SRS 4.5)

**Models to create**:
- `ConfidentialityMarking` — Id, ItemType (enum: Report/Attachment/Meeting/Directive), ItemId, MarkedById (FK→User), MarkedAt, UnmarkedAt, MinimumRank (int?, for Chairman's Office rank-based), AccessChangeDetails (JSON)

**Implementation**:
- Mark any owned item as Confidential with access impact preview
- Show confirmation screen: who has access now → who will have access after marking → who loses access
- Confidential items: accessible only by higher hierarchy levels + marker + Chairman (always)
- Shadow/backup loses access to confidential items of their principal
- Chairman's Office rank-based: restrict to equal/higher rank + Chairman
- Reversible: original marker or admin can unmark
- All marking/unmarking events in audit log
- Hierarchy-based default access: users see own committee + subcommittees (downward visibility)
- No default upward access
- Cross-committee access via shared membership
- Explicit sharing support

### Phase 8: Search, Archives & Audit
**Goal**: Full-text search, archive management, and comprehensive audit logging (SRS 4.6, 4.8)

**Models to create**:
- `AuditLog` — Id, Timestamp, UserId, ActionType, AffectedItemType, AffectedItemId, BeforeValue, AfterValue, IpAddress, SessionId (append-only, no updates/deletes)

**Implementation**:
- Unified search across reports, minutes, directives, attachments, action items
- Keyword, phrase, boolean, date range, content type, committee, status filters
- Search respects access control
- Text extraction from PDF/Word/Excel attachments for search indexing
- Auto-archive on terminal lifecycle status
- Configurable retention policies
- Audit log: every action logged (login, CRUD, status changes, access events, confidentiality changes)
- Audit log viewer with filtering and CSV/PDF export

### Phase 9: Dashboards & Notifications
**Goal**: Role-specific dashboards and enhanced notification system (SRS 4.7)

**Dashboards to create**:
- **Chairman Dashboard**: Executive summaries, open directives, overdue items, org health metrics
- **Chairman's Office Bridge Dashboard**: Incoming reports from L0, outgoing summaries, pending directive relays
- **Committee Head Dashboard**: Pending reports, active directives, upcoming meetings, action items
- **Personal Dashboard**: My tasks, pending actions, recent activity, quick access to committees

**Notification enhancements**:
- Events: report submission, status changes, feedback, directives, meeting invitations, minutes confirmation, action items, overdue items, confidentiality access changes
- Channels: in-app (always), email (configurable), digest option (daily/weekly)
- User preferences: per event type, per channel, frequency

### Phase 10: Report Templates & Polish
**Goal**: Configurable templates, export, responsive design (SRS 4.2.4, remaining NFRs)

**Implementation**:
- Report template designer (define required/optional fields per committee/level)
- Default templates: Progress Report, Incident Report, Decision Request, Status Update, Meeting Prep Brief
- Template assignment by committee or hierarchy level
- PDF/Word/Excel export for reports and meeting minutes
- Mobile-responsive optimization (768px–2560px)
- RTL/Arabic language support
- Performance optimization for large hierarchies
- Contextual help tooltips

### Phase 11: Knowledge Base & AI (Future)
**Goal**: Knowledge base from approved content, AI summarization (SRS Phase 4)

- Organizational knowledge base from non-confidential approved content
- AI-assisted summarization suggestions
- Semantic search capabilities
- REST API for third-party integrations
- Mobile application

---

## Current Models (Phase 1)

### User
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }           // Unique
    public string Name { get; set; }
    public string Role { get; set; }            // "Administrator" only for now
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<MagicLink> MagicLinks { get; set; }
}
```

### MagicLink
```csharp
public class MagicLink
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; }           // 32-byte secure random
    public DateTime ExpiresAt { get; set; }     // 15 minutes
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

### Notification
```csharp
public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public NotificationPriority Priority { get; set; }
    public int? RelatedEntityId { get; set; }
}
```

### DatabaseBackup
```csharp
public class DatabaseBackup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public BackupType Type { get; set; }       // Manual, AutomaticDaily, PreRestore
    public string? CreatedBy { get; set; }
    public bool IsAutomaticDailyBackup { get; set; }
}
```

## Services

| Service | Purpose |
|---------|---------|
| `MagicLinkService` | Generate 32-byte tokens, verify & consume, cleanup expired |
| `EmailService` | Send via Microsoft Graph API (configurable, disabled by default) |
| `NotificationService` | Create, list, mark read, cleanup old notifications |
| `DatabaseBackupService` | Create manual/auto backups, restore, WAL checkpoint |
| `DailyBackupHostedService` | Background service, checks hourly, creates backup every 12h |

## Configuration

### appsettings.Development.json
```json
{
  "DatabaseSettings": {
    "Provider": "SQLite",
    "ConnectionStrings": {
      "SQLite": "Data Source=db/reporting.db"
    }
  }
}
```

### appsettings.json (Email disabled by default)
```json
{
  "EmailSettings": {
    "Enabled": false,
    "TenantId": "", "ClientId": "", "ClientSecret": "",
    "SenderUserId": "", "SenderEmail": "", "SenderName": ""
  }
}
```

## Seeded Data (Phase 1)

| Email | Name | Role |
|-------|------|------|
| admin@reporting.com | System Administrator | Administrator |
| admin1@reporting.com | Administrator One | Administrator |
| admin2@reporting.com | Administrator Two | Administrator |

Phase 2 will replace/extend this with the full ORS_Test_Data.md dataset (~155 users).

## Development Patterns

- **File-scoped namespaces**: `namespace X;` (not `namespace X { }`)
- **Nullable reference types**: Enabled
- **Async/await**: All database operations
- **[BindProperty]**: For Razor Pages form binding
- **TempData**: Flash messages (`TempData["SuccessMessage"]`, `TempData["ErrorMessage"]`)
- **Include()**: Eager loading for navigation properties
- **Int PKs**: Using `int` auto-increment primary keys (not GUIDs) — existing convention

## Reference

- SRS: `ORS_Full_SRS.docx`
- Test Data: `ORS_Test_Data.md`
- Infrastructure template: `ref-only-example/SchedulingSystem/` (namespace changed, domain code not replicated)
