-- Migration Script: Add BreakSupervisionSubstitutions table
-- This table stores temporary supervision substitutions during teacher absences
-- Similar to the Substitutions table but for break supervision duties

-- Create the BreakSupervisionSubstitutions table
CREATE TABLE IF NOT EXISTS BreakSupervisionSubstitutions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AbsenceId INTEGER NOT NULL,
    BreakSupervisionDutyId INTEGER NOT NULL,
    SubstituteTeacherId INTEGER NULL,
    Type INTEGER NOT NULL DEFAULT 0,
    Date TEXT NOT NULL,
    Notes TEXT NULL,
    AssignedAt TEXT NOT NULL,
    AssignedByUserId TEXT NULL,
    EmailSent INTEGER NOT NULL DEFAULT 0,
    EmailSentAt TEXT NULL,
    CONSTRAINT FK_BreakSupervisionSubstitutions_Absences FOREIGN KEY (AbsenceId) REFERENCES Absences(Id),
    CONSTRAINT FK_BreakSupervisionSubstitutions_Duties FOREIGN KEY (BreakSupervisionDutyId) REFERENCES BreakSupervisionDuties(Id),
    CONSTRAINT FK_BreakSupervisionSubstitutions_Teachers FOREIGN KEY (SubstituteTeacherId) REFERENCES Teachers(Id),
    CONSTRAINT FK_BreakSupervisionSubstitutions_Users FOREIGN KEY (AssignedByUserId) REFERENCES AspNetUsers(Id)
);

-- Create indexes for efficient queries
CREATE INDEX IF NOT EXISTS IX_BreakSupervisionSubstitutions_AbsenceId_DutyId
    ON BreakSupervisionSubstitutions (AbsenceId, BreakSupervisionDutyId);

CREATE INDEX IF NOT EXISTS IX_BreakSupervisionSubstitutions_Date_DutyId
    ON BreakSupervisionSubstitutions (Date, BreakSupervisionDutyId);

CREATE INDEX IF NOT EXISTS IX_BreakSupervisionSubstitutions_SubstituteTeacherId
    ON BreakSupervisionSubstitutions (SubstituteTeacherId);

-- Type values:
-- 0 = TeacherSubstitute (another teacher covers)
-- 1 = Cancelled (location unmanned)
-- 2 = CombinedArea (nearby supervisor covers)
