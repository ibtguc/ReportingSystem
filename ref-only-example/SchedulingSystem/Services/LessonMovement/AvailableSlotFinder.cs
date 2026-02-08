using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.Constraints;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Represents a potential timeslot where a lesson could be placed
    /// </summary>
    public class AvailableSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int PeriodId { get; set; }
        public int? RoomId { get; set; }
        public string? RoomName { get; set; }
        public bool HasHardConstraintViolations { get; set; }
        public List<string> HardViolations { get; set; } = new();
        public List<string> SoftViolations { get; set; } = new();
        public double QualityScore { get; set; } // Higher is better (0-100)
        public bool IsCurrentSlot { get; set; }
    }

    /// <summary>
    /// Service to find all available timeslots where a lesson can be placed
    /// </summary>
    public class AvailableSlotFinder
    {
        private readonly ApplicationDbContext _context;
        private readonly TimetableConflictService _conflictService;

        public AvailableSlotFinder(
            ApplicationDbContext context,
            TimetableConflictService conflictService)
        {
            _context = context;
            _conflictService = conflictService;
        }

        /// <summary>
        /// Find all available slots for a lesson, optionally excluding specific timeslots
        /// </summary>
        /// <param name="timetableId">The timetable to search within</param>
        /// <param name="lessonId">The lesson to be placed</param>
        /// <param name="currentScheduledLessonId">If moving existing lesson, its ID to exclude from conflict checks</param>
        /// <param name="excludeSlots">Timeslots to avoid (user-specified restrictions)</param>
        /// <param name="includeCurrentSlot">Whether to include the lesson's current slot in results</param>
        /// <param name="ignoredConstraintCodes">List of constraint codes to ignore during validation</param>
        /// <returns>List of available slots sorted by quality score</returns>
        public async Task<List<AvailableSlot>> FindAvailableSlotsAsync(
            int timetableId,
            int lessonId,
            int? currentScheduledLessonId = null,
            List<(DayOfWeek Day, int PeriodId)>? excludeSlots = null,
            bool includeCurrentSlot = false,
            List<string>? ignoredConstraintCodes = null)
        {
            var availableSlots = new List<AvailableSlot>();

            // Get all periods and days
            var periods = await _context.Periods
                .Where(p => !p.IsBreak)
                .OrderBy(p => p.PeriodNumber)
                .ToListAsync();

            var daysOfWeek = new[] {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday
            };

            // Get lesson details
            var lesson = await _context.Lessons
                .Include(l => l.LessonTeachers).ThenInclude(lt => lt.Teacher)
                .Include(l => l.LessonClasses).ThenInclude(lc => lc.Class)
                .Include(l => l.LessonSubjects).ThenInclude(ls => ls.Subject)
                .Include(l => l.LessonAssignments).ThenInclude(la => la.Teacher)
                .Include(l => l.LessonAssignments).ThenInclude(la => la.Subject)
                .Include(l => l.LessonAssignments).ThenInclude(la => la.Class)
                .AsSplitQuery()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                throw new ArgumentException($"Lesson with ID {lessonId} not found");

            // Get current slot if lesson is already scheduled
            ScheduledLesson? currentScheduledLesson = null;
            if (currentScheduledLessonId.HasValue)
            {
                currentScheduledLesson = await _context.ScheduledLessons
                    .Include(sl => sl.ScheduledLessonRooms).ThenInclude(slr => slr.Room)
                    .Include(sl => sl.ScheduledLessonRooms).ThenInclude(slr => slr.RoomAssignments).ThenInclude(ra => ra.LessonAssignment)
                    .FirstOrDefaultAsync(sl => sl.Id == currentScheduledLessonId.Value);
            }

            // Get suitable rooms for this lesson
            var suitableRooms = await GetSuitableRoomsAsync(lesson);

            // Check each timeslot
            foreach (var day in daysOfWeek)
            {
                foreach (var period in periods)
                {
                    // Skip excluded slots
                    if (excludeSlots != null && excludeSlots.Contains((day, period.Id)))
                        continue;

                    // Check each suitable room
                    foreach (var room in suitableRooms)
                    {
                        var slot = await EvaluateSlotAsync(
                            timetableId,
                            lessonId,
                            day,
                            period.Id,
                            room?.Id,
                            currentScheduledLessonId,
                            ignoredConstraintCodes);

                        // Check if this is the current slot
                        if (currentScheduledLesson != null &&
                            currentScheduledLesson.DayOfWeek == day &&
                            currentScheduledLesson.PeriodId == period.Id &&
                            (currentScheduledLesson.RoomId == room?.Id ||
                             currentScheduledLesson.ScheduledLessonRooms.Any(slr => slr.RoomId == room?.Id)))
                        {
                            slot.IsCurrentSlot = true;
                            if (!includeCurrentSlot)
                                continue;
                        }

                        slot.RoomName = room?.Name;
                        availableSlots.Add(slot);
                    }

                    // Also check with no specific room (allow lessons without room assignments)
                    if (suitableRooms.Contains(null))
                    {
                        var slot = await EvaluateSlotAsync(
                            timetableId,
                            lessonId,
                            day,
                            period.Id,
                            null,
                            currentScheduledLessonId);

                        if (currentScheduledLesson != null &&
                            currentScheduledLesson.DayOfWeek == day &&
                            currentScheduledLesson.PeriodId == period.Id &&
                            currentScheduledLesson.RoomId == null)
                        {
                            slot.IsCurrentSlot = true;
                            if (!includeCurrentSlot)
                                continue;
                        }

                        availableSlots.Add(slot);
                    }
                }
            }

            // Sort by quality score (descending)
            return availableSlots
                .OrderByDescending(s => s.QualityScore)
                .ThenBy(s => s.DayOfWeek)
                .ThenBy(s => s.PeriodId)
                .ToList();
        }

        /// <summary>
        /// Evaluate a specific timeslot for a lesson
        /// </summary>
        private async Task<AvailableSlot> EvaluateSlotAsync(
            int timetableId,
            int lessonId,
            DayOfWeek dayOfWeek,
            int periodId,
            int? roomId,
            int? excludeScheduledLessonId,
            List<string>? ignoredConstraintCodes = null)
        {
            var slot = new AvailableSlot
            {
                DayOfWeek = dayOfWeek,
                PeriodId = periodId,
                RoomId = roomId
            };

            // Validate constraints using TimetableConflictService
            var conflictResult = await _conflictService.CheckConflictsAsync(
                timetableId,
                lessonId,
                dayOfWeek,
                periodId,
                roomId,
                excludeScheduledLessonId,
                ignoredConstraintCodes);

            slot.HardViolations = conflictResult.Errors;
            slot.SoftViolations = conflictResult.Warnings;
            slot.HasHardConstraintViolations = conflictResult.HasErrors;

            // Calculate quality score (0-100)
            slot.QualityScore = CalculateQualityScore(
                conflictResult.Errors,
                conflictResult.Warnings);

            return slot;
        }

        /// <summary>
        /// Calculate quality score for a slot (0-100, higher is better)
        /// </summary>
        private double CalculateQualityScore(
            List<string> hardViolations,
            List<string> softViolations)
        {
            // Start with perfect score
            double score = 100.0;

            // Hard violations make slot unavailable
            if (hardViolations.Any())
                return 0.0;

            // Deduct points for soft violations
            // Each soft violation deducts points (simple approach)
            score -= softViolations.Count * 10.0;

            // Ensure score doesn't go below 1 for valid slots
            return Math.Max(1.0, score);
        }

        /// <summary>
        /// Get suitable rooms for a lesson based on requirements
        /// </summary>
        private async Task<List<Room?>> GetSuitableRoomsAsync(Lesson lesson)
        {
            var rooms = new List<Room?>();

            // Always allow "no room" as an option
            rooms.Add(null);

            // Get all active rooms
            var allRooms = await _context.Rooms
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            // Include all active rooms
            // TODO: Add filtering based on capacity, room type preferences, etc.
            rooms.AddRange(allRooms);

            return rooms;
        }

        /// <summary>
        /// Find available slots grouped by quality tiers
        /// </summary>
        public async Task<AvailableSlotsByQuality> FindAvailableSlotsGroupedAsync(
            int timetableId,
            int lessonId,
            int? currentScheduledLessonId = null,
            List<(DayOfWeek Day, int PeriodId)>? excludeSlots = null)
        {
            var allSlots = await FindAvailableSlotsAsync(
                timetableId,
                lessonId,
                currentScheduledLessonId,
                excludeSlots,
                includeCurrentSlot: false);

            return new AvailableSlotsByQuality
            {
                Perfect = allSlots.Where(s => s.QualityScore >= 95).ToList(),
                Good = allSlots.Where(s => s.QualityScore >= 70 && s.QualityScore < 95).ToList(),
                Acceptable = allSlots.Where(s => s.QualityScore >= 50 && s.QualityScore < 70).ToList(),
                Poor = allSlots.Where(s => s.QualityScore >= 1 && s.QualityScore < 50).ToList(),
                Unavailable = allSlots.Where(s => s.QualityScore == 0).ToList()
            };
        }
    }

    /// <summary>
    /// Available slots grouped by quality tiers
    /// </summary>
    public class AvailableSlotsByQuality
    {
        public List<AvailableSlot> Perfect { get; set; } = new();
        public List<AvailableSlot> Good { get; set; } = new();
        public List<AvailableSlot> Acceptable { get; set; } = new();
        public List<AvailableSlot> Poor { get; set; } = new();
        public List<AvailableSlot> Unavailable { get; set; } = new();

        public int TotalAvailableCount => Perfect.Count + Good.Count + Acceptable.Count + Poor.Count;
    }
}
