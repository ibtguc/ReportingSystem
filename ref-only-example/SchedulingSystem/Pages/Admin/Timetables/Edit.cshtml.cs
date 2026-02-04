using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using SchedulingSystem.Services.LessonMovement;
using System.Text.Json;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly TimetableConflictService _conflictService;
    private readonly LessonMovementService _movementService;

    public EditModel(
        ApplicationDbContext context,
        TimetableConflictService conflictService,
        LessonMovementService movementService)
    {
        _context = context;
        _conflictService = conflictService;
        _movementService = movementService;
    }

    [BindProperty]
    public int? SelectedTimetableId { get; set; }

    public List<SelectListItem> TimetableList { get; set; } = new();
    public Timetable? Timetable { get; set; }
    public List<Period> Periods { get; set; } = new();
    public List<ScheduledLesson> ScheduledLessons { get; set; } = new();
    public List<Lesson> AvailableLessons { get; set; } = new();
    public List<Room> AvailableRooms { get; set; } = new();

    // Lesson to Timetables mapping (shows which timetables each lesson is scheduled in)
    public Dictionary<int, List<LessonTimetableInfo>> LessonTimetables { get; set; } = new();

    // Data for edit lesson definition modal dropdowns
    public List<Teacher> AllTeachers { get; set; } = new();
    public List<Subject> AllSubjects { get; set; } = new();
    public List<Class> AllClasses { get; set; } = new();

    // Availability data for conflict detection (Importance = -3 means absolute unavailability)
    // Key format: "TeacherId:Day:PeriodId" or "ClassId:Day:PeriodId" etc.
    public Dictionary<string, bool> TeacherUnavailability { get; set; } = new();
    public Dictionary<string, bool> ClassUnavailability { get; set; } = new();
    public Dictionary<string, bool> SubjectUnavailability { get; set; } = new();
    public Dictionary<string, bool> RoomUnavailability { get; set; } = new();

    // Break Supervision data for display
    public List<BreakSupervisionDuty> BreakSupervisionDuties { get; set; } = new();
    public List<int> SupervisionPeriods { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // Load all timetables for dropdown
        await LoadTimetableListAsync();

        // If id is provided in query string, use it
        if (id.HasValue)
        {
            SelectedTimetableId = id.Value;
        }
        // If no timetable is selected, automatically select the first one
        else if (!SelectedTimetableId.HasValue && TimetableList.Any())
        {
            SelectedTimetableId = int.Parse(TimetableList.First().Value);
        }

        // Load timetable data if a timetable is selected
        if (SelectedTimetableId.HasValue)
        {
            await LoadTimetableDataAsync(SelectedTimetableId.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Load all timetables for dropdown
        await LoadTimetableListAsync();

        // Load timetable data if a timetable is selected
        if (SelectedTimetableId.HasValue)
        {
            await LoadTimetableDataAsync(SelectedTimetableId.Value);
        }

        return Page();
    }

    private async Task LoadTimetableListAsync()
    {
        var timetables = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.Term)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        TimetableList = timetables.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = $"{t.Name} ({t.SchoolYear?.Name} - {t.Term?.Name}) - {t.Status}"
        }).ToList();
    }

    private async Task LoadTimetableDataAsync(int timetableId)
    {
        // Load timetable
        Timetable = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.Term)
            .FirstOrDefaultAsync(t => t.Id == timetableId);

        if (Timetable == null)
        {
            return;
        }

        // Load periods (excluding breaks)
        Periods = await _context.Periods
            .Where(p => !p.IsBreak)
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();

        // Load all scheduled lessons for this timetable with new many-to-many structure
        // Using AsSplitQuery to avoid cartesian product when a lesson has multiple teachers/subjects/classes
        var scheduledLessonsList = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Class)
            .Include(sl => sl.Period)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Teacher)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Subject)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Class)
            .Where(sl => sl.TimetableId == timetableId)
            .AsSplitQuery()
            .ToListAsync();

        // Deduplicate by ScheduledLesson.Id to ensure unique records
        ScheduledLessons = scheduledLessonsList
            .DistinctBy(sl => sl.Id)
            .ToList();

        // Load available lessons (active lessons only, excluding fully scheduled ones)
        // Using AsSplitQuery to avoid cartesian product when a lesson has multiple teachers/subjects/classes
        var allActiveLessons = await _context.Lessons
            .Include(l => l.LessonSubjects)
                .ThenInclude(ls => ls.Subject)
            .Include(l => l.LessonClasses)
                .ThenInclude(lc => lc.Class)
            .Include(l => l.LessonTeachers)
                .ThenInclude(lt => lt.Teacher)
            .Where(l => l.IsActive)
            .AsSplitQuery()
            .ToListAsync();

        // Get lesson IDs that are scheduled in OTHER timetables (not this one)
        var lessonsInOtherTimetables = await _context.ScheduledLessons
            .Where(sl => sl.TimetableId != timetableId)
            .Select(sl => sl.LessonId)
            .Distinct()
            .ToListAsync();
        var lessonsInOtherTimetablesSet = lessonsInOtherTimetables.ToHashSet();

        // Filter out lessons that are already fully scheduled in this timetable
        // OR are scheduled in any other timetable
        AvailableLessons = new List<Lesson>();
        foreach (var lesson in allActiveLessons)
        {
            // Skip lessons that are scheduled in other timetables
            if (lessonsInOtherTimetablesSet.Contains(lesson.Id))
            {
                continue;
            }

            // Count how many times this lesson is already scheduled in this timetable
            var scheduledCount = ScheduledLessons.Count(sl => sl.LessonId == lesson.Id);

            // Only include if scheduled count is less than frequency per week
            if (scheduledCount < lesson.FrequencyPerWeek)
            {
                AvailableLessons.Add(lesson);
            }
        }

        // Sort available lessons
        AvailableLessons = AvailableLessons
            .OrderBy(l => l.LessonClasses.FirstOrDefault()?.Class?.Name ?? "")
            .ThenBy(l => l.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "")
            .ToList();

        // Load timetable info for each available lesson
        var lessonIds = AvailableLessons.Select(l => l.Id).ToList();
        var scheduledLessonsWithTimetables = await _context.ScheduledLessons
            .Include(sl => sl.Timetable)
            .Where(sl => lessonIds.Contains(sl.LessonId) && sl.TimetableId != null && sl.Timetable != null)
            .ToListAsync();

        LessonTimetables = scheduledLessonsWithTimetables
            .GroupBy(sl => sl.LessonId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(sl => sl.Timetable!)
                    .DistinctBy(t => t.Id)
                    .Select(t => new LessonTimetableInfo
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Status = t.Status
                    })
                    .OrderBy(t => t.Name)
                    .ToList()
            );

        // Load available rooms (active rooms only)
        AvailableRooms = await _context.Rooms
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        // Load all teachers for edit lesson definition modal
        AllTeachers = await _context.Teachers
            .Where(t => t.IsActive)
            .OrderBy(t => t.FirstName)
            .ThenBy(t => t.LastName)
            .ToListAsync();

        // Load all subjects for edit lesson definition modal
        AllSubjects = await _context.Subjects
            .Where(s => s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync();

        // Load all classes for edit lesson definition modal
        AllClasses = await _context.Classes
            .Where(c => c.IsActive)
            .OrderBy(c => c.YearLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();

        // Load absolute unavailability data (Importance = -3)
        await LoadUnavailabilityDataAsync();

        // Load break supervision data
        await LoadBreakSupervisionDataAsync();
    }

    private async Task LoadBreakSupervisionDataAsync()
    {
        // Load break supervision duties for this timetable
        BreakSupervisionDuties = await _context.BreakSupervisionDuties
            .Include(d => d.Room)
            .Include(d => d.Teacher)
            .Where(d => d.IsActive && d.TimetableId == SelectedTimetableId)
            .OrderBy(d => d.Room!.RoomNumber)
            .ThenBy(d => d.DayOfWeek)
            .ThenBy(d => d.PeriodNumber)
            .ToListAsync();

        // Get distinct supervision periods
        SupervisionPeriods = BreakSupervisionDuties
            .Select(d => d.PeriodNumber)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }

    private async Task LoadUnavailabilityDataAsync()
    {
        // Load teacher unavailability (Importance = -3)
        var teacherUnavail = await _context.TeacherAvailabilities
            .Where(ta => ta.Importance == -3)
            .Select(ta => new { ta.TeacherId, ta.DayOfWeek, ta.PeriodId })
            .ToListAsync();
        foreach (var item in teacherUnavail)
        {
            TeacherUnavailability[$"{item.TeacherId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }

        // Load class unavailability (Importance = -3)
        var classUnavail = await _context.ClassAvailabilities
            .Where(ca => ca.Importance == -3)
            .Select(ca => new { ca.ClassId, ca.DayOfWeek, ca.PeriodId })
            .ToListAsync();
        foreach (var item in classUnavail)
        {
            ClassUnavailability[$"{item.ClassId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }

        // Load subject unavailability (Importance = -3)
        var subjectUnavail = await _context.SubjectAvailabilities
            .Where(sa => sa.Importance == -3)
            .Select(sa => new { sa.SubjectId, sa.DayOfWeek, sa.PeriodId })
            .ToListAsync();
        foreach (var item in subjectUnavail)
        {
            SubjectUnavailability[$"{item.SubjectId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }

        // Load room unavailability (Importance = -3)
        var roomUnavail = await _context.RoomAvailabilities
            .Where(ra => ra.Importance == -3)
            .Select(ra => new { ra.RoomId, ra.DayOfWeek, ra.PeriodId })
            .ToListAsync();
        foreach (var item in roomUnavail)
        {
            RoomUnavailability[$"{item.RoomId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }
    }

    public async Task<IActionResult> OnPostDeleteLessonAsync(int lessonId, int timetableId)
    {
        try
        {
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .AsSplitQuery()
                .FirstOrDefaultAsync(sl => sl.Id == lessonId);

            if (scheduledLesson == null)
            {
                ErrorMessage = "Lesson not found.";
                return RedirectToPage(new { id = timetableId });
            }

            var subjectName = scheduledLesson.Lesson!.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "N/A";
            var className = scheduledLesson.Lesson!.LessonClasses.FirstOrDefault()?.Class?.Name ?? "N/A";
            var lessonInfo = $"{subjectName} - {className}";

            _context.ScheduledLessons.Remove(scheduledLesson);
            await _context.SaveChangesAsync();

            SuccessMessage = $"Successfully deleted lesson: {lessonInfo}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting lesson: {ex.Message}";
        }

        return RedirectToPage(new { id = timetableId });
    }

    public async Task<IActionResult> OnPostAddLessonAsync(int timetableId, int lessonId, string dayOfWeek, int periodId, int[]? roomIds, string? roomAssignmentsJson)
    {
        try
        {
            // Validate lesson exists
            var lesson = await _context.Lessons
                .Include(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
                .Include(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
                .Include(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                .AsSplitQuery()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                ErrorMessage = "Selected lesson not found.";
                return RedirectToPage(new { id = timetableId });
            }

            // Parse day of week
            if (!Enum.TryParse<DayOfWeek>(dayOfWeek, out var day))
            {
                ErrorMessage = "Invalid day of week.";
                return RedirectToPage(new { id = timetableId });
            }

            // Check if lesson already exists at this time
            var existingLesson = await _context.ScheduledLessons
                .AnyAsync(sl => sl.TimetableId == timetableId &&
                               sl.DayOfWeek == day &&
                               sl.PeriodId == periodId &&
                               sl.LessonId == lessonId);

            if (existingLesson)
            {
                ErrorMessage = $"This lesson is already scheduled at this time.";
                return RedirectToPage(new { id = timetableId });
            }

            // Create new scheduled lesson (no legacy RoomId)
            var scheduledLesson = new ScheduledLesson
            {
                TimetableId = timetableId,
                LessonId = lessonId,
                DayOfWeek = day,
                PeriodId = periodId,
                RoomId = null // Use ScheduledLessonRooms instead
            };

            _context.ScheduledLessons.Add(scheduledLesson);
            await _context.SaveChangesAsync();

            // Parse room assignments JSON (format: {"roomId": [lessonAssignmentId, ...]})
            Dictionary<int, List<int>>? roomAssignmentsData = null;
            if (!string.IsNullOrWhiteSpace(roomAssignmentsJson))
            {
                try
                {
                    roomAssignmentsData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, List<int>>>(roomAssignmentsJson);
                }
                catch
                {
                    // Ignore JSON parse errors
                }
            }

            // Add rooms and their assignments
            var roomNumbers = new List<string>();
            if (roomIds != null && roomIds.Length > 0)
            {
                foreach (var roomId in roomIds.Distinct())
                {
                    var newRoom = new ScheduledLessonRoom
                    {
                        ScheduledLessonId = scheduledLesson.Id,
                        RoomId = roomId
                    };
                    _context.ScheduledLessonRooms.Add(newRoom);
                    await _context.SaveChangesAsync();

                    // Add room assignments if specified
                    if (roomAssignmentsData != null && roomAssignmentsData.TryGetValue(roomId, out var lessonAssignmentIds))
                    {
                        foreach (var laId in lessonAssignmentIds)
                        {
                            _context.ScheduledLessonRoomAssignments.Add(new ScheduledLessonRoomAssignment
                            {
                                ScheduledLessonRoomId = newRoom.Id,
                                LessonAssignmentId = laId
                            });
                        }
                    }

                    var room = await _context.Rooms.FindAsync(roomId);
                    if (room != null) roomNumbers.Add(room.RoomNumber);
                }
                await _context.SaveChangesAsync();
            }

            var period = await _context.Periods.FindAsync(periodId);

            var subjectName = lesson.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "N/A";
            var className = lesson.LessonClasses.FirstOrDefault()?.Class?.Name ?? "N/A";
            SuccessMessage = $"Successfully added: {subjectName} - {className} on {day} at {period?.Name}" +
                           (roomNumbers.Any() ? $" in {string.Join(", ", roomNumbers)}" : "");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error adding lesson: {ex.Message}";
        }

        return RedirectToPage(new { id = timetableId });
    }

    public async Task<IActionResult> OnPostEditLessonAsync(int timetableId, int scheduledLessonId, string dayOfWeek, int periodId, int[]? roomIds, string? roomAssignmentsJson)
    {
        try
        {
            // Find the existing scheduled lesson
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
                .Include(sl => sl.Room)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.Room)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.RoomAssignments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
            {
                ErrorMessage = "Scheduled lesson not found.";
                return RedirectToPage(new { id = timetableId });
            }

            // Parse day of week
            if (!Enum.TryParse<DayOfWeek>(dayOfWeek, out var day))
            {
                ErrorMessage = "Invalid day of week.";
                return RedirectToPage(new { id = timetableId });
            }

            // Store old values for message
            var oldDay = scheduledLesson.DayOfWeek;
            var oldPeriod = await _context.Periods.FindAsync(scheduledLesson.PeriodId);

            // Update the scheduled lesson
            scheduledLesson.DayOfWeek = day;
            scheduledLesson.PeriodId = periodId;

            // Clear legacy room field (we use multi-room now)
            scheduledLesson.RoomId = null;

            // Remove existing room assignments (including their ScheduledLessonRoomAssignments due to cascade)
            foreach (var slr in scheduledLesson.ScheduledLessonRooms.ToList())
            {
                _context.ScheduledLessonRoomAssignments.RemoveRange(slr.RoomAssignments);
            }
            _context.ScheduledLessonRooms.RemoveRange(scheduledLesson.ScheduledLessonRooms);

            // Parse room assignments JSON (format: {"roomId": [lessonAssignmentId, ...]})
            Dictionary<int, List<int>>? roomAssignmentsData = null;
            if (!string.IsNullOrWhiteSpace(roomAssignmentsJson))
            {
                try
                {
                    roomAssignmentsData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, List<int>>>(roomAssignmentsJson);
                }
                catch
                {
                    // Ignore JSON parse errors, continue without room assignments
                }
            }

            // Add new rooms and their assignments
            if (roomIds != null && roomIds.Length > 0)
            {
                foreach (var roomId in roomIds.Distinct())
                {
                    var newRoom = new ScheduledLessonRoom
                    {
                        ScheduledLessonId = scheduledLessonId,
                        RoomId = roomId
                    };
                    _context.ScheduledLessonRooms.Add(newRoom);

                    // Save to get the new ScheduledLessonRoom ID
                    await _context.SaveChangesAsync();

                    // Add room assignments if specified
                    if (roomAssignmentsData != null && roomAssignmentsData.TryGetValue(roomId, out var lessonAssignmentIds))
                    {
                        foreach (var laId in lessonAssignmentIds)
                        {
                            _context.ScheduledLessonRoomAssignments.Add(new ScheduledLessonRoomAssignment
                            {
                                ScheduledLessonRoomId = newRoom.Id,
                                LessonAssignmentId = laId
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            var newPeriod = await _context.Periods.FindAsync(periodId);

            // Build room info showing all assigned rooms
            var roomInfo = "";
            if (roomIds != null && roomIds.Length > 0)
            {
                var rooms = await _context.Rooms
                    .Where(r => roomIds.Contains(r.Id))
                    .Select(r => r.RoomNumber)
                    .ToListAsync();
                roomInfo = $" in {string.Join(", ", rooms)}";
            }

            var subjectName = scheduledLesson.Lesson!.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "N/A";
            var className = scheduledLesson.Lesson!.LessonClasses.FirstOrDefault()?.Class?.Name ?? "N/A";
            SuccessMessage = $"Successfully updated: {subjectName} - {className} " +
                           $"moved from {oldDay} {oldPeriod?.Name} to {day} {newPeriod?.Name}{roomInfo}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating lesson: {ex.Message}";
        }

        return RedirectToPage(new { id = timetableId });
    }

    public async Task<IActionResult> OnPostToggleLockAsync(int lessonId, int timetableId)
    {
        try
        {
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .AsSplitQuery()
                .FirstOrDefaultAsync(sl => sl.Id == lessonId);

            if (scheduledLesson == null)
            {
                ErrorMessage = "Lesson not found.";
                return RedirectToPage(new { id = timetableId });
            }

            // Toggle the lock status
            scheduledLesson.IsLocked = !scheduledLesson.IsLocked;
            await _context.SaveChangesAsync();

            var status = scheduledLesson.IsLocked ? "locked" : "unlocked";
            var subjectName = scheduledLesson.Lesson!.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "N/A";
            var className = scheduledLesson.Lesson!.LessonClasses.FirstOrDefault()?.Class?.Name ?? "N/A";
            SuccessMessage = $"{subjectName} - {className} is now {status}. " +
                           (scheduledLesson.IsLocked ? "It won't be moved during regeneration." : "It can be moved during regeneration.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error toggling lock status: {ex.Message}";
        }

        return RedirectToPage(new { id = timetableId });
    }

    public async Task<IActionResult> OnGetCheckConflictsAsync(int timetableId, int lessonId, string dayOfWeek, int periodId, int[]? roomIds = null, int? scheduledLessonId = null, List<string>? ignoredConstraints = null)
    {
        try
        {
            // Parse day of week
            if (!Enum.TryParse<DayOfWeek>(dayOfWeek, out var day))
            {
                return new JsonResult(new { success = false, error = "Invalid day of week" });
            }

            // Use first room for conflict checking (conflict service checks one room at a time)
            int? roomId = roomIds?.FirstOrDefault();

            // Check conflicts with ignored constraints
            var result = await _conflictService.CheckConflictsAsync(
                timetableId,
                lessonId,
                day,
                periodId,
                roomId,
                scheduledLessonId,
                ignoredConstraints);

            return new JsonResult(new
            {
                success = true,
                hasErrors = result.HasErrors,
                hasWarnings = result.HasWarnings,
                errors = result.Errors,
                warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    // Helper method to get lessons for a specific day/period
    public List<ScheduledLesson> GetLessonsForSlot(DayOfWeek day, int periodId)
    {
        return ScheduledLessons
            .Where(sl => sl.DayOfWeek == day && sl.PeriodId == periodId)
            .DistinctBy(sl => sl.Id) // Ensure unique ScheduledLesson records
            .ToList();
    }

    // Helper method to get break supervision duties for a specific day and period
    public List<BreakSupervisionDuty> GetSupervisionForSlot(DayOfWeek day, int periodNumber)
    {
        return BreakSupervisionDuties
            .Where(d => d.DayOfWeek == day && d.PeriodNumber == periodNumber)
            .OrderBy(d => d.Room?.RoomNumber)
            .ToList();
    }

    // Helper to determine if supervision row should be shown after a specific period
    // Supervision periods from GPU009 (e.g., 3, 5) indicate when supervision happens
    // We show the row BEFORE that period (after the previous period)
    public bool ShouldShowSupervisionBeforePeriod(int periodNumber)
    {
        return SupervisionPeriods.Contains(periodNumber);
    }

    // Get the supervision period number that should appear before a given period
    public int? GetSupervisionPeriodBefore(int periodNumber)
    {
        return SupervisionPeriods.Contains(periodNumber) ? periodNumber : null;
    }

    // Convert supervision period number to "Break 1", "Break 2", etc.
    public string GetBreakLabel(int periodNumber)
    {
        var index = SupervisionPeriods.IndexOf(periodNumber);
        return index >= 0 ? $"Break {index + 1}" : $"Break";
    }

    // ==================== NEW LESSON MOVEMENT API ENDPOINTS ====================

    /// <summary>
    /// Get all available slots for a scheduled lesson (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetAvailableSlotsAsync(
        int scheduledLessonId,
        string? excludeSlots = null)
    {
        try
        {
            // Parse exclude slots if provided (format: "Sunday,1;Monday,2")
            var excludeList = new List<(DayOfWeek Day, int PeriodId)>();
            if (!string.IsNullOrEmpty(excludeSlots))
            {
                var slots = excludeSlots.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var slot in slots)
                {
                    var parts = slot.Split(',');
                    if (parts.Length == 2 &&
                        Enum.TryParse<DayOfWeek>(parts[0], out var day) &&
                        int.TryParse(parts[1], out var periodId))
                    {
                        excludeList.Add((day, periodId));
                    }
                }
            }

            var availableSlots = await _movementService.GetAvailableSlotsAsync(
                scheduledLessonId,
                excludeList.Any() ? excludeList : null);

            // Get period names for display
            var periods = await _context.Periods.ToDictionaryAsync(p => p.Id, p => p.Name);

            var result = availableSlots.Select(s => new
            {
                dayOfWeek = s.DayOfWeek.ToString(),
                periodId = s.PeriodId,
                periodName = periods.GetValueOrDefault(s.PeriodId, $"Period {s.PeriodId}"),
                roomId = s.RoomId,
                roomName = s.RoomName,
                qualityScore = s.QualityScore,
                hasHardViolations = s.HasHardConstraintViolations,
                hardViolations = s.HardViolations,
                softViolations = s.SoftViolations,
                isCurrentSlot = s.IsCurrentSlot
            });

            return new JsonResult(new { success = true, slots = result });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get available slots grouped by quality (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetAvailableSlotsGroupedAsync(
        int scheduledLessonId,
        string? excludeSlots = null)
    {
        try
        {
            // Parse exclude slots
            var excludeList = new List<(DayOfWeek Day, int PeriodId)>();
            if (!string.IsNullOrEmpty(excludeSlots))
            {
                var slots = excludeSlots.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var slot in slots)
                {
                    var parts = slot.Split(',');
                    if (parts.Length == 2 &&
                        Enum.TryParse<DayOfWeek>(parts[0], out var day) &&
                        int.TryParse(parts[1], out var periodId))
                    {
                        excludeList.Add((day, periodId));
                    }
                }
            }

            var groupedSlots = await _movementService.GetAvailableSlotsGroupedAsync(
                scheduledLessonId,
                excludeList.Any() ? excludeList : null);

            // Get period names for display
            var periods = await _context.Periods.ToDictionaryAsync(p => p.Id, p => p.Name);

            var formatSlots = (List<AvailableSlot> slots) =>
                slots.Select(s => new
                {
                    dayOfWeek = s.DayOfWeek.ToString(),
                    periodId = s.PeriodId,
                    periodName = periods.GetValueOrDefault(s.PeriodId, $"Period {s.PeriodId}"),
                    roomId = s.RoomId,
                    roomName = s.RoomName,
                    qualityScore = s.QualityScore,
                    hasHardViolations = s.HasHardConstraintViolations,
                    softViolations = s.SoftViolations
                }).ToList();

            return new JsonResult(new
            {
                success = true,
                perfect = formatSlots(groupedSlots.Perfect),
                good = formatSlots(groupedSlots.Good),
                acceptable = formatSlots(groupedSlots.Acceptable),
                poor = formatSlots(groupedSlots.Poor),
                unavailable = formatSlots(groupedSlots.Unavailable),
                totalAvailable = groupedSlots.TotalAvailableCount
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Find swap chains to move a lesson to target slot (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetFindSwapChainsAsync(
        int scheduledLessonId,
        string targetDay,
        int targetPeriodId,
        int? targetRoomId = null,
        int maxDepth = 3,
        int timeoutSeconds = 30,
        string? excludeSlots = null)
    {
        try
        {
            // Parse target day
            if (!Enum.TryParse<DayOfWeek>(targetDay, out var day))
            {
                return new JsonResult(new { success = false, error = "Invalid day of week" });
            }

            // Parse exclude slots
            var excludeList = new List<(DayOfWeek Day, int PeriodId)>();
            if (!string.IsNullOrEmpty(excludeSlots))
            {
                var slots = excludeSlots.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var slot in slots)
                {
                    var parts = slot.Split(',');
                    if (parts.Length == 2 &&
                        Enum.TryParse<DayOfWeek>(parts[0], out var excludeDay) &&
                        int.TryParse(parts[1], out var periodId))
                    {
                        excludeList.Add((excludeDay, periodId));
                    }
                }
            }

            var config = new SwapChainConfig
            {
                MaxDepth = maxDepth,
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                ExcludeSlots = excludeList
            };

            var swapChains = await _movementService.FindSwapChainsAsync(
                scheduledLessonId,
                day,
                targetPeriodId,
                targetRoomId,
                config);

            var result = swapChains.Select(chain => new
            {
                isValid = chain.IsValid,
                totalMoves = chain.TotalMoves,
                qualityScore = chain.QualityScore,
                errorMessage = chain.ErrorMessage,
                steps = chain.Steps.Select(step => new
                {
                    stepNumber = step.StepNumber,
                    scheduledLessonId = step.ScheduledLessonId,
                    lessonDescription = step.LessonDescription,
                    from = new
                    {
                        day = step.FromDay.ToString(),
                        periodId = step.FromPeriodId,
                        periodName = step.FromPeriodName,
                        roomId = step.FromRoomId,
                        roomName = step.FromRoomName
                    },
                    to = new
                    {
                        day = step.ToDay.ToString(),
                        periodId = step.ToPeriodId,
                        periodName = step.ToPeriodName,
                        roomId = step.ToRoomId,
                        roomName = step.ToRoomName
                    }
                })
            });

            return new JsonResult(new { success = true, swapChains = result });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Execute a swap chain (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostExecuteSwapChainAsync([FromBody] SwapChainRequest request)
    {
        try
        {
            // Reconstruct the swap chain from the request
            var swapChain = new SwapChain
            {
                Steps = request.Steps.Select(s => new MoveStep
                {
                    ScheduledLessonId = s.ScheduledLessonId,
                    LessonDescription = s.LessonDescription,
                    FromDay = Enum.Parse<DayOfWeek>(s.FromDay),
                    FromPeriodId = s.FromPeriodId,
                    FromPeriodName = s.FromPeriodName,
                    FromRoomId = s.FromRoomId,
                    FromRoomName = s.FromRoomName,
                    ToDay = Enum.Parse<DayOfWeek>(s.ToDay),
                    ToPeriodId = s.ToPeriodId,
                    ToPeriodName = s.ToPeriodName,
                    ToRoomId = s.ToRoomId,
                    ToRoomName = s.ToRoomName,
                    StepNumber = s.StepNumber
                }).ToList(),
                IsValid = true
            };

            var result = await _movementService.ExecuteSwapChainAsync(swapChain, request.Force);

            if (result.Success)
            {
                return new JsonResult(new
                {
                    success = true,
                    message = $"Successfully executed {result.TotalMoves} move(s)",
                    totalMoves = result.TotalMoves
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Validate if a move is possible (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetValidateMoveAsync(
        int scheduledLessonId,
        string targetDay,
        int targetPeriodId,
        int? targetRoomId = null)
    {
        try
        {
            // Parse target day
            if (!Enum.TryParse<DayOfWeek>(targetDay, out var day))
            {
                return new JsonResult(new { success = false, error = "Invalid day of week" });
            }

            var validation = await _movementService.ValidateMoveAsync(
                scheduledLessonId,
                day,
                targetPeriodId,
                targetRoomId);

            return new JsonResult(new
            {
                success = true,
                isValid = validation.IsValid,
                isLocked = validation.IsLocked,
                hasHardViolations = validation.HasHardConstraintViolations,
                qualityScore = validation.QualityScore,
                errorMessage = validation.ErrorMessage,
                hardViolations = validation.HardViolations,
                softViolations = validation.SoftViolations
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Determine movement strategy (direct move vs requires swaps) (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetMovementStrategyAsync(
        int scheduledLessonId,
        string targetDay,
        int targetPeriodId,
        int? targetRoomId = null)
    {
        try
        {
            // Parse target day
            if (!Enum.TryParse<DayOfWeek>(targetDay, out var day))
            {
                return new JsonResult(new { success = false, error = "Invalid day of week" });
            }

            var strategy = await _movementService.DetermineMovementStrategyAsync(
                scheduledLessonId,
                day,
                targetPeriodId,
                targetRoomId);

            return new JsonResult(new
            {
                success = true,
                strategyType = strategy.StrategyType.ToString(),
                canMoveDirectly = strategy.CanMoveDirectly,
                requiresSwaps = strategy.RequiresSwaps,
                errorMessage = strategy.ErrorMessage,
                validation = strategy.Validation != null ? new
                {
                    isValid = strategy.Validation.IsValid,
                    isLocked = strategy.Validation.IsLocked,
                    hasHardViolations = strategy.Validation.HasHardConstraintViolations,
                    qualityScore = strategy.Validation.QualityScore,
                    hardViolations = strategy.Validation.HardViolations,
                    softViolations = strategy.Validation.SoftViolations
                } : null
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    // Request model for swap chain execution
    public class SwapChainRequest
    {
        public List<MoveStepDto> Steps { get; set; } = new();
        public bool Force { get; set; }
    }

    public class MoveStepDto
    {
        public int ScheduledLessonId { get; set; }
        public string LessonDescription { get; set; } = "";
        public string FromDay { get; set; } = "";
        public int FromPeriodId { get; set; }
        public string FromPeriodName { get; set; } = "";
        public int? FromRoomId { get; set; }
        public string? FromRoomName { get; set; }
        public string ToDay { get; set; } = "";
        public int ToPeriodId { get; set; }
        public string ToPeriodName { get; set; } = "";
        public int? ToRoomId { get; set; }
        public string? ToRoomName { get; set; }
        public int StepNumber { get; set; }
    }

    // ==================== LESSON DEFINITION EDITING API ENDPOINTS ====================

    /// <summary>
    /// Get lesson data for editing (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetLessonDefinitionAsync(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.LessonTeachers)
                .ThenInclude(lt => lt.Teacher)
            .Include(l => l.LessonSubjects)
                .ThenInclude(ls => ls.Subject)
            .Include(l => l.LessonClasses)
                .ThenInclude(lc => lc.Class)
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
        {
            return new JsonResult(new { success = false, message = "Lesson not found" });
        }

        var result = new
        {
            success = true,
            lesson = new
            {
                lesson.Id,
                lesson.Duration,
                lesson.FrequencyPerWeek,
                lesson.SpecialRequirements,
                lesson.Description,
                lesson.IsActive,
                teachers = lesson.LessonTeachers.Select(lt => new
                {
                    lt.Id,
                    lt.TeacherId,
                    teacherName = lt.Teacher?.FullName,
                    lt.IsLead,
                    lt.Order,
                    lt.WorkloadPercentage,
                    lt.Role
                }).OrderBy(t => t.Order).ToList(),
                subjects = lesson.LessonSubjects.Select(ls => new
                {
                    ls.Id,
                    ls.SubjectId,
                    subjectName = ls.Subject?.Name,
                    subjectCode = ls.Subject?.Code,
                    ls.IsPrimary,
                    ls.Order
                }).OrderBy(s => s.Order).ToList(),
                classes = lesson.LessonClasses.Select(lc => new
                {
                    lc.Id,
                    lc.ClassId,
                    className = lc.Class?.Name,
                    lc.IsPrimary,
                    lc.Order
                }).OrderBy(c => c.Order).ToList()
            }
        };

        return new JsonResult(result);
    }

    /// <summary>
    /// Update lesson basic properties (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostUpdateLessonDefinitionAsync([FromBody] LessonDefinitionUpdateModel model)
    {
        if (model == null)
        {
            return new JsonResult(new { success = false, message = "Invalid data" });
        }

        var lesson = await _context.Lessons.FindAsync(model.Id);
        if (lesson == null)
        {
            return new JsonResult(new { success = false, message = "Lesson not found" });
        }

        lesson.Duration = model.Duration;
        lesson.FrequencyPerWeek = model.FrequencyPerWeek;
        lesson.SpecialRequirements = model.SpecialRequirements;
        lesson.Description = model.Description;
        lesson.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true, message = "Lesson updated successfully" });
    }

    /// <summary>
    /// Update lesson teachers (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostUpdateLessonTeachersAsync([FromBody] LessonTeachersUpdateModel model)
    {
        if (model == null)
        {
            return new JsonResult(new { success = false, message = "Invalid data" });
        }

        var lesson = await _context.Lessons
            .Include(l => l.LessonTeachers)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
        {
            return new JsonResult(new { success = false, message = "Lesson not found" });
        }

        // Remove existing teachers
        _context.LessonTeachers.RemoveRange(lesson.LessonTeachers);

        // Add new teachers
        if (model.TeacherIds != null && model.TeacherIds.Any())
        {
            int order = 0;
            foreach (var teacherId in model.TeacherIds)
            {
                _context.LessonTeachers.Add(new LessonTeacher
                {
                    LessonId = model.LessonId,
                    TeacherId = teacherId,
                    IsLead = order == 0, // First teacher is lead
                    Order = order++
                });
            }
        }

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true, message = "Teachers updated successfully" });
    }

    /// <summary>
    /// Update lesson subjects (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostUpdateLessonSubjectsAsync([FromBody] LessonSubjectsUpdateModel model)
    {
        if (model == null)
        {
            return new JsonResult(new { success = false, message = "Invalid data" });
        }

        var lesson = await _context.Lessons
            .Include(l => l.LessonSubjects)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
        {
            return new JsonResult(new { success = false, message = "Lesson not found" });
        }

        // Remove existing subjects
        _context.LessonSubjects.RemoveRange(lesson.LessonSubjects);

        // Add new subjects
        if (model.SubjectIds != null && model.SubjectIds.Any())
        {
            int order = 0;
            foreach (var subjectId in model.SubjectIds)
            {
                _context.LessonSubjects.Add(new LessonSubject
                {
                    LessonId = model.LessonId,
                    SubjectId = subjectId,
                    IsPrimary = order == 0, // First subject is primary
                    Order = order++
                });
            }
        }

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true, message = "Subjects updated successfully" });
    }

    /// <summary>
    /// Update lesson classes (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostUpdateLessonClassesAsync([FromBody] LessonClassesUpdateModel model)
    {
        if (model == null)
        {
            return new JsonResult(new { success = false, message = "Invalid data" });
        }

        var lesson = await _context.Lessons
            .Include(l => l.LessonClasses)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
        {
            return new JsonResult(new { success = false, message = "Lesson not found" });
        }

        // Remove existing classes
        _context.LessonClasses.RemoveRange(lesson.LessonClasses);

        // Add new classes
        if (model.ClassIds != null && model.ClassIds.Any())
        {
            int order = 0;
            foreach (var classId in model.ClassIds)
            {
                _context.LessonClasses.Add(new LessonClass
                {
                    LessonId = model.LessonId,
                    ClassId = classId,
                    IsPrimary = order == 0, // First class is primary
                    Order = order++
                });
            }
        }

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true, message = "Classes updated successfully" });
    }

    // Models for lesson definition editing API requests
    public class LessonDefinitionUpdateModel
    {
        public int Id { get; set; }
        public int Duration { get; set; }
        public int FrequencyPerWeek { get; set; }
        public string? SpecialRequirements { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class LessonTeachersUpdateModel
    {
        public int LessonId { get; set; }
        public List<int>? TeacherIds { get; set; }
    }

    public class LessonSubjectsUpdateModel
    {
        public int LessonId { get; set; }
        public List<int>? SubjectIds { get; set; }
    }

    public class LessonClassesUpdateModel
    {
        public int LessonId { get; set; }
        public List<int>? ClassIds { get; set; }
    }

    // ==================== ROOM ASSIGNMENT API ENDPOINTS ====================

    /// <summary>
    /// Get room assignments for a scheduled lesson (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetRoomAssignmentsAsync(int scheduledLessonId)
    {
        try
        {
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Teacher)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Class)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.Room)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.RoomAssignments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
            {
                return new JsonResult(new { success = false, error = "Scheduled lesson not found" });
            }

            var lessonAssignments = scheduledLesson.Lesson?.LessonAssignments?.Select(la => new
            {
                la.Id,
                teacherId = la.TeacherId,
                teacherName = la.Teacher?.FullName,
                subjectId = la.SubjectId,
                subjectName = la.Subject?.Name,
                subjectCode = la.Subject?.Code,
                classId = la.ClassId,
                className = la.Class?.Name,
                la.Notes,
                displayText = BuildAssignmentDisplayText(la)
            }).OrderBy(la => la.Id).ToList();

            var roomAssignments = scheduledLesson.ScheduledLessonRooms.Select(slr => new
            {
                scheduledLessonRoomId = slr.Id,
                roomId = slr.RoomId,
                roomNumber = slr.Room?.RoomNumber,
                roomName = slr.Room?.Name,
                assignedLessonAssignmentIds = slr.RoomAssignments.Select(ra => ra.LessonAssignmentId).ToList()
            }).ToList();

            return new JsonResult(new
            {
                success = true,
                hasLessonAssignments = lessonAssignments?.Any() ?? false,
                hasMultipleRooms = scheduledLesson.ScheduledLessonRooms.Count > 1,
                lessonAssignments,
                roomAssignments
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    private static string BuildAssignmentDisplayText(LessonAssignment la)
    {
        var parts = new List<string>();
        if (la.Teacher != null) parts.Add(la.Teacher.FullName);
        if (la.Subject != null) parts.Add(la.Subject.Code ?? la.Subject.Name);
        if (la.Class != null) parts.Add(la.Class.Name);
        return string.Join(" - ", parts);
    }

    /// <summary>
    /// Get lesson assignments for a lesson (for Schedule Lesson modal)
    /// </summary>
    public async Task<IActionResult> OnGetLessonAssignmentsAsync(int lessonId)
    {
        try
        {
            var lesson = await _context.Lessons
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Subject)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Class)
                .AsSplitQuery()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return new JsonResult(new { success = false, error = "Lesson not found" });
            }

            var lessonAssignments = lesson.LessonAssignments?.Select(la => new
            {
                la.Id,
                teacherId = la.TeacherId,
                teacherName = la.Teacher?.FullName,
                subjectId = la.SubjectId,
                subjectName = la.Subject?.Name,
                subjectCode = la.Subject?.Code,
                classId = la.ClassId,
                className = la.Class?.Name,
                la.Notes,
                displayText = BuildAssignmentDisplayText(la)
            }).OrderBy(la => la.Id).ToList();

            return new JsonResult(new
            {
                success = true,
                hasLessonAssignments = lessonAssignments?.Any() ?? false,
                lessonAssignments
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Update room assignments for a scheduled lesson (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostUpdateRoomAssignmentsAsync([FromBody] RoomAssignmentsUpdateModel model)
    {
        try
        {
            if (model == null || model.ScheduledLessonId <= 0)
            {
                return new JsonResult(new { success = false, error = "Invalid request data" });
            }

            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.RoomAssignments)
                .FirstOrDefaultAsync(sl => sl.Id == model.ScheduledLessonId);

            if (scheduledLesson == null)
            {
                return new JsonResult(new { success = false, error = "Scheduled lesson not found" });
            }

            // Clear existing room assignments
            foreach (var room in scheduledLesson.ScheduledLessonRooms)
            {
                _context.ScheduledLessonRoomAssignments.RemoveRange(room.RoomAssignments);
            }

            // Add new room assignments
            if (model.RoomAssignments != null)
            {
                foreach (var assignment in model.RoomAssignments)
                {
                    var scheduledLessonRoom = scheduledLesson.ScheduledLessonRooms
                        .FirstOrDefault(slr => slr.Id == assignment.ScheduledLessonRoomId);

                    if (scheduledLessonRoom != null && assignment.LessonAssignmentIds != null)
                    {
                        foreach (var lessonAssignmentId in assignment.LessonAssignmentIds)
                        {
                            _context.ScheduledLessonRoomAssignments.Add(new ScheduledLessonRoomAssignment
                            {
                                ScheduledLessonRoomId = scheduledLessonRoom.Id,
                                LessonAssignmentId = lessonAssignmentId
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Validate coverage
            var lessonAssignments = await _context.LessonAssignments
                .Where(la => la.LessonId == scheduledLesson.LessonId)
                .Include(la => la.Teacher)
                .Include(la => la.Subject)
                .Include(la => la.Class)
                .ToListAsync();

            var assignedLessonAssignmentIds = model.RoomAssignments?
                .SelectMany(ra => ra.LessonAssignmentIds ?? new List<int>())
                .Distinct()
                .ToList() ?? new List<int>();

            var uncoveredAssignments = lessonAssignments
                .Where(la => !assignedLessonAssignmentIds.Contains(la.Id))
                .Select(la => BuildAssignmentDisplayText(la))
                .ToList();

            if (uncoveredAssignments.Any() && assignedLessonAssignmentIds.Any())
            {
                return new JsonResult(new
                {
                    success = true,
                    message = "Room assignments updated successfully",
                    warnings = new List<string>
                    {
                        $"Warning: The following combinations are not assigned to any room: {string.Join(", ", uncoveredAssignments)}"
                    }
                });
            }

            return new JsonResult(new { success = true, message = "Room assignments updated successfully" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public class RoomAssignmentsUpdateModel
    {
        public int ScheduledLessonId { get; set; }
        public List<RoomAssignmentItem>? RoomAssignments { get; set; }
    }

    public class RoomAssignmentItem
    {
        public int ScheduledLessonRoomId { get; set; }
        public List<int>? LessonAssignmentIds { get; set; }
    }

    /// <summary>
    /// Validate room assignments cover all lesson participants (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetValidateRoomAssignmentsAsync(int scheduledLessonId)
    {
        try
        {
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Teacher)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Class)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.RoomAssignments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
            {
                return new JsonResult(new { success = false, error = "Scheduled lesson not found" });
            }

            var lessonAssignments = scheduledLesson.Lesson?.LessonAssignments?.ToList() ?? new List<LessonAssignment>();
            var roomAssignments = scheduledLesson.ScheduledLessonRooms
                .SelectMany(slr => slr.RoomAssignments)
                .Select(ra => ra.LessonAssignmentId)
                .Distinct()
                .ToList();

            // No validation needed if no lesson assignments defined
            if (!lessonAssignments.Any())
            {
                return new JsonResult(new
                {
                    success = true,
                    isValid = true,
                    message = "No lesson assignments defined - all participants will show in all rooms"
                });
            }

            // No validation needed if no room assignments defined
            if (!roomAssignments.Any())
            {
                return new JsonResult(new
                {
                    success = true,
                    isValid = true,
                    message = "No room assignments defined - all participants will show in all rooms"
                });
            }

            // Check for uncovered assignments
            var uncoveredAssignments = lessonAssignments
                .Where(la => !roomAssignments.Contains(la.Id))
                .Select(la => BuildAssignmentDisplayText(la))
                .ToList();

            if (uncoveredAssignments.Any())
            {
                return new JsonResult(new
                {
                    success = true,
                    isValid = false,
                    warnings = new List<string>
                    {
                        $"The following combinations are not assigned to any room: {string.Join(", ", uncoveredAssignments)}"
                    }
                });
            }

            return new JsonResult(new
            {
                success = true,
                isValid = true,
                message = "All lesson assignments are covered by room assignments"
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public class LessonTimetableInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimetableStatus Status { get; set; }
    }
}
