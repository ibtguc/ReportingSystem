-- One-time script: Copy Break Supervision Duties from Timetable 1 to Timetable 8
-- Run this AFTER AddTimetableIdToBreakSupervision.sql
-- Date: 2026-01-26

-- Step 1: Assign all existing duties (TimetableId = NULL) to Timetable 1
UPDATE BreakSupervisionDuties
SET TimetableId = 1
WHERE TimetableId IS NULL;

-- Step 2: Copy all duties from Timetable 1 to Timetable 8
-- This creates new duty records with the same Room, Teacher, Day, Period, Points, Notes, IsActive
INSERT INTO BreakSupervisionDuties (RoomId, TeacherId, DayOfWeek, PeriodNumber, Points, Notes, IsActive, TimetableId)
SELECT
    RoomId,
    TeacherId,
    DayOfWeek,
    PeriodNumber,
    Points,
    Notes,
    IsActive,
    8 AS TimetableId
FROM BreakSupervisionDuties
WHERE TimetableId = 1;

-- Step 3: Verify the copy
SELECT
    t.Id AS TimetableId,
    t.Name AS TimetableName,
    COUNT(bsd.Id) AS DutyCount
FROM Timetables t
LEFT JOIN BreakSupervisionDuties bsd ON bsd.TimetableId = t.Id
WHERE t.Id IN (1, 8)
GROUP BY t.Id, t.Name;
