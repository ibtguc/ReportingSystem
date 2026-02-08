# Enhanced Lesson Movement Implementation Guide

**Version:** 1.0
**Date:** 2025-11-12
**Author:** Development Team
**Status:** Planning Document

---

## Table of Contents

1. [Overview](#overview)
2. [Feature Requirements](#feature-requirements)
3. [User Interface Flow](#user-interface-flow)
4. [Architecture Design](#architecture-design)
5. [Backend Implementation](#backend-implementation)
6. [Frontend Implementation](#frontend-implementation)
7. [Algorithm Design](#algorithm-design)
8. [Database Schema](#database-schema)
9. [Step-by-Step Implementation Plan](#step-by-step-implementation-plan)
10. [Testing Strategy](#testing-strategy)
11. [Edge Cases & Considerations](#edge-cases--considerations)

---

## Overview

### Purpose
Enhance the existing timetable editing functionality to support:
- **Single and batch lesson moves** (move multiple lessons simultaneously)
- **Excluded slot selection** (user-defined slots to avoid)
- **Target slot specification** (for single lesson moves)
- **Multi-step move suggestions** (up to 4 moves in a swap chain)
- **Multiple solution options** (always show users 3-5 alternatives)
- **Conflict override capability** (allow moves despite warnings)

### Current State Analysis

**Existing Implementation (Already Available):**
- `LessonMovementService` with swap chain finder
- Available slots API endpoints
- Drag-and-drop functionality
- Conflict detection service
- Basic swap chain execution (up to 3 depth)

**What Needs Enhancement:**
- Increase max depth from 3 to 4
- Add batch lesson selection
- Add excluded slots UI
- Show multiple solution options (not just the first valid one)
- Implement conflict override mechanism
- Target slot selection for single moves

---

## Feature Requirements

### FR-1: Lesson Selection
- ✅ **Single Selection:** Click a lesson to select it
- ✅ **Batch Selection:** Ctrl+Click or checkbox to select multiple lessons
- ✅ **Visual Feedback:** Selected lessons highlighted with distinct border/background
- ✅ **Selection Counter:** Display "X lesson(s) selected"
- ✅ **Clear Selection:** Button to deselect all

### FR-2: Excluded Slots Selection
- ✅ **Visual Marking:** Right-click or Shift+Click cells to mark as excluded
- ✅ **Visual Indicator:** Excluded slots shown with striped pattern or X icon
- ✅ **Counter Display:** "Y slot(s) excluded"
- ✅ **Clear Excluded:** Button to clear all excluded slots
- ✅ **Persistence:** Excluded slots saved in session/UI state (not database)

### FR-3: Target Slot Selection (Single Move Only)
- ✅ **Visual Selection:** Click a cell to mark as target destination
- ✅ **Highlight:** Target slot highlighted with green dashed border
- ✅ **Clear Target:** Click again to deselect
- ✅ **Disabled for Batch:** Target selection disabled when multiple lessons selected

### FR-4: Movement Options Dialog
- ✅ **Automatic Display:** Show after clicking "Find Move Options" button
- ✅ **Multiple Solutions:** Display 3-5 best movement strategies
- ✅ **Sorting:** Solutions sorted by quality (fewest moves, highest score)
- ✅ **Solution Details:** Each option shows:
  - Number of moves required
  - Step-by-step breakdown
  - Quality score
  - Conflicts/warnings (if any)
  - Estimated time to execute

### FR-5: Conflict Override
- ✅ **Warning Display:** Show all hard and soft constraint violations
- ✅ **Color Coding:**
  - Red for hard violations
  - Yellow for soft violations
- ✅ **Override Checkbox:** "I understand the conflicts, proceed anyway"
- ✅ **Confirmation:** Additional confirmation dialog for hard violations
- ✅ **Audit Log:** Record that user overrode conflicts (for reporting)

### FR-6: Multi-Step Moves
- ✅ **Maximum Depth:** Support up to 4 moves in a chain
- ✅ **Visual Preview:** Show all lessons that will be affected
- ✅ **Step Breakdown:** Display each step clearly:
  - Step 1: Move [Lesson A] from [Slot X] to [Slot Y]
  - Step 2: Move [Lesson B] from [Slot Y] to [Slot Z]
  - etc.
- ✅ **Rollback Capability:** If execution fails mid-chain, rollback all changes

### FR-7: Batch Operations
- ✅ **Multi-Lesson Selection:** Select 2-10 lessons at once
- ✅ **Constraint:** All selected lessons must move to different target slots
- ✅ **Find Slots:** Algorithm finds compatible slots for all selected lessons
- ✅ **Combined Quality Score:** Show overall quality of the batch move
- ✅ **Atomic Operation:** All lessons move together or none move

---

## User Interface Flow

### Flow 1: Single Lesson Move with Target Slot

```
1. User clicks on a lesson
   └─→ Lesson highlighted with selection border
   └─→ "1 lesson selected" badge appears

2. User clicks on target empty slot
   └─→ Target slot gets green dashed border
   └─→ "Move to [Day] [Period]" button appears

3. User clicks "Find Move Options"
   └─→ Loading spinner shows
   └─→ Backend finds all possible solutions

4. Modal displays movement options:
   ┌─────────────────────────────────────────────┐
   │  Movement Options (5 found)                 │
   ├─────────────────────────────────────────────┤
   │  ✓ Option 1: Direct Move (Score: 95)       │
   │    • No conflicts                           │
   │    [Execute] [Preview]                      │
   ├─────────────────────────────────────────────┤
   │  ⚠ Option 2: 2-Step Swap (Score: 88)       │
   │    Step 1: Move Math 101 → Monday P2       │
   │    Step 2: Move Physics 201 → Tuesday P3   │
   │    ⚠ Soft conflict: Teacher gap increased  │
   │    [Execute] [Preview]                      │
   ├─────────────────────────────────────────────┤
   │  ⚠ Option 3: 3-Step Swap (Score: 75)       │
   │    ...                                      │
   └─────────────────────────────────────────────┘

5. User clicks "Execute" on preferred option
   └─→ If conflicts exist, show override dialog
   └─→ User confirms
   └─→ Backend executes move(s)
   └─→ Page refreshes or updates via AJAX
   └─→ Success message displayed
```

### Flow 2: Batch Lesson Move with Excluded Slots

```
1. User Ctrl+Clicks multiple lessons (e.g., 3 lessons)
   └─→ Each lesson highlighted
   └─→ "3 lessons selected" badge appears

2. User Shift+Clicks slots to exclude (e.g., Friday all day)
   └─→ Excluded slots show striped pattern with X
   └─→ "8 slots excluded" counter appears

3. User clicks "Find Move Options"
   └─→ Loading: "Finding slots for 3 lessons, excluding 8 slots..."
   └─→ Backend searches for batch move solutions

4. Modal displays batch movement options:
   ┌─────────────────────────────────────────────┐
   │  Batch Movement Options (3 found)           │
   ├─────────────────────────────────────────────┤
   │  ✓ Option 1: 3 Direct Moves (Score: 92)    │
   │    • Math 101 → Monday P2                   │
   │    • Physics 201 → Tuesday P3               │
   │    • Chemistry 301 → Wednesday P1           │
   │    ✓ No conflicts                           │
   │    [Execute] [Preview]                      │
   ├─────────────────────────────────────────────┤
   │  ⚠ Option 2: 4-Step Sequence (Score: 85)   │
   │    Step 1: Move Math 101 → Monday P2        │
   │    Step 2: Move English 102 → Monday P4     │
   │    Step 3: Move Physics 201 → Tuesday P3    │
   │    Step 4: Move Chemistry 301 → Wed P1      │
   │    ⚠ Soft conflict: 1 teacher gap created   │
   │    [Execute] [Preview]                      │
   └─────────────────────────────────────────────┘

5. User clicks "Execute"
   └─→ Conflict override dialog if needed
   └─→ Confirmation
   └─→ Atomic batch execution
   └─→ Success message
```

### Flow 3: Multi-Step Move with Conflicts

```
1. User selects lesson and target
2. User clicks "Find Move Options"
3. Modal shows options with conflicts:

   ┌─────────────────────────────────────────────┐
   │  🔴 Option 3: 4-Step Swap (Score: 68)       │
   │    Step 1: Move Math 101 → Monday P2        │
   │    Step 2: Move Physics 201 → Tuesday P3    │
   │    Step 3: Move Chemistry 301 → Wed P1      │
   │    Step 4: Move History 401 → Thursday P4   │
   │                                             │
   │    🔴 Hard Conflicts:                       │
   │    • Teacher "Dr. Smith" double-booked at   │
   │      Monday P2                              │
   │                                             │
   │    ⚠️ Soft Conflicts:                        │
   │    • Class "10A" has increased gaps         │
   │    • Room "Lab 2" preference not met        │
   │                                             │
   │    ☐ I understand the conflicts and want   │
   │       to proceed anyway                     │
   │                                             │
   │    [Cancel] [Execute Anyway]                │
   └─────────────────────────────────────────────┘

4. User checks override box
5. "Execute Anyway" button becomes enabled
6. User clicks "Execute Anyway"
7. Additional confirmation dialog:
   "Are you sure? This will create scheduling conflicts."
8. User confirms
9. Move executes with audit log entry
```

---

## Architecture Design

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                       USER INTERFACE                        │
│  (Edit.cshtml + enhanced-lesson-movement.js)               │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ AJAX Requests
                   ▼
┌─────────────────────────────────────────────────────────────┐
│                    API CONTROLLERS                          │
│  Pages/Admin/Timetables/Edit.cshtml.cs                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ • OnGetFindMoveOptionsAsync()                        │  │
│  │ • OnGetFindBatchMoveOptionsAsync()                   │  │
│  │ • OnPostExecuteMoveAsync()                           │  │
│  │ • OnPostExecuteBatchMoveAsync()                      │  │
│  │ • OnGetPreviewMoveAsync()                            │  │
│  └──────────────────────────────────────────────────────┘  │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ Service Calls
                   ▼
┌─────────────────────────────────────────────────────────────┐
│                    SERVICE LAYER                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  EnhancedLessonMovementService                      │   │
│  │  • FindMoveOptions(lessonId, target, excluded)      │   │
│  │  • FindBatchMoveOptions(lessonIds, excluded)        │   │
│  │  • ExecuteMove(moveOption, override)                │   │
│  │  • ValidateMove(moveOption)                         │   │
│  │  • PreviewMove(moveOption)                          │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  SwapChainFinderService (Enhanced)                  │   │
│  │  • FindAllSwapChains(maxDepth: 4)                   │   │
│  │  • ScoreSwapChain(chain)                            │   │
│  │  • SortByQuality(chains)                            │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ConflictOverrideService                            │   │
│  │  • ValidateOverride(moveOption, userId)             │   │
│  │  • LogOverride(moveOption, conflicts, userId)       │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  TimetableConflictService (Existing)                │   │
│  │  • CheckConflicts() - already implemented           │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ Data Access
                   ▼
┌─────────────────────────────────────────────────────────────┐
│                    DATA LAYER                               │
│  ApplicationDbContext                                       │
│  • ScheduledLessons                                        │
│  • MoveAuditLogs (new table)                               │
│  • ConflictOverrides (new table)                           │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

**Single Move Request:**
```
User Action → JS captures selection → AJAX → API Controller
→ EnhancedLessonMovementService.FindMoveOptions()
→ SwapChainFinderService.FindAllSwapChains(maxDepth: 4)
→ ConflictService.CheckConflicts() for each option
→ Score & Sort options
→ Return top 5 options → Display in modal
→ User selects option → Execute with override handling
→ Transaction: Execute all steps or rollback
→ Audit log entry → Success/Error response → UI update
```

**Batch Move Request:**
```
User Action → JS captures multiple selections + excluded slots
→ AJAX → API Controller
→ EnhancedLessonMovementService.FindBatchMoveOptions()
→ For each lesson: Find compatible slots (excluding marked)
→ Generate batch move combinations
→ For each combination: Validate all moves together
→ Score combinations
→ Return top 5 batch options → Display in modal
→ User selects → Atomic batch execution
→ Transaction with rollback capability
→ Audit log → Response → UI update
```

---

## Backend Implementation

### 1. New Service: `EnhancedLessonMovementService`

**Location:** `Services/LessonMovement/EnhancedLessonMovementService.cs`

```csharp
public class EnhancedLessonMovementService
{
    private readonly ApplicationDbContext _context;
    private readonly SwapChainFinderService _swapChainFinder;
    private readonly TimetableConflictService _conflictService;
    private readonly ConflictOverrideService _overrideService;

    // Configuration
    private const int MAX_SWAP_DEPTH = 4;
    private const int MAX_OPTIONS_TO_RETURN = 5;
    private const int TIMEOUT_SECONDS = 45;

    /// <summary>
    /// Find all possible move options for a single lesson
    /// </summary>
    public async Task<MoveOptionsResult> FindMoveOptionsAsync(
        int scheduledLessonId,
        DayOfWeek? targetDay = null,
        int? targetPeriodId = null,
        List<(DayOfWeek Day, int PeriodId)> excludedSlots = null)
    {
        var options = new List<MoveOption>();

        // 1. If target slot specified, find moves to that slot
        if (targetDay.HasValue && targetPeriodId.HasValue)
        {
            var targetOptions = await FindMovesToTargetSlotAsync(
                scheduledLessonId,
                targetDay.Value,
                targetPeriodId.Value,
                excludedSlots);
            options.AddRange(targetOptions);
        }
        else
        {
            // 2. Find all available slots (excluding marked ones)
            var availableOptions = await FindMovesToAnySlotAsync(
                scheduledLessonId,
                excludedSlots);
            options.AddRange(availableOptions);
        }

        // 3. Score and sort options
        options = ScoreAndSortOptions(options);

        // 4. Return top 5
        return new MoveOptionsResult
        {
            Options = options.Take(MAX_OPTIONS_TO_RETURN).ToList(),
            TotalOptionsFound = options.Count
        };
    }

    /// <summary>
    /// Find batch move options for multiple lessons
    /// </summary>
    public async Task<BatchMoveOptionsResult> FindBatchMoveOptionsAsync(
        List<int> scheduledLessonIds,
        List<(DayOfWeek Day, int PeriodId)> excludedSlots = null)
    {
        // TODO: Implement batch move logic
        // This is more complex and requires finding compatible slot combinations
        throw new NotImplementedException();
    }

    /// <summary>
    /// Execute a move option (single or multi-step)
    /// </summary>
    public async Task<ExecutionResult> ExecuteMoveAsync(
        MoveOption option,
        bool allowConflictOverride = false,
        string userId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Validate the move
            var validation = await ValidateMoveOptionAsync(option);

            // 2. Check if conflicts exist and override not allowed
            if (validation.HasHardConflicts && !allowConflictOverride)
            {
                return ExecutionResult.Failure(
                    "Hard conflicts exist. Enable override to proceed.");
            }

            // 3. Log override if conflicts exist
            if ((validation.HasHardConflicts || validation.HasSoftConflicts)
                && allowConflictOverride)
            {
                await _overrideService.LogOverrideAsync(
                    option,
                    validation.Conflicts,
                    userId);
            }

            // 4. Execute all steps in order
            foreach (var step in option.Steps.OrderBy(s => s.StepNumber))
            {
                var lesson = await _context.ScheduledLessons
                    .FindAsync(step.ScheduledLessonId);

                if (lesson == null)
                {
                    throw new Exception($"Lesson {step.ScheduledLessonId} not found");
                }

                lesson.DayOfWeek = step.ToDay;
                lesson.PeriodId = step.ToPeriodId;
                lesson.RoomId = step.ToRoomId;
            }

            // 5. Save changes
            await _context.SaveChangesAsync();

            // 6. Commit transaction
            await transaction.CommitAsync();

            // 7. Log successful move
            await LogSuccessfulMoveAsync(option, userId);

            return ExecutionResult.Success(option.Steps.Count);
        }
        catch (Exception ex)
        {
            // Rollback on any error
            await transaction.RollbackAsync();
            return ExecutionResult.Failure($"Move failed: {ex.Message}");
        }
    }

    // Private helper methods

    private async Task<List<MoveOption>> FindMovesToTargetSlotAsync(
        int scheduledLessonId,
        DayOfWeek targetDay,
        int targetPeriodId,
        List<(DayOfWeek, int)> excludedSlots)
    {
        var options = new List<MoveOption>();

        // 1. Try direct move
        var directMove = await TryDirectMoveAsync(
            scheduledLessonId, targetDay, targetPeriodId);
        if (directMove != null)
        {
            options.Add(directMove);
        }

        // 2. Find swap chains (depth 1-4)
        for (int depth = 1; depth <= MAX_SWAP_DEPTH; depth++)
        {
            var swapChains = await _swapChainFinder.FindSwapChainsAsync(
                scheduledLessonId,
                targetDay,
                targetPeriodId,
                depth,
                TimeSpan.FromSeconds(TIMEOUT_SECONDS),
                excludedSlots);

            foreach (var chain in swapChains)
            {
                options.Add(ConvertSwapChainToMoveOption(chain));
            }

            // If we found good options at this depth, don't go deeper
            if (options.Any(o => o.QualityScore >= 80))
            {
                break;
            }
        }

        return options;
    }

    private List<MoveOption> ScoreAndSortOptions(List<MoveOption> options)
    {
        // Score each option
        foreach (var option in options)
        {
            option.QualityScore = CalculateQualityScore(option);
        }

        // Sort: fewer moves first, then by quality score
        return options
            .OrderBy(o => o.Steps.Count)
            .ThenByDescending(o => o.QualityScore)
            .ToList();
    }

    private double CalculateQualityScore(MoveOption option)
    {
        double score = 100.0;

        // Penalty for number of moves
        score -= (option.Steps.Count - 1) * 5; // -5 per additional move

        // Penalty for conflicts
        score -= option.HardConflicts.Count * 20; // -20 per hard conflict
        score -= option.SoftConflicts.Count * 5;  // -5 per soft conflict

        // Bonus for keeping preferred rooms/times
        // TODO: Implement preference checking

        return Math.Max(0, score);
    }
}
```

### 2. Enhanced Service: `SwapChainFinderService`

**Modifications to Existing Service:**

```csharp
// In Services/LessonMovement/SwapChainFinderService.cs

// Update MAX_DEPTH from 3 to 4
private const int MAX_DEPTH = 4;

// Add method to find ALL chains, not just first valid one
public async Task<List<SwapChain>> FindAllSwapChainsAsync(
    int scheduledLessonId,
    DayOfWeek targetDay,
    int targetPeriodId,
    int maxDepth = 4,
    TimeSpan? timeout = null,
    List<(DayOfWeek Day, int PeriodId)> excludedSlots = null)
{
    var allChains = new List<SwapChain>();
    var cancellationToken = new CancellationTokenSource(
        timeout ?? TimeSpan.FromSeconds(30)).Token;

    // Breadth-first search to find all possible chains
    await FindChainsRecursiveAsync(
        scheduledLessonId,
        targetDay,
        targetPeriodId,
        new List<MoveStep>(),
        new HashSet<int>(),
        maxDepth,
        excludedSlots,
        allChains,
        cancellationToken);

    return allChains;
}

private async Task FindChainsRecursiveAsync(
    int currentLessonId,
    DayOfWeek targetDay,
    int targetPeriodId,
    List<MoveStep> currentPath,
    HashSet<int> movedLessons,
    int remainingDepth,
    List<(DayOfWeek, int)> excludedSlots,
    List<SwapChain> results,
    CancellationToken cancellationToken)
{
    if (cancellationToken.IsCancellationRequested)
        return;

    if (remainingDepth <= 0)
        return;

    // Check if target slot is available
    var targetSlotLessons = await GetLessonsInSlotAsync(targetDay, targetPeriodId);

    if (!targetSlotLessons.Any())
    {
        // Found a valid chain! Target slot is empty
        var finalStep = CreateMoveStep(currentLessonId, targetDay, targetPeriodId);
        currentPath.Add(finalStep);

        results.Add(new SwapChain
        {
            Steps = new List<MoveStep>(currentPath),
            IsValid = true,
            TotalMoves = currentPath.Count
        });

        return;
    }

    // Target slot occupied - need to move those lessons first
    foreach (var blockingLesson in targetSlotLessons)
    {
        if (movedLessons.Contains(blockingLesson.Id))
            continue; // Avoid cycles

        // Find available slots for blocking lesson
        var availableSlots = await GetAvailableSlotsAsync(
            blockingLesson.Id,
            excludedSlots);

        foreach (var slot in availableSlots)
        {
            // Add this move to path
            var step = CreateMoveStep(
                blockingLesson.Id, slot.DayOfWeek, slot.PeriodId);
            var newPath = new List<MoveStep>(currentPath) { step };
            var newMovedLessons = new HashSet<int>(movedLessons) { blockingLesson.Id };

            // Recurse
            await FindChainsRecursiveAsync(
                currentLessonId,
                targetDay,
                targetPeriodId,
                newPath,
                newMovedLessons,
                remainingDepth - 1,
                excludedSlots,
                results,
                cancellationToken);
        }
    }
}
```

### 3. New Service: `ConflictOverrideService`

**Location:** `Services/ConflictOverrideService.cs`

```csharp
public class ConflictOverrideService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConflictOverrideService> _logger;

    public async Task LogOverrideAsync(
        MoveOption moveOption,
        List<Conflict> conflicts,
        string userId)
    {
        var overrideLog = new ConflictOverride
        {
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            MoveDescription = SerializeMoveOption(moveOption),
            HardConflictCount = conflicts.Count(c => c.IsHard),
            SoftConflictCount = conflicts.Count(c => !c.IsHard),
            ConflictDetails = SerializeConflicts(conflicts)
        };

        _context.ConflictOverrides.Add(overrideLog);
        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "User {UserId} overrode {HardCount} hard and {SoftCount} soft conflicts",
            userId,
            overrideLog.HardConflictCount,
            overrideLog.SoftConflictCount);
    }

    public async Task<bool> ValidateOverridePermissionAsync(string userId)
    {
        // Check if user has permission to override conflicts
        // This could check user roles, permissions, etc.
        return true; // For now, allow all authenticated users
    }
}
```

### 4. New Models/DTOs

**Location:** `Services/LessonMovement/Models.cs`

```csharp
public class MoveOption
{
    public List<MoveStep> Steps { get; set; } = new();
    public int TotalMoves => Steps.Count;
    public double QualityScore { get; set; }
    public bool HasConflicts => HardConflicts.Any() || SoftConflicts.Any();
    public List<Conflict> HardConflicts { get; set; } = new();
    public List<Conflict> SoftConflicts { get; set; } = new();
    public string Description { get; set; } = "";
    public TimeSpan EstimatedExecutionTime { get; set; }
}

public class MoveOptionsResult
{
    public List<MoveOption> Options { get; set; } = new();
    public int TotalOptionsFound { get; set; }
    public bool HasTargetSlot { get; set; }
    public bool HasExcludedSlots { get; set; }
    public int ExcludedSlotsCount { get; set; }
}

public class BatchMoveOptionsResult
{
    public List<BatchMoveOption> Options { get; set; } = new();
    public int TotalOptionsFound { get; set; }
    public int LessonsToMove { get; set; }
}

public class BatchMoveOption
{
    public List<MoveOption> IndividualMoves { get; set; } = new();
    public double CombinedQualityScore { get; set; }
    public int TotalMoves { get; set; }
    public bool HasConflicts { get; set; }
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int MovesExecuted { get; set; }

    public static ExecutionResult Success(int moves) => new()
    {
        Success = true,
        Message = $"Successfully executed {moves} move(s)",
        MovesExecuted = moves
    };

    public static ExecutionResult Failure(string message) => new()
    {
        Success = false,
        Message = message,
        MovesExecuted = 0
    };
}

public class Conflict
{
    public bool IsHard { get; set; }
    public string Type { get; set; } = ""; // Teacher, Room, Class, etc.
    public string Message { get; set; } = "";
    public string Details { get; set; } = "";
}
```

---

## Frontend Implementation

### 1. Enhanced JavaScript Module

**Location:** `wwwroot/js/enhanced-lesson-movement.js`

```javascript
/**
 * Enhanced Lesson Movement Manager
 * Supports batch selection, excluded slots, multiple options
 */
class EnhancedLessonMovementManager {
    constructor(timetableId) {
        this.timetableId = timetableId;
        this.selectedLessons = new Set(); // Set of scheduledLessonIds
        this.excludedSlots = new Set();   // Set of "day,periodId"
        this.targetSlot = null;            // {day, periodId} or null
        this.init();
    }

    init() {
        this.initLessonSelection();
        this.initSlotExclusion();
        this.initTargetSlotSelection();
        this.initMoveButton();
        this.initClearButtons();
    }

    // ==================== LESSON SELECTION ====================

    initLessonSelection() {
        document.querySelectorAll('.lesson-card').forEach(card => {
            // Click to select/deselect
            card.addEventListener('click', (e) => {
                // Skip if clicking action buttons
                if (e.target.closest('.btn, form')) return;

                const scheduledId = this.getLessonId(card);

                if (e.ctrlKey || e.metaKey) {
                    // Multi-select mode
                    this.toggleLessonSelection(scheduledId, card);
                } else {
                    // Single select mode - clear others
                    this.clearAllSelections();
                    this.toggleLessonSelection(scheduledId, card);
                }

                this.updateSelectionUI();
            });
        });
    }

    toggleLessonSelection(scheduledId, card) {
        if (this.selectedLessons.has(scheduledId)) {
            this.selectedLessons.delete(scheduledId);
            card.classList.remove('lesson-selected');
        } else {
            this.selectedLessons.add(scheduledId);
            card.classList.add('lesson-selected');
        }
    }

    clearAllSelections() {
        document.querySelectorAll('.lesson-card').forEach(card => {
            card.classList.remove('lesson-selected');
        });
        this.selectedLessons.clear();
    }

    // ==================== EXCLUDED SLOTS ====================

    initSlotExclusion() {
        document.querySelectorAll('.table td').forEach(cell => {
            // Skip time column
            if (cell.parentElement.children[0] === cell) return;

            // Shift+Click to exclude/include
            cell.addEventListener('click', (e) => {
                if (e.shiftKey) {
                    e.preventDefault();
                    this.toggleSlotExclusion(cell);
                }
            });

            // Also add right-click context menu
            cell.addEventListener('contextmenu', (e) => {
                e.preventDefault();
                this.toggleSlotExclusion(cell);
            });
        });
    }

    toggleSlotExclusion(cell) {
        const {day, periodId} = this.getCellInfo(cell);
        const key = `${day},${periodId}`;

        if (this.excludedSlots.has(key)) {
            this.excludedSlots.delete(key);
            cell.classList.remove('slot-excluded');
        } else {
            this.excludedSlots.add(key);
            cell.classList.add('slot-excluded');
        }

        this.updateExcludedSlotsUI();
    }

    clearExcludedSlots() {
        this.excludedSlots.clear();
        document.querySelectorAll('.slot-excluded').forEach(cell => {
            cell.classList.remove('slot-excluded');
        });
        this.updateExcludedSlotsUI();
    }

    // ==================== TARGET SLOT SELECTION ====================

    initTargetSlotSelection() {
        document.querySelectorAll('.table td').forEach(cell => {
            // Skip time column
            if (cell.parentElement.children[0] === cell) return;

            // Click to set target (only if single lesson selected)
            cell.addEventListener('click', (e) => {
                if (e.shiftKey || e.ctrlKey || e.metaKey) return;
                if (e.target.closest('.lesson-card')) return;

                // Only allow target selection for single lesson
                if (this.selectedLessons.size !== 1) return;

                this.setTargetSlot(cell);
            });
        });
    }

    setTargetSlot(cell) {
        // Clear previous target
        document.querySelectorAll('.slot-target').forEach(c => {
            c.classList.remove('slot-target');
        });

        const {day, periodId} = this.getCellInfo(cell);

        // Toggle target
        if (this.targetSlot &&
            this.targetSlot.day === day &&
            this.targetSlot.periodId === periodId) {
            this.targetSlot = null;
        } else {
            this.targetSlot = {day, periodId};
            cell.classList.add('slot-target');
        }

        this.updateMoveButtonUI();
    }

    // ==================== MOVE OPTIONS DIALOG ====================

    async showMoveOptionsDialog() {
        try {
            this.showLoadingOverlay('Finding movement options...');

            // Build request based on selection
            let response;

            if (this.selectedLessons.size === 1) {
                // Single lesson move
                const lessonId = Array.from(this.selectedLessons)[0];
                response = await this.findSingleMoveOptions(lessonId);
            } else {
                // Batch move
                const lessonIds = Array.from(this.selectedLessons);
                response = await this.findBatchMoveOptions(lessonIds);
            }

            this.hideLoadingOverlay();

            if (!response.success) {
                this.showError(response.error);
                return;
            }

            this.displayMoveOptionsModal(response.data);

        } catch (error) {
            this.hideLoadingOverlay();
            this.showError(`Error: ${error.message}`);
        }
    }

    async findSingleMoveOptions(lessonId) {
        const params = new URLSearchParams({
            scheduledLessonId: lessonId,
            excludedSlots: this.getExcludedSlotsParam()
        });

        if (this.targetSlot) {
            params.append('targetDay', this.targetSlot.day);
            params.append('targetPeriodId', this.targetSlot.periodId);
        }

        const response = await fetch(
            `?handler=FindMoveOptions&${params}`);
        return await response.json();
    }

    async findBatchMoveOptions(lessonIds) {
        const params = new URLSearchParams({
            scheduledLessonIds: lessonIds.join(','),
            excludedSlots: this.getExcludedSlotsParam()
        });

        const response = await fetch(
            `?handler=FindBatchMoveOptions&${params}`);
        return await response.json();
    }

    displayMoveOptionsModal(data) {
        const modal = document.getElementById('moveOptionsModal');
        const modalBody = modal.querySelector('.modal-body');

        let html = `
            <div class="mb-3">
                <h6>Found ${data.totalOptionsFound} option(s)</h6>
                ${this.selectedLessons.size > 1 ?
                    `<p class="text-muted">Moving ${this.selectedLessons.size} lessons</p>` : ''}
                ${this.excludedSlots.size > 0 ?
                    `<p class="text-muted">${this.excludedSlots.size} slot(s) excluded</p>` : ''}
            </div>
        `;

        // Display each option
        data.options.forEach((option, index) => {
            html += this.renderMoveOption(option, index);
        });

        if (data.options.length === 0) {
            html += `
                <div class="alert alert-warning">
                    <strong>No movement options found.</strong>
                    <p class="mb-0">Try:</p>
                    <ul class="mb-0">
                        <li>Removing some excluded slots</li>
                        <li>Choosing a different target slot</li>
                        <li>Checking if lessons are locked</li>
                    </ul>
                </div>
            `;
        }

        modalBody.innerHTML = html;

        // Attach execute handlers
        this.attachMoveOptionHandlers();

        // Show modal
        new bootstrap.Modal(modal).show();
    }

    renderMoveOption(option, index) {
        const badgeClass = this.getQualityBadgeClass(option.qualityScore);
        const hasHardConflicts = option.hardConflicts && option.hardConflicts.length > 0;
        const hasSoftConflicts = option.softConflicts && option.softConflicts.length > 0;

        let html = `
            <div class="card mb-3 move-option-card ${hasHardConflicts ? 'border-danger' : ''}">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <div>
                        <strong>Option ${index + 1}</strong>
                        <span class="badge bg-${badgeClass} ms-2">
                            ${option.totalMoves} move(s)
                        </span>
                        <span class="badge bg-secondary ms-1">
                            Score: ${option.qualityScore.toFixed(0)}
                        </span>
                        ${hasHardConflicts ?
                            '<span class="badge bg-danger ms-1">⚠ Hard Conflicts</span>' : ''}
                        ${hasSoftConflicts && !hasHardConflicts ?
                            '<span class="badge bg-warning text-dark ms-1">⚠ Soft Conflicts</span>' : ''}
                    </div>
                </div>
                <div class="card-body">
        `;

        // Display steps
        if (option.steps && option.steps.length > 0) {
            html += '<ol class="mb-3">';
            option.steps.forEach(step => {
                html += `
                    <li class="mb-2">
                        <strong>${step.lessonDescription}</strong><br>
                        <small class="text-muted">
                            From: ${step.from.day} ${step.from.periodName}
                            ${step.from.roomName ? `(${step.from.roomName})` : ''}
                            →
                            To: ${step.to.day} ${step.to.periodName}
                            ${step.to.roomName ? `(${step.to.roomName})` : ''}
                        </small>
                    </li>
                `;
            });
            html += '</ol>';
        }

        // Display conflicts if any
        if (hasHardConflicts) {
            html += `
                <div class="alert alert-danger mb-3">
                    <strong>🔴 Hard Conflicts:</strong>
                    <ul class="mb-0 mt-2">
                        ${option.hardConflicts.map(c =>
                            `<li>${c.message}</li>`).join('')}
                    </ul>
                </div>
            `;
        }

        if (hasSoftConflicts) {
            html += `
                <div class="alert alert-warning mb-3">
                    <strong>⚠️ Soft Conflicts:</strong>
                    <ul class="mb-0 mt-2">
                        ${option.softConflicts.map(c =>
                            `<li>${c.message}</li>`).join('')}
                    </ul>
                </div>
            `;
        }

        // Override checkbox for conflicts
        if (hasHardConflicts || hasSoftConflicts) {
            html += `
                <div class="form-check mb-3">
                    <input class="form-check-input override-checkbox"
                           type="checkbox"
                           id="override-${index}">
                    <label class="form-check-label" for="override-${index}">
                        I understand the conflicts and want to proceed anyway
                    </label>
                </div>
            `;
        }

        // Action buttons
        html += `
                    <div class="d-flex gap-2">
                        <button class="btn btn-primary execute-move-btn"
                                data-option-index="${index}"
                                ${hasHardConflicts || hasSoftConflicts ? 'disabled' : ''}>
                            <i class="bi bi-check-circle"></i> Execute This Option
                        </button>
                        <button class="btn btn-outline-secondary preview-move-btn"
                                data-option-index="${index}">
                            <i class="bi bi-eye"></i> Preview
                        </button>
                    </div>
                </div>
            </div>
        `;

        return html;
    }

    attachMoveOptionHandlers() {
        // Override checkbox handlers
        document.querySelectorAll('.override-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                const index = e.target.id.split('-')[1];
                const executeBtn = document.querySelector(
                    `.execute-move-btn[data-option-index="${index}"]`);
                executeBtn.disabled = !e.target.checked;
            });
        });

        // Execute button handlers
        document.querySelectorAll('.execute-move-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const index = parseInt(e.currentTarget.dataset.optionIndex);
                await this.executeMove(index);
            });
        });

        // Preview button handlers
        document.querySelectorAll('.preview-move-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const index = parseInt(e.currentTarget.dataset.optionIndex);
                this.previewMove(index);
            });
        });
    }

    async executeMove(optionIndex) {
        // Get the option from the stored data
        // (You'll need to store the options when displaying the modal)

        const confirmed = confirm(
            'Are you sure you want to execute this move? This action cannot be undone.'
        );

        if (!confirmed) return;

        try {
            this.showLoadingOverlay('Executing move...');

            const response = await fetch('?handler=ExecuteMove', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken':
                        document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    optionIndex: optionIndex,
                    allowOverride: this.isOverrideChecked(optionIndex)
                })
            });

            const result = await response.json();
            this.hideLoadingOverlay();

            if (result.success) {
                this.showSuccess(result.message);
                setTimeout(() => window.location.reload(), 1500);
            } else {
                this.showError(result.error);
            }

        } catch (error) {
            this.hideLoadingOverlay();
            this.showError(`Error: ${error.message}`);
        }
    }

    // ==================== UI HELPERS ====================

    updateSelectionUI() {
        const counter = document.getElementById('selectedLessonsCounter');
        if (this.selectedLessons.size > 0) {
            counter.textContent = `${this.selectedLessons.size} lesson(s) selected`;
            counter.classList.remove('d-none');
        } else {
            counter.classList.add('d-none');
        }

        this.updateMoveButtonUI();
    }

    updateExcludedSlotsUI() {
        const counter = document.getElementById('excludedSlotsCounter');
        if (this.excludedSlots.size > 0) {
            counter.textContent = `${this.excludedSlots.size} slot(s) excluded`;
            counter.classList.remove('d-none');
        } else {
            counter.classList.add('d-none');
        }
    }

    updateMoveButtonUI() {
        const moveBtn = document.getElementById('findMoveOptionsBtn');

        if (this.selectedLessons.size > 0) {
            moveBtn.disabled = false;

            if (this.selectedLessons.size === 1 && this.targetSlot) {
                moveBtn.innerHTML = `
                    <i class="bi bi-arrow-right-circle"></i>
                    Move to ${this.targetSlot.day} Period ${this.targetSlot.periodId}
                `;
            } else {
                moveBtn.innerHTML = `
                    <i class="bi bi-search"></i>
                    Find Move Options
                `;
            }
        } else {
            moveBtn.disabled = true;
            moveBtn.innerHTML = `
                <i class="bi bi-search"></i>
                Select lesson(s) first
            `;
        }
    }

    getQualityBadgeClass(score) {
        if (score >= 90) return 'success';
        if (score >= 75) return 'info';
        if (score >= 60) return 'warning';
        return 'danger';
    }

    // ... other helper methods ...
}
```

### 2. UI Enhancements to Edit.cshtml

**Add to Edit.cshtml (after the header, around line 37):**

```html
<!-- Movement Control Panel -->
<div class="card shadow-sm mb-3">
    <div class="card-header bg-info text-white">
        <i class="bi bi-arrows-move"></i> <strong>Lesson Movement Tools</strong>
    </div>
    <div class="card-body">
        <div class="row g-3">
            <!-- Selection Info -->
            <div class="col-md-3">
                <div class="d-flex align-items-center gap-2">
                    <span id="selectedLessonsCounter" class="badge bg-primary d-none">
                        0 lesson(s) selected
                    </span>
                    <button class="btn btn-sm btn-outline-secondary"
                            id="clearSelectionBtn">
                        Clear Selection
                    </button>
                </div>
            </div>

            <!-- Excluded Slots Info -->
            <div class="col-md-3">
                <div class="d-flex align-items-center gap-2">
                    <span id="excludedSlotsCounter" class="badge bg-warning text-dark d-none">
                        0 slot(s) excluded
                    </span>
                    <button class="btn btn-sm btn-outline-secondary"
                            id="clearExcludedBtn">
                        Clear Excluded
                    </button>
                </div>
            </div>

            <!-- Action Buttons -->
            <div class="col-md-6">
                <div class="d-flex gap-2 justify-content-end">
                    <button class="btn btn-success"
                            id="findMoveOptionsBtn"
                            disabled>
                        <i class="bi bi-search"></i> Select lesson(s) first
                    </button>
                </div>
            </div>
        </div>

        <!-- Instructions -->
        <div class="mt-3 pt-3 border-top">
            <small class="text-muted">
                <strong>How to use:</strong>
                <ul class="mb-0">
                    <li><strong>Select lessons:</strong> Click to select one, Ctrl+Click for multiple</li>
                    <li><strong>Exclude slots:</strong> Shift+Click or Right-click cells to mark as excluded</li>
                    <li><strong>Set target:</strong> Click an empty cell to set as target (single lesson only)</li>
                    <li><strong>Find options:</strong> Click "Find Move Options" to see all possibilities</li>
                </ul>
            </small>
        </div>
    </div>
</div>
```

**Add new modal at the end of Edit.cshtml:**

```html
<!-- Move Options Modal -->
<div class="modal fade" id="moveOptionsModal" tabindex="-1" aria-labelledby="moveOptionsModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-xl modal-dialog-scrollable">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="moveOptionsModalLabel">
                    <i class="bi bi-shuffle"></i> Movement Options
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <!-- Content populated by JavaScript -->
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
            </div>
        </div>
    </div>
</div>
```

**Add CSS styles in the `@section Styles` block:**

```css
/* Lesson Selection */
.lesson-card.lesson-selected {
    box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.8), 0 4px 12px rgba(0,0,0,0.3) !important;
    transform: scale(1.05);
    z-index: 10;
}

/* Excluded Slots */
.slot-excluded {
    background: repeating-linear-gradient(
        45deg,
        #ffeaa7,
        #ffeaa7 10px,
        #ffd79a 10px,
        #ffd79a 20px
    ) !important;
    position: relative;
}

.slot-excluded::after {
    content: "✕";
    position: absolute;
    top: 5px;
    right: 5px;
    color: #d63031;
    font-weight: bold;
    font-size: 20px;
    pointer-events: none;
}

/* Target Slot */
.slot-target {
    background-color: #d4edda !important;
    border: 3px dashed #28a745 !important;
    position: relative;
}

.slot-target::before {
    content: "🎯";
    position: absolute;
    top: 5px;
    left: 5px;
    font-size: 20px;
    pointer-events: none;
}

/* Move Options Card */
.move-option-card {
    transition: all 0.3s;
}

.move-option-card:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}
```

---

## Algorithm Design

### 1. Single Lesson Move Algorithm

**Pseudocode:**

```
FUNCTION FindMoveOptions(lessonId, targetSlot?, excludedSlots):
    options = []

    IF targetSlot IS SPECIFIED:
        // User wants to move to specific slot

        // Try direct move
        IF IsSlotAvailable(targetSlot):
            directMove = CreateDirectMove(lessonId, targetSlot)
            validation = ValidateMove(directMove)
            directMove.conflicts = validation.conflicts
            directMove.score = CalculateScore(directMove, validation)
            options.ADD(directMove)
        ENDIF

        // Find swap chains (depth 1-4)
        FOR depth = 1 TO 4:
            swapChains = FindSwapChains(lessonId, targetSlot, depth, excludedSlots)

            FOR EACH chain IN swapChains:
                moveOption = ConvertToMoveOption(chain)
                validation = ValidateSwapChain(chain)
                moveOption.conflicts = validation.conflicts
                moveOption.score = CalculateScore(moveOption, validation)
                options.ADD(moveOption)
            ENDFOR

            // Early exit if we found good options
            IF options.HAS_ANY(o => o.score >= 80):
                BREAK
            ENDIF
        ENDFOR
    ELSE:
        // User wants to see all possibilities
        allSlots = GetAllSlots() - excludedSlots

        FOR EACH slot IN allSlots:
            // Try direct move to this slot
            IF IsSlotAvailable(slot):
                directMove = CreateDirectMove(lessonId, slot)
                validation = ValidateMove(directMove)
                directMove.conflicts = validation.conflicts
                directMove.score = CalculateScore(directMove, validation)
                options.ADD(directMove)
            ENDIF
        ENDFOR

        // Sort by score and return top options
        options = options.SORT_BY_SCORE_DESC()
    ENDIF

    // Score and sort all options
    options = ScoreAndSort(options)

    // Return top 5
    RETURN options.TAKE(5)
ENDFUNCTION


FUNCTION FindSwapChains(lessonId, targetSlot, maxDepth, excludedSlots):
    chains = []
    visited = SET()

    CALL FindChainsRecursive(
        lessonId,
        targetSlot,
        currentPath = [],
        visited,
        maxDepth,
        excludedSlots,
        chains)

    RETURN chains
ENDFUNCTION


FUNCTION FindChainsRecursive(
    currentLessonId,
    targetSlot,
    currentPath,
    visited,
    remainingDepth,
    excludedSlots,
    results):

    IF remainingDepth <= 0:
        RETURN  // Max depth reached
    ENDIF

    // Check if target slot is available
    lessonsInTarget = GetLessonsInSlot(targetSlot)

    IF lessonsInTarget IS EMPTY:
        // Success! Found a valid chain
        finalStep = CreateMoveStep(currentLessonId, targetSlot)
        currentPath.ADD(finalStep)

        chain = NEW SwapChain {
            Steps = COPY(currentPath),
            IsValid = TRUE,
            TotalMoves = currentPath.LENGTH
        }
        results.ADD(chain)
        RETURN
    ENDIF

    // Target slot is occupied - need to move blocking lessons
    FOR EACH blockingLesson IN lessonsInTarget:
        IF blockingLesson.Id IN visited:
            CONTINUE  // Avoid cycles
        ENDIF

        // Find where blocking lesson can go
        availableSlots = GetAvailableSlots(blockingLesson.Id) - excludedSlots

        FOR EACH slot IN availableSlots:
            // Create new path with this move
            step = CreateMoveStep(blockingLesson.Id, slot)
            newPath = COPY(currentPath)
            newPath.ADD(step)

            newVisited = COPY(visited)
            newVisited.ADD(blockingLesson.Id)

            // Recurse
            CALL FindChainsRecursive(
                currentLessonId,
                targetSlot,
                newPath,
                newVisited,
                remainingDepth - 1,
                excludedSlots,
                results)
        ENDFOR
    ENDFOR
ENDFUNCTION


FUNCTION CalculateScore(moveOption, validation):
    score = 100.0

    // Penalty for number of moves
    score -= (moveOption.TotalMoves - 1) * 5

    // Penalty for conflicts
    score -= validation.HardConflicts.COUNT * 20
    score -= validation.SoftConflicts.COUNT * 5

    // Bonus for maintaining preferences
    IF validation.MaintainsPreferredRoom:
        score += 10
    ENDIF

    IF validation.MaintainsPreferredTime:
        score += 10
    ENDIF

    // Bonus for reducing gaps
    IF validation.ReducesGaps:
        score += 15
    ENDIF

    RETURN MAX(0, score)
ENDFUNCTION
```

### 2. Batch Move Algorithm

**Pseudocode:**

```
FUNCTION FindBatchMoveOptions(lessonIds, excludedSlots):
    // This is more complex - need to find compatible combinations

    batchOptions = []
    allSlots = GetAllSlots() - excludedSlots

    // Generate all possible combinations (this can explode combinatorially)
    // Use heuristics to limit search space

    // Approach 1: Greedy - move lessons one by one
    greedyOption = FindGreedyBatchMove(lessonIds, excludedSlots)
    IF greedyOption IS VALID:
        batchOptions.ADD(greedyOption)
    ENDIF

    // Approach 2: Try to keep lessons in same relative positions
    relativeOption = FindRelativePositionBatchMove(lessonIds, excludedSlots)
    IF relativeOption IS VALID:
        batchOptions.ADD(relativeOption)
    ENDIF

    // Approach 3: Optimize for minimal total moves
    optimalOption = FindOptimalBatchMove(lessonIds, excludedSlots, maxTotalMoves = 10)
    IF optimalOption IS VALID:
        batchOptions.ADD(optimalOption)
    ENDIF

    // Score and return top options
    RETURN ScoreAndSort(batchOptions).TAKE(5)
ENDFUNCTION


FUNCTION FindGreedyBatchMove(lessonIds, excludedSlots):
    individualMoves = []
    occupiedSlots = SET(excludedSlots)

    FOR EACH lessonId IN lessonIds:
        // Find best available slot for this lesson
        availableSlots = GetAvailableSlots(lessonId) - occupiedSlots

        IF availableSlots IS EMPTY:
            RETURN INVALID  // Can't find slot for this lesson
        ENDIF

        bestSlot = availableSlots.ORDER_BY_QUALITY().FIRST()
        move = CreateDirectMove(lessonId, bestSlot)
        individualMoves.ADD(move)

        // Mark slot as occupied for next iterations
        occupiedSlots.ADD(bestSlot)
    ENDFOR

    RETURN NEW BatchMoveOption {
        IndividualMoves = individualMoves,
        TotalMoves = individualMoves.COUNT,
        Score = CalculateBatchScore(individualMoves)
    }
ENDFUNCTION
```

---

## Database Schema

### New Tables

#### 1. ConflictOverrides

```sql
CREATE TABLE ConflictOverrides (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    MoveDescription NVARCHAR(MAX) NOT NULL,
    HardConflictCount INT NOT NULL DEFAULT 0,
    SoftConflictCount INT NOT NULL DEFAULT 0,
    ConflictDetails NVARCHAR(MAX) NULL,

    CONSTRAINT FK_ConflictOverrides_AspNetUsers
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

CREATE INDEX IX_ConflictOverrides_UserId ON ConflictOverrides(UserId);
CREATE INDEX IX_ConflictOverrides_Timestamp ON ConflictOverrides(Timestamp);
```

#### 2. MoveAuditLogs

```sql
CREATE TABLE MoveAuditLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TimetableId INT NOT NULL,
    MoveType NVARCHAR(50) NOT NULL, -- 'Single', 'Batch', 'SwapChain'
    TotalMoves INT NOT NULL,
    MoveDetails NVARCHAR(MAX) NOT NULL, -- JSON
    Success BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL,

    CONSTRAINT FK_MoveAuditLogs_Users
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    CONSTRAINT FK_MoveAuditLogs_Timetables
        FOREIGN KEY (TimetableId) REFERENCES Timetables(Id)
);

CREATE INDEX IX_MoveAuditLogs_TimetableId ON MoveAuditLogs(TimetableId);
CREATE INDEX IX_MoveAuditLogs_Timestamp ON MoveAuditLogs(Timestamp);
```

### Migration Code

```csharp
// Migrations/YYYYMMDDHHMMSS_AddLessonMovementTables.cs

public partial class AddLessonMovementTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConflictOverrides",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(maxLength: 450, nullable: false),
                Timestamp = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                MoveDescription = table.Column<string>(nullable: false),
                HardConflictCount = table.Column<int>(nullable: false, defaultValue: 0),
                SoftConflictCount = table.Column<int>(nullable: false, defaultValue: 0),
                ConflictDetails = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConflictOverrides", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConflictOverrides_AspNetUsers",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MoveAuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(maxLength: 450, nullable: false),
                Timestamp = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                TimetableId = table.Column<int>(nullable: false),
                MoveType = table.Column<string>(maxLength: 50, nullable: false),
                TotalMoves = table.Column<int>(nullable: false),
                MoveDetails = table.Column<string>(nullable: false),
                Success = table.Column<bool>(nullable: false, defaultValue: true),
                ErrorMessage = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MoveAuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_MoveAuditLogs_Users",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MoveAuditLogs_Timetables",
                    column: x => x.TimetableId,
                    principalTable: "Timetables",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ConflictOverrides_UserId",
            table: "ConflictOverrides",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ConflictOverrides_Timestamp",
            table: "ConflictOverrides",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_MoveAuditLogs_TimetableId",
            table: "MoveAuditLogs",
            column: "TimetableId");

        migrationBuilder.CreateIndex(
            name: "IX_MoveAuditLogs_Timestamp",
            table: "MoveAuditLogs",
            column: "Timestamp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ConflictOverrides");
        migrationBuilder.DropTable(name: "MoveAuditLogs");
    }
}
```

---

## Step-by-Step Implementation Plan

### Phase 1: Foundation (Week 1)

**Tasks:**
1. ✅ Create database migrations
   - Add ConflictOverrides table
   - Add MoveAuditLogs table
   - Run migrations

2. ✅ Create DTOs and models
   - `MoveOption`, `MoveOptionsResult`
   - `BatchMoveOption`, `BatchMoveOptionsResult`
   - `ExecutionResult`, `Conflict`

3. ✅ Update existing `SwapChainFinderService`
   - Change MAX_DEPTH from 3 to 4
   - Add `FindAllSwapChainsAsync` method (find all, not just first)
   - Test with existing functionality

**Deliverables:**
- Database schema updated
- Models created and tested
- SwapChainFinderService enhanced

---

### Phase 2: Backend Services (Week 2)

**Tasks:**
1. ✅ Create `EnhancedLessonMovementService`
   - Implement `FindMoveOptionsAsync` (single lesson)
   - Implement scoring algorithm
   - Add conflict checking integration
   - Unit tests

2. ✅ Create `ConflictOverrideService`
   - Implement override logging
   - Implement permission checking
   - Unit tests

3. ✅ Add API endpoints to `Edit.cshtml.cs`
   - `OnGetFindMoveOptionsAsync`
   - `OnPostExecuteMoveAsync`
   - Integration tests

**Deliverables:**
- Services implemented and tested
- API endpoints working
- Postman/API tests passing

---

### Phase 3: Frontend - Selection & Exclusion (Week 3)

**Tasks:**
1. ✅ Implement lesson selection
   - Click to select
   - Ctrl+Click for multi-select
   - Visual feedback (CSS)
   - Selection counter

2. ✅ Implement slot exclusion
   - Shift+Click to exclude
   - Right-click alternative
   - Visual feedback (striped pattern)
   - Exclusion counter

3. ✅ Implement target slot selection
   - Click empty cell for target
   - Visual feedback (green border)
   - Disable for batch mode

4. ✅ Add control panel UI
   - Counters display
   - Clear buttons
   - Instructions

**Deliverables:**
- Users can select lessons
- Users can exclude slots
- Users can set target
- UI is intuitive

---

### Phase 4: Frontend - Move Options Dialog (Week 4)

**Tasks:**
1. ✅ Create move options modal
   - Bootstrap modal structure
   - Dynamic content rendering
   - Option cards with details

2. ✅ Implement option display
   - Show steps breakdown
   - Display conflicts
   - Show quality scores
   - Color coding

3. ✅ Add override UI
   - Checkbox for override
   - Enable/disable execute button
   - Confirmation dialog

4. ✅ Add execute functionality
   - AJAX call to backend
   - Loading overlay
   - Success/error handling
   - Page refresh

**Deliverables:**
- Move options modal working
- Users can see multiple options
- Users can execute moves
- Conflicts can be overridden

---

### Phase 5: Batch Operations (Week 5)

**Tasks:**
1. ✅ Implement batch move backend
   - `FindBatchMoveOptionsAsync` in service
   - Greedy algorithm implementation
   - Batch scoring
   - API endpoint

2. ✅ Implement batch move frontend
   - Handle multiple selections
   - Display batch options
   - Atomic execution
   - Rollback on failure

3. ✅ Testing
   - Test with 2 lessons
   - Test with 5 lessons
   - Test with 10 lessons
   - Test edge cases

**Deliverables:**
- Batch moves working
- Tested with various sizes
- Performance acceptable

---

### Phase 6: Polish & Testing (Week 6)

**Tasks:**
1. ✅ Performance optimization
   - Add caching where appropriate
   - Optimize database queries
   - Profile slow operations
   - Add timeout handling

2. ✅ UI/UX improvements
   - Smooth animations
   - Better error messages
   - Loading indicators
   - Keyboard shortcuts

3. ✅ Comprehensive testing
   - Unit tests (80%+ coverage)
   - Integration tests
   - E2E tests with Selenium
   - Performance tests

4. ✅ Documentation
   - User guide
   - Developer documentation
   - API documentation
   - Video tutorial

**Deliverables:**
- Feature complete and polished
- All tests passing
- Documentation complete
- Ready for production

---

## Testing Strategy

### Unit Tests

**Service Tests:**
```csharp
[Fact]
public async Task FindMoveOptions_SingleLesson_ReturnsMultipleOptions()
{
    // Arrange
    var service = CreateService();
    var lessonId = 1;

    // Act
    var result = await service.FindMoveOptionsAsync(lessonId);

    // Assert
    Assert.NotNull(result);
    Assert.True(result.Options.Count > 0);
    Assert.True(result.Options.Count <= 5);
    Assert.True(result.Options.All(o => o.Steps.Count <= 4));
}

[Fact]
public async Task FindMoveOptions_WithTargetSlot_IncludesDirectMoveIfPossible()
{
    // Arrange
    var service = CreateService();
    var lessonId = 1;
    var targetDay = DayOfWeek.Monday;
    var targetPeriod = 2;

    // Act
    var result = await service.FindMoveOptionsAsync(
        lessonId, targetDay, targetPeriod);

    // Assert
    var directMove = result.Options.FirstOrDefault(o => o.Steps.Count == 1);
    Assert.NotNull(directMove);
}

[Fact]
public async Task ExecuteMove_WithConflicts_RequiresOverride()
{
    // Arrange
    var service = CreateService();
    var option = CreateMoveOptionWithHardConflicts();

    // Act
    var result = await service.ExecuteMoveAsync(option, allowConflictOverride: false);

    // Assert
    Assert.False(result.Success);
    Assert.Contains("override", result.Message.ToLower());
}

[Fact]
public async Task ExecuteMove_FailsMidChain_RollsBack()
{
    // Arrange
    var service = CreateService();
    var option = CreateMultiStepMove();

    // Simulate failure on step 2
    MockFailureOnStep(2);

    // Act
    var result = await service.ExecuteMoveAsync(option);

    // Assert
    Assert.False(result.Success);
    // Verify no lessons were actually moved
    Assert.True(await VerifyNoLessonsMoved());
}
```

### Integration Tests

```csharp
[Fact]
public async Task API_FindMoveOptions_ReturnsValidJson()
{
    // Arrange
    var client = CreateTestClient();

    // Act
    var response = await client.GetAsync(
        "/Admin/Timetables/Edit?handler=FindMoveOptions&scheduledLessonId=1");

    // Assert
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<MoveOptionsResult>(json);
    Assert.NotNull(result);
}
```

### E2E Tests (Selenium)

```csharp
[Fact]
public void UserCanSelectLessonAndFindMoveOptions()
{
    // Arrange
    var driver = CreateWebDriver();
    NavigateToEditTimetable(driver);

    // Act
    var lessonCard = driver.FindElement(By.CssSelector(".lesson-card"));
    lessonCard.Click();

    var moveButton = driver.FindElement(By.Id("findMoveOptionsBtn"));
    Assert.False(moveButton.GetAttribute("disabled") == "true");

    moveButton.Click();

    // Wait for modal
    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    var modal = wait.Until(d => d.FindElement(By.Id("moveOptionsModal")));

    // Assert
    Assert.True(modal.Displayed);
    var options = driver.FindElements(By.CssSelector(".move-option-card"));
    Assert.True(options.Count > 0);
}
```

---

## Edge Cases & Considerations

### 1. Locked Lessons
**Issue:** User tries to move a locked lesson
**Solution:** Show error message, suggest unlocking first

### 2. Circular Dependencies
**Issue:** Swap chain creates a cycle (A → B → A)
**Solution:** Track visited lessons in recursive search, skip cycles

### 3. Timeout
**Issue:** Search takes too long (>30 seconds)
**Solution:**
- Implement timeout mechanism
- Return partial results if found
- Show message "Search stopped after 30s, showing X options found"

### 4. No Options Found
**Issue:** Algorithm can't find any valid moves
**Solution:**
- Show helpful message explaining why
- Suggest: removing excluded slots, unlocking lessons, choosing different target

### 5. Performance with Deep Searches
**Issue:** 4-depth search with many lessons is slow
**Solution:**
- Use early exit when good options found
- Limit breadth of search (max 5 alternatives per step)
- Show progress indicator
- Cache intermediate results

### 6. Concurrent Edits
**Issue:** Two users editing same timetable
**Solution:**
- Use optimistic locking (check timestamps)
- Show error if data changed
- Offer to refresh and retry

### 7. Database Transaction Failures
**Issue:** One step in chain fails during execution
**Solution:**
- Wrap all steps in transaction
- Rollback on any failure
- Log error for debugging
- Show clear error message to user

### 8. Very Large Timetables
**Issue:** 1000+ lessons slow down UI
**Solution:**
- Implement pagination or lazy loading
- Index database properly
- Use efficient queries (avoid N+1)
- Add loading indicators

### 9. Excluded Slots = All Slots
**Issue:** User excludes so many slots that no options remain
**Solution:**
- Validate that some slots are available
- Show warning "Too many slots excluded"
- Suggest clearing some exclusions

### 10. Batch Move with Incompatible Lessons
**Issue:** Selected lessons can't be moved together (conflicts)
**Solution:**
- Detect incompatibility early
- Show which lessons conflict
- Suggest moving separately or changing selection

---

## Performance Targets

| Operation | Target | Maximum |
|-----------|--------|---------|
| Find move options (single, no target) | < 2s | < 5s |
| Find move options (single, with target) | < 5s | < 15s |
| Find move options (batch, 3 lessons) | < 10s | < 30s |
| Execute move (1 step) | < 500ms | < 1s |
| Execute move (4 steps) | < 2s | < 5s |
| Page load with 500 lessons | < 3s | < 10s |

---

## Success Metrics

**Feature Adoption:**
- % of timetable edits using new movement tools
- Average time saved per move operation
- Reduction in manual errors

**User Satisfaction:**
- User feedback score (1-5)
- Number of support tickets related to moving lessons
- Feature usage frequency

**Technical Quality:**
- Test coverage > 80%
- No critical bugs in production
- Performance targets met
- Zero data loss incidents

---

## Next Steps After Implementation

1. **User Training**
   - Create video tutorials
   - Conduct training sessions
   - Provide quick reference guide

2. **Monitoring**
   - Add analytics to track usage
   - Monitor performance metrics
   - Collect user feedback

3. **Future Enhancements**
   - AI-powered move suggestions
   - Undo/redo functionality
   - Move history visualization
   - Export move reports
   - Bulk operations (move all Math lessons, etc.)

---

## Appendix A: API Endpoints Summary

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/Admin/Timetables/Edit?handler=FindMoveOptions` | GET | Find move options for single lesson |
| `/Admin/Timetables/Edit?handler=FindBatchMoveOptions` | GET | Find move options for batch |
| `/Admin/Timetables/Edit?handler=ExecuteMove` | POST | Execute a move option |
| `/Admin/Timetables/Edit?handler=ExecuteBatchMove` | POST | Execute batch move |
| `/Admin/Timetables/Edit?handler=PreviewMove` | GET | Preview move without executing |

---

## Appendix B: Configuration Options

```json
{
  "LessonMovement": {
    "MaxSwapDepth": 4,
    "MaxOptionsToReturn": 5,
    "SearchTimeoutSeconds": 30,
    "AllowConflictOverride": true,
    "RequireOverrideJustification": false,
    "MaxBatchSize": 10,
    "EnableAuditLogging": true
  }
}
```

---

**End of Implementation Guide**

*This is a living document. Update as requirements change or new insights emerge during implementation.*
