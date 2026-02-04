using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Timetable> Timetables { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Timetables = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.Term)
            .Include(t => t.ScheduledLessons)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(int id)
    {
        try
        {
            var timetable = await _context.Timetables.FindAsync(id);
            if (timetable == null)
            {
                ErrorMessage = "Timetable not found.";
                return RedirectToPage();
            }

            timetable.Status = TimetableStatus.Published;
            timetable.PublishedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            SuccessMessage = $"Timetable '{timetable.Name}' has been published.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error publishing timetable: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnpublishAsync(int id)
    {
        try
        {
            var timetable = await _context.Timetables.FindAsync(id);
            if (timetable == null)
            {
                ErrorMessage = "Timetable not found.";
                return RedirectToPage();
            }

            timetable.Status = TimetableStatus.Draft;
            await _context.SaveChangesAsync();

            SuccessMessage = $"Timetable '{timetable.Name}' has been unpublished.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error unpublishing timetable: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var timetable = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timetable == null)
            {
                ErrorMessage = "Timetable not found.";
                return RedirectToPage();
            }

            var timetableName = timetable.Name;

            _context.Timetables.Remove(timetable);
            await _context.SaveChangesAsync();

            SuccessMessage = $"Timetable '{timetableName}' has been deleted.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting timetable: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRenameAsync(int id, string newName)
    {
        try
        {
            var timetable = await _context.Timetables.FindAsync(id);

            if (timetable == null)
            {
                ErrorMessage = "Timetable not found.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                ErrorMessage = "Timetable name cannot be empty.";
                return RedirectToPage();
            }

            var oldName = timetable.Name;
            timetable.Name = newName.Trim();
            await _context.SaveChangesAsync();

            SuccessMessage = $"Timetable renamed from '{oldName}' to '{timetable.Name}'.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error renaming timetable: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCopyAsync(int id, string newName)
    {
        try
        {
            // Step 1: Load source timetable with scheduled lessons and room assignments
            var sourceTimetable = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                    .ThenInclude(sl => sl.ScheduledLessonRooms)
                        .ThenInclude(slr => slr.RoomAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (sourceTimetable == null)
            {
                ErrorMessage = "Timetable not found.";
                return RedirectToPage();
            }

            // Use provided name or generate default
            var timetableName = string.IsNullOrWhiteSpace(newName)
                ? $"{sourceTimetable.Name} (Copy)"
                : newName.Trim();

            // Step 2: Create new timetable
            var newTimetable = new Timetable
            {
                Name = timetableName,
                SchoolYearId = sourceTimetable.SchoolYearId,
                TermId = sourceTimetable.TermId,
                CreatedDate = DateTime.Now,
                Status = TimetableStatus.Draft,
                PublishedDate = null,
                Notes = $"Copy of '{sourceTimetable.Name}' created on {DateTime.Now:yyyy-MM-dd HH:mm}. Lessons were duplicated for isolation.",
                GenerationDurationMs = null
            };

            _context.Timetables.Add(newTimetable);
            await _context.SaveChangesAsync();

            // Step 3: Copy all scheduled lessons first (still pointing to old lesson IDs)
            var newScheduledLessons = new List<ScheduledLesson>();
            var scheduledLessonRoomIdMapping = new Dictionary<int, int>(); // oldRoomId -> newRoomId

            foreach (var sourceScheduledLesson in sourceTimetable.ScheduledLessons)
            {
                var newScheduledLesson = new ScheduledLesson
                {
                    LessonId = sourceScheduledLesson.LessonId, // Still pointing to old lesson ID
                    DayOfWeek = sourceScheduledLesson.DayOfWeek,
                    PeriodId = sourceScheduledLesson.PeriodId,
                    RoomId = sourceScheduledLesson.RoomId,
                    WeekNumber = sourceScheduledLesson.WeekNumber,
                    TimetableId = newTimetable.Id,
                    IsLocked = sourceScheduledLesson.IsLocked
                };

                _context.ScheduledLessons.Add(newScheduledLesson);
                await _context.SaveChangesAsync();

                // Copy room assignments for multi-room scenarios
                foreach (var sourceRoom in sourceScheduledLesson.ScheduledLessonRooms)
                {
                    var newRoom = new ScheduledLessonRoom
                    {
                        ScheduledLessonId = newScheduledLesson.Id,
                        RoomId = sourceRoom.RoomId,
                        PrimaryTeacherIdForRoom = sourceRoom.PrimaryTeacherIdForRoom,
                        Notes = sourceRoom.Notes,
                        StudentCount = sourceRoom.StudentCount
                    };
                    _context.Add(newRoom);
                    await _context.SaveChangesAsync();

                    // Store room mapping for room assignments later
                    scheduledLessonRoomIdMapping[sourceRoom.Id] = newRoom.Id;
                }

                newScheduledLessons.Add(newScheduledLesson);
            }
            await _context.SaveChangesAsync();

            // Step 4: Loop through new scheduled lessons and replicate lessons
            var lessonIdMapping = new Dictionary<int, int>(); // oldId -> newId
            var lessonAssignmentIdMapping = new Dictionary<int, int>(); // oldAssignmentId -> newAssignmentId
            var replicatedLessonsCount = 0;

            foreach (var scNew in newScheduledLessons)
            {
                var lOldId = scNew.LessonId;

                // Check if we've already created a replica for this lesson
                if (!lessonIdMapping.ContainsKey(lOldId))
                {
                    // Load the old lesson with all associations
                    var lOld = await _context.Lessons
                        .Include(l => l.LessonTeachers)
                        .Include(l => l.LessonClasses)
                        .Include(l => l.LessonSubjects)
                        .Include(l => l.LessonAssignments)
                        .FirstOrDefaultAsync(l => l.Id == lOldId);

                    if (lOld == null)
                    {
                        // Skip if lesson not found (shouldn't happen, but safety check)
                        continue;
                    }

                    // Create replica of the lesson (lNew)
                    var lNew = new Lesson
                    {
                        Duration = lOld.Duration,
                        FrequencyPerWeek = lOld.FrequencyPerWeek,
                        ClassPeriodsPerWeek = lOld.ClassPeriodsPerWeek,
                        TeacherPeriodsPerWeek = lOld.TeacherPeriodsPerWeek,
                        NumberOfStudents = lOld.NumberOfStudents,
                        MaleStudents = lOld.MaleStudents,
                        FemaleStudents = lOld.FemaleStudents,
                        WeekValue = lOld.WeekValue,
                        YearValue = lOld.YearValue,
                        FromDate = lOld.FromDate,
                        ToDate = lOld.ToDate,
                        PartitionNumber = lOld.PartitionNumber,
                        WeeklyPeriodsInTerms = lOld.WeeklyPeriodsInTerms,
                        StudentGroup = lOld.StudentGroup,
                        HomeRoom = lOld.HomeRoom,
                        RequiredRoomType = lOld.RequiredRoomType,
                        MinDoublePeriods = lOld.MinDoublePeriods,
                        MaxDoublePeriods = lOld.MaxDoublePeriods,
                        BlockSize = lOld.BlockSize,
                        Priority = lOld.Priority,
                        ConsecutiveSubjectsClass = lOld.ConsecutiveSubjectsClass,
                        ConsecutiveSubjectsTeacher = lOld.ConsecutiveSubjectsTeacher,
                        Codes = lOld.Codes,
                        SpecialRequirements = lOld.SpecialRequirements,
                        Description = lOld.Description,
                        ForegroundColor = lOld.ForegroundColor,
                        BackgroundColor = lOld.BackgroundColor,
                        IsActive = lOld.IsActive
                    };

                    _context.Lessons.Add(lNew);
                    await _context.SaveChangesAsync();

                    // Copy LessonTeachers
                    foreach (var lt in lOld.LessonTeachers)
                    {
                        _context.LessonTeachers.Add(new LessonTeacher
                        {
                            LessonId = lNew.Id,
                            TeacherId = lt.TeacherId,
                            IsLead = lt.IsLead,
                            Order = lt.Order,
                            WorkloadPercentage = lt.WorkloadPercentage,
                            Role = lt.Role
                        });
                    }

                    // Copy LessonClasses
                    foreach (var lc in lOld.LessonClasses)
                    {
                        _context.LessonClasses.Add(new LessonClass
                        {
                            LessonId = lNew.Id,
                            ClassId = lc.ClassId,
                            IsPrimary = lc.IsPrimary,
                            Order = lc.Order
                        });
                    }

                    // Copy LessonSubjects
                    foreach (var ls in lOld.LessonSubjects)
                    {
                        _context.LessonSubjects.Add(new LessonSubject
                        {
                            LessonId = lNew.Id,
                            SubjectId = ls.SubjectId,
                            IsPrimary = ls.IsPrimary,
                            Order = ls.Order
                        });
                    }

                    await _context.SaveChangesAsync();

                    // Copy LessonAssignments (teacher-subject-class combinations)
                    foreach (var la in lOld.LessonAssignments)
                    {
                        var newAssignment = new LessonAssignment
                        {
                            LessonId = lNew.Id,
                            TeacherId = la.TeacherId,
                            SubjectId = la.SubjectId,
                            ClassId = la.ClassId,
                            Notes = la.Notes,
                            Order = la.Order
                        };
                        _context.LessonAssignments.Add(newAssignment);
                        await _context.SaveChangesAsync();

                        // Store assignment mapping for room assignments later
                        lessonAssignmentIdMapping[la.Id] = newAssignment.Id;
                    }

                    // Store the mapping
                    lessonIdMapping[lOldId] = lNew.Id;
                    replicatedLessonsCount++;
                }

                // Update scNew to point to the new lesson ID
                scNew.LessonId = lessonIdMapping[lOldId];
            }

            await _context.SaveChangesAsync();

            // Step 5: Copy ScheduledLessonRoomAssignments using the mappings
            var copiedRoomAssignmentsCount = 0;
            foreach (var sourceScheduledLesson in sourceTimetable.ScheduledLessons)
            {
                foreach (var sourceRoom in sourceScheduledLesson.ScheduledLessonRooms)
                {
                    foreach (var sourceRoomAssignment in sourceRoom.RoomAssignments)
                    {
                        // Check if we have mappings for both room and lesson assignment
                        if (scheduledLessonRoomIdMapping.TryGetValue(sourceRoom.Id, out var newRoomId) &&
                            lessonAssignmentIdMapping.TryGetValue(sourceRoomAssignment.LessonAssignmentId, out var newAssignmentId))
                        {
                            var newRoomAssignment = new ScheduledLessonRoomAssignment
                            {
                                ScheduledLessonRoomId = newRoomId,
                                LessonAssignmentId = newAssignmentId
                            };
                            _context.ScheduledLessonRoomAssignments.Add(newRoomAssignment);
                            copiedRoomAssignmentsCount++;
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();

            // Step 6: Copy break supervision duties
            var sourceSupervisionDuties = await _context.BreakSupervisionDuties
                .Where(d => d.TimetableId == id)
                .ToListAsync();

            var copiedDutiesCount = 0;
            foreach (var sourceDuty in sourceSupervisionDuties)
            {
                var newDuty = new BreakSupervisionDuty
                {
                    RoomId = sourceDuty.RoomId,
                    TeacherId = sourceDuty.TeacherId,
                    DayOfWeek = sourceDuty.DayOfWeek,
                    PeriodNumber = sourceDuty.PeriodNumber,
                    Points = sourceDuty.Points,
                    Notes = sourceDuty.Notes,
                    IsActive = sourceDuty.IsActive,
                    TimetableId = newTimetable.Id
                };
                _context.BreakSupervisionDuties.Add(newDuty);
                copiedDutiesCount++;
            }
            await _context.SaveChangesAsync();

            SuccessMessage = $"Created unpublished copy '{newTimetable.Name}' with {replicatedLessonsCount} duplicated lessons, {newScheduledLessons.Count} scheduled entries, {copiedRoomAssignmentsCount} room assignments, and {copiedDutiesCount} supervision duties.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error copying timetable: {ex.Message}";
        }

        return RedirectToPage();
    }
}
