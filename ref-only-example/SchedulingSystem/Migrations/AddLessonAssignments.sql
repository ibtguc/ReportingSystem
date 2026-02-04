-- Migration: Add LessonAssignments and ScheduledLessonRoomAssignments tables
-- This enables specifying which teacher teaches which subject to which class,
-- and which assignment is in which room for multi-room lessons.
-- Date: 2026-01-26

-- Step 1: Create LessonAssignments table
-- Links teacher-subject-class combinations within a lesson
CREATE TABLE IF NOT EXISTS LessonAssignments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    LessonId INTEGER NOT NULL,
    TeacherId INTEGER NULL,
    SubjectId INTEGER NULL,
    ClassId INTEGER NULL,
    Notes TEXT NULL,
    [Order] INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE,
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id) ON DELETE CASCADE,
    FOREIGN KEY (SubjectId) REFERENCES Subjects(Id) ON DELETE CASCADE,
    FOREIGN KEY (ClassId) REFERENCES Classes(Id) ON DELETE CASCADE
);

-- Step 2: Create indexes for LessonAssignments
CREATE INDEX IF NOT EXISTS IX_LessonAssignments_LessonId ON LessonAssignments(LessonId);
CREATE INDEX IF NOT EXISTS IX_LessonAssignments_TeacherId ON LessonAssignments(TeacherId);
CREATE INDEX IF NOT EXISTS IX_LessonAssignments_SubjectId ON LessonAssignments(SubjectId);
CREATE INDEX IF NOT EXISTS IX_LessonAssignments_ClassId ON LessonAssignments(ClassId);
CREATE INDEX IF NOT EXISTS IX_LessonAssignments_Lesson_Teacher_Subject_Class
    ON LessonAssignments(LessonId, TeacherId, SubjectId, ClassId);

-- Step 3: Create ScheduledLessonRoomAssignments table
-- Links LessonAssignments to specific rooms in scheduled lessons
CREATE TABLE IF NOT EXISTS ScheduledLessonRoomAssignments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduledLessonRoomId INTEGER NOT NULL,
    LessonAssignmentId INTEGER NOT NULL,
    FOREIGN KEY (ScheduledLessonRoomId) REFERENCES ScheduledLessonRooms(Id) ON DELETE CASCADE,
    FOREIGN KEY (LessonAssignmentId) REFERENCES LessonAssignments(Id) ON DELETE CASCADE
);

-- Step 4: Create indexes for ScheduledLessonRoomAssignments
CREATE INDEX IF NOT EXISTS IX_ScheduledLessonRoomAssignments_ScheduledLessonRoomId
    ON ScheduledLessonRoomAssignments(ScheduledLessonRoomId);
CREATE INDEX IF NOT EXISTS IX_ScheduledLessonRoomAssignments_LessonAssignmentId
    ON ScheduledLessonRoomAssignments(LessonAssignmentId);

-- Note: These tables are optional
-- If no LessonAssignments exist for a lesson, fall back to showing all teacher/subject/class combinations
-- If no ScheduledLessonRoomAssignments exist for a room, fall back to showing all participants
