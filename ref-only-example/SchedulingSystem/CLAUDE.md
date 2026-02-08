# CLAUDE.md - AI Assistant Guidelines

This document provides essential context for AI assistants working with the SchedulingSystem codebase.

## Project Overview

**SchedulingSystem** is a comprehensive timetabling application for educational institutions built with ASP.NET Core 8.0 Razor Pages. It provides automated schedule generation, lesson movement with conflict resolution, and real-time debugging capabilities for scheduling algorithms.

## Technology Stack

- **Framework**: ASP.NET Core 8.0 with Razor Pages
- **Language**: C# with nullable reference types enabled
- **Database**: SQLite (development) / SQL Server (production) via Entity Framework Core 8.0
- **Real-time**: SignalR for live updates
- **Authentication**: Cookie-based with magic link email login
- **Email**: Microsoft Graph API for email notifications

## Project Structure

```
SchedulingSystem/
├── Data/                           # Database context and seeding
│   ├── ApplicationDbContext.cs     # Main EF Core DbContext
│   ├── SeedData.cs                 # Initial data seeding
│   └── UserSeeder.cs               # Admin user seeding
├── Filters/                        # MVC action filters
│   └── AutomaticBackupFilter.cs    # Automatic daily backup filter
├── Hubs/                           # SignalR hubs for real-time updates
│   ├── TimetableGenerationHub.cs   # Timetable generation progress
│   └── DebugRecursiveHub.cs        # Algorithm debugging events
├── Models/                         # Domain entities
│   ├── Teacher.cs, Class.cs, Subject.cs, Room.cs, Period.cs
│   ├── Lesson.cs                   # Template lesson definition
│   ├── ScheduledLesson.cs          # Lesson placed in time slot
│   ├── Timetable.cs                # Main timetable container
│   ├── LessonTeacher.cs, LessonClass.cs, LessonSubject.cs  # Many-to-many junctions
│   ├── DatabaseBackup.cs           # Backup metadata entity
│   ├── User.cs                     # System user entity
│   └── [Various availability and substitution models]
├── Pages/                          # Razor Pages
│   ├── Admin/                      # Admin-only pages (requires auth)
│   │   ├── Dashboard.cshtml        # Navigation hub (home page after login)
│   │   ├── Backup/                 # Database backup management
│   │   ├── Timetables/             # Timetable management & algorithms
│   │   ├── Teachers/               # Full CRUD + ManageAvailability + ManageQualifications
│   │   ├── Classes/                # Full CRUD + ManageAvailability
│   │   ├── Subjects/               # Full CRUD + ManageAvailability
│   │   ├── Rooms/                  # Full CRUD + ManageAvailability
│   │   ├── Lessons/                # Dashboard (matrix view) + Edit page
│   │   ├── Departments/            # Full CRUD
│   │   ├── Users/                  # User management CRUD
│   │   ├── Import/                 # Data import (UNTIS, CSV)
│   │   └── Substitutions/          # Teacher substitution management
│   ├── Auth/                       # Login, Logout, Verify pages
│   ├── Teachers/                   # Teacher-specific views
│   └── Shared/                     # Layout and partial views
├── Services/                       # Business logic layer
│   ├── Constraints/                # Constraint validation system
│   │   ├── IConstraintValidator.cs # Constraint validator interface
│   │   ├── ConstraintValidatorService.cs  # Implementation
│   │   ├── ConstraintDefinitions.cs       # HC/SC definitions
│   │   └── ValidationModels.cs     # Validation result types
│   ├── LessonMovement/             # Scheduling algorithms
│   │   ├── RecursiveConflictResolutionAlgorithm.cs  # Main CSP solver
│   │   ├── KempeChainTabuSearch.cs                  # Kempe chain
│   │   ├── MusicalChairsAlgorithm.cs                # Sequential displacement
│   │   ├── SwapChainSolver.cs                       # Simple swaps
│   │   ├── AvailableSlotFinder.cs                   # Slot finder
│   │   └── LessonMovementService.cs                 # Orchestrator
│   ├── DatabaseBackupService.cs    # Backup creation, restore, management
│   ├── MagicLinkService.cs         # Magic link token generation/validation
│   ├── SchedulingService*.cs       # Various scheduling implementations
│   ├── SubstitutionService.cs      # Teacher substitution logic
│   ├── UntisImportService.cs       # UNTIS data import
│   └── [Other services]
├── wwwroot/                        # Static files (CSS, JS, libraries)
├── UNTIS_Export/                   # Sample UNTIS export files
├── Program.cs                      # Application entry point and DI config
├── appsettings.json                # Production configuration
└── appsettings.Development.json    # Development configuration
```

## Key Domain Concepts

### Entities

| Entity | Description |
|--------|-------------|
| `Timetable` | Container for a school schedule (per year/term) |
| `Lesson` | Template definition with teachers, classes, subjects |
| `ScheduledLesson` | Lesson placed at a specific day/period/room |
| `Teacher`, `Class`, `Subject`, `Room`, `Period` | Core resources |
| `LessonTeacher`, `LessonClass`, `LessonSubject` | Many-to-many junctions |
| `TeacherAvailability`, `ClassAvailability`, etc. | Time slot preferences |

### Constraint System

The scheduling system uses a comprehensive constraint validation framework:

**Hard Constraints (HC-1 to HC-12)** - Must be satisfied:
- `HC-1`: Teacher double-booking prevention
- `HC-2`: Class double-booking prevention
- `HC-3`: Room double-booking prevention
- `HC-4` to `HC-7`: Absolute unavailability (importance = -3)
- `HC-8` to `HC-12`: Max consecutive periods, locked lessons, workload limits

**Soft Constraints (SC-1 to SC-11)** - Should be satisfied (preferences):
- `SC-1` to `SC-4`: Time preferences for teachers/classes/subjects/rooms
- `SC-5`, `SC-6`: Lunch break requirements
- `SC-7`: No gaps in class schedule
- `SC-8`, `SC-9`: Room type matching
- `SC-10`, `SC-11`: Minimum periods per day

**Special Exemptions:**
- `"xy"` - Intern/placeholder teacher (exempt from teacher constraints)
- `"v-res"` - Reserve class (exempt from class double-booking)
- `"Team"` - Team class (exempt from double-booking)
- `"Teamraum"` - Team room (exempt from room double-booking)

### Availability Importance Scale (UNTIS)

```
-3: Must NOT schedule (hard constraint)
-2: Strongly prefer NOT to schedule
-1: Mildly prefer NOT to schedule
 0: Neutral
+1: Mildly prefer to schedule
+2: Strongly prefer to schedule
+3: MUST schedule (hard constraint)
```

## Development Commands

```bash
# Run the application
dotnet run

# Build
dotnet build

# Run with watch (hot reload)
dotnet watch run

# Create EF migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Reset database (delete and recreate)
rm scheduling.db && dotnet run
```

## Configuration

### appsettings.json Structure

```json
{
  "DatabaseSettings": {
    "Provider": "SQLite",  // or "SqlServer"
    "ConnectionStrings": {
      "SQLite": "Data Source=scheduling.db",
      "SqlServer": "Server=...;Database=...;..."
    }
  },
  "DatabaseValidation": {
    "Enabled": true,
    "FailOnMismatch": true
  },
  "EmailSettings": {
    "Enabled": true,
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "SenderEmail": "..."
  }
}
```

## Code Conventions

### C# Style

- Use `nullable` reference types (project-wide enabled)
- Prefer `init` properties for immutable state
- Use file-scoped namespaces
- Follow async/await patterns for database operations
- Inject services via constructor DI

### Razor Pages Patterns

- Page models use `OnGetAsync()` / `OnPostAsync()` handlers
- Use `[BindProperty]` for form binding
- Tag helpers preferred over HTML helpers
- Keep page models thin; delegate to services

### Database Patterns

- Use `ApplicationDbContext` for all database access
- Eager load related entities with `.Include()`
- Many-to-many relationships use explicit junction tables (not EF implicit)
- Cascade delete configured in `OnModelCreating()`

### Service Registration

All services are registered in `Program.cs`:
- Use `AddScoped<>()` for database-dependent services
- Use `AddSingleton<>()` for configuration objects
- SignalR hubs registered with `MapHub<>()`

## Scheduling Algorithms

### RecursiveConflictResolutionAlgorithm

The main scheduling algorithm using CSP with backtracking:

**Key Features:**
- Immutable state pattern with `RecursionState`
- Cycle prevention via visited lesson tracking
- Conflict prioritization (fewest conflicts first)
- Most Constrained Variable (MCV) heuristic
- Memoization for valid destinations
- Time limit enforcement via Stopwatch

**State Object:**
```csharp
public class RecursionState {
    public Dictionary<int, TimeSlot> ProposedMoves { get; init; }
    public HashSet<int> VisitedLessons { get; init; }
    public Dictionary<string, HashSet<int>> OccupiedSlots { get; init; }
    public int CurrentDepth { get; init; }
    public List<string> IgnoredConstraints { get; init; }
}
```

### Other Algorithms

- **Kempe Chain Tabu Search**: Graph-based resolution using color exchange
- **Musical Chairs**: Sequential displacement algorithm
- **Swap Chain Solver**: Simple direct swaps
- **Simulated Annealing**: Probabilistic optimization

## SignalR Hubs

### TimetableGenerationHub (`/hubs/timetableGeneration`)
- Progress updates during timetable generation
- Events: `UpdateProgress`, `GenerationComplete`, `GenerationError`

### DebugRecursiveHub (`/hubs/debugRecursive`)
- Real-time algorithm debugging visualization
- Events: `UpdateStats`, `AddNode`, `Joined`

## Authentication Flow

1. User enters email on `/Auth/Login`
2. System generates magic link token via `MagicLinkService`
3. Email sent via Microsoft Graph API (if enabled)
4. In development, link displayed directly on page
5. User clicks link to `/Auth/Verify?token=...`
6. Cookie set, redirected to `/Admin/Dashboard`

## Backup System

The backup system (`/Admin/Backup`) provides database backup management:

### Features
- **Create Backup**: Manual backup with name/description, optional auto-download
- **Automatic Backups**: Triggered twice daily (every 12 hours) before data modifications
- **Download**: Download backup files
- **Restore**: Restore database from backup (creates pre-restore backup first, preserves all backup records)
- **Delete**: Remove backup records and files
- **Database Maintenance**: Force WAL checkpoint and cleanup

### Backup Types
- `Manual`: User-created backups
- `AutomaticDaily`: System-created backups (twice daily, every 12 hours)
- `PreRestore`: Created automatically before restore operations

### Automatic Backup Mechanisms
Two mechanisms ensure regular backups:
1. **DailyBackupHostedService** (Background Service): Runs on app startup and checks hourly
2. **AutomaticBackupFilter** (Action Filter): Creates backup before POST/PUT/DELETE if none exists in last 12 hours

### Backup Restore Process
When restoring a backup:
1. Creates a pre-restore backup of current state
2. Saves all backup records in memory
3. Closes database connections and clears connection pools
4. Deletes WAL/SHM files (contain stale data)
5. Copies backup file to replace database
6. Re-inserts all backup records into restored database (preserves history)

### Database Maintenance (WAL Cleanup)
The "Force Write & Clean Up" button:
1. Executes WAL checkpoint (writes all changes to main DB)
2. Switches to DELETE journal mode (removes WAL/SHM files)
3. Database stays in DELETE mode until app restart
4. DELETE mode works fine, just slightly less concurrent write performance

### AJAX Create Backup
The create backup form uses AJAX (`OnPostCreateAjaxAsync`) to:
1. Create the backup
2. Trigger download if checkbox checked
3. Refresh page to show new backup in grid

## User Management

The user management system (`/Admin/Users`) provides:
- Full CRUD operations for system users
- Email-based identification (used for magic link login)
- Role assignment (Admin, Teacher, etc.)
- Active/Inactive status management

## Common Patterns

### Loading a ScheduledLesson with Relations

```csharp
var lessons = await _context.ScheduledLessons
    .Include(sl => sl.Lesson)
        .ThenInclude(l => l.LessonTeachers)
            .ThenInclude(lt => lt.Teacher)
    .Include(sl => sl.Lesson)
        .ThenInclude(l => l.LessonClasses)
            .ThenInclude(lc => lc.Class)
    .Include(sl => sl.Lesson)
        .ThenInclude(l => l.LessonSubjects)
            .ThenInclude(ls => ls.Subject)
    .Include(sl => sl.Period)
    .Include(sl => sl.Room)
    .Where(sl => sl.TimetableId == timetableId)
    .ToListAsync();
```

### Constraint Validation

```csharp
var result = await _constraintValidator.ValidateHardConstraintsAsync(
    scheduledLesson,
    existingSchedule,
    new ValidationContext { IgnoredConstraints = new List<string> { "HC-3" } }
);

if (!result.IsValid)
{
    foreach (var violation in result.Violations)
    {
        // Handle violation
    }
}
```

### Clone Pattern for Immutable State

```csharp
var newState = state with
{
    ProposedMoves = new Dictionary<int, TimeSlot>(state.ProposedMoves),
    VisitedLessons = new HashSet<int>(state.VisitedLessons),
    CurrentDepth = state.CurrentDepth + 1
};
```

## Admin Pages UI Patterns

### Entity Management Pages (Teachers, Classes, Subjects, Rooms)

Each entity has a consistent set of pages:

| Page | Purpose |
|------|---------|
| `Index.cshtml` | List view with action buttons (Availability, Details, Edit, Delete) |
| `Create.cshtml` | Form to add new entity |
| `Edit.cshtml` | Form to modify entity + link to ManageAvailability |
| `Details.cshtml` | Read-only view with Time Constraints section |
| `Delete.cshtml` | Confirmation page with cascade warnings |
| `ManageAvailability.cshtml` | Weekly grid for availability preferences |

### Index Page Action Buttons

All Index pages have a consistent button group for each row:
```html
<div class="btn-group btn-group-sm">
    <a asp-page="ManageAvailability" class="btn btn-outline-secondary" title="Manage Availability">
        <i class="bi bi-calendar-week"></i>
    </a>
    <a asp-page="Details" class="btn btn-outline-info" title="View Details">
        <i class="bi bi-eye"></i>
    </a>
    <a asp-page="Edit" class="btn btn-outline-primary" title="Edit">
        <i class="bi bi-pencil"></i>
    </a>
    <a asp-page="Delete" class="btn btn-outline-danger" title="Delete">
        <i class="bi bi-trash"></i>
    </a>
</div>
```

### Details Page Time Constraints Section

All Details pages show:
- Header with "Manage Constraints" button linking to ManageAvailability
- Info message when no constraints exist
- Table of constraints when they exist
- Consistent badge styling for importance levels

### Lessons Dashboard (`/Admin/Lessons/Dashboard`)

Features:
- **Lesson Matrix**: Subject × Class grid (initially hidden, toggle with button)
- **Filter Panel**: Filter by Class, Subject, or Teacher
- **Sortable Lessons Table**: Click column headers to sort (ID, Class, Subject, Teacher, Freq/Week, Duration)
- **Search**: Real-time search across lessons
- **CRUD Actions**: Edit and Delete buttons per row

### Lessons Edit Page (`/Admin/Lessons/Edit`)

Layout:
- **Row 1**: Lesson Properties (full width) - Duration, Frequency, Room Type, Description, Requirements, Active
- **Row 2**: Three columns - Classes, Teachers, Subjects (each with add/remove functionality)

### Timetable Views (MoveLessons, Edit)

**Conflict Highlighting:**
- Double-booking conflicts get `.has-conflict` class (red background)
- Availability conflicts get `.has-availability-conflict` class (bright green border)
- Related conflicts highlighted on hover with `.conflict-related`
- Non-conflicting lessons dimmed with `.conflict-dimmed`
- Icons in conflict cards have white background for visibility

**Availability Conflict Detection:**
- Checks teacher, class, subject, and room unavailability (Importance = -3)
- Displays green border (4px solid #00ff00) around affected lessons
- Shows indicator at bottom of card: "⚠️ Teacher, Class unavailable" etc.
- Entity IDs passed via data attributes: `data-teacher-ids`, `data-class-ids`, etc.
- Unavailability data loaded from database and serialized to JavaScript

**Lesson Card Badges:**
- Co-teaching: `<i class="bi bi-people-fill"></i>`
- Multi-subject: `<i class="bi bi-book-half"></i>`
- Multi-room: `<i class="bi bi-door-open-fill"></i>`
- Multi-class: `<i class="bi bi-people"></i>`
- Locked: `<i class="bi bi-lock-fill"></i>` (warning badge)

**Selection Mode:**
- Selected lesson has blue border/shadow with `.selected` class
- Click lesson card to select for movement

## Testing & Debugging

### Debug Pages

- `/Admin/Timetables/LessonMoveResult` - Lesson movement results with solution options
- `/Admin/Timetables/DebugLessonMoveLive` - SignalR live debugging
- `/Admin/Timetables/DebugRecursiveConflict` - Legacy tree visualization

### Lesson Move Result Features

The LessonMoveResult page (`/Admin/Timetables/LessonMoveResult`) provides comprehensive results for lesson movement:

**Solution Options:**
- Displays multiple solution options found by the algorithm
- Each solution shows movements with From/To slots
- **Save As Draft** button to save a solution as a new draft timetable
- **Evaluate Ignored Constraints** button to check constraint violations

**Substitution Mode:**
- Enable via checkbox in MoveLessons confirmation dialog
- Restricts movement to slots containing "substitution" subject lessons
- Allows substitution lessons to swap with selected lesson for coverage
- Only allows substitution lesson to leave if another remains in the slot (2+ per slot)
- Supports removal solutions where L1 is unscheduled and a substitution lesson covers

**TimeSlot Format:**
- `TimeSlot.ToString()` returns `"Day-P#"` format (e.g., `"Sunday-P1"`)
- Alternative formats: `"Sunday, Period 1"` or `"Sunday - Period 1"`

### Common Issues

**No Solutions Found:**
- Check if constraints are too restrictive
- Increase `maxTimeMinutes` parameter
- Verify lessons are not locked (`HC-10`)
- Review available time slots

**Slow Performance:**
- Reduce `maxDepth`
- Enable more ignored constraints
- Use conflict prioritization

**Database Schema Mismatch:**
- Delete `scheduling.db` to recreate
- Or set `DatabaseValidation:Enabled = false`

## Important Files for AI Assistants

When modifying the codebase, pay attention to:

1. **Program.cs** - Service registration, middleware, hub mapping
2. **ApplicationDbContext.cs** - Entity relationships, indexes
3. **ConstraintDefinitions.cs** - Constraint codes and special cases
4. **RecursiveConflictResolutionAlgorithm.cs** - Main scheduling logic
5. **ConstraintValidatorService.cs** - Constraint implementation

## File Naming Conventions

- Razor Pages: `{Name}.cshtml` + `{Name}.cshtml.cs`
- Services: `{Name}Service.cs`
- Models: Singular noun (e.g., `Teacher.cs`, not `Teachers.cs`)
- Junction tables: `{Entity1}{Entity2}.cs` (e.g., `LessonTeacher.cs`)

## Don't Forget

- The `_Trash/` folder contains archived documentation - do not use as reference
- Database files (`*.db`) are gitignored
- Magic links expire after 15 minutes
- Automatic backups trigger twice daily (every 12 hours) via `AutomaticBackupFilter` and `DailyBackupHostedService`
- Admin pages require authentication (`/Admin/*`)
- Auth pages allow anonymous access (`/Auth/*`)
- Substitution lessons are identified by having "substitution" as subject name
- Reserve class (`v-res`) is exempt from class double-booking constraints
- When using AJAX POST handlers, always include `@Html.AntiForgeryToken()` in the view
- TimeSlot format is `"Day-P#"` (e.g., `"Sunday-P1"`) - parse accordingly
- The default ignored constraint for MoveLessons is HC-4 unchecked (teacher availability enforced)
- Data Protection keys stored in `/keys` folder - users stay logged in after deployments
- WAL/SHM cleanup switches to DELETE journal mode (returns to WAL on app restart)
- Backup restore preserves all backup records by re-inserting them after database replacement

## Recent Changes

### LessonMoveResult Page (formerly DebugLessonMove)
- Renamed from DebugLessonMove to LessonMoveResult
- Improved input parameters section showing timetable name and selected lessons table
- Ignored constraints displayed as text list instead of badges
- Removed debug execution tree section

### Availability Conflict Detection
- Added to MoveLessons and Edit timetable pages
- Detects absolute unavailability (Importance = -3) for teachers, classes, subjects, rooms
- Displays bright green border around affected lesson cards
- Shows indicator at bottom of card specifying which entity type is unavailable

### Print Page Improvements (`/Admin/Timetables/Print`)
- Increased font sizes for better readability:
  - Table headers: 16px
  - Period cells: 14px
  - Lesson cards: 13px (subject code: 15px, details: 12px)
- Increased cell padding to 8px
- Manual print scale control (50%-150%) for both single and batch printing
- Scale dropdown uses CSS transform to resize print content

### Timetable Edit Page
- "Edit lesson" button renamed to "Change Time Slot"
- Modal title updated to "Change Time Slot" with calendar icon
- Room selection changed from single dropdown to checkbox list (multiple rooms)
- Room assignments use `ScheduledLessonRooms` table (not legacy `RoomId` field)

### Lessons Edit Page Fix
- Fixed class add button not working (incorrect element ID generation)
- Added `getListId()` helper to handle "class" → "Classes" pluralization

### Substitution Planning Mode
- Added to MoveLessons/LessonMoveResult pages
- Checkbox to enable substitution-only slot movement
- Supports removal solutions where a substitution lesson covers

### Save As Draft Feature
- LessonMoveResult solutions can be saved as new draft timetables
- Clones base timetable and applies selected solution movements
- Handles removal movements (lessons marked as REMOVED)

### Backup System Enhancements
- AJAX-based backup creation with optional download
- Page auto-refresh after backup creation

### UI Improvements
- White background for icons in conflict-highlighted cards
- Improved visibility of badges on red background

### Timetable Copy with Lesson Isolation (January 2026)
- **Copy button** added to Admin/Timetables for each timetable
- **Rename button** added to Admin/Timetables for each timetable
- When copying a timetable:
  1. Creates new timetable with Draft status
  2. Copies all ScheduledLessons (pointing to old lesson IDs initially)
  3. Loops through each new ScheduledLesson:
     - Loads the original Lesson with all associations
     - Creates a replica of the Lesson (including LessonTeachers, LessonClasses, LessonSubjects)
     - Updates the ScheduledLesson to point to the new Lesson ID
  4. This ensures complete isolation - editing lessons in copied timetable won't affect original
- Modal dialogs prompt for new name before copy/rename operations

### Lessons Dashboard Enhancements (January 2026)
- **Timetables column**: Shows which timetable(s) each lesson is scheduled in
  - Color-coded badges: green (Published), yellow (Draft), gray (Archived)
  - Clickable links to timetable Edit page
  - Shows "Not scheduled" for lessons not in any timetable
- **Multi-subject display**: Shows all subjects sorted alphabetically (not just first)
- **Filter improvements**: Filters now match ANY subject/class/teacher when lesson has multiple
  - Added `data-subject-ids` attribute to rows
  - Updated `applyLessonFilters()` and `filterLessonsByCell()` to use `.includes()` checks

### Schedule Lesson Dialog Enhancement (January 2026)
- Lesson dropdown in Admin/Timetables/Edit now shows:
  - All teachers concatenated by comma (not just first teacher)
  - Timetables where lesson is already scheduled (after "|" separator)
  - Example: `7A - Math (Smith, Jones) [2/4 scheduled] | In: Main Timetable`

### Database Maintenance (January 2026)
- Added to Admin/Backup page
- Shows database file sizes (main DB, WAL, SHM files)
- "Force Write & Clean Up" button to checkpoint WAL and delete WAL/SHM files

### Session Configuration
- Cookie authentication timeout: **30 days** of inactivity (`Program.cs` line 110)
- Sliding expiration enabled - each request resets the timer

### Break Supervision Module (January 2026)
Implemented break supervision duty management from GPU009.TXT import.

**Model:**
- `BreakSupervisionDuty` - links teachers to rooms (supervision locations) at specific days/periods
- Supervision locations are stored in the existing `Room` table (e.g., "Hof1", "Hof2", "oben", "unten")
- `TeacherId` is nullable (for unassigned slots)

**Database Table:**
```sql
CREATE TABLE BreakSupervisionDuties (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId INTEGER NOT NULL,        -- GPU009 Col 1: Corridor (Room)
    TeacherId INTEGER NULL,         -- GPU009 Col 2: Teacher
    DayOfWeek INTEGER NOT NULL,     -- GPU009 Col 3: Day Number (.NET DayOfWeek enum, 1=Monday)
    PeriodNumber INTEGER NOT NULL,  -- GPU009 Col 4: Period Number
    Points INTEGER NOT NULL DEFAULT 30, -- GPU009 Col 5: Points
    Notes TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1
);
```

**GPU009.TXT Format (UNTIS Export):**
```
Column 1: Corridor (Room/Location)
Column 2: Teacher name (can be empty for unassigned)
Column 3: Day Number (1=Monday, 2=Tuesday, etc.)
Column 4: Period Number
Column 5: Points

Example:
"Hof1";"Smith";1;3;30;
```

**Pages:**
- `/Admin/BreakSupervision/Index` - Matrix view (Location × Day × Period), inline teacher assignment
- Import via `/Admin/Import/Untis` - GPU009 section with import/clear buttons

**Key Files:**
- `Models/BreakSupervisionDuty.cs` - Entity model
- `Data/ApplicationDbContext.cs` - DbSet and configuration
- `Services/UntisImportService.cs` - `ImportBreakSupervisionAsync()`, `ClearBreakSupervisionDataAsync()`
- `Pages/Admin/BreakSupervision/Index.cshtml(.cs)` - Management UI
- `Pages/Admin/Import/Untis.cshtml(.cs)` - Import handlers

**View Integration (Completed):**
- Break supervision rows displayed in `/Admin/Timetables/Edit` and `/Admin/Timetables/Print`
- Single view mode: Shows all supervision duties with Location:Teacher badges
- Batch print modes (all-teachers, all-subjects, all-classes): Shows supervision duties integrated into each timetable
  - All-teachers: Shows only that teacher's supervision locations
  - All-subjects/All-classes: Shows all supervision duties for context
- White styling with Location:Teacher badges (gray for unassigned)
- Rows labeled as "Break 1", "Break 2" etc. (not "P3", "P5")
- Print page has "Show break supervision" checkbox to toggle visibility
- Teacher filter highlights matching teacher's supervision duties in green

**Substitution System Integration (Completed):**
- `/Admin/Substitutions/Daily` page now shows uncovered break supervision duties when a teacher is absent
- Summary card shows count of uncovered supervision duties
- Dedicated "Uncovered Break Supervision Duties" section lists:
  - Absent teacher name
  - Period number
  - Location (room number)
  - Absence type
- Link to Break Supervision Management page for assigning coverage

## Project Status Overview (January 2026)

### System Capabilities Summary

The SchedulingSystem is a **production-ready** timetabling application with the following major modules:

| Module | Files | Description |
|--------|-------|-------------|
| **Timetable Core** | `Pages/Admin/Timetables/*` | Create, edit, copy, delete timetables; schedule lessons |
| **Lesson Management** | `Pages/Admin/Lessons/*` | Dashboard, edit page with multi-teacher/class/subject support |
| **Entity CRUD** | `Pages/Admin/Teachers,Classes,Subjects,Rooms/*` | Full CRUD + availability management |
| **Break Supervision** | `Pages/Admin/BreakSupervision/*` | Supervision duty management and display |
| **Substitutions** | `Pages/Admin/Substitutions/*`, `Pages/Admin/Absences/*` | Absence tracking, substitute assignment |
| **Import** | `Pages/Admin/Import/*` | UNTIS data import (teachers, classes, subjects, rooms, lessons, availability, supervision) |
| **Backup** | `Pages/Admin/Backup/*` | Manual and automatic backups, restore, WAL cleanup |
| **Authentication** | `Pages/Auth/*` | Magic link login, 30-day session persistence |

### Key Architectural Patterns

1. **Multi-Value Relationships**: Lessons support multiple teachers, classes, subjects, and rooms via junction tables (`LessonTeacher`, `LessonClass`, `LessonSubject`, `ScheduledLessonRoom`)

2. **Timetable Isolation**: When copying a timetable, all lessons are duplicated to prevent cross-timetable side effects

3. **Constraint System**: Two-tier constraint validation:
   - Hard Constraints (HC-1 to HC-12): Must be satisfied
   - Soft Constraints (SC-1 to SC-11): Preferences, can be ignored

4. **Room Assignment**: Uses `ScheduledLessonRooms` table (not legacy `RoomId` field) for multi-room support

5. **Break Supervision**: Separate from lessons, stored in `BreakSupervisionDuties` table with temporary substitutions in `BreakSupervisionSubstitutions`

### Database Schema Key Tables

```
Timetables (1) ─────< ScheduledLessons (M)
                           │
                           ├──> Lessons ──< LessonTeachers ──> Teachers
                           │           ├──< LessonClasses ──> Classes
                           │           └──< LessonSubjects ──> Subjects
                           │
                           └──< ScheduledLessonRooms ──> Rooms

BreakSupervisionDuties ──> Teachers, Rooms
BreakSupervisionSubstitutions ──> Absences, BreakSupervisionDuties, Teachers

Absences ──> Teachers
         └──< Substitutions ──> ScheduledLessons, Teachers
```

## Session Notes for Next AI Assistant

### Current State (January 2026)
The system is functional with the following key architectural decisions:

1. **Lesson Isolation on Timetable Copy**: When copying a timetable, ALL lessons used in that timetable are duplicated. This prevents unintended side effects when editing lessons in one timetable affecting others. The copy logic is in `Pages/Admin/Timetables/Index.cshtml.cs` → `OnPostCopyAsync()`.

2. **Multi-value Entities**: Lessons can have multiple teachers, classes, and subjects. The UI and filters have been updated to handle this:
   - Dashboard shows all subjects alphabetically
   - Filters match ANY value (not just first)
   - Data attributes store comma-separated IDs: `data-teacher-ids`, `data-class-ids`, `data-subject-ids`

3. **Timetable Visibility**: Both the Lessons Dashboard and the Schedule Lesson dialog show which timetables each lesson appears in. This helps users understand the impact of editing shared lessons.

4. **Filter Persistence**: Filters on major pages persist across navigation using `wwwroot/js/filter-state.js`:
   - Saves to both URL query params and sessionStorage
   - Survives page refreshes and navigation to Edit pages
   - Known filter keys: `teacher`, `subject`, `class`, `room`, `hideNonMatching`
   - Pages with filter persistence: Lessons/Dashboard, Timetables/Edit, Timetables/MoveLessons, Timetables/Compare

5. **Automatic Backups (Twice Daily)**: Two mechanisms ensure backups every 12 hours:
   - `DailyBackupHostedService` (Background Service): Runs on app startup and checks hourly
   - `AutomaticBackupFilter` (Action Filter): Creates backup before POST/PUT/DELETE if none exists in last 12 hours

6. **Break Supervision**: Imported from UNTIS GPU009.TXT with correct column mapping:
   - Column 1: Corridor (Room) → RoomId
   - Column 2: Teacher → TeacherId
   - Column 3: Day Number → DayOfWeek
   - Column 4: Period Number → PeriodNumber
   - Column 5: Points → Points

### Known Limitations / Future Improvements
- **Lesson versioning**: Currently lessons are either shared or fully duplicated. A more sophisticated versioning system could allow selective sharing.
- **Break Supervision Coverage Assignment**: Currently the substitution board shows uncovered break supervision duties but there's no direct mechanism to assign a substitute teacher for supervision. The user must manually update the supervision assignment in the Break Supervision Management page.

### Recent Changes (This Session - January 2026)

**Break Supervision Display Overhaul:**
- Changed period labels from "P3"/"P5" to "Break 1"/"Break 2" format
- Added `GetBreakLabel(int periodNumber)` helper method to all relevant pages:
  - `Pages/Admin/Timetables/Print.cshtml.cs`
  - `Pages/Admin/Timetables/Edit.cshtml.cs`
  - `Pages/Admin/Substitutions/Daily.cshtml.cs`
  - `Pages/Admin/Absences/FindSubstitutes.cshtml.cs`
  - `Pages/Admin/BreakSupervision/Index.cshtml.cs`
- Changed styling from orange/yellow to white background (gray for unassigned)
- Added green highlight for filtered teacher's supervision duties in Print page
- Changed icon from `bi-eye` to `bi-cup-hot` for break supervision
- BreakSupervision/Index page now uses Sunday-Thursday (not Monday-Friday)

**Multi-Subject Lesson Display (Print Page):**
- Changed from diagonal gradient to split cells layout
- Each subject gets its own cell with its background color
- Teachers/class/room info displayed in centered row below subjects
- All subject names now centered in lesson cards (single and multi-subject)
- CSS classes: `.multi-subject-card`, `.subject-row`, `.subject-cell`, `.teachers-row`

**Multi-Room Selection in Change Time Slot Dialog:**
- Replaced single room dropdown with checkbox list for multiple rooms
- Updated `OnPostEditLessonAsync` signature: `int? roomId` → `int[] roomIds`
- Clears legacy `RoomId` field, uses `ScheduledLessonRooms` for all assignments
- JavaScript handles pre-selecting rooms and clearing on modal close
- Added `data-room-ids` attribute to edit buttons (comma-separated)

**Print Page Improvements:**
- Added "Show break supervision" checkbox (checked by default)
- Removed thick left black border from lesson cards
- Removed `border-left: 4px solid #333` from CSS
- Removed `border-left-color` from inline styles

**Break Supervision Substitution System:**
- Created `BreakSupervisionSubstitution` model for temporary coverage
- Similar pattern to lesson substitutions (date-specific, not permanent)
- Updated FindSubstitutes and Daily pages to show/manage supervision substitutions

**LessonAssignment and Room Assignment System (January 2026):**
New models for specifying which teacher teaches which subject to which class, and which combinations are in which rooms:

- **`LessonAssignment`** model (`Models/LessonAssignment.cs`):
  - Represents a teacher-subject-class combination within a lesson
  - Optional fields: TeacherId, SubjectId, ClassId, Notes, Order
  - If no assignments defined, system shows all participants together (fallback behavior)

- **`ScheduledLessonRoomAssignment`** model (`Models/ScheduledLessonRoomAssignment.cs`):
  - Links a `LessonAssignment` to a specific `ScheduledLessonRoom`
  - Allows specifying which teacher-subject-class combination is in which room
  - Used for multi-room lessons where different teachers/subjects use different rooms

- **Database tables** (`Migrations/AddLessonAssignments.sql`):
  ```sql
  LessonAssignments (Id, LessonId, TeacherId, SubjectId, ClassId, Notes, Order)
  ScheduledLessonRoomAssignments (Id, ScheduledLessonRoomId, LessonAssignmentId)
  ```

- **UI Updates**:
  - Lessons/Edit: "Teacher-Subject-Class Assignments" collapsible section
  - Lessons/Details: Shows assignments if defined
  - Timetables/Edit: Room assignments UI in "Change Time Slot" modal (when multiple rooms selected and lesson has assignments)
  - Timetables/Print: Displays room-specific assignments in `_LessonCards` partial

- **Validation**:
  - Real-time validation in UI warns when some assignments aren't assigned to any room
  - `OnGetValidateRoomAssignmentsAsync` endpoint for checking coverage
  - Warning shown on save if coverage is incomplete

- **Timetable Copy Behavior**:
  - LessonAssignments are duplicated with the lesson when copying a timetable
  - ScheduledLessonRoomAssignments are copied using ID mappings to maintain relationships

### Key Files Modified (This Session)
1. `Pages/Admin/Timetables/Print.cshtml(.cs)` - Multi-subject split, break labels, show/hide checkbox, no left border, room assignments display
2. `Pages/Admin/Timetables/_LessonCards.cshtml` - Multi-subject split layout, no left border, room-specific assignments display
3. `Pages/Admin/Timetables/Edit.cshtml(.cs)` - Multi-room selection, break labels, room assignments UI and API
4. `Pages/Admin/Substitutions/Daily.cshtml(.cs)` - Break labels, supervision styling
5. `Pages/Admin/Absences/FindSubstitutes.cshtml(.cs)` - Break labels
6. `Pages/Admin/BreakSupervision/Index.cshtml(.cs)` - Break labels, Sunday-Thursday
7. `Pages/Admin/Lessons/Edit.cshtml(.cs)` - LessonAssignment management UI
8. `Pages/Admin/Lessons/Details.cshtml(.cs)` - Display LessonAssignments
9. `Pages/Admin/Timetables/Index.cshtml.cs` - Copy logic for LessonAssignments and ScheduledLessonRoomAssignments
10. `Models/LessonAssignment.cs` - New model (created)
11. `Models/ScheduledLessonRoomAssignment.cs` - New model (created)
12. `Data/ApplicationDbContext.cs` - DbSets and relationships for new models

### Deployment Notes (IIS)

**Background Services:**
- Set App Pool Idle Timeout to 0 (disabled)
- Set Start Mode to AlwaysRunning
- Enable Preload on the site
- Consider installing Application Initialization module

**Data Protection Keys (for persistent logins):**
- Create `/keys` folder in application root
- Grant IIS app pool identity write permission to `/keys` folder
- First deployment after this change will still log everyone out
- Subsequent deployments will preserve sessions

**Folder Permissions Required:**
- `/keys` - Write (for auth cookie encryption keys)
- `/db` - Write (for SQLite database)
- `/db/Backups` - Write (for backup files)

### Testing Recommendations

**Filter persistence:**
1. Apply filters on Timetables/Edit page
2. Edit a lesson and save
3. Verify filters are restored after redirect

**Automatic backups:**
1. Delete all existing backups
2. Restart the application
3. After ~30 seconds, check Admin/Backup page
4. Verify an automatic backup was created

**WAL cleanup:**
1. Make some database changes
2. Click "Force Write & Clean Up" in Admin/Backup
3. Verify WAL/SHM files are deleted
4. Database continues to work in DELETE mode

**Backup restore:**
1. Create a manual backup
2. Make some changes
3. Restore the backup
4. Verify all backup records are preserved (including pre-restore)
5. Verify data is restored correctly
