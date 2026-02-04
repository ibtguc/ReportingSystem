-- Migration: Add TimetableId to BreakSupervisionDuties
-- This links break supervision duties to specific timetables, similar to ScheduledLessons
-- Date: 2026-01-26

-- Step 1: Add TimetableId column (nullable initially for backward compatibility)
ALTER TABLE BreakSupervisionDuties ADD COLUMN TimetableId INTEGER NULL
    REFERENCES Timetables(Id) ON DELETE CASCADE;

-- Step 2: Create index for efficient querying by timetable
CREATE INDEX IF NOT EXISTS IX_BreakSupervisionDuties_TimetableId
    ON BreakSupervisionDuties(TimetableId);

-- Step 3: Create composite index for timetable + location + time slot
CREATE INDEX IF NOT EXISTS IX_BreakSupervisionDuties_Timetable_Room_Day_Period
    ON BreakSupervisionDuties(TimetableId, RoomId, DayOfWeek, PeriodNumber);

-- Note: After running this migration:
-- 1. Existing duties will have TimetableId = NULL (treated as "unassigned/legacy")
-- 2. Run the copy script to assign duties to specific timetables
-- 3. Update the application code to always set TimetableId when creating new duties
