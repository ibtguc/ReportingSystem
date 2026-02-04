# Scheduling System

A comprehensive timetabling system for educational institutions built with ASP.NET Core Razor Pages.

## Overview

This system provides automated timetable generation, lesson movement with conflict resolution, and real-time debugging capabilities for scheduling algorithms.

## Key Features

### 1. Timetable Generation
- Multiple scheduling algorithms (Kempe Chain, Musical Chairs, Simulated Annealing, Recursive Conflict Resolution)
- Constraint validation (hard and soft constraints)
- Real-time progress updates via SignalR

### 2. Lesson Movement (`Pages/Admin/Timetables/MoveLessons.cshtml`)
**Purpose**: Interactive page for moving lessons between time slots with automatic conflict resolution.

**Algorithms Available**:
- **Swap Chain Solver**: Simple swap-based resolution
- **Kempe Chain Tabu Search**: Graph-based resolution using Kempe chains
- **Multi-Step Kempe Chain Tabu Search**: Extended Kempe chain with multiple iterations
- **Musical Chairs**: Sequential lesson displacement algorithm
- **Recursive Conflict Resolution**: CSP-based backtracking with multi-conflict handling

**Key Features**:
- View modes: Compact and Detailed
- Constraint selection (HC-1 through HC-6, SC-1 through SC-6)
- Lock/unlock lessons
- Avoid specific time slots
- Real-time solution generation
- Debug mode for algorithm visualization

### 3. Recursive Conflict Resolution Algorithm
**File**: `Services/LessonMovement/RecursiveConflictResolutionAlgorithm.cs`

**Algorithm Overview**:
- CSP-based approach with backtracking
- Handles multiple simultaneous conflicts (N-to-M lesson movements)
- Immutable state pattern to prevent side effects
- Cycle prevention with visited lesson tracking
- Early exit heuristics for optimization
- Memoization for repeated (lesson, slot) lookups
- Global time limit enforcement

**Key Optimizations**:
1. **Conflict Prioritization**: Sorts destination slots by number of conflicts (fewest first)
2. **Most Constrained Variable (MCV)**: Resolves most constrained conflicts first
3. **Early Exit**: Aborts if any conflict has no valid destinations
4. **Memoization**: Caches valid destinations for (lesson, slot) pairs
5. **Time Management**: Graceful timeout handling with Stopwatch

**State Management**:
```csharp
public class RecursionState
{
    public Dictionary<int, TimeSlot> ProposedMoves { get; init; }  // Pending moves
    public HashSet<int> VisitedLessons { get; init; }              // Cycle prevention
    public Dictionary<string, HashSet<int>> OccupiedSlots { get; init; }  // Virtual timetable state
    public int CurrentDepth { get; init; }                          // Recursion depth
    public int OriginalSelectedLessonId { get; init; }             // Root lesson
    public Dictionary<int, TimeSlot> OriginalPositions { get; init; }  // Prevent returning to start
    public List<string> IgnoredConstraints { get; init; }          // Constraint filtering
}
```

### 4. Debug Lesson Move Page (NEW)
**Files**:
- `Pages/Admin/Timetables/DebugLessonMove.cshtml` (.cshtml.cs)
- `Pages/Admin/Timetables/DebugLessonMoveLive.cshtml` (.cshtml.cs) - SignalR version

**Purpose**: Comprehensive debugging tool for the lesson movement algorithm with synchronous recursive implementation.

**Key Features**:
- **Synchronous Recursive Algorithm**: Simple, direct implementation for easier debugging
- **Multiple Solution Generation**: Explores solution space to provide options
- **Solution Validation**: Prevents circular moves (LessonID returning to original slots)
- **Performance Optimization**:
  - Debug event limit (5000 nodes) to prevent page hang
  - Path signature tracking to avoid redundant exploration
  - Solution deduplication using hash comparison
  - Early stopping after consecutive duplicates
- **Timeout Enforcement**:
  - Checks timeout at start of every recursive call
  - Checks timeout after each recursive return
  - Propagates timeout through entire call stack
- **Detailed Statistics**:
  - Total attempts vs recursive attempts
  - Unique paths explored
  - Nodes explored (limited to 5000 for display)
  - Max depth reached
  - Elapsed time with timeout status

**Recent Improvements**:
1. Added ScheduledLessonId and LessonId columns to solution tables
2. Validate no LessonID returns to any of its original slots
3. Fixed timeout enforcement across all recursive levels
4. Added debug event cap to prevent page hang with large trees
5. Path signature tracking to avoid retrying same approaches

### 5. Legacy Debug Pages
**File**: `Pages/Admin/Timetables/DebugRecursiveConflict.cshtml`

**Purpose**: Real-time visualization of the Recursive Conflict Resolution algorithm execution.

**Features**:
- **Configuration Summary**: Displays all algorithm inputs (timetable, lessons, constraints, time limits)
- **Live Statistics Dashboard**:
  - Nodes explored
  - Maximum depth reached
  - Solutions found
  - Elapsed time
- **Interactive Tree Visualization**:
  - Nested, collapsible tree structure
  - Each node shows:
    - Lesson being moved (description, ID)
    - Target time slot
    - Original position
    - Elapsed time at this step
    - Conflicting lessons (if any)
    - Visited lessons (cycle prevention tracking)
    - Proposed moves (shared state tracking)
    - Quality score (if solution found)
    - Result status (success, timeout, cycle, locked, etc.)
  - Color coding:
    - Green: Success
    - Red: Failure
    - Orange: Resolving
    - Gray: Timeout/Max depth
- **Export Functionality**: Export debug tree as JSON

**How to Access**:
1. Go to MoveLessons page
2. Select lessons and configure algorithm
3. Click "Debug" button (next to "Confirm & Generate")
4. Opens in new tab with real-time updates

**SignalR Integration**:
- Hub: `Hubs/DebugRecursiveHub.cs`
- Endpoint: `/hubs/debugRecursive`
- Events:
  - `UpdateStats`: Statistics updates
  - `AddNode`: New tree node for each recursive call
  - `Joined`: Confirmation of session join

### 5. Database Models

**Core Entities**:
- `Timetable`: Main timetable container
- `ScheduledLesson`: Lesson placed in a time slot
- `Lesson`: Template lesson definition
- `Teacher`, `Class`, `Subject`, `Room`: Resources
- `Period`: Time slot definition

**Relationships**:
- Many-to-Many: Lessons ↔ Teachers, Classes, Subjects, Rooms
- Foreign Keys: ScheduledLesson → Timetable, Lesson, Period, Room

## Architecture

### Services

**Lesson Movement Services** (`Services/LessonMovement/`):
- `AvailableSlotFinder.cs`: Finds valid time slots for lessons
- `SwapChainSolver.cs`: Simple swap-based conflict resolution
- `KempeChainTabuSearch.cs`: Kempe chain implementation
- `MultiStepKempeChainTabuSearch.cs`: Extended Kempe chain
- `MusicalChairsAlgorithm.cs`: Sequential displacement algorithm
- `RecursiveConflictResolutionAlgorithm.cs`: CSP-based recursive resolution
- `LessonMovementService.cs`: Orchestrates all algorithms
- `SimpleTimetableGenerationService.cs`: Entry point for generation

**Constraint Services** (`Services/Constraints/`):
- `IConstraintValidator`: Interface for constraint validation
- `ConstraintValidatorService.cs`: Implements hard and soft constraint checking

**Other Services**:
- `SchedulingService.cs`: Basic scheduling service
- `SchedulingServiceEnhanced.cs`: Enhanced with soft constraints
- `SchedulingServiceSimulatedAnnealing.cs`: Simulated annealing implementation
- `TimetableConflictService.cs`: Conflict detection
- `SubstitutionService.cs`: Teacher substitution management

### SignalR Hubs

**`Hubs/TimetableGenerationHub.cs`**: Real-time updates for timetable generation
**`Hubs/DebugRecursiveHub.cs`**: Real-time updates for debug visualization

### Database

**Context**: `Data/ApplicationDbContext.cs`
**Provider Support**: SQLite and SQL Server
**Seeding**: `Data/SeedData.cs`, `Data/UserSeeder.cs`

## Configuration

### appsettings.json

```json
{
  "DatabaseSettings": {
    "Provider": "SQLite",  // or "SqlServer"
    "ConnectionStrings": {
      "SQLite": "Data Source=scheduling.db",
      "SqlServer": "Server=...;Database=...;..."
    }
  },
  "EmailSettings": {
    // Email configuration for notifications
  },
  "DatabaseValidation": {
    "Enabled": true,
    "FailOnMismatch": true
  }
}
```

### Soft Constraint Weights

Configured in `Models/SoftConstraintWeights.cs`:
- Teacher preferences
- Class preferences
- Consecutive lessons
- Daily lesson limits
- Gap penalties

### Algorithm Configuration

**Recursive Conflict Resolution Parameters**:
- `maxDepth`: Maximum recursion depth (default: 10)
- `maxIterations`: Maximum attempts (converted to time limit)
- `maxTimeMinutes`: Global time limit in minutes (default: 3)
- `ignoredConstraints`: List of constraint IDs to ignore
- `debugSessionId`: Optional session ID for debug visualization

## Current Project Status (January 2026)

### Core Features - Fully Implemented

| Feature | Status | Description |
|---------|--------|-------------|
| **Timetable Management** | ✅ Complete | Create, edit, copy, rename, archive timetables |
| **Lesson Scheduling** | ✅ Complete | Schedule lessons with multiple teachers/classes/subjects/rooms |
| **Constraint System** | ✅ Complete | Hard constraints (HC-1 to HC-12), Soft constraints (SC-1 to SC-11) |
| **Conflict Detection** | ✅ Complete | Real-time conflict highlighting, availability conflict detection |
| **UNTIS Import** | ✅ Complete | Import teachers, classes, subjects, rooms, lessons, availability |
| **Break Supervision** | ✅ Complete | Manage break supervision duties, view in timetables |
| **Substitution System** | ✅ Complete | Teacher absences, substitute assignment, daily board |
| **Print System** | ✅ Complete | Print timetables with filters, multi-subject support |
| **Backup System** | ✅ Complete | Automatic (twice daily), manual backups, restore |
| **Authentication** | ✅ Complete | Magic link email login, 30-day session persistence |

### Recent Features (January 2026)

**Timetable Copy with Lesson Isolation**
- Copying a timetable creates isolated copies of all lessons
- Editing lessons in copied timetable won't affect original
- Full isolation of LessonTeachers, LessonClasses, LessonSubjects

**Multi-Room Support**
- Lessons can be assigned to multiple rooms
- "Change Time Slot" dialog uses checkbox list for room selection
- Uses `ScheduledLessonRooms` junction table

**Multi-Subject Display**
- Lessons with multiple subjects show split cells (one per subject)
- Each subject cell has its own background color
- Teachers row displayed below subject cells

**Break Supervision Module**
- Import from UNTIS GPU009.TXT
- Matrix view management (Location × Day × Period)
- Integration in timetable views (Edit, Print)
- Labels as "Break 1", "Break 2" (not period numbers)
- White styling (gray for unassigned)
- Show/hide checkbox in Print page

**Break Supervision Substitutions**
- Temporary supervision coverage during teacher absence
- Similar to lesson substitution system
- FindSubstitutes page shows affected supervision duties

## Development Notes

### Debugging Tips

1. **Use the Debug Page**: Best way to understand algorithm behavior
2. **Check Logs**: Algorithm logs key decisions at Debug level
3. **Review State**: Each debug node shows complete state (visited, proposed moves)
4. **Time Limits**: If solutions not found, increase maxTimeMinutes
5. **Constraints**: Try ignoring some constraints if too restrictive

### Common Issues

**No Solutions Found**:
- Check if constraints are too restrictive
- Increase time limit
- Verify lessons are not locked
- Review available time slots

**Slow Performance**:
- Reduce maxDepth
- Enable more ignored constraints
- Use conflict prioritization (already enabled)

**Compilation Errors**:
- Ensure all using statements are present
- Check init property usage (use Clone() for modifications)
- Verify DI registrations in Program.cs

## File Structure

```
SchedulingSystem/
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── SeedData.cs
│   └── UserSeeder.cs
├── Hubs/
│   ├── TimetableGenerationHub.cs
│   └── DebugRecursiveHub.cs
├── Models/
│   ├── Timetable.cs
│   ├── ScheduledLesson.cs
│   ├── Lesson.cs
│   ├── Teacher.cs, Class.cs, Subject.cs, Room.cs, Period.cs
│   ├── SoftConstraintWeights.cs
│   └── SimulatedAnnealingConfig.cs
├── Pages/
│   └── Admin/
│       └── Timetables/
│           ├── MoveLessons.cshtml (+ .cshtml.cs)
│           └── DebugRecursiveConflict.cshtml (+ .cshtml.cs)
├── Services/
│   ├── LessonMovement/
│   │   ├── RecursiveConflictResolutionAlgorithm.cs
│   │   ├── KempeChainModels.cs
│   │   ├── AvailableSlotFinder.cs
│   │   ├── SwapChainSolver.cs
│   │   ├── KempeChainTabuSearch.cs
│   │   ├── MultiStepKempeChainTabuSearch.cs
│   │   ├── MusicalChairsAlgorithm.cs
│   │   ├── LessonMovementService.cs
│   │   └── SimpleTimetableGenerationService.cs
│   ├── Constraints/
│   │   ├── IConstraintValidator.cs
│   │   └── ConstraintValidatorService.cs
│   ├── SchedulingService.cs
│   ├── TimetableConflictService.cs
│   └── SubstitutionService.cs
└── Program.cs
```

## Testing

To test the debug functionality:

1. Navigate to `/Admin/Timetables/MoveLessons?timetableId={id}`
2. Select one or more lessons to move
3. Choose "Recursive Conflict Resolution" algorithm
4. Configure constraints and parameters
5. Click "Debug" button
6. Observe real-time tree visualization in new tab
7. Expand/collapse nodes to see recursive call hierarchy
8. Review visited lessons and proposed moves to understand algorithm flow

## Deployment

### Development

```bash
# Run the application
dotnet run

# Run with hot reload
dotnet watch run

# Apply migrations
dotnet ef database update
```

### Production (IIS)

#### Basic Setup
1. Publish the application: `dotnet publish -c Release`
2. Create an IIS website pointing to the publish folder
3. Ensure the App Pool is set to "No Managed Code"
4. Grant write permissions to the required folders (see below)

#### Folder Permissions

The IIS App Pool identity needs write access to these folders:

| Folder | Purpose |
|--------|---------|
| `/db` | SQLite database files |
| `/db/Backups` | Database backup files |
| `/keys` | Data Protection keys (auth cookie encryption) |

**To grant permissions:**
1. Right-click folder → Properties → Security → Edit
2. Add `IIS AppPool\YourAppPoolName`
3. Grant "Modify" permission

#### Background Services Configuration

The application includes a **Background Hosted Service** (`DailyBackupHostedService`) that runs automatic backups every 12 hours. For this to work reliably in IIS:

**Important IIS Settings:**

| Setting | Location | Recommended Value | Why |
|---------|----------|-------------------|-----|
| **Idle Timeout** | App Pool → Advanced Settings → Process Model | `0` (disabled) | Prevents IIS from stopping the app after inactivity |
| **Regular Time Interval** | App Pool → Advanced Settings → Recycling | `0` (disabled) or `1740` (29 hours) | Prevents recycling during work hours |
| **Start Mode** | App Pool → Advanced Settings → General | `AlwaysRunning` | Starts app immediately, not on first request |
| **Preload Enabled** | Site → Advanced Settings | `True` | Warms up the app on IIS start |

**Application Initialization Module:**
For best results, install the IIS Application Initialization module:
```
Install-WindowsFeature Web-AppInit
```

This ensures:
- App starts immediately when IIS starts
- App restarts immediately after recycling
- Background services run continuously

#### Data Protection Keys (Persistent Login)

Users stay logged in after deployments because encryption keys are persisted:
- Keys stored in `/keys` folder
- **Important**: Create `/keys` folder manually and grant write permission before first run
- First deployment after adding this feature will log everyone out (new keys generated)
- Subsequent deployments preserve sessions

#### SQLite WAL Mode Notes

When using SQLite in production:
- The database uses WAL (Write-Ahead Logging) mode for better concurrency
- WAL creates `-wal` and `-shm` files alongside the main database
- Use the "Force Write & Clean Up" button in Admin/Backup to:
  - Write all pending changes to the main database
  - Remove WAL/SHM files (switches to DELETE journal mode)
  - Database returns to WAL mode on next app restart

#### Backup System

- **Automatic backups**: Created twice daily (every 12 hours)
- **Manual backups**: Create anytime via Admin/Backup page
- **Restore**: Preserves backup history after restore
- **Pre-restore backup**: Automatically created before each restore

### Docker (Optional)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
EXPOSE 80
ENTRYPOINT ["dotnet", "SchedulingSystem.dll"]
```

## Future Enhancements

### High Priority
- **Lesson Versioning**: More sophisticated system to allow selective sharing vs isolation of lessons
- **Direct Supervision Coverage**: Assign substitute teachers directly for break supervision from absence page
- **Bulk Operations**: Multi-select for lesson operations

### Medium Priority
- Pause/resume debug execution
- Step-by-step debugging
- Visualization of constraint violations
- Performance profiling per node
- Solution comparison view
- Export to formats other than JSON (e.g., DOT for Graphviz)

### Low Priority
- Report generation (workload reports, coverage reports)
- Mobile-responsive timetable views
- External calendar integration (iCal export)
