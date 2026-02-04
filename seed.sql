-- =============================================================================
-- ReportingSystem - Database Seeding Script
-- =============================================================================
-- This script sets up the organizational structure, users, roles, and
-- initial data needed before reporting can begin.
--
-- Target: SQLite (development) / SQL Server (production)
-- Usage:  sqlite3 ReportingSystem/reporting.db < seed.sql
--         OR apply via EF Core SeedData.cs for automatic seeding
--
-- NOTE: Run this on a FRESH database (after EF Core creates tables via
--       EnsureCreatedAsync). If data already exists, clear tables first.
-- =============================================================================

-- =============================================================================
-- 1. ORGANIZATIONAL UNITS (Hierarchical Structure)
-- =============================================================================
-- Level values: 0=Root, 1=Campus, 2=Faculty, 3=Department, 4=Sector, 5=Team
-- The GUC organizational hierarchy as defined in the SRS.
-- =============================================================================

-- Clear existing data (in dependency order)
DELETE FROM Delegations;
DELETE FROM MagicLinks;
DELETE FROM Notifications;
DELETE FROM Users;
DELETE FROM OrganizationalUnits;

-- Reset auto-increment counters (SQLite)
DELETE FROM sqlite_sequence WHERE name IN ('OrganizationalUnits', 'Users', 'Delegations', 'MagicLinks', 'Notifications');

-- ---- Root Organization ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (1, 'German University in Cairo', 'GUC', 'The German University in Cairo - Root Organization', 0, NULL, 0, 1, datetime('now'), NULL);

-- ---- Campuses (Level 1) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (2, 'Main Campus', 'MAIN', 'GUC Main Campus - Cairo', 1, 1, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (3, 'New Campus', 'NEWC', 'GUC New Capital Campus', 1, 1, 2, 1, datetime('now'), NULL);

-- ---- Faculties / Divisions (Level 2) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (4, 'Faculty of Engineering', 'ENG', 'Engineering and Technology Faculty', 2, 2, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (5, 'Faculty of Media Engineering and Technology', 'MET', 'Media Engineering & Technology Faculty', 2, 2, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (6, 'Faculty of Management Technology', 'MGT', 'Business and Management Faculty', 2, 2, 3, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (7, 'Faculty of Pharmacy and Biotechnology', 'PHAR', 'Pharmacy and Biotechnology Faculty', 2, 2, 4, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (8, 'Faculty of Applied Sciences and Arts', 'ASA', 'Applied Sciences and Arts Faculty', 2, 2, 5, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (9, 'IT & Administration Division', 'ITADM', 'Information Technology and Administration', 2, 2, 6, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (10, 'Faculty of Engineering - New Campus', 'ENG-NC', 'Engineering Faculty at New Capital Campus', 2, 3, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (11, 'Faculty of MET - New Campus', 'MET-NC', 'MET Faculty at New Capital Campus', 2, 3, 2, 1, datetime('now'), NULL);

-- ---- Departments (Level 3) ----
-- Engineering departments
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (12, 'Computer Science & Engineering', 'CSE', 'Computer Science and Engineering Department', 3, 4, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (13, 'Mechanical Engineering', 'ME', 'Mechanical Engineering Department', 3, 4, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (14, 'Electronics & Communications Engineering', 'ECE', 'Electronics and Communications Department', 3, 4, 3, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (15, 'Architecture & Urban Design', 'ARCH', 'Architecture and Urban Design Department', 3, 4, 4, 1, datetime('now'), NULL);

-- MET departments
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (16, 'Computer Science', 'CS', 'Computer Science Department (MET)', 3, 5, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (17, 'Digital Media', 'DM', 'Digital Media Department', 3, 5, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (18, 'Networks & Information Systems', 'NIS', 'Networks and Information Systems Department', 3, 5, 3, 1, datetime('now'), NULL);

-- Management departments
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (19, 'Economics', 'ECON', 'Economics Department', 3, 6, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (20, 'Finance & Accounting', 'FIN', 'Finance and Accounting Department', 3, 6, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (21, 'Management', 'MGMT', 'Management Department', 3, 6, 3, 1, datetime('now'), NULL);

-- IT & Admin departments
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (22, 'Software Development', 'SDEV', 'Software Development Department', 3, 9, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (23, 'IT Infrastructure', 'INFRA', 'IT Infrastructure and Networks Department', 3, 9, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (24, 'Human Resources', 'HR', 'Human Resources Department', 3, 9, 3, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (25, 'Quality Assurance & Audit', 'QA', 'Quality Assurance and Internal Audit', 3, 9, 4, 1, datetime('now'), NULL);

-- ---- Sectors / Sections (Level 4) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (26, 'Web Systems Section', 'SDEV-WEB', 'Web Application Development Section', 4, 22, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (27, 'Mobile Development Section', 'SDEV-MOB', 'Mobile Application Development Section', 4, 22, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (28, 'Network Operations Section', 'INFRA-NET', 'Network Operations and Maintenance', 4, 23, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (29, 'Server & Cloud Section', 'INFRA-SRV', 'Server Administration and Cloud Services', 4, 23, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (30, 'AI & Machine Learning Section', 'CSE-AI', 'Artificial Intelligence and ML Research', 4, 12, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (31, 'Software Engineering Section', 'CSE-SE', 'Software Engineering Research and Teaching', 4, 12, 2, 1, datetime('now'), NULL);

-- ---- Teams (Level 5) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (32, 'Backend Team', 'WEB-BE', 'Backend Development Team', 5, 26, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (33, 'Frontend Team', 'WEB-FE', 'Frontend Development Team', 5, 26, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (34, 'QA & Testing Team', 'WEB-QA', 'Quality Assurance and Testing Team', 5, 26, 3, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (35, 'iOS Team', 'MOB-IOS', 'iOS Mobile Development Team', 5, 27, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (36, 'Android Team', 'MOB-AND', 'Android Mobile Development Team', 5, 27, 2, 1, datetime('now'), NULL);


-- =============================================================================
-- 2. USERS (Employees with Roles across the Organization)
-- =============================================================================
-- Roles: Administrator, ReportOriginator, ReportReviewer, TeamManager,
--        DepartmentHead, Executive, Auditor
-- =============================================================================

-- ---- Executives (University Leadership) - Org: Root / Campuses ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (1, 'president@guc.edu.eg', 'Prof. Ahmed Hassan', 'Executive', 1, 'University President', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (2, 'vp.academic@guc.edu.eg', 'Prof. Mona El-Said', 'Executive', 1, 'VP for Academic Affairs', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (3, 'vp.admin@guc.edu.eg', 'Dr. Khaled Ibrahim', 'Executive', 1, 'VP for Administration', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (4, 'dean.main@guc.edu.eg', 'Prof. Sara Mahmoud', 'Executive', 2, 'Dean - Main Campus', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (5, 'dean.newcampus@guc.edu.eg', 'Prof. Omar Fathy', 'Executive', 3, 'Dean - New Campus', 1, datetime('now'), NULL);

-- ---- Administrators (System Admins) - Org: IT & Admin ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (6, 'admin@guc.edu.eg', 'System Administrator', 'Administrator', 9, 'System Administrator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (7, 'admin2@guc.edu.eg', 'Tarek Nabil', 'Administrator', 22, 'IT Systems Administrator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (8, 'admin3@guc.edu.eg', 'Yasmine Farouk', 'Administrator', 23, 'Infrastructure Administrator', 1, datetime('now'), NULL);

-- ---- Department Heads ----
-- Engineering Faculty department heads
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (9, 'head.cse@guc.edu.eg', 'Prof. Nadia Kamel', 'DepartmentHead', 12, 'Head of CS & Engineering', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (10, 'head.me@guc.edu.eg', 'Prof. Ayman Soliman', 'DepartmentHead', 13, 'Head of Mechanical Engineering', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (11, 'head.ece@guc.edu.eg', 'Prof. Laila Abdel-Fattah', 'DepartmentHead', 14, 'Head of Electronics & Comm.', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (12, 'head.arch@guc.edu.eg', 'Prof. Hisham Ragab', 'DepartmentHead', 15, 'Head of Architecture', 1, datetime('now'), NULL);

-- MET Faculty department heads
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (13, 'head.cs@guc.edu.eg', 'Prof. Fatma Zaki', 'DepartmentHead', 16, 'Head of Computer Science', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (14, 'head.dm@guc.edu.eg', 'Dr. Ramy Shoukry', 'DepartmentHead', 17, 'Head of Digital Media', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (15, 'head.nis@guc.edu.eg', 'Dr. Dina El-Masry', 'DepartmentHead', 18, 'Head of Networks & IS', 1, datetime('now'), NULL);

-- Management Faculty department heads
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (16, 'head.econ@guc.edu.eg', 'Prof. Sameh Attia', 'DepartmentHead', 19, 'Head of Economics', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (17, 'head.fin@guc.edu.eg', 'Dr. Noha Salah', 'DepartmentHead', 20, 'Head of Finance & Accounting', 1, datetime('now'), NULL);

-- IT & Admin department heads
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (18, 'head.sdev@guc.edu.eg', 'Eng. Mahmoud Adel', 'DepartmentHead', 22, 'Head of Software Development', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (19, 'head.infra@guc.edu.eg', 'Eng. Heba Mostafa', 'DepartmentHead', 23, 'Head of IT Infrastructure', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (20, 'head.hr@guc.edu.eg', 'Dr. Amira Youssef', 'DepartmentHead', 24, 'Head of Human Resources', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (21, 'head.qa@guc.edu.eg', 'Dr. Sherif Hassan', 'DepartmentHead', 25, 'Head of QA & Audit', 1, datetime('now'), NULL);

-- ---- Team Managers ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (22, 'mgr.web@guc.edu.eg', 'Eng. Ali Kamal', 'TeamManager', 26, 'Web Systems Section Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (23, 'mgr.mobile@guc.edu.eg', 'Eng. Salma Reda', 'TeamManager', 27, 'Mobile Development Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (24, 'mgr.netops@guc.edu.eg', 'Eng. Hassan Tawfik', 'TeamManager', 28, 'Network Operations Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (25, 'mgr.cloud@guc.edu.eg', 'Eng. Rana Mohamed', 'TeamManager', 29, 'Server & Cloud Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (26, 'mgr.backend@guc.edu.eg', 'Eng. Youssef Magdy', 'TeamManager', 32, 'Backend Team Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (27, 'mgr.frontend@guc.edu.eg', 'Eng. Nourhan Sayed', 'TeamManager', 33, 'Frontend Team Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (28, 'mgr.testing@guc.edu.eg', 'Eng. Karim Wael', 'TeamManager', 34, 'QA & Testing Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (29, 'mgr.ai@guc.edu.eg', 'Dr. Mariam Gamal', 'TeamManager', 30, 'AI & ML Section Lead', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (30, 'mgr.se@guc.edu.eg', 'Dr. Wael Abdelrahman', 'TeamManager', 31, 'Software Engineering Lead', 1, datetime('now'), NULL);

-- ---- Report Reviewers (mid-level staff who review reports before escalation) ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (31, 'reviewer.cse1@guc.edu.eg', 'Dr. Hany Mourad', 'ReportReviewer', 12, 'Senior Lecturer - CSE', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (32, 'reviewer.cse2@guc.edu.eg', 'Dr. Nesma Said', 'ReportReviewer', 12, 'Associate Professor - CSE', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (33, 'reviewer.met@guc.edu.eg', 'Dr. Tamer Hosny', 'ReportReviewer', 16, 'Senior Lecturer - CS', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (34, 'reviewer.sdev@guc.edu.eg', 'Eng. Waleed Emad', 'ReportReviewer', 22, 'Senior Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (35, 'reviewer.infra@guc.edu.eg', 'Eng. Maha Lotfy', 'ReportReviewer', 23, 'Senior Infrastructure Engineer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (36, 'reviewer.mgt@guc.edu.eg', 'Dr. Azza Helmy', 'ReportReviewer', 6, 'Senior Lecturer - Management', 1, datetime('now'), NULL);

-- ---- Report Originators (staff who create and submit reports) ----
-- Software Development team members
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (37, 'dev.backend1@guc.edu.eg', 'Ahmed Samir', 'ReportOriginator', 32, 'Backend Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (38, 'dev.backend2@guc.edu.eg', 'Mohamed Ashraf', 'ReportOriginator', 32, 'Backend Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (39, 'dev.frontend1@guc.edu.eg', 'Farida Hassan', 'ReportOriginator', 33, 'Frontend Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (40, 'dev.frontend2@guc.edu.eg', 'Omar Khaled', 'ReportOriginator', 33, 'Frontend Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (41, 'qa.tester1@guc.edu.eg', 'Reem Adel', 'ReportOriginator', 34, 'QA Engineer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (42, 'dev.mobile1@guc.edu.eg', 'Amr Fawzy', 'ReportOriginator', 35, 'iOS Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (43, 'dev.mobile2@guc.edu.eg', 'Layla Mahmoud', 'ReportOriginator', 36, 'Android Developer', 1, datetime('now'), NULL);

-- IT Infrastructure team members
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (44, 'netops1@guc.edu.eg', 'Mostafa Gamal', 'ReportOriginator', 28, 'Network Engineer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (45, 'netops2@guc.edu.eg', 'Dalia Ayman', 'ReportOriginator', 28, 'Network Technician', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (46, 'sysadmin1@guc.edu.eg', 'Kareem Hisham', 'ReportOriginator', 29, 'Systems Administrator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (47, 'cloud1@guc.edu.eg', 'Nada Sherif', 'ReportOriginator', 29, 'Cloud Engineer', 1, datetime('now'), NULL);

-- Academic staff as originators
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (48, 'faculty.cse1@guc.edu.eg', 'Dr. Bassem Aly', 'ReportOriginator', 12, 'Lecturer - CSE', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (49, 'faculty.cse2@guc.edu.eg', 'Dr. Hoda Farid', 'ReportOriginator', 12, 'Assistant Lecturer - CSE', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (50, 'faculty.me1@guc.edu.eg', 'Dr. Adel Ramadan', 'ReportOriginator', 13, 'Lecturer - Mechanical Eng.', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (51, 'faculty.cs1@guc.edu.eg', 'Dr. Samar Nour', 'ReportOriginator', 16, 'Lecturer - Computer Science', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (52, 'faculty.dm1@guc.edu.eg', 'Dr. Yara Essam', 'ReportOriginator', 17, 'Lecturer - Digital Media', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (53, 'faculty.econ1@guc.edu.eg', 'Dr. Magdy Abbas', 'ReportOriginator', 19, 'Lecturer - Economics', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (54, 'faculty.fin1@guc.edu.eg', 'Dr. Iman Khalil', 'ReportOriginator', 20, 'Lecturer - Finance', 1, datetime('now'), NULL);

-- HR staff
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (55, 'hr.staff1@guc.edu.eg', 'Marwa Elsayed', 'ReportOriginator', 24, 'HR Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (56, 'hr.staff2@guc.edu.eg', 'Nabil Tharwat', 'ReportOriginator', 24, 'HR Coordinator', 1, datetime('now'), NULL);

-- ---- Auditors ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (57, 'auditor1@guc.edu.eg', 'Dr. Hazem Barakat', 'Auditor', 25, 'Internal Auditor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (58, 'auditor2@guc.edu.eg', 'Eng. Nevine Sami', 'Auditor', 25, 'Quality Auditor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (59, 'auditor3@guc.edu.eg', 'Dr. Tarek Mansour', 'Auditor', 1, 'External Audit Liaison', 1, datetime('now'), NULL);

-- ---- Inactive user (for testing) ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (60, 'former.staff@guc.edu.eg', 'Retired Professor', 'ReportOriginator', NULL, 'Former Faculty Member', 0, datetime('now', '-365 days'), datetime('now', '-180 days'));


-- =============================================================================
-- 3. DELEGATIONS (Sample authority transfers)
-- =============================================================================
-- These represent typical delegation scenarios in the organization.
-- =============================================================================

-- Active delegation: Head of CSE delegates to a reviewer during conference travel
INSERT INTO Delegations (Id, DelegatorId, DelegateId, StartDate, EndDate, Reason, Scope, IsActive, CreatedAt, RevokedAt)
VALUES (1, 9, 31, date('now', '-2 days'), date('now', '+12 days'), 'International conference attendance - IEEE 2026', 'Full', 1, datetime('now', '-3 days'), NULL);

-- Active delegation: Head of Software Dev delegates approval to Web Section Lead
INSERT INTO Delegations (Id, DelegatorId, DelegateId, StartDate, EndDate, Reason, Scope, IsActive, CreatedAt, RevokedAt)
VALUES (2, 18, 22, date('now', '-1 day'), date('now', '+6 days'), 'Annual leave', 'ApprovalOnly', 1, datetime('now', '-2 days'), NULL);

-- Upcoming delegation: Head of Infrastructure will delegate next month
INSERT INTO Delegations (Id, DelegatorId, DelegateId, StartDate, EndDate, Reason, Scope, IsActive, CreatedAt, RevokedAt)
VALUES (3, 19, 24, date('now', '+14 days'), date('now', '+28 days'), 'Training program abroad', 'Full', 1, datetime('now'), NULL);

-- Past delegation: VP Admin delegated to Dean Main Campus (already completed)
INSERT INTO Delegations (Id, DelegatorId, DelegateId, StartDate, EndDate, Reason, Scope, IsActive, CreatedAt, RevokedAt)
VALUES (4, 3, 4, date('now', '-60 days'), date('now', '-45 days'), 'Medical leave recovery', 'Full', 1, datetime('now', '-65 days'), NULL);

-- Revoked delegation: Head of HR revoked early
INSERT INTO Delegations (Id, DelegatorId, DelegateId, StartDate, EndDate, Reason, Scope, IsActive, CreatedAt, RevokedAt)
VALUES (5, 20, 55, date('now', '-10 days'), date('now', '+5 days'), 'Business travel - cancelled', 'ReportingOnly', 0, datetime('now', '-12 days'), datetime('now', '-8 days'));

-- Active reporting-only delegation in MET faculty
INSERT INTO Delegations (Id, DelegatorId, DelegateId, StartDate, EndDate, Reason, Scope, IsActive, CreatedAt, RevokedAt)
VALUES (6, 13, 33, date('now'), date('now', '+20 days'), 'Sabbatical research period', 'ReportingOnly', 1, datetime('now', '-1 day'), NULL);


-- =============================================================================
-- 4. SAMPLE NOTIFICATIONS
-- =============================================================================
-- Pre-populate some notifications so the notification system shows data.
-- NotificationType: 0=ReportSubmitted, 1=ReportApproved, 2=ReportRejected,
--   3=FeedbackReceived, 4=DecisionMade, 5=RecommendationIssued,
--   6=ConfirmationRequested, 7=DeadlineApproaching, 8=General
-- NotificationPriority: 0=Low, 1=Normal, 2=High, 3=Urgent
-- =============================================================================

-- Welcome notifications for key users
INSERT INTO Notifications (Id, UserId, Type, Title, Message, ActionUrl, IsRead, CreatedAt, ReadAt, Priority, RelatedEntityId)
VALUES (1, '6', 8, 'Welcome to HORS', 'Welcome to the Hierarchical Organizational Reporting System. Your administrator account has been configured.', '/Admin/Dashboard', 0, datetime('now'), NULL, 1, NULL);

INSERT INTO Notifications (Id, UserId, Type, Title, Message, ActionUrl, IsRead, CreatedAt, ReadAt, Priority, RelatedEntityId)
VALUES (2, '1', 8, 'System Ready', 'The reporting system is now configured with organizational units and user accounts. You may begin reviewing reports.', '/Admin/Dashboard', 0, datetime('now'), NULL, 1, NULL);

INSERT INTO Notifications (Id, UserId, Type, Title, Message, ActionUrl, IsRead, CreatedAt, ReadAt, Priority, RelatedEntityId)
VALUES (3, '9', 7, 'Delegation Active', 'Your authority has been delegated to Dr. Hany Mourad while you attend the IEEE conference.', '/Admin/Delegations', 1, datetime('now', '-2 days'), datetime('now', '-2 days'), 2, 1);

INSERT INTO Notifications (Id, UserId, Type, Title, Message, ActionUrl, IsRead, CreatedAt, ReadAt, Priority, RelatedEntityId)
VALUES (4, '31', 7, 'Delegation Received', 'You have received delegated authority from Prof. Nadia Kamel (Head of CSE). This delegation is active until the end of the conference period.', '/Admin/Delegations', 1, datetime('now', '-2 days'), datetime('now', '-1 day'), 2, 1);

INSERT INTO Notifications (Id, UserId, Type, Title, Message, ActionUrl, IsRead, CreatedAt, ReadAt, Priority, RelatedEntityId)
VALUES (5, '18', 7, 'Delegation Active', 'Your approval authority has been delegated to Eng. Ali Kamal during your annual leave.', '/Admin/Delegations', 0, datetime('now', '-1 day'), NULL, 1, 2);


-- =============================================================================
-- SUMMARY
-- =============================================================================
-- Organizational Units: 36 units across 6 levels
--   - 1 Root (GUC)
--   - 2 Campuses (Main, New Capital)
--   - 8 Faculties/Divisions
--   - 14 Departments
--   - 6 Sectors/Sections
--   - 5 Teams
--
-- Users: 60 accounts across all roles
--   - 5 Executives (President, VPs, Deans)
--   - 3 Administrators (System admins)
--   - 13 Department Heads
--   - 9 Team Managers
--   - 6 Report Reviewers
--   - 20 Report Originators (developers, faculty, HR staff)
--   - 3 Auditors
--   - 1 Inactive user (for testing)
--
-- Delegations: 6 samples
--   - 3 Active (including 1 reporting-only)
--   - 1 Upcoming (starts in 2 weeks)
--   - 1 Past (completed)
--   - 1 Revoked
--
-- Notifications: 5 initial system notifications
-- =============================================================================
