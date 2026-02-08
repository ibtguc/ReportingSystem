-- SQL Script for Break Supervision Module
-- Run this on existing SQLite database to add break supervision table
-- Generated for SchedulingSystem - January 2026
--
-- NOTE: Supervision locations (Hof1, Hof2, oben, unten, etc.) are stored
-- in the existing Rooms table. This table only stores the duty assignments.
--
-- GPU009.TXT columns:
-- 1. Corridor (Room) - supervision location
-- 2. Teacher - assigned teacher name
-- 3. Day Number - day of week (1=Monday, 5=Friday)
-- 4. Period Number - period when supervision occurs
-- 5. Points - point value for the duty

-- =====================================================
-- Table: BreakSupervisionDuties
-- Stores teacher assignments to supervision locations (Rooms)
-- =====================================================
CREATE TABLE IF NOT EXISTS BreakSupervisionDuties (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId INTEGER NOT NULL,           -- GPU009 Col 1: Corridor
    TeacherId INTEGER NULL,            -- GPU009 Col 2: Teacher (nullable for unassigned)
    DayOfWeek INTEGER NOT NULL,        -- GPU009 Col 3: Day Number
    PeriodNumber INTEGER NOT NULL,     -- GPU009 Col 4: Period Number
    Points INTEGER NOT NULL DEFAULT 30, -- GPU009 Col 5: Points
    Notes TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE RESTRICT,
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id) ON DELETE RESTRICT
);

-- Composite index for location-based queries (Room + Day + Period)
CREATE INDEX IF NOT EXISTS IX_BreakSupervisionDuties_Room_Day_Period
ON BreakSupervisionDuties (RoomId, DayOfWeek, PeriodNumber);

-- Composite index for teacher-based queries (substitution planning)
CREATE INDEX IF NOT EXISTS IX_BreakSupervisionDuties_Teacher_Day_Period
ON BreakSupervisionDuties (TeacherId, DayOfWeek, PeriodNumber);

-- =====================================================
-- Migration from old schema (if needed):
-- =====================================================
-- ALTER TABLE BreakSupervisionDuties RENAME COLUMN BreakSlot TO PeriodNumber;
-- ALTER TABLE BreakSupervisionDuties RENAME COLUMN DurationMinutes TO Points;

-- =====================================================
-- Verification queries (uncomment to run)
-- =====================================================
-- SELECT name FROM sqlite_master WHERE type='table' AND name = 'BreakSupervisionDuties';
-- SELECT * FROM BreakSupervisionDuties;
-- SELECT d.*, r.RoomNumber, r.Name as RoomName, t.FirstName as TeacherName
-- FROM BreakSupervisionDuties d
-- LEFT JOIN Rooms r ON d.RoomId = r.Id
-- LEFT JOIN Teachers t ON d.TeacherId = t.Id;

-- =====================================================
-- Notes:
-- - DayOfWeek uses .NET DayOfWeek enum values:
--   0 = Sunday, 1 = Monday, 2 = Tuesday, 3 = Wednesday,
--   4 = Thursday, 5 = Friday, 6 = Saturday
-- - PeriodNumber values from GPU009.TXT indicate which period
--   the supervision duty occurs (e.g., 3, 5)
-- - RoomId references the Rooms table (supervision locations like
--   "Hof1", "Hof2", "oben", "unten" are stored as rooms)
-- - TeacherId is nullable for unassigned slots
-- - IsActive uses INTEGER (SQLite boolean: 0=false, 1=true)
-- =====================================================
