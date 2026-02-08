# ORS — Organizational Reporting System

## Project Overview

ASP.NET Core 8.0 Razor Pages web application for hierarchical organizational reporting with bi-directional communication flows, structured meeting management, confidentiality controls, and full audit trail.

**Current Status**: Phases 1-6 Complete — Phase 7 (Confidentiality & Access Control) is NEXT

## Tech Stack

- **Framework**: ASP.NET Core 8.0, Razor Pages (NOT MVC)
- **ORM**: EF Core 8.0 with SQLite (dev) / SQL Server (prod), `EnsureCreatedAsync()` (no migrations)
- **Auth**: Cookie-based with magic link passwordless login (15-min token, 30-day sliding session)
- **Email**: Microsoft Graph API (disabled by default in dev)
- **Frontend**: Bootstrap 5.3.3, Bootstrap Icons (CDN), jQuery + jQuery Validation
- **Data Protection**: Keys persisted to `/keys` folder

## Quick Start

```bash
# Install .NET SDK if not present (needed in sandboxed environments)
apt-get update -qq && apt-get install -y -qq dotnet-sdk-8.0

dotnet build ReportingSystem/ReportingSystem.csproj
dotnet run --project ReportingSystem/ReportingSystem.csproj
# Dev URL: http://localhost:5296
```

**Sandboxed Environment Setup** (run these before building if .NET SDK is not installed):

```bash
# 1. Install .NET SDK 8.0
apt-get update -qq && apt-get install -y -qq dotnet-sdk-8.0

# 2. Fix DNS resolution (resolv.conf may be missing in sandboxed environments)
echo "nameserver 8.8.8.8" > /etc/resolv.conf

# 3. Bypass proxy for NuGet (https_proxy env var may interfere with NuGet restore)
export no_proxy="${no_proxy:+$no_proxy,}api.nuget.org,nuget.org,www.nuget.org"

# 4. Now build
dotnet build ReportingSystem/ReportingSystem.csproj
```

**Important**: After schema changes (new models/DbSets), delete the existing SQLite DB file (`ReportingSystem/db/reporting.db`) before running — `EnsureCreatedAsync()` won't add new tables to an existing database.

## SRS Reference

- Full requirements: `ORS_Full_SRS.docx`
- Test data: `ORS_Test_Data.md` (~155 users, 5 sectors, 4 hierarchy levels)
- Infrastructure template: `ref-only-example/SchedulingSystem/` (namespace reference only)

---

## Project Structure

```
ReportingSystem/
├── Data/
│   ├── ApplicationDbContext.cs      # EF Core DbContext (~330 lines)
│   │                                 # DbSets: Users, MagicLinks, Committees, CommitteeMemberships,
│   │                                 # ShadowAssignments, Reports, Attachments, ReportStatusHistories,
│   │                                 # ReportSourceLinks, Directives, DirectiveStatusHistories,
│   │                                 # Meetings, MeetingAgendaItems, MeetingAttendees,
│   │                                 # MeetingDecisions, ActionItems, Notifications, DatabaseBackups
│   ├── SeedData.cs                  # Placeholder for domain seeding
│   ├── UserSeeder.cs                # Seeds 3 admin users
│   └── OrganizationSeeder.cs        # Seeds ~155 users, ~185 committees, memberships (853 lines)
├── Filters/
│   └── AutomaticBackupFilter.cs     # Pre-POST/PUT/DELETE backup trigger
├── Models/
│   ├── User.cs                      # User + MagicLink + SystemRole enum
│   ├── Committee.cs                 # Committee + HierarchyLevel enum (Phase 2)
│   ├── CommitteeMembership.cs       # Membership + CommitteeRole enum (Phase 2)
│   ├── ShadowAssignment.cs          # Shadow/backup assignments (Phase 2)
│   ├── Report.cs                    # Report + ReportType/ReportStatus enums (Phase 3)
│   ├── Attachment.cs                # File attachments for reports (Phase 3)
│   ├── ReportStatusHistory.cs       # Audit trail for status transitions (Phase 3)
│   ├── ReportSourceLink.cs          # Summary→Source report links (Phase 4)
│   ├── Directive.cs                 # Directive + DirectiveType/Priority/Status enums (Phase 5)
│   ├── DirectiveStatusHistory.cs    # Audit trail for directive status transitions (Phase 5)
│   ├── Meeting.cs                   # Meeting + MeetingType/MeetingStatus/RecurrencePattern enums (Phase 6)
│   ├── MeetingAgendaItem.cs         # Structured agenda items with presenter + linked reports (Phase 6)
│   ├── MeetingAttendee.cs           # Attendees with RSVP + minutes confirmation (Phase 6)
│   ├── MeetingDecision.cs           # Formal decisions + DecisionType enum (Phase 6)
│   ├── ActionItem.cs                # Trackable action items + ActionItemStatus enum (Phase 6)
│   ├── Notification.cs              # In-app notifications
│   └── DatabaseBackup.cs            # Backup records
├── Pages/
│   ├── Admin/
│   │   ├── Dashboard.cshtml(.cs)    # Stats: org + report + directive + meeting counts
│   │   ├── Backup/Index             # Backup management (SystemAdmin only)
│   │   ├── Users/                   # CRUD: Index, Create, Edit, Details, Delete
│   │   └── Organization/
│   │       ├── Index                # Org tree visualization
│   │       ├── _CommitteeTreeNode   # Recursive tree partial
│   │       └── Committees/          # CRUD: Index, Create, Edit, Details, Delete
│   ├── Auth/
│   │   ├── Login                    # Magic link request
│   │   ├── Verify                   # Token verification + sign-in
│   │   └── Logout                   # Sign out
│   ├── Reports/
│   │   ├── Index                    # Filterable report list (committee, status, type, mine)
│   │   ├── Create                   # New report with file uploads
│   │   ├── Details                  # Full view + actions + source/summary links + drill-down
│   │   ├── Edit                     # Edit draft / create revision
│   │   ├── CreateSummary            # Summary creation with source report selection
│   │   ├── DrillDown                # Upward/downward chain visualization
│   │   └── _DrillDownNode           # Recursive partial for drill-down tree
│   ├── Directives/
│   │   ├── Index                    # Filterable directive list with status pipeline
│   │   ├── Create                   # Issue directive (optionally linked to report)
│   │   ├── Details                  # Full view + actions + propagation tree + forwarding
│   │   └── Track                    # Overdue directives dashboard
│   ├── Meetings/
│   │   ├── Index                    # Filterable meeting list with status pipeline
│   │   ├── Create                   # Schedule meeting with committee + auto-invite
│   │   ├── Details                  # Full view + agenda + attendees + RSVP + decisions + action items
│   │   ├── Minutes                  # Minutes entry (per-agenda-item notes + overall summary)
│   │   └── ActionItems              # Consolidated action items dashboard with status transitions
│   ├── Shared/_Layout.cshtml        # Nav: Home, Organization, Reports, Directives, Meetings, Administration, User
│   ├── Index.cshtml                 # Landing → redirects to Dashboard or Login
│   └── Error.cshtml
├── Services/
│   ├── MagicLinkService.cs          # Token gen/verify/cleanup (216 lines)
│   ├── EmailService.cs              # Microsoft Graph email sending (292 lines)
│   ├── NotificationService.cs       # In-app notifications CRUD (155 lines)
│   ├── DatabaseBackupService.cs     # Backup create/restore/delete/WAL (571 lines)
│   ├── DailyBackupHostedService.cs  # Background 12h backup scheduler (96 lines)
│   ├── OrganizationService.cs       # Committee/membership/shadow CRUD (235 lines)
│   ├── ReportService.cs             # Reports, status workflow, summarization, drill-down (547 lines)
│   ├── DirectiveService.cs          # Directives, status transitions, propagation, overdue (~340 lines)
│   └── MeetingService.cs            # Meetings, agenda, attendees, RSVP, minutes, decisions, action items (~420 lines)
├── wwwroot/                         # Static files (Bootstrap, jQuery, uploads/)
├── Program.cs                       # App configuration & DI (~140 lines)
├── appsettings.json                 # Production config
└── appsettings.Development.json     # Dev config (SQLite)
```

---

## Organizational Hierarchy Model

```
Chairman/CEO
  └── Chairman's Office (4 members, rank-based confidentiality 1=senior 4=junior)
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

- **Upward**: Reports L4→L3→...→L0→Chairman's Office→Chairman (with progressive summarization)
- **Downward**: Chairman→Chairman's Office→L0→...→L4 (directives with propagation tracking)
- **Lateral**: Within-committee meetings with structured agendas, minutes, confirmations
- **Local Decisions**: Committees make decisions within delegated authority, documented in upward reports

---

## Implementation Roadmap

### Phase 1: Infrastructure Foundation [COMPLETE]
- [x] ASP.NET Core 8.0 project with Razor Pages + EF Core + SQLite
- [x] Cookie-based magic link authentication (15-min token, 30-day session)
- [x] Models: User, MagicLink, Notification, DatabaseBackup
- [x] Services: MagicLinkService, EmailService, NotificationService, DatabaseBackupService, DailyBackupHostedService
- [x] Pages: Auth (Login/Verify/Logout), Admin (Dashboard/Users CRUD/Backup)
- [x] AutomaticBackupFilter, Data Protection keys, 3 seeded admin users

### Phase 2: Organization & Hierarchy Model [COMPLETE]
- [x] Models: Committee (HierarchyLevel L0-L4), CommitteeMembership (Head/Member), ShadowAssignment
- [x] User model extended: SystemRole enum, Title, Phone, ChairmanOfficeRank
- [x] OrganizationService: full CRUD for committees, memberships, shadows, stats
- [x] Pages: Org tree visualization, Committee CRUD with member/shadow management
- [x] OrganizationSeeder: ~155 users, ~185 committees, ~500+ memberships, 5 sectors

### Phase 3: Report Lifecycle [COMPLETE]
- [x] Models: Report (8 statuses, 3 types), Attachment, ReportStatusHistory
- [x] ReportService: CRUD, status workflow (Draft→Submitted→UnderReview→Approved/Feedback→Revised), attachments
- [x] Pages: Index (filtered), Create (with uploads), Details (with actions), Edit (draft+revision)
- [x] Status transitions: submit, start review, request feedback, revise (new version), approve, archive
- [x] Dashboard updated with report statistics

### Phase 4: Progressive Summarization & Drill-Down [COMPLETE]
- [x] Model: ReportSourceLink (many-to-many summary↔source with annotations)
- [x] ReportService extended: CreateSummaryAsync, GetDrillDownTreeAsync (recursive), GetSummarizableReportsAsync, GetSummariesOfReportAsync, GetSummarizationDepthAsync
- [x] Pages: CreateSummary (committee-scoped source selection), DrillDown (upward+downward chain), _DrillDownNode (recursive partial)
- [x] Details page: source reports section, summaries-of-this section, drill-down button
- [x] Layout nav: Reports dropdown includes Create Summary link

### Phase 5: Directives & Feedback [COMPLETE]
- [x] Models: Directive (7 statuses, 5 types, 3 priorities), DirectiveStatusHistory
- [x] DirectiveService: CRUD, 7-status workflow (Issued→Delivered→Acknowledged→InProgress→Implemented→Verified→Closed)
- [x] Propagation: ForwardDirectiveAsync creates child directives with ParentDirectiveId chain
- [x] Propagation tree visualization: GetPropagationTreeAsync walks root→leaves
- [x] Access control: CanUserIssueDirectivesAsync, IsUserTargetOfDirectiveAsync, GetTargetableCommitteesAsync
- [x] Overdue tracking: GetOverdueDirectivesAsync, GetApproachingDeadlineDirectivesAsync
- [x] Pages: Index (status pipeline + filters), Create (with report linking), Details (actions + propagation tree + forwarding), Track (overdue dashboard)
- [x] Auto-mark "Delivered" on target view, explicit "Acknowledged" by target
- [x] Dashboard updated with directive statistics (active, issued, in progress, overdue)
- [x] Layout nav: Directives dropdown (All Directives, Issue Directive, Track Overdue)

### Phase 6: Meeting Management [COMPLETE]
- [x] Models: Meeting (6 statuses, 4 types, 3 recurrence patterns), MeetingAgendaItem, MeetingAttendee (RSVP + Confirmation), MeetingDecision (4 types), ActionItem (4 statuses)
- [x] MeetingService: full lifecycle — schedule, start, minutes entry/submit, attendee confirmation, finalization
- [x] Meeting scheduling: committee-scoped, auto-invite committee members, moderator role
- [x] Structured agenda: ordered items with allocated time, presenter, linked reports, discussion notes
- [x] RSVP workflow: Accept/Decline/Tentative with comments
- [x] Minutes: per-agenda-item discussion notes + overall summary, save draft + submit for confirmation
- [x] Confirmation workflow: each attendee Confirms/Requests Revision/Abstains — finalized only when all responded
- [x] Decisions: captured per meeting with DecisionType (Approval/Direction/Resolution/Deferral) and deadlines
- [x] Action items: Assigned→InProgress→Completed→Verified lifecycle, overdue tracking, consolidated dashboard
- [x] Pages: Index (status pipeline + filters), Create (with auto-invite), Details (full meeting view with all sub-entities), Minutes (structured entry), ActionItems (consolidated dashboard with status actions)
- [x] Dashboard updated with meeting statistics (total, scheduled, awaiting confirmation, overdue actions)
- [x] Layout nav: Meetings dropdown (All Meetings, Schedule Meeting, Action Items)

### Phase 7: Confidentiality & Access Control [NEXT]
**Goal**: Hierarchy-based access control and confidentiality marking (SRS 4.5)

**Model**: ConfidentialityMarking — per-item marking with rank-based Chairman's Office access, shadow exclusion, reversible

### Phase 8: Search, Archives & Audit
**Goal**: Full-text search, archive management, comprehensive audit logging (SRS 4.6, 4.8)

**Model**: AuditLog — append-only, unified search across all content types

### Phase 9: Dashboards & Notifications
**Goal**: Role-specific dashboards (Chairman/Office/Head/Personal), enhanced notifications (SRS 4.7)

### Phase 10: Report Templates & Polish
**Goal**: Configurable templates, PDF/Word export, RTL/Arabic support (SRS 4.2.4)

### Phase 11: Knowledge Base & AI (Future)
**Goal**: Knowledge base, AI summarization, REST API, mobile app (SRS Phase 4)

---

## Current Models (Summary)

| Model | Phase | Key Fields | Relationships |
|-------|-------|------------|---------------|
| `User` | 1+2 | Email, Name, SystemRole, Title, Phone, ChairmanOfficeRank | → MagicLinks, CommitteeMemberships |
| `MagicLink` | 1 | Token (unique), ExpiresAt, IsUsed, IpAddress, UserAgent | → User |
| `Committee` | 2 | Name, HierarchyLevel (L0-L4), ParentCommitteeId, Sector | → Parent, SubCommittees, Memberships, Shadows |
| `CommitteeMembership` | 2 | UserId, CommitteeId, Role (Head/Member), EffectiveFrom/To | → User, Committee |
| `ShadowAssignment` | 2 | PrincipalUserId, ShadowUserId, CommitteeId, IsActive | → Users, Committee |
| `Report` | 3 | Title, ReportType, Status, AuthorId, CommitteeId, BodyContent, Version, OriginalReportId | → Author, Committee, Attachments, StatusHistory, SourceLinks, SummaryLinks, Revisions |
| `Attachment` | 3 | ReportId, FileName, StoragePath, ContentType, FileSizeBytes | → Report, UploadedBy |
| `ReportStatusHistory` | 3 | ReportId, OldStatus, NewStatus, ChangedById, Comments | → Report, ChangedBy |
| `ReportSourceLink` | 4 | SummaryReportId, SourceReportId, Annotation | → SummaryReport, SourceReport |
| `Directive` | 5 | Title, DirectiveType, Priority, Status, IssuerId, TargetCommitteeId, TargetUserId, RelatedReportId, ParentDirectiveId, BodyContent, ForwardingAnnotation, Deadline | → Issuer, TargetCommittee, TargetUser, RelatedReport, ParentDirective, ChildDirectives, StatusHistory |
| `DirectiveStatusHistory` | 5 | DirectiveId, OldStatus, NewStatus, ChangedById, Comments | → Directive, ChangedBy |
| `Meeting` | 6 | Title, MeetingType, Status, CommitteeId, ModeratorId, Description, Location, ScheduledAt, DurationMinutes, RecurrencePattern, MinutesContent, MinutesSubmittedAt, MinutesFinalizedAt | → Committee, Moderator, AgendaItems, Attendees, Decisions, ActionItems |
| `MeetingAgendaItem` | 6 | MeetingId, OrderIndex, TopicTitle, Description, AllocatedMinutes, PresenterId, LinkedReportId, DiscussionNotes | → Meeting, Presenter, LinkedReport |
| `MeetingAttendee` | 6 | MeetingId, UserId, RsvpStatus, RsvpComment, RsvpAt, ConfirmationStatus, ConfirmationComment, ConfirmedAt | → Meeting, User |
| `MeetingDecision` | 6 | MeetingId, AgendaItemId, DecisionText, DecisionType, Deadline | → Meeting, AgendaItem, ActionItems |
| `ActionItem` | 6 | MeetingId, MeetingDecisionId, Title, Description, AssignedToId, AssignedById, Status, Deadline, CompletedAt, VerifiedAt | → Meeting, MeetingDecision, AssignedTo, AssignedBy |
| `Notification` | 1 | UserId, Type, Title, Message, IsRead, Priority | — |
| `DatabaseBackup` | 1 | Name, FileName, FilePath, Type, CreatedBy | — |

### Key Enums
- `SystemRole`: SystemAdmin, Chairman, ChairmanOffice, CommitteeUser
- `HierarchyLevel`: TopLevel(0), Directors(1), Functions(2), Processes(3), Tasks(4)
- `CommitteeRole`: Head, Member
- `ReportType`: Detailed, Summary, ExecutiveSummary
- `ReportStatus`: Draft, Submitted, UnderReview, FeedbackRequested, Revised, Summarized, Approved, Archived
- `DirectiveType`: Instruction, Approval, CorrectiveAction, Feedback, InformationNotice
- `DirectivePriority`: Normal, High, Urgent
- `DirectiveStatus`: Issued, Delivered, Acknowledged, InProgress, Implemented, Verified, Closed
- `MeetingType`: Regular, Emergency, Annual, SpecialSession
- `MeetingStatus`: Scheduled, InProgress, MinutesEntry, MinutesReview, Finalized, Cancelled
- `RecurrencePattern`: None, Weekly, Biweekly, Monthly
- `RsvpStatus`: Pending, Accepted, Declined, Tentative
- `ConfirmationStatus`: Pending, Confirmed, RevisionRequested, Abstained
- `DecisionType`: Approval, Direction, Resolution, Deferral
- `ActionItemStatus`: Assigned, InProgress, Completed, Verified

## Services (with key methods)

| Service | Key Methods |
|---------|------------|
| `MagicLinkService` | GenerateMagicLinkAsync, VerifyMagicLinkAsync, CleanupExpiredLinksAsync |
| `EmailService` | SendMagicLinkEmailAsync, SendNotificationEmailAsync |
| `NotificationService` | CreateNotificationAsync, GetUserNotificationsAsync, MarkAsReadAsync, GetUnreadCountAsync |
| `DatabaseBackupService` | CreateManualBackupAsync, RestoreBackupAsync, DeleteBackupAsync, GetStatisticsAsync, ForceWalCheckpointAsync |
| `DailyBackupHostedService` | ExecuteAsync (hourly check, 12h interval) |
| `OrganizationService` | GetAllCommitteesAsync, GetHierarchyTreeAsync, CreateCommitteeAsync, AddMembershipAsync, AddShadowAssignmentAsync, GetOrganizationStatsAsync |
| `ReportService` | GetReportsAsync, CreateReportAsync, UpdateReportAsync, SubmitReportAsync, StartReviewAsync, RequestFeedbackAsync, ReviseReportAsync, ApproveReportAsync, ArchiveReportAsync, AddAttachmentAsync, CreateSummaryAsync, GetDrillDownTreeAsync, GetSummarizableReportsAsync, GetSummarizationDepthAsync, CanUserReviewReportAsync, GetReportStatsAsync |
| `DirectiveService` | GetDirectivesAsync, GetDirectiveByIdAsync, GetDirectivesForUserAsync, CreateDirectiveAsync, ForwardDirectiveAsync, MarkDeliveredAsync, AcknowledgeAsync, StartProgressAsync, MarkImplementedAsync, VerifyAsync, CloseAsync, GetPropagationTreeAsync, GetOverdueDirectivesAsync, GetApproachingDeadlineDirectivesAsync, CanUserIssueDirectivesAsync, IsUserTargetOfDirectiveAsync, GetTargetableCommitteesAsync, GetForwardableCommitteesAsync, GetDirectiveStatsAsync |
| `MeetingService` | GetMeetingsAsync, GetMeetingByIdAsync, GetMeetingsForUserAsync, CreateMeetingAsync, UpdateMeetingAsync, CancelMeetingAsync, StartMeetingAsync, BeginMinutesEntryAsync, SubmitMinutesAsync, TryFinalizeMinutesAsync, AddAttendeeAsync, AddAttendeesFromCommitteeAsync, RemoveAttendeeAsync, UpdateRsvpAsync, UpdateConfirmationAsync, AddAgendaItemAsync, UpdateAgendaItemAsync, RemoveAgendaItemAsync, UpdateAgendaDiscussionNotesAsync, AddDecisionAsync, RemoveDecisionAsync, CreateActionItemAsync, StartActionItemAsync, CompleteActionItemAsync, VerifyActionItemAsync, GetActionItemsForUserAsync, GetAllActionItemsAsync, GetOverdueActionItemsAsync, CanUserScheduleMeetingAsync, IsUserModeratorAsync, IsUserAttendeeAsync, GetSchedulableCommitteesAsync, GetMeetingStatsAsync, GetUpcomingMeetingsAsync |

## Pages (Route Map)

| Page | Route | Handlers |
|------|-------|----------|
| Index | `/` | GET → redirect to Dashboard or Login |
| Login | `/Auth/Login` | GET, POST (generate magic link) |
| Verify | `/Auth/Verify?token=` | GET (verify & sign in) |
| Logout | `/Auth/Logout` | GET (sign out) |
| Dashboard | `/Admin/Dashboard` | GET (org + report stats) |
| Users Index | `/Admin/Users` | GET |
| Users Create | `/Admin/Users/Create` | GET, POST |
| Users Edit | `/Admin/Users/Edit?id=` | GET, POST |
| Users Details | `/Admin/Users/Details?id=` | GET |
| Users Delete | `/Admin/Users/Delete?id=` | GET, POST |
| Backup | `/Admin/Backup` | GET, POST:Create/Restore/Delete/WalCheckpoint, GET:Download |
| Org Tree | `/Admin/Organization` | GET |
| Committees Index | `/Admin/Organization/Committees` | GET (LevelFilter, SectorFilter) |
| Committee Create | `/Admin/Organization/Committees/Create` | GET, POST |
| Committee Edit | `/Admin/Organization/Committees/Edit?id=` | GET, POST |
| Committee Details | `/Admin/Organization/Committees/Details?id=` | GET, POST:AddMember/RemoveMember/ToggleRole |
| Committee Delete | `/Admin/Organization/Committees/Delete?id=` | GET, POST |
| Reports Index | `/Reports` | GET (CommitteeId, Status, ReportType, ShowMine, IncludeArchived) |
| Report Create | `/Reports/Create` | GET, POST (with file uploads) |
| Report Details | `/Reports/Details/{id}` | GET, POST:Submit/StartReview/RequestFeedback/Approve/Archive/RemoveAttachment |
| Report Edit | `/Reports/Edit/{id}?revise=` | GET, POST |
| Create Summary | `/Reports/CreateSummary?committeeId=` | GET, POST |
| Drill Down | `/Reports/DrillDown/{id}` | GET |
| Directives Index | `/Directives` | GET (Status, Priority, ShowMine, IncludeClosed) |
| Directive Create | `/Directives/Create?ReportId=` | GET, POST |
| Directive Details | `/Directives/Details/{id}` | GET, POST:Acknowledge/StartProgress/Implement/Verify/Close/Forward |
| Directive Track | `/Directives/Track` | GET (overdue + approaching deadline) |
| Meetings Index | `/Meetings` | GET (CommitteeId, Status, ShowMine, IncludePast) |
| Meeting Create | `/Meetings/Create` | GET, POST (with auto-invite committee members) |
| Meeting Details | `/Meetings/Details/{id}` | GET, POST:StartMeeting/BeginMinutes/CancelMeeting/AddAttendee/RemoveAttendee/AddCommitteeMembers/RsvpAccept/RsvpDecline/RsvpTentative/AddAgendaItem/RemoveAgendaItem/AddDecision/RemoveDecision/AddActionItem/ConfirmMinutes/RequestRevision/Abstain |
| Meeting Minutes | `/Meetings/Minutes/{id}` | GET, POST:Save/Submit (per-agenda-item notes + overall minutes) |
| Action Items | `/Meetings/ActionItems` | GET (Status, ShowMine), POST:Start/Complete/Verify |

## Authorization

- `/Admin/*`, `/Reports/*`, `/Directives/*`, and `/Meetings/*` → `[Authorize]` (any authenticated user)
- `/Admin/Backup/*` → `SystemAdminOnly` policy
- `/Auth/*` and `/` → `[AllowAnonymous]`
- Report actions: committee membership checks (submit), head-of-committee/parent checks (review)
- Directive issuing: Chairman/ChairmanOffice/SystemAdmin or committee heads
- Directive actions: target committee members can acknowledge/implement; issuer can verify/close
- Meeting scheduling: Chairman/ChairmanOffice/SystemAdmin or committee heads
- Meeting minutes: only moderator can enter/edit/submit minutes
- Minutes confirmation: only attendees can confirm/request revision/abstain
- Action item transitions: assignee can start/complete; moderator/head can verify

## Configuration

### appsettings.Development.json
```json
{ "DatabaseSettings": { "Provider": "SQLite", "ConnectionStrings": { "SQLite": "Data Source=db/reporting.db" } } }
```

### appsettings.json (Email — disabled by default)
```json
{ "EmailSettings": { "Enabled": false, "TenantId": "", "ClientId": "", "ClientSecret": "", "SenderUserId": "", "SenderEmail": "", "SenderName": "" } }
```

## Seeded Data (Phase 2)

OrganizationSeeder seeds the full ORS_Test_Data.md dataset:
- 1 System Admin (admin@org.edu), 1 Chairman, 4 Chairman's Office members (ranked 1-4)
- 5 L0 General Secretaries + 5 shadows
- 19 L1 Directorates across 5 sectors, ~65 L2 Functions, ~100 L3 Processes
- ~500+ memberships (heads + members), 5 shadow assignments, cross-committee memberships

## Development Patterns

- **File-scoped namespaces**: `namespace X;` (not `namespace X { }`)
- **Nullable reference types**: Enabled
- **Async/await**: All database operations
- **[BindProperty]**: For Razor Pages form binding, `[BindProperty(SupportsGet = true)]` for query params
- **TempData**: Flash messages (`TempData["SuccessMessage"]`, `TempData["ErrorMessage"]`)
- **Include()**: Eager loading for navigation properties
- **Int PKs**: `int` auto-increment primary keys (not GUIDs) — established convention
- **ModelState.Remove()**: For navigation properties in POST handlers (`"Report.Author"`, `"Report.Committee"`)
- **Self-referential FKs**: Committee.ParentCommitteeId, Report.OriginalReportId, Directive.ParentDirectiveId
- **DrillDownNode DTO**: Defined after ReportService class in same file, used for recursive tree rendering
- **DirectivePropagationNode DTO**: Defined after DirectiveService class in same file, used for propagation tree
- **AgendaItemNotes DTO**: Defined after MinutesModel page model class in same file, used for per-item discussion notes

## Git

- **Branch**: `claude/ors-development-continued-1LlZI`
- **Push command**: `git push -u origin claude/ors-development-continued-1LlZI`

---

## Session Handoff Prompt

Use this prompt when starting a new Claude Code session to resume work on this project:

```
You are continuing development of the ORS (Organizational Reporting System) — an ASP.NET Core 8.0 Razor Pages web app for hierarchical organizational reporting.

CRITICAL CONTEXT:
- Branch: claude/ors-development-continued-1LlZI
- This is Razor Pages (NOT MVC). Pages live in /Pages/, not /Controllers/.
- EF Core 8.0 with SQLite (dev). No migrations — uses EnsureCreatedAsync().
- IMPORTANT: After adding new models/DbSets, delete `ReportingSystem/db/reporting.db` before running — EnsureCreatedAsync() won't alter existing DBs.
- File-scoped namespaces: `namespace X;`
- Int auto-increment PKs (NOT GUIDs)
- All DB ops are async/await
- [BindProperty] for form binding, TempData for flash messages
- ModelState.Remove() for navigation properties in POST handlers

ENVIRONMENT SETUP (sandboxed — run if .NET SDK not installed):
  apt-get update -qq && apt-get install -y -qq dotnet-sdk-8.0
  echo "nameserver 8.8.8.8" > /etc/resolv.conf
  export no_proxy="${no_proxy:+$no_proxy,}api.nuget.org,nuget.org,www.nuget.org"
  dotnet build ReportingSystem/ReportingSystem.csproj

COMPLETED PHASES:
- Phase 1 (Infrastructure): Auth (magic link), backup, notifications, user CRUD
- Phase 2 (Organization): Committee hierarchy (L0-L4 tree), memberships, shadows, seeder with ~155 users
- Phase 3 (Report Lifecycle): Report CRUD, 8-status workflow (Draft→Submitted→UnderReview→Approved→Archived), attachments, versioning, status history
- Phase 4 (Summarization): ReportSourceLink model, summary creation linking to sources, recursive drill-down tree visualization, bidirectional chain navigation
- Phase 5 (Directives): Directive model (7 statuses, 5 types, 3 priorities), propagation chain via ParentDirectiveId, forwarding with annotations, overdue tracking, status pipeline view
- Phase 6 (Meetings): Meeting scheduling, structured agenda, RSVP, minutes entry with per-agenda-item notes, attendee confirmation workflow (all must Confirm/Abstain to finalize), decisions with DecisionType, action items (Assigned→InProgress→Completed→Verified), overdue action item tracking

CURRENT STATE:
- 18 model classes, 9 services, 28 page models, 34+ Razor views
- MeetingService.cs (~420 lines) handles: meeting lifecycle, agenda, RSVP, minutes, confirmation, decisions, action items, overdue tracking
- DirectiveService.cs (~340 lines) handles: directive CRUD, 7-status workflow, propagation tree, forwarding, overdue tracking, access control
- ReportService.cs (547 lines) handles: report CRUD, status transitions, file attachments, summary creation, recursive drill-down tree building, access control checks
- ApplicationDbContext.cs (~330 lines) with 18 DbSets and full relationship configuration
- OrganizationSeeder.cs (853 lines) seeds entire test dataset

NEXT: Phase 7 — Confidentiality & Access Control (hierarchy-based access, confidentiality marking, SRS 4.5)

Read claude.md for full roadmap, model definitions, service methods, and page routes.
```
