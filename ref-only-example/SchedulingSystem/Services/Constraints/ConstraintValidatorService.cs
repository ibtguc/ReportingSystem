using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Services.Constraints;

/// <summary>
/// Centralized service for constraint validation in the scheduling system.
/// Implements all constraint checking logic in one place for consistency.
/// </summary>
public class ConstraintValidatorService : IConstraintValidator
{
    private readonly ApplicationDbContext _context;

    public ConstraintValidatorService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Main Validation Methods

    public async Task<ValidationResult> ValidateHardConstraintsAsync(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null)
    {
        context ??= new ValidationContext();
        var result = new ValidationResult { IsValid = true };

        var hardConstraints = ConstraintDefinitions.GetHardConstraints();

        foreach (var constraint in hardConstraints)
        {
            if (!context.ShouldCheckConstraint(constraint.Code))
                continue;

            var constraintResult = await ValidateConstraintAsync(
                constraint.Code,
                lesson,
                existingSchedule,
                context);

            if (!constraintResult.Satisfied)
            {
                result.HardViolations.Add(new ConstraintViolation
                {
                    ConstraintCode = constraint.Code,
                    ConstraintName = constraint.Name,
                    Type = ConstraintType.Hard,
                    Message = constraintResult.Message,
                    Details = constraintResult.Details
                });

                // Early exit if requested
                if (context.EarlyExit)
                    break;
            }
        }

        result.IsValid = !result.HasErrors;
        return result;
    }

    public async Task<ValidationResult> ValidateSoftConstraintsAsync(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null)
    {
        context ??= new ValidationContext();
        var result = new ValidationResult { IsValid = true };

        if (!context.IncludeSoftConstraints)
            return result;

        var softConstraints = ConstraintDefinitions.GetSoftConstraints();

        foreach (var constraint in softConstraints)
        {
            if (!context.ShouldCheckConstraint(constraint.Code))
                continue;

            var constraintResult = await ValidateConstraintAsync(
                constraint.Code,
                lesson,
                existingSchedule,
                context);

            if (!constraintResult.Satisfied)
            {
                result.SoftViolations.Add(new ConstraintViolation
                {
                    ConstraintCode = constraint.Code,
                    ConstraintName = constraint.Name,
                    Type = ConstraintType.Soft,
                    Message = constraintResult.Message,
                    Details = constraintResult.Details
                });
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateAllConstraintsAsync(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null)
    {
        context ??= new ValidationContext();

        var hardResult = await ValidateHardConstraintsAsync(lesson, existingSchedule, context);

        // Only check soft constraints if no hard violations (or if not early exit)
        if (!context.EarlyExit || !hardResult.HasErrors)
        {
            var softResult = await ValidateSoftConstraintsAsync(lesson, existingSchedule, context);
            hardResult.Merge(softResult);
        }

        return hardResult;
    }

    public async Task<ConstraintResult> ValidateConstraintAsync(
        string constraintCode,
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null)
    {
        context ??= new ValidationContext();

        return constraintCode.ToUpper() switch
        {
            "HC-1" => await ValidateTeacherDoubleBooking(lesson, existingSchedule, context),
            "HC-2" => await ValidateClassDoubleBooking(lesson, existingSchedule, context),
            "HC-3" => await ValidateRoomDoubleBooking(lesson, existingSchedule, context),
            "HC-4" => await ValidateTeacherAbsoluteUnavailability(lesson, existingSchedule, context),
            "HC-5" => await ValidateClassAbsoluteUnavailability(lesson, existingSchedule, context),
            "HC-6" => await ValidateRoomAbsoluteUnavailability(lesson, existingSchedule, context),
            "HC-7" => await ValidateSubjectAbsoluteUnavailability(lesson, existingSchedule, context),
            "HC-8" => await ValidateTeacherMaxConsecutivePeriods(lesson, existingSchedule, context),
            "HC-9" => await ValidateClassMaxConsecutiveSameSubject(lesson, existingSchedule, context),
            "HC-10" => await ValidateLockedLesson(lesson, existingSchedule, context),
            "HC-11" => await ValidateTeacherMaxPeriodsPerDay(lesson, existingSchedule, context),
            "HC-12" => await ValidateClassMaxPeriodsPerDay(lesson, existingSchedule, context),
            "SC-1" => await ValidateTeacherPreferenceUnavailability(lesson, existingSchedule, context),
            "SC-2" => await ValidateClassPreferenceUnavailability(lesson, existingSchedule, context),
            "SC-3" => await ValidateSubjectTimePreferences(lesson, existingSchedule, context),
            "SC-4" => await ValidateRoomPreferences(lesson, existingSchedule, context),
            "SC-5" => await ValidateTeacherLunchBreak(lesson, existingSchedule, context),
            "SC-6" => await ValidateClassLunchBreak(lesson, existingSchedule, context),
            "SC-7" => await ValidateNoGapsInClassSchedule(lesson, existingSchedule, context),
            "SC-8" => await ValidateRoomTypePreferenceMismatch(lesson, existingSchedule, context),
            "SC-9" => await ValidateSubjectPreferredRoom(lesson, existingSchedule, context),
            "SC-10" => await ValidateTeacherMinPeriodsPerDay(lesson, existingSchedule, context),
            "SC-11" => await ValidateClassMinPeriodsPerDay(lesson, existingSchedule, context),
            _ => new ConstraintResult
            {
                ConstraintCode = constraintCode,
                Satisfied = true,
                Message = $"Unknown constraint code: {constraintCode}"
            }
        };
    }

    public async Task<TimetableValidationResult> ValidateTimetableAsync(
        int timetableId,
        ValidationContext? context = null)
    {
        context ??= new ValidationContext();

        var result = new TimetableValidationResult { IsValid = true };

        // Load all scheduled lessons for this timetable
        var scheduledLessons = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Where(sl => sl.TimetableId == timetableId)
            .ToListAsync();

        result.TotalLessons = scheduledLessons.Count;

        // Validate each lesson
        foreach (var lesson in scheduledLessons)
        {
            var otherLessons = scheduledLessons.Where(sl => sl.Id != lesson.Id).ToList();
            var validationResult = await ValidateAllConstraintsAsync(lesson, otherLessons, context);

            if (validationResult.HasErrors || validationResult.HasWarnings)
            {
                result.Conflicts.Add(new TimetableConflict
                {
                    ScheduledLessonId = lesson.Id,
                    Type = validationResult.HasErrors ? ConflictSeverity.Error : ConflictSeverity.Warning,
                    Messages = validationResult.HasErrors
                        ? validationResult.GetErrorMessages()
                        : validationResult.GetWarningMessages(),
                    ConstraintCodes = validationResult.HasErrors
                        ? validationResult.HardViolations.Select(v => v.ConstraintCode).ToList()
                        : validationResult.SoftViolations.Select(v => v.ConstraintCode).ToList()
                });
            }
        }

        result.IsValid = result.LessonsWithErrors == 0;
        return result;
    }

    public async Task<bool> CanScheduleAtAsync(
        int lessonId,
        DayOfWeek dayOfWeek,
        int periodId,
        int? roomId,
        List<ScheduledLesson> existingSchedule)
    {
        // Create temporary scheduled lesson for validation
        var lesson = await _context.Lessons
            .Include(l => l.LessonTeachers)
                .ThenInclude(lt => lt.Teacher)
            .Include(l => l.LessonClasses)
                .ThenInclude(lc => lc.Class)
            .Include(l => l.LessonSubjects)
                .ThenInclude(ls => ls.Subject)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return false;

        var scheduledLesson = new ScheduledLesson
        {
            Id = 0, // Temporary ID
            LessonId = lessonId,
            Lesson = lesson,
            DayOfWeek = dayOfWeek,
            PeriodId = periodId,
            RoomId = roomId
        };

        var context = new ValidationContext
        {
            IncludeSoftConstraints = false,
            EarlyExit = true
        };

        var result = await ValidateHardConstraintsAsync(scheduledLesson, existingSchedule, context);
        return result.IsValid;
    }

    #endregion

    #region Hard Constraint Validators

    /// <summary>
    /// HC-1: Teacher Double-Booking
    /// </summary>
    private async Task<ConstraintResult> ValidateTeacherDoubleBooking(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-1",
            Satisfied = true
        };

        // Load lesson teachers if not already loaded
        var lessonTeachers = lesson.Lesson?.LessonTeachers;
        if (lessonTeachers == null || !lessonTeachers.Any())
        {
            lessonTeachers = await _context.LessonTeachers
                .Include(lt => lt.Teacher)
                .Where(lt => lt.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonTeacher in lessonTeachers)
        {
            // Special case: Skip intern teacher "xy"
            if (ConstraintDefinitions.SpecialCases.IsInternTeacher(lessonTeacher.Teacher))
                continue;

            // Check for conflicts
            var conflicts = existingSchedule
                .Where(sl => sl.Id != lesson.Id)
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.PeriodId == lesson.PeriodId)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonTeachers
                    .Any(lt => lt.TeacherId == lessonTeacher.TeacherId))
                .ToList();

            if (conflicts.Any())
            {
                var conflict = conflicts.First();
                var conflictClass = conflict.Lesson?.LessonClasses?.FirstOrDefault()?.Class?.Name ?? "Unknown";
                var conflictSubject = conflict.Lesson?.LessonSubjects?.FirstOrDefault()?.Subject?.Name ?? "Unknown";

                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.TeacherDoubleBooking.ErrorMessageTemplate,
                    lessonTeacher.Teacher?.FullName ?? "Unknown",
                    conflictClass,
                    conflictSubject);
                result.Details["TeacherId"] = lessonTeacher.TeacherId;
                result.Details["ConflictingLessonId"] = conflict.Id;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-2: Class Double-Booking
    /// </summary>
    private async Task<ConstraintResult> ValidateClassDoubleBooking(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-2",
            Satisfied = true
        };

        // Load lesson classes if not already loaded
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonClass in lessonClasses)
        {
            // Special case: Skip special classes (v-res, Team)
            if (ConstraintDefinitions.SpecialCases.IsSpecialClass(lessonClass.Class))
                continue;

            // Check for conflicts
            var conflicts = existingSchedule
                .Where(sl => sl.Id != lesson.Id)
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.PeriodId == lesson.PeriodId)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonClasses
                    .Any(lc => lc.ClassId == lessonClass.ClassId))
                .ToList();

            if (conflicts.Any())
            {
                var conflict = conflicts.First();
                var conflictSubject = conflict.Lesson?.LessonSubjects?.FirstOrDefault()?.Subject?.Name ?? "Unknown";
                var conflictTeacher = conflict.Lesson?.LessonTeachers?.FirstOrDefault()?.Teacher?.FullName ?? "Unknown";

                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.ClassDoubleBooking.ErrorMessageTemplate,
                    lessonClass.Class?.Name ?? "Unknown",
                    conflictSubject,
                    conflictTeacher);
                result.Details["ClassId"] = lessonClass.ClassId;
                result.Details["ConflictingLessonId"] = conflict.Id;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-3: Room Double-Booking
    /// </summary>
    private async Task<ConstraintResult> ValidateRoomDoubleBooking(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-3",
            Satisfied = true
        };

        // Get all rooms for this lesson (legacy RoomId + new ScheduledLessonRooms)
        var roomIds = new List<int>();

        if (lesson.RoomId.HasValue)
            roomIds.Add(lesson.RoomId.Value);

        if (lesson.ScheduledLessonRooms != null && lesson.ScheduledLessonRooms.Any())
            roomIds.AddRange(lesson.ScheduledLessonRooms.Select(slr => slr.RoomId));

        foreach (var roomId in roomIds.Distinct())
        {
            // Load room to check if it's a special room
            var room = await _context.Rooms.FindAsync(roomId);

            // Special case: Skip special rooms (Teamraum)
            if (ConstraintDefinitions.SpecialCases.IsSpecialRoom(room))
                continue;

            // Check for conflicts
            var conflicts = existingSchedule
                .Where(sl => sl.Id != lesson.Id)
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.PeriodId == lesson.PeriodId)
                .Where(sl => sl.RoomId == roomId ||
                            (sl.ScheduledLessonRooms != null &&
                             sl.ScheduledLessonRooms.Any(slr => slr.RoomId == roomId)))
                .ToList();

            if (conflicts.Any())
            {
                var conflict = conflicts.First();
                var conflictClass = conflict.Lesson?.LessonClasses?.FirstOrDefault()?.Class?.Name ?? "Unknown";
                var conflictSubject = conflict.Lesson?.LessonSubjects?.FirstOrDefault()?.Subject?.Name ?? "Unknown";

                // Use already-loaded room from above (line 408)
                var roomNumber = room?.RoomNumber ?? roomId.ToString();

                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.RoomDoubleBooking.ErrorMessageTemplate,
                    roomNumber,
                    conflictClass,
                    conflictSubject);
                result.Details["RoomId"] = roomId;
                result.Details["ConflictingLessonId"] = conflict.Id;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-4: Teacher Absolute Unavailability
    /// </summary>
    private async Task<ConstraintResult> ValidateTeacherAbsoluteUnavailability(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-4",
            Satisfied = true
        };

        // Load lesson teachers if not already loaded
        var lessonTeachers = lesson.Lesson?.LessonTeachers;
        if (lessonTeachers == null || !lessonTeachers.Any())
        {
            lessonTeachers = await _context.LessonTeachers
                .Include(lt => lt.Teacher)
                .Where(lt => lt.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonTeacher in lessonTeachers)
        {
            // Special case: Skip intern teacher "xy"
            if (ConstraintDefinitions.SpecialCases.IsInternTeacher(lessonTeacher.Teacher))
                continue;

            // Check availability
            var unavailability = await _context.TeacherAvailabilities
                .Where(ta => ta.TeacherId == lessonTeacher.TeacherId)
                .Where(ta => ta.DayOfWeek == lesson.DayOfWeek)
                .Where(ta => ta.PeriodId == lesson.PeriodId)
                .Where(ta => ta.Importance == ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .FirstOrDefaultAsync();

            if (unavailability != null)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.TeacherAbsoluteUnavailability.ErrorMessageTemplate,
                    lessonTeacher.Teacher?.FullName ?? "Unknown",
                    unavailability.Reason ?? "Not specified");
                result.Details["TeacherId"] = lessonTeacher.TeacherId;
                result.Details["AvailabilityId"] = unavailability.Id;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-5: Class Absolute Unavailability
    /// </summary>
    private async Task<ConstraintResult> ValidateClassAbsoluteUnavailability(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-5",
            Satisfied = true
        };

        // Load lesson classes if not already loaded
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonClass in lessonClasses)
        {
            // Special case: Skip special classes (v-res, Team)
            if (ConstraintDefinitions.SpecialCases.IsSpecialClass(lessonClass.Class))
                continue;

            // Check availability
            var unavailability = await _context.ClassAvailabilities
                .Where(ca => ca.ClassId == lessonClass.ClassId)
                .Where(ca => ca.DayOfWeek == lesson.DayOfWeek)
                .Where(ca => ca.PeriodId == lesson.PeriodId)
                .Where(ca => ca.Importance == ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .FirstOrDefaultAsync();

            if (unavailability != null)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.ClassAbsoluteUnavailability.ErrorMessageTemplate,
                    lessonClass.Class?.Name ?? "Unknown");
                result.Details["ClassId"] = lessonClass.ClassId;
                result.Details["AvailabilityId"] = unavailability.Id;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-6: Room Absolute Unavailability
    /// </summary>
    private async Task<ConstraintResult> ValidateRoomAbsoluteUnavailability(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-6",
            Satisfied = true
        };

        // Get all rooms for this lesson
        var roomIds = new List<int>();
        if (lesson.RoomId.HasValue)
            roomIds.Add(lesson.RoomId.Value);
        if (lesson.ScheduledLessonRooms != null && lesson.ScheduledLessonRooms.Any())
            roomIds.AddRange(lesson.ScheduledLessonRooms.Select(slr => slr.RoomId));

        foreach (var roomId in roomIds.Distinct())
        {
            var unavailability = await _context.RoomAvailabilities
                .Where(ra => ra.RoomId == roomId)
                .Where(ra => ra.DayOfWeek == lesson.DayOfWeek)
                .Where(ra => ra.PeriodId == lesson.PeriodId)
                .Where(ra => ra.Importance == ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync();

            if (unavailability != null)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.RoomAbsoluteUnavailability.ErrorMessageTemplate,
                    unavailability.Room?.RoomNumber ?? roomId.ToString());
                result.Details["RoomId"] = roomId;
                result.Details["AvailabilityId"] = unavailability.Id;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-7: Subject Absolute Unavailability
    /// NOTE: Checks ALL subjects for a lesson (lessons can have multiple subjects)
    /// </summary>
    private async Task<ConstraintResult> ValidateSubjectAbsoluteUnavailability(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-7",
            Satisfied = true
        };

        // Load lesson subjects if not already loaded
        var lessonSubjects = lesson.Lesson?.LessonSubjects;
        if (lessonSubjects == null || !lessonSubjects.Any())
        {
            lessonSubjects = await _context.LessonSubjects
                .Include(ls => ls.Subject)
                .Where(ls => ls.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        // Check unavailability for ALL subjects (lesson can have multiple subjects)
        foreach (var lessonSubject in lessonSubjects)
        {
            if (lessonSubject.Subject == null)
                continue;

            var unavailability = await _context.SubjectAvailabilities
                .Where(sa => sa.SubjectId == lessonSubject.SubjectId)
                .Where(sa => sa.DayOfWeek == lesson.DayOfWeek)
                .Where(sa => sa.PeriodId == lesson.PeriodId)
                .Where(sa => sa.Importance == ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .FirstOrDefaultAsync();

            if (unavailability != null)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.SubjectAbsoluteUnavailability.ErrorMessageTemplate,
                    lessonSubject.Subject.Name);
                result.Details["SubjectId"] = lessonSubject.SubjectId;
                result.Details["AvailabilityId"] = unavailability.Id;
                return result; // Return on first violation found
            }
        }

        return result;
    }

    /// <summary>
    /// HC-8: Teacher Max Consecutive Periods
    /// </summary>
    private async Task<ConstraintResult> ValidateTeacherMaxConsecutivePeriods(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-8",
            Satisfied = true
        };

        // Load lesson teachers if not already loaded
        var lessonTeachers = lesson.Lesson?.LessonTeachers;
        if (lessonTeachers == null || !lessonTeachers.Any())
        {
            lessonTeachers = await _context.LessonTeachers
                .Include(lt => lt.Teacher)
                .Where(lt => lt.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonTeacher in lessonTeachers)
        {
            // Special case: Skip intern teacher "xy"
            if (ConstraintDefinitions.SpecialCases.IsInternTeacher(lessonTeacher.Teacher))
                continue;

            var teacher = lessonTeacher.Teacher;
            if (teacher?.MaxConsecutivePeriods == null)
                continue;

            // Get all periods for this teacher on this day
            var teacherSchedule = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonTeachers
                    .Any(lt => lt.TeacherId == teacher.Id))
                .Select(sl => new { sl.PeriodId, sl.Period })
                .Union(new[] { new { lesson.PeriodId, lesson.Period } })
                .OrderBy(p => p.PeriodId)
                .ToList();

            // Check consecutive periods
            int consecutiveCount = 1;
            for (int i = 1; i < teacherSchedule.Count; i++)
            {
                if (teacherSchedule[i].PeriodId == teacherSchedule[i - 1].PeriodId + 1)
                {
                    consecutiveCount++;
                    if (consecutiveCount > teacher.MaxConsecutivePeriods)
                    {
                        result.Satisfied = false;
                        result.Message = string.Format(
                            ConstraintDefinitions.Constraints.TeacherMaxConsecutivePeriods.ErrorMessageTemplate,
                            teacher.FullName,
                            teacher.MaxConsecutivePeriods);
                        result.Details["TeacherId"] = teacher.Id;
                        result.Details["ConsecutiveCount"] = consecutiveCount;
                        return result;
                    }
                }
                else
                {
                    consecutiveCount = 1;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// HC-9: Class Max Consecutive Same Subject
    /// NOTE: Checks ALL subjects for a lesson (lessons can have multiple subjects)
    /// </summary>
    private async Task<ConstraintResult> ValidateClassMaxConsecutiveSameSubject(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-9",
            Satisfied = true
        };

        // Load lesson classes if not already loaded
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        // Load lesson subjects if not already loaded
        var lessonSubjects = lesson.Lesson?.LessonSubjects;
        if (lessonSubjects == null || !lessonSubjects.Any())
        {
            lessonSubjects = await _context.LessonSubjects
                .Include(ls => ls.Subject)
                .Where(ls => ls.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        // Check each subject for consecutive violations (lesson can have multiple subjects)
        foreach (var lessonSubject in lessonSubjects)
        {
            foreach (var lessonClass in lessonClasses)
            {
                // Special case: Skip special classes (v-res, Team)
                if (ConstraintDefinitions.SpecialCases.IsSpecialClass(lessonClass.Class))
                    continue;

                var classEntity = lessonClass.Class;
                if (classEntity?.MaxConsecutiveSubjects == null)
                    continue;

                // Get all periods for this class with this subject on this day
                var classSchedule = existingSchedule
                    .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                    .Where(sl => sl.Lesson != null &&
                                sl.Lesson.LessonSubjects.Any(ls => ls.SubjectId == lessonSubject.SubjectId) &&
                                sl.Lesson.LessonClasses.Any(lc => lc.ClassId == classEntity.Id))
                    .Select(sl => sl.PeriodId)
                    .Union(new[] { lesson.PeriodId })
                    .OrderBy(p => p)
                    .ToList();

                // Check consecutive periods of same subject
                int consecutiveCount = 1;
                for (int i = 1; i < classSchedule.Count; i++)
                {
                    if (classSchedule[i] == classSchedule[i - 1] + 1)
                    {
                        consecutiveCount++;
                        if (consecutiveCount > classEntity.MaxConsecutiveSubjects)
                        {
                            result.Satisfied = false;
                            result.Message = string.Format(
                                ConstraintDefinitions.Constraints.ClassMaxConsecutiveSameSubject.ErrorMessageTemplate,
                                classEntity.Name,
                                lessonSubject.Subject?.Name ?? "Unknown",
                                classEntity.MaxConsecutiveSubjects);
                            result.Details["ClassId"] = classEntity.Id;
                            result.Details["SubjectId"] = lessonSubject.SubjectId;
                            result.Details["ConsecutiveCount"] = consecutiveCount;
                            return result;
                        }
                    }
                    else
                    {
                        consecutiveCount = 1;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// HC-10: Locked Lesson
    /// </summary>
    private Task<ConstraintResult> ValidateLockedLesson(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-10",
            Satisfied = true
        };

        // This constraint is mainly for preventing modifications
        // It's typically checked before attempting to move/delete a lesson
        // For validation during scheduling, we skip this check
        // The actual enforcement happens in the Edit page handlers

        return Task.FromResult(result);
    }

    #endregion

    #region Soft Constraint Validators

    /// <summary>
    /// SC-1: Teacher Preference Unavailability
    /// </summary>
    private async Task<ConstraintResult> ValidateTeacherPreferenceUnavailability(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-1",
            Satisfied = true
        };

        // Load lesson teachers if not already loaded
        var lessonTeachers = lesson.Lesson?.LessonTeachers;
        if (lessonTeachers == null || !lessonTeachers.Any())
        {
            lessonTeachers = await _context.LessonTeachers
                .Include(lt => lt.Teacher)
                .Where(lt => lt.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonTeacher in lessonTeachers)
        {
            // Special case: Skip intern teacher "xy"
            if (ConstraintDefinitions.SpecialCases.IsInternTeacher(lessonTeacher.Teacher))
                continue;

            // Check for preference (importance -2 or -1)
            var preference = await _context.TeacherAvailabilities
                .Where(ta => ta.TeacherId == lessonTeacher.TeacherId)
                .Where(ta => ta.DayOfWeek == lesson.DayOfWeek)
                .Where(ta => ta.PeriodId == lesson.PeriodId)
                .Where(ta => ta.Importance < 0 && ta.Importance > ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .FirstOrDefaultAsync();

            if (preference != null)
            {
                var preferenceStrength = ConstraintDefinitions.ImportanceScale
                    .GetPreferenceStrengthDescription(preference.Importance);

                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.TeacherPreferenceUnavailability.ErrorMessageTemplate,
                    lessonTeacher.Teacher?.FullName ?? "Unknown",
                    preferenceStrength);
                result.Details["TeacherId"] = lessonTeacher.TeacherId;
                result.Details["Importance"] = preference.Importance;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// SC-2: Class Preference Unavailability
    /// </summary>
    private async Task<ConstraintResult> ValidateClassPreferenceUnavailability(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-2",
            Satisfied = true
        };

        // Load lesson classes if not already loaded
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonClass in lessonClasses)
        {
            // Special case: Skip special classes (v-res, Team)
            if (ConstraintDefinitions.SpecialCases.IsSpecialClass(lessonClass.Class))
                continue;

            var preference = await _context.ClassAvailabilities
                .Where(ca => ca.ClassId == lessonClass.ClassId)
                .Where(ca => ca.DayOfWeek == lesson.DayOfWeek)
                .Where(ca => ca.PeriodId == lesson.PeriodId)
                .Where(ca => ca.Importance < 0 && ca.Importance > ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .FirstOrDefaultAsync();

            if (preference != null)
            {
                var preferenceStrength = ConstraintDefinitions.ImportanceScale
                    .GetPreferenceStrengthDescription(preference.Importance);

                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.ClassPreferenceUnavailability.ErrorMessageTemplate,
                    lessonClass.Class?.Name ?? "Unknown",
                    preferenceStrength);
                result.Details["ClassId"] = lessonClass.ClassId;
                result.Details["Importance"] = preference.Importance;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// SC-3: Subject Time Preferences
    /// NOTE: Checks ALL subjects for a lesson (lessons can have multiple subjects)
    /// </summary>
    private async Task<ConstraintResult> ValidateSubjectTimePreferences(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-3",
            Satisfied = true
        };

        // Load lesson subjects if not already loaded
        var lessonSubjects = lesson.Lesson?.LessonSubjects;
        if (lessonSubjects == null || !lessonSubjects.Any())
        {
            lessonSubjects = await _context.LessonSubjects
                .Include(ls => ls.Subject)
                .Where(ls => ls.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        // Check time preferences for ALL subjects (lesson can have multiple subjects)
        foreach (var lessonSubject in lessonSubjects)
        {
            var preference = await _context.SubjectAvailabilities
                .Where(sa => sa.SubjectId == lessonSubject.SubjectId)
                .Where(sa => sa.DayOfWeek == lesson.DayOfWeek)
                .Where(sa => sa.PeriodId == lesson.PeriodId)
                .Where(sa => sa.Importance < 0 && sa.Importance > ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .Include(sa => sa.Subject)
                .FirstOrDefaultAsync();

            if (preference != null)
            {
                var preferenceStrength = ConstraintDefinitions.ImportanceScale
                    .GetPreferenceStrengthDescription(preference.Importance);

                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.SubjectTimePreferences.ErrorMessageTemplate,
                    preference.Subject?.Name ?? "Unknown",
                    preferenceStrength);
                result.Details["SubjectId"] = lessonSubject.SubjectId;
                result.Details["Importance"] = preference.Importance;
                return result; // Return on first violation found
            }
        }

        return result;
    }

    /// <summary>
    /// SC-4: Room Preferences
    /// </summary>
    private async Task<ConstraintResult> ValidateRoomPreferences(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-4",
            Satisfied = true
        };

        var roomIds = new List<int>();
        if (lesson.RoomId.HasValue)
            roomIds.Add(lesson.RoomId.Value);
        if (lesson.ScheduledLessonRooms != null && lesson.ScheduledLessonRooms.Any())
            roomIds.AddRange(lesson.ScheduledLessonRooms.Select(slr => slr.RoomId));

        foreach (var roomId in roomIds.Distinct())
        {
            var preference = await _context.RoomAvailabilities
                .Where(ra => ra.RoomId == roomId)
                .Where(ra => ra.DayOfWeek == lesson.DayOfWeek)
                .Where(ra => ra.PeriodId == lesson.PeriodId)
                .Where(ra => ra.Importance < 0 && ra.Importance > ConstraintDefinitions.ImportanceScale.MustNotSchedule)
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync();

            if (preference != null)
            {
                var preferenceStrength = ConstraintDefinitions.ImportanceScale
                    .GetPreferenceStrengthDescription(preference.Importance);

                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.RoomPreferences.ErrorMessageTemplate,
                    preference.Room?.RoomNumber ?? roomId.ToString(),
                    preferenceStrength);
                result.Details["RoomId"] = roomId;
                result.Details["Importance"] = preference.Importance;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// SC-5: Teacher Lunch Break
    /// </summary>
    private async Task<ConstraintResult> ValidateTeacherLunchBreak(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-5",
            Satisfied = true
        };

        // Simplified: Assume periods 4, 5, 6 are lunch periods
        var lunchPeriods = new[] { 4, 5, 6 };
        if (!lunchPeriods.Contains(lesson.PeriodId))
            return result;

        // Load lesson teachers
        var lessonTeachers = lesson.Lesson?.LessonTeachers;
        if (lessonTeachers == null || !lessonTeachers.Any())
        {
            lessonTeachers = await _context.LessonTeachers
                .Include(lt => lt.Teacher)
                .Where(lt => lt.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonTeacher in lessonTeachers)
        {
            // Special case: Skip intern teacher "xy"
            if (ConstraintDefinitions.SpecialCases.IsInternTeacher(lessonTeacher.Teacher))
                continue;

            var teacher = lessonTeacher.Teacher;
            if (teacher?.MinLunchBreak == null || teacher.MinLunchBreak <= 0)
                continue;

            // Check if teacher has classes during lunch
            var lunchSchedule = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => lunchPeriods.Contains(sl.PeriodId))
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonTeachers
                    .Any(lt => lt.TeacherId == teacher.Id))
                .ToList();

            if (lunchSchedule.Any())
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.TeacherLunchBreak.ErrorMessageTemplate,
                    teacher.FullName);
                result.Details["TeacherId"] = teacher.Id;
                result.Details["MinLunchBreak"] = teacher.MinLunchBreak;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// SC-6: Class Lunch Break
    /// </summary>
    private async Task<ConstraintResult> ValidateClassLunchBreak(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-6",
            Satisfied = true
        };

        // Simplified: Assume periods 4, 5, 6 are lunch periods
        var lunchPeriods = new[] { 4, 5, 6 };
        if (!lunchPeriods.Contains(lesson.PeriodId))
            return result;

        // Load lesson classes
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonClass in lessonClasses)
        {
            // Special case: Skip special classes (v-res, Team)
            if (ConstraintDefinitions.SpecialCases.IsSpecialClass(lessonClass.Class))
                continue;

            var classEntity = lessonClass.Class;
            if (classEntity?.MinLunchBreak == null || classEntity.MinLunchBreak <= 0)
                continue;

            var lunchSchedule = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => lunchPeriods.Contains(sl.PeriodId))
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonClasses
                    .Any(lc => lc.ClassId == classEntity.Id))
                .ToList();

            if (lunchSchedule.Any())
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.ClassLunchBreak.ErrorMessageTemplate,
                    classEntity.Name);
                result.Details["ClassId"] = classEntity.Id;
                result.Details["MinLunchBreak"] = classEntity.MinLunchBreak;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// SC-7: No Gaps in Class Schedule
    /// </summary>
    private async Task<ConstraintResult> ValidateNoGapsInClassSchedule(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-7",
            Satisfied = true
        };

        // Load lesson classes
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonClass in lessonClasses)
        {
            var classEntity = lessonClass.Class;
            if (classEntity == null)
                continue;

            // Get all periods for this class on this day (including the new lesson)
            var classPeriods = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonClasses
                    .Any(lc => lc.ClassId == classEntity.Id))
                .Select(sl => sl.PeriodId)
                .Union(new[] { lesson.PeriodId })
                .OrderBy(p => p)
                .ToList();

            if (classPeriods.Count <= 1)
                continue;

            // Check for gaps
            var minPeriod = classPeriods.Min();
            var maxPeriod = classPeriods.Max();

            for (int p = minPeriod + 1; p < maxPeriod; p++)
            {
                if (!classPeriods.Contains(p))
                {
                    result.Satisfied = false;
                    result.Message = string.Format(
                        ConstraintDefinitions.Constraints.NoGapsInClassSchedule.ErrorMessageTemplate,
                        classEntity.Name);
                    result.Details["ClassId"] = classEntity.Id;
                    result.Details["GapPeriod"] = p;
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// SC-8: Room Type Preference Mismatch
    /// </summary>
    private async Task<ConstraintResult> ValidateRoomTypePreferenceMismatch(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-8",
            Satisfied = true
        };

        var requiredRoomType = lesson.Lesson?.RequiredRoomType;
        if (string.IsNullOrWhiteSpace(requiredRoomType))
            return result;

        // Check room type for legacy RoomId
        if (lesson.RoomId.HasValue)
        {
            var room = await _context.Rooms.FindAsync(lesson.RoomId.Value);
            if (room != null && !string.Equals(room.RoomType, requiredRoomType, StringComparison.OrdinalIgnoreCase))
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.RoomTypePreferenceMismatch.ErrorMessageTemplate,
                    room.RoomType ?? "Unknown",
                    requiredRoomType);
                result.Details["RoomId"] = room.Id;
                result.Details["ActualType"] = room.RoomType;
                result.Details["RequiredType"] = requiredRoomType;
                return result;
            }
        }

        // Check room types for multi-room scenario
        if (lesson.ScheduledLessonRooms != null && lesson.ScheduledLessonRooms.Any())
        {
            foreach (var slr in lesson.ScheduledLessonRooms)
            {
                var room = slr.Room ?? await _context.Rooms.FindAsync(slr.RoomId);
                if (room != null && !string.Equals(room.RoomType, requiredRoomType, StringComparison.OrdinalIgnoreCase))
                {
                    result.Satisfied = false;
                    result.Message = string.Format(
                        ConstraintDefinitions.Constraints.RoomTypePreferenceMismatch.ErrorMessageTemplate,
                        room.RoomType ?? "Unknown",
                        requiredRoomType);
                    result.Details["RoomId"] = room.Id;
                    result.Details["ActualType"] = room.RoomType;
                    result.Details["RequiredType"] = requiredRoomType;
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// SC-9: Subject Preferred Room
    /// NOTE: Checks ALL subjects for a lesson (lessons can have multiple subjects)
    /// </summary>
    private async Task<ConstraintResult> ValidateSubjectPreferredRoom(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-9",
            Satisfied = true
        };

        // Load lesson subjects if not already loaded
        var lessonSubjects = lesson.Lesson?.LessonSubjects;
        if (lessonSubjects == null || !lessonSubjects.Any())
        {
            lessonSubjects = await _context.LessonSubjects
                .Include(ls => ls.Subject)
                .Where(ls => ls.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        // Check preferred room for ALL subjects (lesson can have multiple subjects)
        foreach (var lessonSubject in lessonSubjects)
        {
            var subject = lessonSubject.Subject;
            if (subject?.PreferredRoomId == null)
                continue;

            // Check if using preferred room
            var usingPreferredRoom = lesson.RoomId == subject.PreferredRoomId ||
                                    (lesson.ScheduledLessonRooms != null &&
                                     lesson.ScheduledLessonRooms.Any(slr => slr.RoomId == subject.PreferredRoomId));

            if (!usingPreferredRoom)
            {
                var preferredRoom = await _context.Rooms.FindAsync(subject.PreferredRoomId);
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.SubjectPreferredRoom.ErrorMessageTemplate,
                    subject.Name,
                    preferredRoom?.RoomNumber ?? subject.PreferredRoomId.ToString());
                result.Details["SubjectId"] = subject.Id;
                result.Details["PreferredRoomId"] = subject.PreferredRoomId;
                return result; // Return on first violation found
            }
        }

        return result;
    }

    /// <summary>
    /// SC-10: Teacher Min Periods Per Day
    /// </summary>
    private async Task<ConstraintResult> ValidateTeacherMinPeriodsPerDay(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-10",
            Satisfied = true
        };

        // Load lesson teachers
        var lessonTeachers = lesson.Lesson?.LessonTeachers;
        if (lessonTeachers == null || !lessonTeachers.Any())
        {
            lessonTeachers = await _context.LessonTeachers
                .Include(lt => lt.Teacher)
                .Where(lt => lt.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonTeacher in lessonTeachers)
        {
            // Special case: Skip intern teacher "xy"
            if (ConstraintDefinitions.SpecialCases.IsInternTeacher(lessonTeacher.Teacher))
                continue;

            var teacher = lessonTeacher.Teacher;
            if (teacher?.MinPeriodsPerDay == null)
                continue;

            // Count periods for this teacher on this day
            var periodsToday = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonTeachers
                    .Any(lt => lt.TeacherId == teacher.Id))
                .Count() + 1; // +1 for current lesson

            if (periodsToday < teacher.MinPeriodsPerDay)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.TeacherMinPeriodsPerDay.ErrorMessageTemplate,
                    teacher.FullName,
                    teacher.MinPeriodsPerDay);
                result.Details["TeacherId"] = teacher.Id;
                result.Details["CurrentPeriods"] = periodsToday;
                result.Details["MinRequired"] = teacher.MinPeriodsPerDay;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-11: Teacher Max Periods Per Day
    /// </summary>
    private async Task<ConstraintResult> ValidateTeacherMaxPeriodsPerDay(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-11",
            Satisfied = true
        };

        // Load lesson teachers
        var lessonTeachers = lesson.Lesson?.LessonTeachers;
        if (lessonTeachers == null || !lessonTeachers.Any())
        {
            lessonTeachers = await _context.LessonTeachers
                .Include(lt => lt.Teacher)
                .Where(lt => lt.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonTeacher in lessonTeachers)
        {
            // Special case: Skip intern teacher "xy"
            if (ConstraintDefinitions.SpecialCases.IsInternTeacher(lessonTeacher.Teacher))
                continue;

            var teacher = lessonTeacher.Teacher;
            if (teacher?.MaxPeriodsPerDay == null)
                continue;

            // Count periods for this teacher on this day
            var periodsToday = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonTeachers
                    .Any(lt => lt.TeacherId == teacher.Id))
                .Count() + 1; // +1 for current lesson

            if (periodsToday > teacher.MaxPeriodsPerDay)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.TeacherMaxPeriodsPerDay.ErrorMessageTemplate,
                    teacher.FullName,
                    teacher.MaxPeriodsPerDay);
                result.Details["TeacherId"] = teacher.Id;
                result.Details["CurrentPeriods"] = periodsToday;
                result.Details["MaxAllowed"] = teacher.MaxPeriodsPerDay;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// SC-11: Class Min Periods Per Day
    /// </summary>
    private async Task<ConstraintResult> ValidateClassMinPeriodsPerDay(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "SC-11",
            Satisfied = true
        };

        // Load lesson classes
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonClass in lessonClasses)
        {
            // Special case: Skip special classes (v-res, Team)
            if (ConstraintDefinitions.SpecialCases.IsSpecialClass(lessonClass.Class))
                continue;

            var classEntity = lessonClass.Class;
            if (classEntity?.MinPeriodsPerDay == null)
                continue;

            // Count periods for this class on this day
            var periodsToday = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonClasses
                    .Any(lc => lc.ClassId == classEntity.Id))
                .Count() + 1; // +1 for current lesson

            if (periodsToday < classEntity.MinPeriodsPerDay)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.ClassMinPeriodsPerDay.ErrorMessageTemplate,
                    classEntity.Name,
                    classEntity.MinPeriodsPerDay);
                result.Details["ClassId"] = classEntity.Id;
                result.Details["CurrentPeriods"] = periodsToday;
                result.Details["MinRequired"] = classEntity.MinPeriodsPerDay;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// HC-12: Class Max Periods Per Day
    /// </summary>
    private async Task<ConstraintResult> ValidateClassMaxPeriodsPerDay(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext context)
    {
        var result = new ConstraintResult
        {
            ConstraintCode = "HC-12",
            Satisfied = true
        };

        // Load lesson classes
        var lessonClasses = lesson.Lesson?.LessonClasses;
        if (lessonClasses == null || !lessonClasses.Any())
        {
            lessonClasses = await _context.LessonClasses
                .Include(lc => lc.Class)
                .Where(lc => lc.LessonId == lesson.LessonId)
                .ToListAsync();
        }

        foreach (var lessonClass in lessonClasses)
        {
            // Special case: Skip special classes (v-res, Team)
            if (ConstraintDefinitions.SpecialCases.IsSpecialClass(lessonClass.Class))
                continue;

            var classEntity = lessonClass.Class;
            if (classEntity?.MaxPeriodsPerDay == null)
                continue;

            // Count periods for this class on this day
            var periodsToday = existingSchedule
                .Where(sl => sl.DayOfWeek == lesson.DayOfWeek)
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonClasses
                    .Any(lc => lc.ClassId == classEntity.Id))
                .Count() + 1; // +1 for current lesson

            if (periodsToday > classEntity.MaxPeriodsPerDay)
            {
                result.Satisfied = false;
                result.Message = string.Format(
                    ConstraintDefinitions.Constraints.ClassMaxPeriodsPerDay.ErrorMessageTemplate,
                    classEntity.Name,
                    classEntity.MaxPeriodsPerDay);
                result.Details["ClassId"] = classEntity.Id;
                result.Details["CurrentPeriods"] = periodsToday;
                result.Details["MaxAllowed"] = classEntity.MaxPeriodsPerDay;
                return result;
            }
        }

        return result;
    }

    #endregion
}
