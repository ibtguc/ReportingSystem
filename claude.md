# ReportingSystem - HORS (Hierarchical Organizational Reporting System)

## Project Overview

ASP.NET Core 8.0 Razor Pages web application for hierarchical organizational reporting with bi-directional communication flows.

**Current Status**: Phase 1 Infrastructure Complete

## Tech Stack

- **Framework**: ASP.NET Core 8.0, Razor Pages (not MVC)
- **Database**: EF Core 8.0 with SQLite (dev) / SQL Server (prod)
- **Auth**: Cookie-based with magic link passwordless login
  - 15-minute token expiry
  - 30-day sliding session cookie
- **Email**: Microsoft Graph API (disabled by default in dev)
- **Frontend**: Bootstrap 5.3.3, Bootstrap Icons (CDN), jQuery + jQuery Validation
- **Data Protection**: Keys persisted to `/keys` folder

## Quick Start

```bash
# Build
dotnet build ReportingSystem/ReportingSystem.csproj

# Run (creates db/reporting.db automatically)
dotnet run --project ReportingSystem/ReportingSystem.csproj

# Dev URL
http://localhost:5296
```

## Project Structure

```
ReportingSystem/
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext with 4 DbSets
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
│   │   ├── Backup/Index.cshtml    # Backup management UI
│   │   ├── Users/                 # CRUD for users
│   │   └── Dashboard.cshtml       # Admin home (placeholder)
│   ├── Auth/                      # [AllowAnonymous]
│   │   ├── Login.cshtml           # Magic link request
│   │   ├── Verify.cshtml          # Token verification
│   │   └── Logout.cshtml          # Sign out
│   ├── Shared/
│   │   └── _Layout.cshtml         # Main layout with nav
│   ├── Index.cshtml               # Public landing page
│   └── Error.cshtml               # Error page
├── Services/
│   ├── MagicLinkService.cs        # Token generation/verification
│   ├── EmailService.cs            # Microsoft Graph email
│   ├── NotificationService.cs     # In-app notifications CRUD
│   ├── DatabaseBackupService.cs   # Backup create/restore/delete
│   └── DailyBackupHostedService.cs # Background backup scheduler
├── wwwroot/
│   ├── css/site.css
│   ├── js/site.js
│   └── lib/                       # Bootstrap, jQuery (bundled)
├── Program.cs                     # App configuration & DI
├── appsettings.json               # Production config
└── appsettings.Development.json   # Dev config (SQLite)
```

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
    public string UserId { get; set; }          // String (not FK to User)
    public NotificationType Type { get; set; }  // Enum
    public string Title { get; set; }
    public string Message { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public NotificationPriority Priority { get; set; }
    public int? RelatedEntityId { get; set; }
}

public enum NotificationType
{
    ReportSubmitted, ReportApproved, ReportRejected,
    FeedbackReceived, DecisionMade, RecommendationIssued,
    ConfirmationRequested, DeadlineApproaching, General
}

public enum NotificationPriority
{
    Low, Normal, High, Urgent
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
    public BackupType Type { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsAutomaticDailyBackup { get; set; }
}

public enum BackupType { Manual, AutomaticDaily, PreRestore }
```

## Services

| Service | Purpose |
|---------|---------|
| `MagicLinkService` | Generate 32-byte tokens, verify & consume, cleanup expired |
| `EmailService` | Send via Microsoft Graph API (configurable, disabled by default) |
| `NotificationService` | Create, list, mark read, cleanup old notifications |
| `DatabaseBackupService` | Create manual/auto backups, restore, WAL checkpoint |
| `DailyBackupHostedService` | Background service, checks hourly, creates backup every 12h |

## Key Features

### Authentication Flow
1. User enters email on `/Auth/Login`
2. `MagicLinkService` generates token, creates `MagicLink` record
3. In dev mode: link displayed on page
4. In prod: `EmailService` sends email with link
5. User clicks `/Auth/Verify?token=xxx`
6. `MagicLinkService.VerifyMagicLinkAsync` validates token
7. Cookie issued via `HttpContext.SignInAsync`

### Database Backup System
- **Automatic**: Every 12 hours via `DailyBackupHostedService`
- **Pre-modification**: `AutomaticBackupFilter` triggers on POST/PUT/DELETE
- **Manual**: Admin can create named backups
- **Pre-restore**: Automatic backup before any restore
- **WAL checkpoint**: Force SQLite WAL flush
- **Storage**: `db/Backups/` folder

### Authorization
- `/Admin/*` requires authentication (cookie)
- `/Auth/*` allows anonymous
- `/Index` and `/Error` allow anonymous
- Single role: "Administrator" (future: multi-role RBAC)

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

### appsettings.json (Email - disabled by default)
```json
{
  "EmailSettings": {
    "Enabled": false,
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "SenderUserId": "",
    "SenderEmail": "",
    "SenderName": ""
  }
}
```

## Seeded Data

| Email | Name | Role |
|-------|------|------|
| admin@reporting.com | System Administrator | Administrator |
| admin1@reporting.com | Administrator One | Administrator |
| admin2@reporting.com | Administrator Two | Administrator |

## Development Patterns

- **File-scoped namespaces**: `namespace X;` (not `namespace X { }`)
- **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`)
- **Async/await**: All database operations
- **[BindProperty]**: For Razor Pages form binding
- **TempData**: Flash messages (`TempData["SuccessMessage"]`, `TempData["ErrorMessage"]`)
- **Include()**: Eager loading for navigation properties

## Future Phases (TODO)

### Phase 2: Organization & User Hierarchy
- `OrganizationalUnit` model with self-referential hierarchy
- Extend `User` with org unit FK and expanded roles
- `Delegation` model for temporary authority transfer
- Org tree visualization

### Phase 3: Report Templates & Entry
- `ReportTemplate`, `ReportField`, `ReportPeriod` models
- `Report`, `ReportFieldValue`, `Attachment` models
- Dynamic form generation from templates
- Draft auto-save, bulk import

### Phase 4: Upward Flow
- `SuggestedAction`, `ResourceRequest`, `SupportRequest` models
- Entry forms, status tracking

### Phase 5: Workflow & Tagging
- Approval workflow (Draft → Submitted → Under Review → Approved/Rejected)
- `ConfirmationTag` for section verification
- `Comment` with @mentions
- Deadline enforcement

### Phase 6: Downward Flow
- `Feedback`, `Recommendation`, `Decision` models
- Linked to originating requests

### Phase 7: Aggregation & Drill-Down
- Aggregation rules per field
- Drill-down navigation
- `AuditLog` for data lineage

### Phase 8: Dashboards & Export
- Role-based KPI dashboards
- Chart visualizations
- PDF/Word/Excel export
- Ad-hoc report builder

### Phase 9: Notifications & Polish
- Enhanced notifications with preferences
- Responsive design polish

## Reference

Infrastructure based on `/ref-only-example/SchedulingSystem/` with namespace changes. Domain-specific scheduling code was not replicated.
