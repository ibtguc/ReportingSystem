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

-- ---- Administrative Services Division (Level 2) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (37, 'Administrative Services Division', 'ADMIN', 'Central Administrative Services', 2, 2, 7, 1, datetime('now'), NULL);

-- ---- Administrative Departments (Level 3) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (38, 'Finance Office', 'CFO', 'Central Finance and Budget Office', 3, 37, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (39, 'Legal & Compliance', 'LEGAL', 'Legal Affairs and Regulatory Compliance', 3, 37, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (40, 'Procurement & Contracts', 'PROC', 'Procurement and Contract Management', 3, 37, 3, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (41, 'Student Affairs', 'STUD', 'Student Services and Support', 3, 37, 4, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (42, 'Facilities Management', 'FAC', 'Campus Facilities and Maintenance', 3, 37, 5, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (43, 'Marketing & Communications', 'MKTG', 'Marketing, PR, and External Communications', 3, 37, 6, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (44, 'Research & Innovation', 'RIO', 'Research Support and Innovation Office', 3, 37, 7, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (45, 'International Relations', 'INTL', 'International Partnerships and Exchange Programs', 3, 37, 8, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (46, 'Library & Information Services', 'LIB', 'University Library and Digital Resources', 3, 37, 9, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (47, 'Security & Safety', 'SEC', 'Campus Security and Safety Management', 3, 37, 10, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (48, 'Registrar Office', 'REG', 'Student Registration and Academic Records', 3, 37, 11, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (49, 'Career Services', 'CAREER', 'Career Development and Alumni Relations', 3, 37, 12, 1, datetime('now'), NULL);

-- ---- Student Affairs Sections (Level 4) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (50, 'Admissions Office', 'STUD-ADM', 'Student Admissions and Enrollment', 4, 41, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (51, 'Student Counseling', 'STUD-COUNS', 'Student Counseling and Wellness', 4, 41, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (52, 'Student Activities', 'STUD-ACT', 'Clubs, Events, and Student Life', 4, 41, 3, 1, datetime('now'), NULL);

-- ---- Facilities Management Sections (Level 4) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (53, 'Building Maintenance', 'FAC-MAINT', 'Building and Infrastructure Maintenance', 4, 42, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (54, 'Grounds & Landscaping', 'FAC-GRND', 'Grounds and Landscaping Services', 4, 42, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (55, 'Transportation Services', 'FAC-TRANS', 'Campus Transportation and Fleet', 4, 42, 3, 1, datetime('now'), NULL);

-- ---- Finance Office Sections (Level 4) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (56, 'Accounts Payable', 'CFO-AP', 'Accounts Payable and Vendor Payments', 4, 38, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (57, 'Accounts Receivable', 'CFO-AR', 'Accounts Receivable and Collections', 4, 38, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (58, 'Budget & Planning', 'CFO-BUD', 'Budget Planning and Analysis', 4, 38, 3, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (59, 'Payroll', 'CFO-PAY', 'Payroll Administration', 4, 38, 4, 1, datetime('now'), NULL);

-- ---- HR Sections (Level 4) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (60, 'Recruitment & Hiring', 'HR-REC', 'Talent Acquisition and Recruitment', 4, 24, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (61, 'Training & Development', 'HR-TRN', 'Employee Training and Development', 4, 24, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (62, 'Compensation & Benefits', 'HR-COMP', 'Compensation and Benefits Administration', 4, 24, 3, 1, datetime('now'), NULL);

-- ---- Library Sections (Level 4) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (63, 'Circulation Services', 'LIB-CIRC', 'Library Circulation and Lending', 4, 46, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (64, 'Digital Resources', 'LIB-DIG', 'Digital Library and E-Resources', 4, 46, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (65, 'Archives & Special Collections', 'LIB-ARCH', 'Archives and Special Collections', 4, 46, 3, 1, datetime('now'), NULL);

-- ---- Marketing Sections (Level 4) ----
INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (66, 'Digital Marketing', 'MKTG-DIG', 'Digital Marketing and Social Media', 4, 43, 1, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (67, 'Public Relations', 'MKTG-PR', 'Public Relations and Media', 4, 43, 2, 1, datetime('now'), NULL);

INSERT INTO OrganizationalUnits (Id, Name, Code, Description, Level, ParentId, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES (68, 'Events Management', 'MKTG-EVT', 'University Events and Conferences', 4, 43, 3, 1, datetime('now'), NULL);


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
VALUES (60, 'former.staff@guc.edu.eg', 'Dr. Hamed Farouk', 'ReportOriginator', NULL, 'Former Faculty Member', 0, datetime('now', '-365 days'), datetime('now', '-180 days'));

-- ===========================================================================
-- ADDITIONAL ADMINISTRATIVE DEPARTMENT HEADS (New departments)
-- ===========================================================================

-- Administrative Services Division heads
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (61, 'head.admin@guc.edu.eg', 'Dr. Nadia Abdel-Rahman', 'Executive', 37, 'Director of Administrative Services', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (62, 'head.cfo@guc.edu.eg', 'Mr. Karim Mansour', 'DepartmentHead', 38, 'Chief Financial Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (63, 'head.legal@guc.edu.eg', 'Dr. Laila Ghanem', 'DepartmentHead', 39, 'Head of Legal & Compliance', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (64, 'head.proc@guc.edu.eg', 'Mr. Ashraf El-Deeb', 'DepartmentHead', 40, 'Head of Procurement', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (65, 'head.student@guc.edu.eg', 'Dr. Rania Fouad', 'DepartmentHead', 41, 'Dean of Students', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (66, 'head.facilities@guc.edu.eg', 'Eng. Mohsen Abdallah', 'DepartmentHead', 42, 'Head of Facilities Management', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (67, 'head.marketing@guc.edu.eg', 'Ms. Dina El-Sayed', 'DepartmentHead', 43, 'Head of Marketing & Communications', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (68, 'head.research@guc.edu.eg', 'Prof. Tarek Zaki', 'DepartmentHead', 44, 'Director of Research & Innovation', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (69, 'head.intl@guc.edu.eg', 'Dr. Yasmin Rashid', 'DepartmentHead', 45, 'Director of International Relations', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (70, 'head.library@guc.edu.eg', 'Dr. Mahmoud Salem', 'DepartmentHead', 46, 'University Librarian', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (71, 'head.security@guc.edu.eg', 'Col. Ahmed El-Mahdy', 'DepartmentHead', 47, 'Chief Security Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (72, 'head.registrar@guc.edu.eg', 'Ms. Fatma Nour', 'DepartmentHead', 48, 'University Registrar', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (73, 'head.career@guc.edu.eg', 'Mr. Hany Ibrahim', 'DepartmentHead', 49, 'Director of Career Services', 1, datetime('now'), NULL);

-- ===========================================================================
-- SECTION MANAGERS / TEAM MANAGERS (New sections)
-- ===========================================================================

-- Student Affairs section managers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (74, 'mgr.admissions@guc.edu.eg', 'Ms. Reem Abdel-Aziz', 'TeamManager', 50, 'Admissions Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (75, 'mgr.counseling@guc.edu.eg', 'Dr. Noha Fathy', 'TeamManager', 51, 'Head Counselor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (76, 'mgr.activities@guc.edu.eg', 'Mr. Omar Tawfik', 'TeamManager', 52, 'Student Activities Coordinator', 1, datetime('now'), NULL);

-- Facilities section managers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (77, 'mgr.maintenance@guc.edu.eg', 'Eng. Saeed Mostafa', 'TeamManager', 53, 'Maintenance Supervisor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (78, 'mgr.grounds@guc.edu.eg', 'Mr. Ibrahim Khalil', 'TeamManager', 54, 'Grounds Supervisor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (79, 'mgr.transport@guc.edu.eg', 'Mr. Mahmoud Ramzy', 'TeamManager', 55, 'Transportation Manager', 1, datetime('now'), NULL);

-- Finance section managers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (80, 'mgr.ap@guc.edu.eg', 'Ms. Heba Kamel', 'TeamManager', 56, 'Accounts Payable Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (81, 'mgr.ar@guc.edu.eg', 'Mr. Wael Hassan', 'TeamManager', 57, 'Accounts Receivable Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (82, 'mgr.budget@guc.edu.eg', 'Ms. Amira Yousry', 'TeamManager', 58, 'Budget & Planning Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (83, 'mgr.payroll@guc.edu.eg', 'Ms. Nevine Adel', 'TeamManager', 59, 'Payroll Manager', 1, datetime('now'), NULL);

-- HR section managers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (84, 'mgr.recruitment@guc.edu.eg', 'Ms. Sara Farid', 'TeamManager', 60, 'Recruitment Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (85, 'mgr.training@guc.edu.eg', 'Mr. Khaled El-Masry', 'TeamManager', 61, 'Training Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (86, 'mgr.benefits@guc.edu.eg', 'Ms. Mona Sayed', 'TeamManager', 62, 'Benefits Administrator', 1, datetime('now'), NULL);

-- Library section managers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (87, 'mgr.circulation@guc.edu.eg', 'Ms. Hanan Mostafa', 'TeamManager', 63, 'Circulation Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (88, 'mgr.digital@guc.edu.eg', 'Mr. Tamer El-Naggar', 'TeamManager', 64, 'Digital Resources Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (89, 'mgr.archives@guc.edu.eg', 'Ms. Azza Soliman', 'TeamManager', 65, 'Archives Manager', 1, datetime('now'), NULL);

-- Marketing section managers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (90, 'mgr.digmktg@guc.edu.eg', 'Mr. Yasser Reda', 'TeamManager', 66, 'Digital Marketing Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (91, 'mgr.pr@guc.edu.eg', 'Ms. Eman Gamal', 'TeamManager', 67, 'Public Relations Manager', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (92, 'mgr.events@guc.edu.eg', 'Ms. Layla Hamdy', 'TeamManager', 68, 'Events Manager', 1, datetime('now'), NULL);

-- ===========================================================================
-- REPORT REVIEWERS (Additional senior staff)
-- ===========================================================================

-- Finance reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (93, 'reviewer.finance@guc.edu.eg', 'Mr. Samir Abdel-Wahab', 'ReportReviewer', 38, 'Senior Financial Analyst', 1, datetime('now'), NULL);

-- Legal reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (94, 'reviewer.legal@guc.edu.eg', 'Ms. Mariam Shehata', 'ReportReviewer', 39, 'Senior Legal Counsel', 1, datetime('now'), NULL);

-- Student Affairs reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (95, 'reviewer.student@guc.edu.eg', 'Dr. Ahmed Fouad', 'ReportReviewer', 41, 'Senior Student Advisor', 1, datetime('now'), NULL);

-- Facilities reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (96, 'reviewer.facilities@guc.edu.eg', 'Eng. Hossam El-Din', 'ReportReviewer', 42, 'Senior Facilities Engineer', 1, datetime('now'), NULL);

-- Marketing reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (97, 'reviewer.mktg@guc.edu.eg', 'Ms. Nadia Kamal', 'ReportReviewer', 43, 'Senior Marketing Specialist', 1, datetime('now'), NULL);

-- Research reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (98, 'reviewer.research@guc.edu.eg', 'Dr. Adel El-Sayed', 'ReportReviewer', 44, 'Senior Research Coordinator', 1, datetime('now'), NULL);

-- Library reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (99, 'reviewer.library@guc.edu.eg', 'Ms. Dalia Ibrahim', 'ReportReviewer', 46, 'Senior Librarian', 1, datetime('now'), NULL);

-- Engineering faculty additional reviewers
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (100, 'reviewer.eng1@guc.edu.eg', 'Dr. Hossam Shalaby', 'ReportReviewer', 4, 'Associate Professor - Engineering', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (101, 'reviewer.eng2@guc.edu.eg', 'Dr. Iman Rasheed', 'ReportReviewer', 4, 'Assistant Professor - Engineering', 1, datetime('now'), NULL);

-- Management faculty reviewer
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (102, 'reviewer.mgt2@guc.edu.eg', 'Dr. Yehia Abdel-Fattah', 'ReportReviewer', 6, 'Associate Professor - Management', 1, datetime('now'), NULL);

-- ===========================================================================
-- REPORT ORIGINATORS (Staff across all departments)
-- ===========================================================================

-- ---- Finance Office staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (103, 'fin.ap1@guc.edu.eg', 'Ms. Sherine Mahmoud', 'ReportOriginator', 56, 'Accounts Payable Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (104, 'fin.ap2@guc.edu.eg', 'Mr. Ahmed Wagdy', 'ReportOriginator', 56, 'Accounts Payable Clerk', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (105, 'fin.ar1@guc.edu.eg', 'Ms. Manal Fawzy', 'ReportOriginator', 57, 'Accounts Receivable Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (106, 'fin.ar2@guc.edu.eg', 'Mr. Hesham Nabil', 'ReportOriginator', 57, 'Collections Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (107, 'fin.budget1@guc.edu.eg', 'Ms. Salwa Ahmed', 'ReportOriginator', 58, 'Budget Analyst', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (108, 'fin.payroll1@guc.edu.eg', 'Ms. Nagwa Farouk', 'ReportOriginator', 59, 'Payroll Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (109, 'fin.payroll2@guc.edu.eg', 'Mr. Mohamed Fathy', 'ReportOriginator', 59, 'Payroll Clerk', 1, datetime('now'), NULL);

-- ---- Legal & Compliance staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (110, 'legal.counsel1@guc.edu.eg', 'Mr. Sherif El-Ghandour', 'ReportOriginator', 39, 'Legal Counsel', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (111, 'legal.compliance@guc.edu.eg', 'Ms. Noura Hafez', 'ReportOriginator', 39, 'Compliance Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (112, 'legal.contracts@guc.edu.eg', 'Mr. Hazem Barakat', 'ReportOriginator', 39, 'Contracts Specialist', 1, datetime('now'), NULL);

-- ---- Procurement staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (113, 'proc.buyer1@guc.edu.eg', 'Mr. Adel Shokry', 'ReportOriginator', 40, 'Senior Buyer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (114, 'proc.buyer2@guc.edu.eg', 'Ms. Ghada Lotfy', 'ReportOriginator', 40, 'Procurement Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (115, 'proc.vendor@guc.edu.eg', 'Mr. Tariq El-Shafiey', 'ReportOriginator', 40, 'Vendor Manager', 1, datetime('now'), NULL);

-- ---- Student Affairs staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (116, 'student.adm1@guc.edu.eg', 'Ms. Asmaa Ragab', 'ReportOriginator', 50, 'Admissions Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (117, 'student.adm2@guc.edu.eg', 'Mr. Ahmed El-Bakry', 'ReportOriginator', 50, 'Admissions Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (118, 'student.adm3@guc.edu.eg', 'Ms. Fatma Hassan', 'ReportOriginator', 50, 'Admissions Assistant', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (119, 'student.couns1@guc.edu.eg', 'Dr. Ramy Shawky', 'ReportOriginator', 51, 'Student Counselor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (120, 'student.couns2@guc.edu.eg', 'Ms. Maha El-Sherif', 'ReportOriginator', 51, 'Wellness Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (121, 'student.act1@guc.edu.eg', 'Mr. Mostafa Gamal', 'ReportOriginator', 52, 'Activities Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (122, 'student.act2@guc.edu.eg', 'Ms. Yasmine Nour', 'ReportOriginator', 52, 'Clubs Administrator', 1, datetime('now'), NULL);

-- ---- Facilities staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (123, 'fac.maint1@guc.edu.eg', 'Mr. Sobhy Mohamed', 'ReportOriginator', 53, 'Maintenance Technician', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (124, 'fac.maint2@guc.edu.eg', 'Mr. Essam Abdel-Aziz', 'ReportOriginator', 53, 'HVAC Technician', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (125, 'fac.maint3@guc.edu.eg', 'Mr. Ramadan Sayed', 'ReportOriginator', 53, 'Electrician', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (126, 'fac.grounds1@guc.edu.eg', 'Mr. Abdel-Hakim Fathy', 'ReportOriginator', 54, 'Groundskeeper', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (127, 'fac.trans1@guc.edu.eg', 'Mr. Saber Hassan', 'ReportOriginator', 55, 'Fleet Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (128, 'fac.trans2@guc.edu.eg', 'Mr. Gamal Abdel-Nasser', 'ReportOriginator', 55, 'Transport Scheduler', 1, datetime('now'), NULL);

-- ---- Marketing staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (129, 'mktg.digital1@guc.edu.eg', 'Ms. Rania El-Khouly', 'ReportOriginator', 66, 'Social Media Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (130, 'mktg.digital2@guc.edu.eg', 'Mr. Amr Shawky', 'ReportOriginator', 66, 'Content Creator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (131, 'mktg.pr1@guc.edu.eg', 'Ms. Sahar Mohamed', 'ReportOriginator', 67, 'PR Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (132, 'mktg.pr2@guc.edu.eg', 'Mr. Kareem Helal', 'ReportOriginator', 67, 'Media Relations Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (133, 'mktg.events1@guc.edu.eg', 'Ms. Marwa Tawfik', 'ReportOriginator', 68, 'Events Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (134, 'mktg.events2@guc.edu.eg', 'Mr. Tarek El-Guindy', 'ReportOriginator', 68, 'Conference Planner', 1, datetime('now'), NULL);

-- ---- Research & Innovation staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (135, 'research.coord@guc.edu.eg', 'Dr. Samia El-Banna', 'ReportOriginator', 44, 'Research Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (136, 'research.grants@guc.edu.eg', 'Ms. Ola Abdel-Hamid', 'ReportOriginator', 44, 'Grants Administrator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (137, 'research.ip@guc.edu.eg', 'Mr. Hesham Khairy', 'ReportOriginator', 44, 'IP & Technology Transfer Officer', 1, datetime('now'), NULL);

-- ---- International Relations staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (138, 'intl.exchange@guc.edu.eg', 'Ms. Nermeen Samir', 'ReportOriginator', 45, 'Exchange Program Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (139, 'intl.partner@guc.edu.eg', 'Mr. Walid Helmy', 'ReportOriginator', 45, 'Partnership Development Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (140, 'intl.visa@guc.edu.eg', 'Ms. Lobna Abdel-Azim', 'ReportOriginator', 45, 'International Student Advisor', 1, datetime('now'), NULL);

-- ---- Library staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (141, 'lib.circ1@guc.edu.eg', 'Ms. Hala Mostafa', 'ReportOriginator', 63, 'Circulation Librarian', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (142, 'lib.circ2@guc.edu.eg', 'Mr. Ramy El-Fiky', 'ReportOriginator', 63, 'Library Assistant', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (143, 'lib.digital1@guc.edu.eg', 'Mr. Emad Khalifa', 'ReportOriginator', 64, 'Digital Resources Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (144, 'lib.digital2@guc.edu.eg', 'Ms. Aya Mohamed', 'ReportOriginator', 64, 'E-Resources Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (145, 'lib.archives@guc.edu.eg', 'Ms. Dina Abdel-Moneim', 'ReportOriginator', 65, 'Archivist', 1, datetime('now'), NULL);

-- ---- Security staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (146, 'sec.ops1@guc.edu.eg', 'Mr. Mohamed El-Sharkawy', 'ReportOriginator', 47, 'Security Operations Officer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (147, 'sec.safety@guc.edu.eg', 'Mr. Abdel-Rahman Youssef', 'ReportOriginator', 47, 'Safety Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (148, 'sec.emergency@guc.edu.eg', 'Mr. Hazem El-Badry', 'ReportOriginator', 47, 'Emergency Response Coordinator', 1, datetime('now'), NULL);

-- ---- Registrar staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (149, 'reg.records@guc.edu.eg', 'Ms. Nadia Hassan', 'ReportOriginator', 48, 'Records Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (150, 'reg.schedule@guc.edu.eg', 'Mr. Ali Abdel-Ghani', 'ReportOriginator', 48, 'Academic Scheduler', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (151, 'reg.transcript@guc.edu.eg', 'Ms. Soha Ramadan', 'ReportOriginator', 48, 'Transcripts Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (152, 'reg.graduation@guc.edu.eg', 'Ms. Heba El-Sawaf', 'ReportOriginator', 48, 'Graduation Coordinator', 1, datetime('now'), NULL);

-- ---- Career Services staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (153, 'career.advisor1@guc.edu.eg', 'Ms. Rasha Abdel-Salam', 'ReportOriginator', 49, 'Career Advisor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (154, 'career.intern@guc.edu.eg', 'Mr. Hazem Taha', 'ReportOriginator', 49, 'Internship Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (155, 'career.alumni@guc.edu.eg', 'Ms. Mai El-Sherbiny', 'ReportOriginator', 49, 'Alumni Relations Officer', 1, datetime('now'), NULL);

-- ---- HR additional staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (156, 'hr.rec1@guc.edu.eg', 'Ms. Dalia Fawzy', 'ReportOriginator', 60, 'Talent Acquisition Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (157, 'hr.rec2@guc.edu.eg', 'Mr. Karim Essam', 'ReportOriginator', 60, 'Recruitment Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (158, 'hr.training1@guc.edu.eg', 'Ms. Lamia Abdel-Fattah', 'ReportOriginator', 61, 'L&D Specialist', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (159, 'hr.training2@guc.edu.eg', 'Mr. Bassem Shokry', 'ReportOriginator', 61, 'Training Coordinator', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (160, 'hr.comp1@guc.edu.eg', 'Ms. Enas Mohamed', 'ReportOriginator', 62, 'Compensation Analyst', 1, datetime('now'), NULL);

-- ---- Additional academic faculty staff ----
-- CSE additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (161, 'faculty.cse3@guc.edu.eg', 'Dr. Mohamed El-Menshawy', 'ReportOriginator', 12, 'Senior Lecturer - CSE', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (162, 'faculty.cse4@guc.edu.eg', 'Dr. Heba Abdel-Aal', 'ReportOriginator', 12, 'Assistant Professor - CSE', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (163, 'faculty.cse5@guc.edu.eg', 'Eng. Tarek Fathy', 'ReportOriginator', 12, 'Teaching Assistant - CSE', 1, datetime('now'), NULL);

-- Mechanical Engineering additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (164, 'faculty.me2@guc.edu.eg', 'Dr. Sherif El-Gohary', 'ReportOriginator', 13, 'Associate Professor - ME', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (165, 'faculty.me3@guc.edu.eg', 'Dr. Amira Khairy', 'ReportOriginator', 13, 'Assistant Professor - ME', 1, datetime('now'), NULL);

-- ECE additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (166, 'faculty.ece1@guc.edu.eg', 'Dr. Nabil El-Fishawy', 'ReportOriginator', 14, 'Senior Lecturer - ECE', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (167, 'faculty.ece2@guc.edu.eg', 'Dr. Reem Bahgat', 'ReportOriginator', 14, 'Assistant Professor - ECE', 1, datetime('now'), NULL);

-- Architecture additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (168, 'faculty.arch1@guc.edu.eg', 'Dr. Mona El-Tayeb', 'ReportOriginator', 15, 'Senior Lecturer - Architecture', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (169, 'faculty.arch2@guc.edu.eg', 'Dr. Yasser Wahba', 'ReportOriginator', 15, 'Assistant Professor - Architecture', 1, datetime('now'), NULL);

-- MET CS additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (170, 'faculty.cs2@guc.edu.eg', 'Dr. Eman Helal', 'ReportOriginator', 16, 'Associate Professor - CS', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (171, 'faculty.cs3@guc.edu.eg', 'Dr. Ahmed Fathy', 'ReportOriginator', 16, 'Assistant Professor - CS', 1, datetime('now'), NULL);

-- Digital Media additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (172, 'faculty.dm2@guc.edu.eg', 'Dr. Sherine Fouad', 'ReportOriginator', 17, 'Senior Lecturer - Digital Media', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (173, 'faculty.dm3@guc.edu.eg', 'Dr. Wessam Abdel-Wahab', 'ReportOriginator', 17, 'Assistant Professor - Digital Media', 1, datetime('now'), NULL);

-- Networks additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (174, 'faculty.nis1@guc.edu.eg', 'Dr. Karim El-Shabrawy', 'ReportOriginator', 18, 'Senior Lecturer - Networks', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (175, 'faculty.nis2@guc.edu.eg', 'Dr. Hoda Khalifa', 'ReportOriginator', 18, 'Assistant Professor - Networks', 1, datetime('now'), NULL);

-- Economics additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (176, 'faculty.econ2@guc.edu.eg', 'Dr. Amr El-Shafei', 'ReportOriginator', 19, 'Associate Professor - Economics', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (177, 'faculty.econ3@guc.edu.eg', 'Dr. Nevine Salem', 'ReportOriginator', 19, 'Assistant Professor - Economics', 1, datetime('now'), NULL);

-- Finance & Accounting (academic) additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (178, 'faculty.fin2@guc.edu.eg', 'Dr. Hazem El-Nahas', 'ReportOriginator', 20, 'Associate Professor - Finance', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (179, 'faculty.fin3@guc.edu.eg', 'Dr. Salma Mostafa', 'ReportOriginator', 20, 'Assistant Professor - Accounting', 1, datetime('now'), NULL);

-- Management (academic) additional faculty
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (180, 'faculty.mgmt1@guc.edu.eg', 'Dr. Tamer Abdel-Ghaffar', 'ReportOriginator', 21, 'Senior Lecturer - Management', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (181, 'faculty.mgmt2@guc.edu.eg', 'Dr. Nihal Fathy', 'ReportOriginator', 21, 'Assistant Professor - Management', 1, datetime('now'), NULL);

-- ---- Additional IT staff ----
-- Software Development additional
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (182, 'dev.backend3@guc.edu.eg', 'Eng. Hany Abdel-Malak', 'ReportOriginator', 32, 'Senior Backend Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (183, 'dev.backend4@guc.edu.eg', 'Eng. Marwa Sobhy', 'ReportOriginator', 32, 'Backend Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (184, 'dev.frontend3@guc.edu.eg', 'Eng. Maged El-Deeb', 'ReportOriginator', 33, 'Senior Frontend Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (185, 'dev.frontend4@guc.edu.eg', 'Eng. Nora Samy', 'ReportOriginator', 33, 'UI Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (186, 'qa.tester2@guc.edu.eg', 'Eng. Ahmed El-Gammal', 'ReportOriginator', 34, 'Senior QA Engineer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (187, 'qa.tester3@guc.edu.eg', 'Eng. Heba Saad', 'ReportOriginator', 34, 'Automation Engineer', 1, datetime('now'), NULL);

-- Mobile additional
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (188, 'dev.mobile3@guc.edu.eg', 'Eng. Youssef Abdel-Rahim', 'ReportOriginator', 35, 'Senior iOS Developer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (189, 'dev.mobile4@guc.edu.eg', 'Eng. Sara El-Kady', 'ReportOriginator', 36, 'Senior Android Developer', 1, datetime('now'), NULL);

-- Infrastructure additional
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (190, 'netops3@guc.edu.eg', 'Eng. Ahmed Lotfy', 'ReportOriginator', 28, 'Senior Network Engineer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (191, 'cloud2@guc.edu.eg', 'Eng. Hazem Shawky', 'ReportOriginator', 29, 'Senior Cloud Engineer', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (192, 'sysadmin2@guc.edu.eg', 'Eng. Ola Magdy', 'ReportOriginator', 29, 'Systems Administrator', 1, datetime('now'), NULL);

-- ---- AI & ML Section staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (193, 'ai.researcher1@guc.edu.eg', 'Dr. Khaled El-Ayat', 'ReportOriginator', 30, 'AI Researcher', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (194, 'ai.researcher2@guc.edu.eg', 'Dr. Noha Ghanem', 'ReportOriginator', 30, 'ML Engineer', 1, datetime('now'), NULL);

-- ---- Software Engineering Section staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (195, 'se.researcher1@guc.edu.eg', 'Dr. Amr Abdel-Hamid', 'ReportOriginator', 31, 'SE Researcher', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (196, 'se.researcher2@guc.edu.eg', 'Dr. Hana El-Sherif', 'ReportOriginator', 31, 'DevOps Researcher', 1, datetime('now'), NULL);

-- ---- Additional QA & Audit staff ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (197, 'qa.analyst1@guc.edu.eg', 'Ms. Samar Raafat', 'ReportOriginator', 25, 'Quality Analyst', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (198, 'qa.analyst2@guc.edu.eg', 'Mr. Mohamed Abdel-Aziz', 'ReportOriginator', 25, 'Compliance Analyst', 1, datetime('now'), NULL);

-- ---- Additional Auditors ----
INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (199, 'auditor4@guc.edu.eg', 'Mr. Wael El-Shaarawy', 'Auditor', 25, 'Senior Internal Auditor', 1, datetime('now'), NULL);

INSERT INTO Users (Id, Email, Name, Role, OrganizationalUnitId, JobTitle, IsActive, CreatedAt, LastLoginAt)
VALUES (200, 'auditor5@guc.edu.eg', 'Ms. Yasmine Fouad', 'Auditor', 38, 'Financial Auditor', 1, datetime('now'), NULL);


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
-- 5. REPORT TEMPLATES
-- =============================================================================
-- Define the report templates that users will fill out.
-- Schedule: Daily, Weekly, BiWeekly, Monthly, Quarterly, Annual, Custom
-- =============================================================================

DELETE FROM ReportFieldValues;
DELETE FROM Attachments;
DELETE FROM Reports;
DELETE FROM ReportPeriods;
DELETE FROM ReportFields;
DELETE FROM ReportTemplateAssignments;
DELETE FROM ReportTemplates;
DELETE FROM sqlite_sequence WHERE name IN ('ReportTemplates', 'ReportTemplateAssignments', 'ReportFields', 'ReportPeriods', 'Reports', 'ReportFieldValues', 'Attachments');

-- Template 1: Monthly Department Status Report
INSERT INTO ReportTemplates (Id, Name, Description, Schedule, Version, VersionNotes, IncludeSuggestedActions, IncludeNeededResources, IncludeNeededSupport, AutoSaveIntervalSeconds, AllowPrePopulation, AllowBulkImport, MaxAttachmentSizeMb, AllowedFileTypes, IsActive, CreatedAt, UpdatedAt, CreatedById)
VALUES (1, 'Monthly Department Status Report', 'Standard monthly report for department heads covering KPIs, activities, challenges, and resource needs.', 'Monthly', 1, 'Initial template version', 1, 1, 1, 60, 1, 0, 10, '.pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg', 1, datetime('now'), NULL, 6);

-- Template 2: Weekly Team Progress Report
INSERT INTO ReportTemplates (Id, Name, Description, Schedule, Version, VersionNotes, IncludeSuggestedActions, IncludeNeededResources, IncludeNeededSupport, AutoSaveIntervalSeconds, AllowPrePopulation, AllowBulkImport, MaxAttachmentSizeMb, AllowedFileTypes, IsActive, CreatedAt, UpdatedAt, CreatedById)
VALUES (2, 'Weekly Team Progress Report', 'Weekly progress report for team leads covering sprint progress, blockers, and upcoming work.', 'Weekly', 1, 'Initial template version', 1, 0, 1, 30, 1, 0, 5, '.pdf,.doc,.docx,.png,.jpg,.jpeg', 1, datetime('now'), NULL, 6);

-- Template 3: Quarterly Academic Performance Report
INSERT INTO ReportTemplates (Id, Name, Description, Schedule, Version, VersionNotes, IncludeSuggestedActions, IncludeNeededResources, IncludeNeededSupport, AutoSaveIntervalSeconds, AllowPrePopulation, AllowBulkImport, MaxAttachmentSizeMb, AllowedFileTypes, IsActive, CreatedAt, UpdatedAt, CreatedById)
VALUES (3, 'Quarterly Academic Performance Report', 'Quarterly report for faculty departments covering teaching effectiveness, research output, and student outcomes.', 'Quarterly', 1, 'Initial template version', 1, 1, 1, 60, 1, 1, 20, '.pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg,.pptx', 1, datetime('now'), NULL, 6);

-- Template 4: Annual Executive Summary Report
INSERT INTO ReportTemplates (Id, Name, Description, Schedule, Version, VersionNotes, IncludeSuggestedActions, IncludeNeededResources, IncludeNeededSupport, AutoSaveIntervalSeconds, AllowPrePopulation, AllowBulkImport, MaxAttachmentSizeMb, AllowedFileTypes, IsActive, CreatedAt, UpdatedAt, CreatedById)
VALUES (4, 'Annual Executive Summary Report', 'Annual summary report prepared by campus deans and division heads for the university president.', 'Annual', 1, 'Initial template version', 1, 1, 1, 60, 1, 1, 50, '.pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg,.pptx', 1, datetime('now'), NULL, 6);

-- Template 5: IT Infrastructure Health Report (monthly)
INSERT INTO ReportTemplates (Id, Name, Description, Schedule, Version, VersionNotes, IncludeSuggestedActions, IncludeNeededResources, IncludeNeededSupport, AutoSaveIntervalSeconds, AllowPrePopulation, AllowBulkImport, MaxAttachmentSizeMb, AllowedFileTypes, IsActive, CreatedAt, UpdatedAt, CreatedById)
VALUES (5, 'IT Infrastructure Health Report', 'Monthly report on IT infrastructure status, uptime, incidents, and capacity.', 'Monthly', 1, 'Initial template version', 1, 1, 0, 60, 1, 1, 10, '.pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg', 1, datetime('now'), NULL, 6);


-- =============================================================================
-- 6. REPORT FIELDS (Template field definitions)
-- =============================================================================
-- FieldType: 0=Text, 1=Numeric, 2=Date, 3=Dropdown, 4=Checkbox, 5=FileUpload, 6=RichText, 7=TableGrid
-- =============================================================================

-- ---- Template 1 Fields: Monthly Department Status Report ----
INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (1, 1, 'Department Summary', 'dept_summary', 'Provide a brief overview of the department''s status this month.', 6, 'General', 0, 1, 1, NULL, NULL, 50, 2000, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (2, 1, 'Total Active Staff', 'total_staff', 'Number of currently active staff in the department.', 1, 'Key Metrics', 1, 1, 1, 0, 500, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 1, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (3, 1, 'Projects Completed', 'projects_completed', 'Number of projects completed this month.', 1, 'Key Metrics', 1, 2, 1, 0, 100, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (4, 1, 'Projects In Progress', 'projects_in_progress', 'Number of projects currently in progress.', 1, 'Key Metrics', 1, 3, 1, 0, 100, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (5, 1, 'Budget Utilization (%)', 'budget_utilization', 'Percentage of allocated budget used this month.', 1, 'Key Metrics', 1, 4, 1, 0, 100, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (6, 1, 'Overall Status', 'overall_status', 'Select the overall status of the department.', 3, 'Key Metrics', 1, 5, 1, NULL, NULL, NULL, NULL, NULL, NULL, '["On Track","At Risk","Behind Schedule","Exceeding Expectations"]', 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (7, 1, 'Key Achievements', 'achievements', 'List the key achievements this month.', 6, 'Activities', 2, 1, 0, NULL, NULL, NULL, 3000, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (8, 1, 'Current Challenges', 'challenges', 'Describe any challenges or blockers encountered.', 6, 'Activities', 2, 2, 0, NULL, NULL, NULL, 3000, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (9, 1, 'Plans for Next Month', 'next_month_plans', 'Key plans and objectives for the upcoming month.', 6, 'Planning', 3, 1, 0, NULL, NULL, NULL, 3000, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (10, 1, 'Requires Executive Attention', 'needs_attention', 'Check if this report requires immediate executive attention.', 4, 'Planning', 3, 2, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, 'false', 0, 1, datetime('now'));

-- ---- Template 2 Fields: Weekly Team Progress Report ----
INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (11, 2, 'Sprint/Week Summary', 'week_summary', 'Brief overview of the week''s progress.', 0, 'Summary', 0, 1, 1, NULL, NULL, 10, 500, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (12, 2, 'Tasks Completed', 'tasks_completed', 'Number of tasks completed this week.', 1, 'Metrics', 1, 1, 1, 0, 200, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (13, 2, 'Tasks In Progress', 'tasks_in_progress', 'Number of tasks currently being worked on.', 1, 'Metrics', 1, 2, 1, 0, 200, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (14, 2, 'Blockers', 'blockers', 'Describe any current blockers preventing progress.', 6, 'Issues', 2, 1, 0, NULL, NULL, NULL, 2000, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (15, 2, 'Team Velocity', 'velocity', 'Story points or tasks completed per team member.', 1, 'Metrics', 1, 3, 0, 0, 100, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (16, 2, 'Next Week Focus', 'next_week', 'Key tasks and priorities for next week.', 0, 'Planning', 3, 1, 1, NULL, NULL, 10, 1000, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

-- ---- Template 5 Fields: IT Infrastructure Health Report ----
INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (17, 5, 'System Uptime (%)', 'uptime_pct', 'Overall system uptime percentage for the month.', 1, 'Availability', 0, 1, 1, 0, 100, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (18, 5, 'Total Incidents', 'total_incidents', 'Total number of infrastructure incidents this month.', 1, 'Incidents', 1, 1, 1, 0, 500, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (19, 5, 'Critical Incidents', 'critical_incidents', 'Number of critical (P1/P2) incidents.', 1, 'Incidents', 1, 2, 1, 0, 100, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (20, 5, 'Server Capacity Status', 'server_capacity', 'Current server capacity utilization level.', 3, 'Capacity', 2, 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, '["Green (< 60%)","Yellow (60-80%)","Orange (80-90%)","Red (> 90%)"]', 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (21, 5, 'Network Bandwidth Usage (%)', 'network_usage', 'Average network bandwidth utilization.', 1, 'Capacity', 2, 2, 1, 0, 100, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));

INSERT INTO ReportFields (Id, ReportTemplateId, Label, FieldKey, HelpText, Type, Section, SectionOrder, FieldOrder, IsRequired, MinValue, MaxValue, MinLength, MaxLength, RegexPattern, ValidationMessage, OptionsJson, IsCalculated, Formula, VisibilityConditionJson, TableColumnsJson, DefaultValue, PrePopulateFromPrevious, IsActive, CreatedAt)
VALUES (22, 5, 'Incident Summary', 'incident_summary', 'Brief summary of major incidents and resolutions.', 6, 'Incidents', 1, 3, 0, NULL, NULL, NULL, 3000, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 1, datetime('now'));


-- =============================================================================
-- 7. TEMPLATE ASSIGNMENTS
-- =============================================================================

-- Monthly Dept Report assigned to all department heads
INSERT INTO ReportTemplateAssignments (Id, ReportTemplateId, AssignmentType, TargetId, RoleValue, IncludeSubUnits, CreatedAt)
VALUES (1, 1, 'Role', NULL, 'DepartmentHead', 0, datetime('now'));

-- Weekly Team Report assigned to IT & Admin division (includes sub-units)
INSERT INTO ReportTemplateAssignments (Id, ReportTemplateId, AssignmentType, TargetId, RoleValue, IncludeSubUnits, CreatedAt)
VALUES (2, 2, 'OrgUnit', 9, NULL, 1, datetime('now'));

-- Weekly Team Report also assigned to team managers by role
INSERT INTO ReportTemplateAssignments (Id, ReportTemplateId, AssignmentType, TargetId, RoleValue, IncludeSubUnits, CreatedAt)
VALUES (3, 2, 'Role', NULL, 'TeamManager', 0, datetime('now'));

-- Quarterly Academic Report assigned to Engineering Faculty
INSERT INTO ReportTemplateAssignments (Id, ReportTemplateId, AssignmentType, TargetId, RoleValue, IncludeSubUnits, CreatedAt)
VALUES (4, 3, 'OrgUnit', 4, NULL, 1, datetime('now'));

-- Quarterly Academic Report assigned to MET Faculty
INSERT INTO ReportTemplateAssignments (Id, ReportTemplateId, AssignmentType, TargetId, RoleValue, IncludeSubUnits, CreatedAt)
VALUES (5, 3, 'OrgUnit', 5, NULL, 1, datetime('now'));

-- Annual Executive Report assigned to executives
INSERT INTO ReportTemplateAssignments (Id, ReportTemplateId, AssignmentType, TargetId, RoleValue, IncludeSubUnits, CreatedAt)
VALUES (6, 4, 'Role', NULL, 'Executive', 0, datetime('now'));

-- IT Infrastructure Report assigned to Head of Infrastructure
INSERT INTO ReportTemplateAssignments (Id, ReportTemplateId, AssignmentType, TargetId, RoleValue, IncludeSubUnits, CreatedAt)
VALUES (7, 5, 'Individual', 19, NULL, 0, datetime('now'));


-- =============================================================================
-- 8. REPORT PERIODS
-- =============================================================================
-- PeriodStatus: 0=Upcoming, 1=Open, 2=Closed, 3=Archived
-- =============================================================================

-- Monthly Dept Report periods
INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (1, 1, 'January 2026', '2026-01-01', '2026-01-31', '2026-02-05', 3, 2, 1, datetime('now'));

INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (2, 1, 'February 2026', '2026-02-01', '2026-02-28', '2026-03-05', 3, 1, 1, datetime('now'));

INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (3, 1, 'March 2026', '2026-03-01', '2026-03-31', '2026-04-05', 3, 0, 1, datetime('now'));

-- Weekly Team Report periods
INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (4, 2, 'Week of Jan 27, 2026', '2026-01-27', '2026-02-02', '2026-02-03', 1, 2, 1, datetime('now'));

INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (5, 2, 'Week of Feb 03, 2026', '2026-02-03', '2026-02-09', '2026-02-10', 1, 1, 1, datetime('now'));

-- Quarterly Academic periods
INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (6, 3, 'Q4 2025', '2025-10-01', '2025-12-31', '2026-01-15', 5, 2, 1, datetime('now'));

INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (7, 3, 'Q1 2026', '2026-01-01', '2026-03-31', '2026-04-15', 5, 1, 1, datetime('now'));

-- IT Infrastructure Health Report periods
INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (8, 5, 'January 2026', '2026-01-01', '2026-01-31', '2026-02-05', 3, 2, 1, datetime('now'));

INSERT INTO ReportPeriods (Id, ReportTemplateId, Name, StartDate, EndDate, SubmissionDeadline, GracePeriodDays, Status, IsActive, CreatedAt)
VALUES (9, 5, 'February 2026', '2026-02-01', '2026-02-28', '2026-03-05', 3, 1, 1, datetime('now'));


-- =============================================================================
-- 9. SAMPLE REPORTS
-- =============================================================================

-- Submitted report: Head of Software Dev - Monthly Dept Report - January 2026
INSERT INTO Reports (Id, ReportTemplateId, ReportPeriodId, SubmittedById, AssignedReviewerId, Status, SubmittedAt, ReviewedAt, ReviewComments, IsLocked, LastAutoSaveAt, AmendmentCount, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (1, 1, 1, 18, 3, 'Approved', datetime('now', '-25 days'), datetime('now', '-23 days'), 'Good comprehensive report. Approved.', 1, datetime('now', '-25 days'), 0, 0, datetime('now', '-30 days'), datetime('now', '-23 days'));

-- Report field values for Report 1
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (1, 1, 1, '<p>The Software Development department had a productive January. We completed 3 major projects and are on track with our Q1 roadmap. Team morale is high after the successful launch of the Student Portal v2.</p>', NULL, 0, datetime('now', '-25 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (2, 1, 2, '24', 24, 0, datetime('now', '-25 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (3, 1, 3, '3', 3, 0, datetime('now', '-25 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (4, 1, 4, '5', 5, 0, datetime('now', '-25 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (5, 1, 5, '72', 72, 0, datetime('now', '-25 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (6, 1, 6, 'On Track', NULL, 0, datetime('now', '-25 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (7, 1, 7, '<ul><li>Student Portal v2 launched successfully</li><li>API gateway migration completed</li><li>Automated testing coverage increased to 85%</li></ul>', NULL, 0, datetime('now', '-25 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (8, 1, 8, '<p>Two senior developers resigned, creating a knowledge gap in the mobile team. Recruitment is in progress.</p>', NULL, 0, datetime('now', '-25 days'), NULL);

-- Draft report: Head of IT Infrastructure - Monthly Dept Report - February 2026
INSERT INTO Reports (Id, ReportTemplateId, ReportPeriodId, SubmittedById, AssignedReviewerId, Status, SubmittedAt, ReviewedAt, ReviewComments, IsLocked, LastAutoSaveAt, AmendmentCount, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (2, 1, 2, 19, NULL, 'Draft', NULL, NULL, NULL, 0, datetime('now', '-1 day'), 0, 0, datetime('now', '-3 days'), datetime('now', '-1 day'));

-- Submitted report: Backend Team Lead - Weekly Progress - Week of Feb 03
INSERT INTO Reports (Id, ReportTemplateId, ReportPeriodId, SubmittedById, AssignedReviewerId, Status, SubmittedAt, ReviewedAt, ReviewComments, IsLocked, LastAutoSaveAt, AmendmentCount, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (3, 2, 5, 26, 22, 'Submitted', datetime('now', '-1 day'), NULL, NULL, 0, datetime('now', '-1 day'), 0, 0, datetime('now', '-2 days'), datetime('now', '-1 day'));

-- Report field values for Report 3
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (9, 3, 11, 'Sprint 4 is progressing well. Completed migration of authentication service to new identity provider.', NULL, 0, datetime('now', '-1 day'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (10, 3, 12, '8', 8, 0, datetime('now', '-1 day'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (11, 3, 13, '4', 4, 0, datetime('now', '-1 day'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (12, 3, 14, '<p>Waiting for DevOps team to provision staging environment for new API.</p>', NULL, 0, datetime('now', '-1 day'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (13, 3, 16, 'Complete API integration tests. Begin user acceptance testing for the reporting module.', NULL, 0, datetime('now', '-1 day'), NULL);

-- Submitted report: Head of Infrastructure - IT Health - January 2026 (Approved)
INSERT INTO Reports (Id, ReportTemplateId, ReportPeriodId, SubmittedById, AssignedReviewerId, Status, SubmittedAt, ReviewedAt, ReviewComments, IsLocked, LastAutoSaveAt, AmendmentCount, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (4, 5, 8, 19, 3, 'Approved', datetime('now', '-20 days'), datetime('now', '-18 days'), 'Excellent uptime numbers. Approved.', 1, datetime('now', '-20 days'), 0, 0, datetime('now', '-25 days'), datetime('now', '-18 days'));

INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (14, 4, 17, '99.7', 99.7, 0, datetime('now', '-20 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (15, 4, 18, '12', 12, 0, datetime('now', '-20 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (16, 4, 19, '1', 1, 0, datetime('now', '-20 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (17, 4, 20, 'Yellow (60-80%)', NULL, 0, datetime('now', '-20 days'), NULL);
INSERT INTO ReportFieldValues (Id, ReportId, ReportFieldId, Value, NumericValue, WasPrePopulated, CreatedAt, UpdatedAt)
VALUES (18, 4, 21, '65', 65, 0, datetime('now', '-20 days'), NULL);


-- =============================================================================
-- 10. UPWARD FLOW DATA (Phase 4)
-- =============================================================================
-- SuggestedActions, ResourceRequests, SupportRequests attached to reports
-- =============================================================================

DELETE FROM SuggestedActions;
DELETE FROM ResourceRequests;
DELETE FROM SupportRequests;
DELETE FROM sqlite_sequence WHERE name IN ('SuggestedActions', 'ResourceRequests', 'SupportRequests');

-- ---- Suggested Actions ----
-- ActionCategory: ProcessImprovement, CostReduction, QualityEnhancement, Innovation, RiskMitigation
-- ActionPriority: Critical, High, Medium, Low
-- ActionStatus: Submitted, UnderReview, Approved, Rejected, Implemented, Deferred

-- Suggested actions for Report 1 (Approved Monthly Dept Report - Software Dev)
INSERT INTO SuggestedActions (Id, ReportId, Title, Description, Justification, ExpectedOutcome, Timeline, Category, Priority, Status, ReviewedById, ReviewedAt, ReviewComments, CreatedAt, UpdatedAt)
VALUES (1, 1, 'Implement automated code review tool', 'Integrate SonarQube or similar static analysis tool into our CI/CD pipeline to catch code quality issues early.', 'Manual code reviews are time-consuming and inconsistent. Automated tools can catch 70% of common issues.', 'Reduced code review time by 40%, improved code quality consistency.', 'Q2 2026', 'QualityEnhancement', 'High', 'Approved', 3, datetime('now', '-20 days'), 'Approved. Please coordinate with IT Infrastructure for licensing.', datetime('now', '-25 days'), datetime('now', '-20 days'));

INSERT INTO SuggestedActions (Id, ReportId, Title, Description, Justification, ExpectedOutcome, Timeline, Category, Priority, Status, ReviewedById, ReviewedAt, ReviewComments, CreatedAt, UpdatedAt)
VALUES (2, 1, 'Establish developer mentorship program', 'Create a structured mentorship program pairing senior developers with junior staff for knowledge transfer.', 'Recent resignations highlighted knowledge concentration risk. Need systematic knowledge sharing.', 'Improved knowledge retention, faster onboarding of new hires, reduced bus factor risk.', 'March 2026', 'RiskMitigation', 'Medium', 'Implemented', 3, datetime('now', '-18 days'), 'Great initiative. Already implementing.', datetime('now', '-25 days'), datetime('now', '-18 days'));

INSERT INTO SuggestedActions (Id, ReportId, Title, Description, Justification, ExpectedOutcome, Timeline, Category, Priority, Status, ReviewedById, ReviewedAt, ReviewComments, CreatedAt, UpdatedAt)
VALUES (3, 1, 'Migrate to containerized deployments', 'Move all applications to Docker/Kubernetes for improved scalability and deployment consistency.', 'Current deployment process is manual and error-prone. Containers provide reproducibility.', 'Faster deployments, reduced environment-related bugs, easier scaling.', 'Q3 2026', 'ProcessImprovement', 'Medium', 'UnderReview', NULL, NULL, NULL, datetime('now', '-25 days'), NULL);

-- Suggested actions for Report 3 (Submitted Weekly Report - Backend Team)
INSERT INTO SuggestedActions (Id, ReportId, Title, Description, Justification, ExpectedOutcome, Timeline, Category, Priority, Status, ReviewedById, ReviewedAt, ReviewComments, CreatedAt, UpdatedAt)
VALUES (4, 3, 'Implement API rate limiting', 'Add rate limiting to public APIs to prevent abuse and ensure fair usage.', 'Recent spike in API calls caused performance degradation for legitimate users.', 'Improved API stability, protection against abuse, better resource allocation.', 'Feb 2026', 'RiskMitigation', 'High', 'Submitted', NULL, NULL, NULL, datetime('now', '-1 day'), NULL);

INSERT INTO SuggestedActions (Id, ReportId, Title, Description, Justification, ExpectedOutcome, Timeline, Category, Priority, Status, ReviewedById, ReviewedAt, ReviewComments, CreatedAt, UpdatedAt)
VALUES (5, 3, 'Reduce database query redundancy', 'Implement caching layer for frequently accessed but rarely changing data.', 'Database profiling shows 40% of queries are redundant within same session.', 'Reduced database load, improved response times, lower infrastructure costs.', 'March 2026', 'CostReduction', 'Medium', 'Submitted', NULL, NULL, NULL, datetime('now', '-1 day'), NULL);

-- Suggested action for Report 4 (IT Infrastructure Report)
INSERT INTO SuggestedActions (Id, ReportId, Title, Description, Justification, ExpectedOutcome, Timeline, Category, Priority, Status, ReviewedById, ReviewedAt, ReviewComments, CreatedAt, UpdatedAt)
VALUES (6, 4, 'Implement predictive monitoring with ML', 'Use machine learning to predict infrastructure failures before they occur based on metrics patterns.', 'Reactive monitoring causes downtime. Predictive approach can prevent 60% of incidents.', 'Reduced unplanned downtime, proactive maintenance, improved SLA compliance.', 'Q2 2026', 'Innovation', 'High', 'Approved', 3, datetime('now', '-15 days'), 'Excellent proposal. Allocate resources from innovation budget.', datetime('now', '-20 days'), datetime('now', '-15 days'));


-- ---- Resource Requests ----
-- ResourceCategory: Budget, Equipment, Software, Personnel, Materials, Facilities, Training
-- ResourceUrgency: Critical, High, Medium, Low
-- ResourceStatus: Submitted, UnderReview, Approved, PartiallyApproved, Rejected, Fulfilled

-- Resource requests for Report 1 (Software Dev)
INSERT INTO ResourceRequests (Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency, EstimatedCost, Currency, Status, ApprovedAmount, ReviewedById, ReviewedAt, FulfilledAt, CreatedAt, UpdatedAt)
VALUES (1, 1, 'Senior Backend Developer Hire', 'Request to hire 2 senior backend developers to fill positions vacated by resignations.', 2, 'Team is understaffed after 2 resignations. Current workload is unsustainable.', 'Personnel', 'High', 480000.00, 'EGP', 'Approved', 480000.00, 20, datetime('now', '-22 days'), NULL, datetime('now', '-25 days'), datetime('now', '-22 days'));

INSERT INTO ResourceRequests (Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency, EstimatedCost, Currency, Status, ApprovedAmount, ReviewedById, ReviewedAt, FulfilledAt, CreatedAt, UpdatedAt)
VALUES (2, 1, 'JetBrains Team Tools License', 'Annual license for JetBrains IDEs (IntelliJ, WebStorm, ReSharper) for the development team.', 24, 'Current licenses expiring. Tool is critical for developer productivity.', 'Software', 'Medium', 45000.00, 'EGP', 'Approved', 45000.00, 3, datetime('now', '-20 days'), datetime('now', '-19 days'), datetime('now', '-25 days'), datetime('now', '-19 days'));

INSERT INTO ResourceRequests (Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency, EstimatedCost, Currency, Status, ApprovedAmount, ReviewedById, ReviewedAt, FulfilledAt, CreatedAt, UpdatedAt)
VALUES (3, 1, 'Cloud Infrastructure Budget Increase', 'Request 25% increase to monthly cloud infrastructure budget for new projects.', NULL, 'New projects requiring additional cloud resources. Current budget fully utilized.', 'Budget', 'Medium', 25000.00, 'EGP', 'PartiallyApproved', 15000.00, 3, datetime('now', '-18 days'), NULL, datetime('now', '-25 days'), datetime('now', '-18 days'));

-- Resource requests for Report 3 (Backend Team)
INSERT INTO ResourceRequests (Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency, EstimatedCost, Currency, Status, ApprovedAmount, ReviewedById, ReviewedAt, FulfilledAt, CreatedAt, UpdatedAt)
VALUES (4, 3, 'Developer Training - AWS Certification', 'AWS Solutions Architect certification training for 3 team members.', 3, 'Team needs AWS skills for upcoming cloud migration project.', 'Training', 'Medium', 15000.00, 'EGP', 'Submitted', NULL, NULL, NULL, NULL, datetime('now', '-1 day'), NULL);

INSERT INTO ResourceRequests (Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency, EstimatedCost, Currency, Status, ApprovedAmount, ReviewedById, ReviewedAt, FulfilledAt, CreatedAt, UpdatedAt)
VALUES (5, 3, 'High-Performance Development Workstations', 'Upgrade to M3 MacBook Pro for backend team members for improved build times.', 4, 'Current machines struggle with Docker and IDE simultaneously. Build times are 3x longer than needed.', 'Equipment', 'Low', 320000.00, 'EGP', 'Submitted', NULL, NULL, NULL, NULL, datetime('now', '-1 day'), NULL);

-- Resource requests for Report 4 (IT Infrastructure)
INSERT INTO ResourceRequests (Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency, EstimatedCost, Currency, Status, ApprovedAmount, ReviewedById, ReviewedAt, FulfilledAt, CreatedAt, UpdatedAt)
VALUES (6, 4, 'Additional Server Rack', 'New server rack to accommodate growth in on-premise infrastructure.', 1, 'Current capacity at 75%. Expected growth will exceed capacity by Q3.', 'Facilities', 'High', 150000.00, 'EGP', 'Approved', 150000.00, 3, datetime('now', '-17 days'), NULL, datetime('now', '-20 days'), datetime('now', '-17 days'));

INSERT INTO ResourceRequests (Id, ReportId, Title, Description, Quantity, Justification, Category, Urgency, EstimatedCost, Currency, Status, ApprovedAmount, ReviewedById, ReviewedAt, FulfilledAt, CreatedAt, UpdatedAt)
VALUES (7, 4, 'Network Monitoring Software', 'Enterprise license for PRTG Network Monitor for comprehensive monitoring.', 1, 'Current monitoring has gaps. Need unified view across all network segments.', 'Software', 'Medium', 35000.00, 'EGP', 'Fulfilled', 35000.00, 3, datetime('now', '-18 days'), datetime('now', '-16 days'), datetime('now', '-20 days'), datetime('now', '-16 days'));


-- ---- Support Requests ----
-- SupportCategory: ManagementIntervention, CrossDeptCoordination, TechnicalAssistance, Training, ConflictResolution, PolicyClarification
-- SupportUrgency: Critical, High, Medium, Low
-- SupportStatus: Submitted, Acknowledged, InProgress, Resolved, Closed

-- Support requests for Report 1 (Software Dev)
INSERT INTO SupportRequests (Id, ReportId, Title, Description, CurrentSituation, DesiredOutcome, Category, Urgency, Status, Resolution, AssignedToId, AcknowledgedById, AcknowledgedAt, ResolvedById, ResolvedAt, CreatedAt, UpdatedAt)
VALUES (1, 1, 'Expedite procurement for development tools', 'Need management intervention to speed up procurement process for critical development tools.', 'Procurement request submitted 45 days ago still pending. Standard lead time is 21 days.', 'Approval within 1 week to prevent project delays.', 'ManagementIntervention', 'High', 'Resolved', 'Escalated to VP Admin. Procurement approved within 3 days.', 3, 3, datetime('now', '-24 days'), 3, datetime('now', '-22 days'), datetime('now', '-25 days'), datetime('now', '-22 days'));

INSERT INTO SupportRequests (Id, ReportId, Title, Description, CurrentSituation, DesiredOutcome, Category, Urgency, Status, Resolution, AssignedToId, AcknowledgedById, AcknowledgedAt, ResolvedById, ResolvedAt, CreatedAt, UpdatedAt)
VALUES (2, 1, 'Cross-team API integration coordination', 'Need coordination between Backend Team and IT Infrastructure for API gateway setup.', 'Teams working in silos. API gateway configuration not aligned with backend requirements.', 'Joint planning session and aligned technical specifications.', 'CrossDeptCoordination', 'Medium', 'Closed', 'Meeting held between teams. Technical specs aligned. Integration completed.', 24, 18, datetime('now', '-23 days'), 18, datetime('now', '-20 days'), datetime('now', '-25 days'), datetime('now', '-20 days'));

-- Support requests for Report 3 (Backend Team)
INSERT INTO SupportRequests (Id, ReportId, Title, Description, CurrentSituation, DesiredOutcome, Category, Urgency, Status, Resolution, AssignedToId, AcknowledgedById, AcknowledgedAt, ResolvedById, ResolvedAt, CreatedAt, UpdatedAt)
VALUES (3, 3, 'Staging environment provisioning delay', 'DevOps team has not provisioned the requested staging environment for 2 weeks.', 'Cannot proceed with integration testing. Sprint velocity affected.', 'Staging environment available within 3 days.', 'CrossDeptCoordination', 'High', 'InProgress', NULL, 25, 22, datetime('now'), NULL, NULL, datetime('now', '-1 day'), datetime('now'));

INSERT INTO SupportRequests (Id, ReportId, Title, Description, CurrentSituation, DesiredOutcome, Category, Urgency, Status, Resolution, AssignedToId, AcknowledgedById, AcknowledgedAt, ResolvedById, ResolvedAt, CreatedAt, UpdatedAt)
VALUES (4, 3, 'Database performance optimization assistance', 'Need DBA assistance for query optimization in the reporting module.', 'Complex queries taking 10+ seconds. Team lacks deep SQL optimization expertise.', 'Queries optimized to under 2 seconds response time.', 'TechnicalAssistance', 'Medium', 'Submitted', NULL, NULL, NULL, NULL, NULL, NULL, datetime('now', '-1 day'), NULL);

-- Support request for Report 4 (IT Infrastructure)
INSERT INTO SupportRequests (Id, ReportId, Title, Description, CurrentSituation, DesiredOutcome, Category, Urgency, Status, Resolution, AssignedToId, AcknowledgedById, AcknowledgedAt, ResolvedById, ResolvedAt, CreatedAt, UpdatedAt)
VALUES (5, 4, 'Security policy clarification for cloud resources', 'Need clarification on security policies for hybrid cloud deployments.', 'Conflicting guidance from IT Security and Compliance departments.', 'Unified security policy document for hybrid cloud.', 'PolicyClarification', 'Medium', 'Resolved', 'Met with IT Security and Compliance. Created unified policy document.', 21, 19, datetime('now', '-18 days'), 21, datetime('now', '-15 days'), datetime('now', '-20 days'), datetime('now', '-15 days'));

INSERT INTO SupportRequests (Id, ReportId, Title, Description, CurrentSituation, DesiredOutcome, Category, Urgency, Status, Resolution, AssignedToId, AcknowledgedById, AcknowledgedAt, ResolvedById, ResolvedAt, CreatedAt, UpdatedAt)
VALUES (6, 4, 'Vendor negotiation support', 'Need management support for contract negotiation with network equipment vendor.', 'Vendor offering 10% discount. Believe 20% is achievable based on competitor quotes.', 'Better pricing terms saving estimated 50,000 EGP annually.', 'ManagementIntervention', 'Low', 'Acknowledged', NULL, 3, 3, datetime('now', '-17 days'), NULL, NULL, datetime('now', '-20 days'), datetime('now', '-17 days'));


-- =============================================================================
-- SUMMARY
-- =============================================================================
-- Organizational Units: 68 units across 6 levels
--   - 1 Root (GUC)
--   - 2 Campuses (Main, New Capital)
--   - 9 Faculties/Divisions (incl. Administrative Services Division)
--   - 26 Departments (14 original + 12 new admin departments)
--   - 25 Sectors/Sections (6 original + 19 new admin sections)
--   - 5 Teams
--
-- Users: 200 accounts across all roles
--   - 6 Executives (President, VPs, Deans, Admin Services Director)
--   - 3 Administrators (System admins)
--   - 25 Department Heads (13 original + 12 new)
--   - 28 Team Managers (9 original + 19 new section managers)
--   - 16 Report Reviewers (6 original + 10 new)
--   - 116 Report Originators (developers, faculty, admin staff)
--   - 5 Auditors (3 original + 2 new)
--   - 1 Inactive user (for testing)
--
-- Delegations: 6 samples
--   - 3 Active (including 1 reporting-only)
--   - 1 Upcoming (starts in 2 weeks)
--   - 1 Past (completed)
--   - 1 Revoked
--
-- Report Templates: 5 templates
--   - Monthly Department Status Report (10 fields)
--   - Weekly Team Progress Report (6 fields)
--   - Quarterly Academic Performance Report (no fields yet)
--   - Annual Executive Summary Report (no fields yet)
--   - IT Infrastructure Health Report (6 fields)
--
-- Template Assignments: 7 assignments (by role, org unit, individual)
-- Report Periods: 9 periods across templates
-- Sample Reports: 4 reports (1 approved, 1 draft, 1 submitted, 1 approved)
-- Report Field Values: 18 sample data entries
--
-- Upward Flow (Phase 4):
--   Suggested Actions: 6 items
--     - 3 Approved, 1 Implemented, 1 Under Review, 1 Submitted
--     - Categories: Quality Enhancement, Risk Mitigation, Process Improvement, Cost Reduction, Innovation
--   Resource Requests: 7 items
--     - 3 Approved, 1 Fulfilled, 1 Partially Approved, 2 Submitted
--     - Categories: Personnel, Software, Budget, Training, Equipment, Facilities
--   Support Requests: 6 items
--     - 2 Resolved, 1 Closed, 1 In Progress, 1 Acknowledged, 1 Submitted
--     - Categories: Management Intervention, Cross-Dept Coordination, Technical Assistance, Policy Clarification
--
-- Notifications: 5 initial system notifications
--
-- Workflow & Tagging (Phase 5):
--   Comments: 8 items (threaded discussions with replies)
--   ConfirmationTags: 6 items (pending, confirmed, declined, etc.)
--
-- Downward Flow (Phase 6):
--   Feedback: 6 items (management responses with categories)
--   Recommendations: 5 items (directives with target scope)
--   Decisions: 6 items (responses to upward flow requests)
-- =============================================================================


-- =============================================================================
-- 11. WORKFLOW & TAGGING DATA (Phase 5)
-- =============================================================================
-- Comments and ConfirmationTags for reports
-- =============================================================================

DELETE FROM Comments;
DELETE FROM ConfirmationTags;
DELETE FROM sqlite_sequence WHERE name IN ('Comments', 'ConfirmationTags');

-- ---- Comments ----
-- CommentStatus: Active, Edited, Deleted, Hidden

-- Comments on Report 1 (Software Dev Monthly Report - Approved)
INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (1, 1, 3, 'Excellent progress on the Student Portal v2. The launch metrics look very promising. @head.sdev@guc.edu.eg can you share the user adoption numbers?', 'Active', 'Key Achievements', NULL, '[18]', datetime('now', '-24 days'), NULL);

INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (2, 1, 18, 'Thank you! First week shows 85% adoption rate among active students. Full metrics report will be shared next week.', 'Active', NULL, 1, NULL, datetime('now', '-24 days', '+2 hours'), NULL);

INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (3, 1, 9, 'The automated testing coverage improvement is noteworthy. This aligns with our faculty-wide quality initiative.', 'Active', 'Key Metrics', NULL, NULL, datetime('now', '-23 days'), NULL);

INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (4, 1, 20, 'Regarding the two senior developer resignations - HR is prioritizing the recruitment. @mgr.backend@guc.edu.eg please coordinate with HR for technical interviews.', 'Active', 'Current Challenges', NULL, '[26]', datetime('now', '-22 days'), NULL);

INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (5, 1, 26, 'Confirmed. I have already sent the technical requirements to HR and am available for interviews next week.', 'Active', NULL, 4, NULL, datetime('now', '-22 days', '+3 hours'), NULL);

-- Comments on Report 3 (Backend Team Weekly - Submitted)
INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (6, 3, 22, 'Good progress on the authentication migration. However, the staging environment blocker needs immediate attention. @mgr.cloud@guc.edu.eg can you prioritize this?', 'Active', 'Blockers', NULL, '[25]', datetime('now', '-12 hours'), NULL);

INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (7, 3, 25, 'Apologies for the delay. We had capacity issues. Environment will be ready by end of day tomorrow.', 'Active', NULL, 6, NULL, datetime('now', '-10 hours'), NULL);

-- Comments on Report 4 (IT Infrastructure Report - Approved)
INSERT INTO Comments (Id, ReportId, AuthorId, Content, Status, SectionReference, ParentCommentId, MentionedUserIdsJson, CreatedAt, UpdatedAt)
VALUES (8, 4, 3, 'The 99.7% uptime is excellent. Please prepare a brief for the next executive meeting on how we achieved this.', 'Active', 'Availability', NULL, NULL, datetime('now', '-17 days'), NULL);


-- ---- Confirmation Tags ----
-- ConfirmationStatus: Pending, Confirmed, RevisionRequested, Declined, Expired, Cancelled

-- Confirmation tags for Report 1
INSERT INTO ConfirmationTags (Id, ReportId, RequestedById, TaggedUserId, SectionReference, Message, Status, Response, RequestedAt, RespondedAt, ExpiresAt, ReminderSentAt, CreatedAt)
VALUES (1, 1, 18, 34, 'Key Metrics', 'Please confirm the testing coverage numbers are accurate based on our CI/CD reports.', 'Confirmed', 'Verified. The 85% coverage matches our SonarQube dashboard.', datetime('now', '-26 days'), datetime('now', '-25 days'), datetime('now', '-19 days'), NULL, datetime('now', '-26 days'));

INSERT INTO ConfirmationTags (Id, ReportId, RequestedById, TaggedUserId, SectionReference, Message, Status, Response, RequestedAt, RespondedAt, ExpiresAt, ReminderSentAt, CreatedAt)
VALUES (2, 1, 18, 26, 'Key Achievements', 'Please confirm the Student Portal launch details are complete.', 'Confirmed', 'Confirmed. Launch date and features list is accurate.', datetime('now', '-26 days'), datetime('now', '-26 days', '+4 hours'), datetime('now', '-19 days'), NULL, datetime('now', '-26 days'));

-- Confirmation tags for Report 3
INSERT INTO ConfirmationTags (Id, ReportId, RequestedById, TaggedUserId, SectionReference, Message, Status, Response, RequestedAt, RespondedAt, ExpiresAt, ReminderSentAt, CreatedAt)
VALUES (3, 3, 26, 37, 'Metrics', 'Can you verify the task completion numbers from Jira?', 'Confirmed', 'Numbers match Jira sprint report.', datetime('now', '-2 days'), datetime('now', '-1 day'), datetime('now', '+5 days'), NULL, datetime('now', '-2 days'));

INSERT INTO ConfirmationTags (Id, ReportId, RequestedById, TaggedUserId, SectionReference, Message, Status, Response, RequestedAt, RespondedAt, ExpiresAt, ReminderSentAt, CreatedAt)
VALUES (4, 3, 26, 38, 'Blockers', 'Please confirm the staging environment issue description is accurate.', 'Pending', NULL, datetime('now', '-1 day'), NULL, datetime('now', '+6 days'), NULL, datetime('now', '-1 day'));

-- Confirmation tags for Report 4
INSERT INTO ConfirmationTags (Id, ReportId, RequestedById, TaggedUserId, SectionReference, Message, Status, Response, RequestedAt, RespondedAt, ExpiresAt, ReminderSentAt, CreatedAt)
VALUES (5, 4, 19, 24, 'Incidents', 'Please verify the incident counts match our ITSM records.', 'Confirmed', 'Verified against ServiceNow. Numbers are correct.', datetime('now', '-21 days'), datetime('now', '-20 days'), datetime('now', '-14 days'), NULL, datetime('now', '-21 days'));

INSERT INTO ConfirmationTags (Id, ReportId, RequestedById, TaggedUserId, SectionReference, Message, Status, Response, RequestedAt, RespondedAt, ExpiresAt, ReminderSentAt, CreatedAt)
VALUES (6, 4, 19, 25, 'Capacity', 'Please confirm server capacity status is current.', 'RevisionRequested', 'The capacity figure needs updating - we added 2 new servers last week. Please update to Green status.', datetime('now', '-21 days'), datetime('now', '-20 days'), datetime('now', '-14 days'), NULL, datetime('now', '-21 days'));


-- =============================================================================
-- 12. DOWNWARD FLOW DATA (Phase 6)
-- =============================================================================
-- Feedback, Recommendations, and Decisions from management
-- =============================================================================

DELETE FROM Feedbacks;
DELETE FROM Recommendations;
DELETE FROM Decisions;
DELETE FROM sqlite_sequence WHERE name IN ('Feedbacks', 'Recommendations', 'Decisions');

-- ---- Feedback ----
-- FeedbackCategory: PositiveRecognition, Concern, Observation, Question, General
-- FeedbackVisibility: Private, TeamWide, DepartmentWide, OrganizationWide
-- FeedbackStatus: Active, Resolved, Archived

-- Feedback on Report 1 (Software Dev Monthly)
INSERT INTO Feedbacks (Id, ReportId, AuthorId, Subject, Content, Category, Visibility, SectionReference, ReportFieldId, ParentFeedbackId, IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse, Status, CreatedAt, UpdatedAt)
VALUES (1, 1, 3, 'Excellent team performance', 'The Software Development department has shown exceptional performance this month. The Student Portal v2 launch demonstrates strong execution capabilities.', 'PositiveRecognition', 'DepartmentWide', 'General', NULL, NULL, 1, datetime('now', '-22 days'), 'Thank you for the recognition. The team worked hard and we are proud of the outcome.', 'Active', datetime('now', '-23 days'), datetime('now', '-22 days'));

INSERT INTO Feedbacks (Id, ReportId, AuthorId, Subject, Content, Category, Visibility, SectionReference, ReportFieldId, ParentFeedbackId, IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse, Status, CreatedAt, UpdatedAt)
VALUES (2, 1, 3, 'Staffing concern noted', 'The loss of two senior developers is concerning. Please ensure knowledge transfer documentation is prioritized and keep me updated on recruitment progress.', 'Concern', 'Private', 'Current Challenges', 8, NULL, 1, datetime('now', '-21 days'), 'Understood. We have initiated knowledge transfer sessions and HR is actively recruiting.', 'Resolved', datetime('now', '-22 days'), datetime('now', '-21 days'));

INSERT INTO Feedbacks (Id, ReportId, AuthorId, Subject, Content, Category, Visibility, SectionReference, ReportFieldId, ParentFeedbackId, IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse, Status, CreatedAt, UpdatedAt)
VALUES (3, 1, 2, 'Alignment with academic goals', 'The automated testing improvements align well with our academic quality standards. Consider sharing this approach with other technical departments.', 'Observation', 'OrganizationWide', 'Key Metrics', NULL, NULL, 0, NULL, NULL, 'Active', datetime('now', '-20 days'), NULL);

-- Feedback on Report 3 (Backend Team Weekly)
INSERT INTO Feedbacks (Id, ReportId, AuthorId, Subject, Content, Category, Visibility, SectionReference, ReportFieldId, ParentFeedbackId, IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse, Status, CreatedAt, UpdatedAt)
VALUES (4, 3, 22, 'Question about API rate limiting', 'What specific rate limits are you proposing? We need to ensure they dont affect legitimate high-volume API users.', 'Question', 'TeamWide', 'Issues', NULL, NULL, 0, NULL, NULL, 'Active', datetime('now', '-6 hours'), NULL);

-- Feedback on Report 4 (IT Infrastructure)
INSERT INTO Feedbacks (Id, ReportId, AuthorId, Subject, Content, Category, Visibility, SectionReference, ReportFieldId, ParentFeedbackId, IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse, Status, CreatedAt, UpdatedAt)
VALUES (5, 4, 3, 'Outstanding uptime achievement', 'The 99.7% uptime is among the best in our sector. This sets a benchmark for reliability excellence.', 'PositiveRecognition', 'OrganizationWide', 'Availability', NULL, NULL, 1, datetime('now', '-16 days'), 'Thank you. The team has worked diligently on proactive monitoring and quick incident response.', 'Active', datetime('now', '-17 days'), datetime('now', '-16 days'));

INSERT INTO Feedbacks (Id, ReportId, AuthorId, Subject, Content, Category, Visibility, SectionReference, ReportFieldId, ParentFeedbackId, IsAcknowledged, AcknowledgedAt, AcknowledgmentResponse, Status, CreatedAt, UpdatedAt)
VALUES (6, 4, 1, 'Capacity planning observation', 'Yellow status on server capacity needs attention before it becomes critical. Please include a capacity expansion plan in next months report.', 'Concern', 'Private', 'Capacity', NULL, NULL, 1, datetime('now', '-15 days'), 'Noted. We have already procured additional server rack (approved in this report). Expansion plan will be detailed next month.', 'Resolved', datetime('now', '-16 days'), datetime('now', '-15 days'));


-- ---- Recommendations ----
-- RecommendationCategory: ProcessChange, SkillDevelopment, PerformanceImprovement, Compliance, StrategicAlignment, ResourceOptimization, General
-- RecommendationPriority: Critical, High, Medium, Low
-- RecommendationScope: Individual, Team, Department, OrganizationWide
-- RecommendationStatus: Draft, Issued, Acknowledged, InProgress, Completed, Cancelled

-- Recommendations linked to reports
INSERT INTO Recommendations (Id, ReportId, IssuedById, TargetOrgUnitId, TargetUserId, Title, Description, Rationale, Timeline, Category, Priority, TargetScope, Status, EffectiveDate, DueDate, CascadeToSubUnits, AcknowledgmentCount, CreatedAt, UpdatedAt)
VALUES (1, 1, 3, 22, NULL, 'Implement mandatory code documentation standards', 'All code changes must include documentation updates. Establish documentation review as part of PR process.', 'Recent resignations highlighted knowledge concentration risk. Proper documentation ensures continuity.', 'Implement within 30 days', 'ProcessChange', 'High', 'Department', 'InProgress', datetime('now', '-20 days'), datetime('now', '+10 days'), 1, 3, datetime('now', '-20 days'), datetime('now', '-15 days'));

INSERT INTO Recommendations (Id, ReportId, IssuedById, TargetOrgUnitId, TargetUserId, Title, Description, Rationale, Timeline, Category, Priority, TargetScope, Status, EffectiveDate, DueDate, CascadeToSubUnits, AcknowledgmentCount, CreatedAt, UpdatedAt)
VALUES (2, 4, 3, 23, NULL, 'Establish disaster recovery drills', 'Conduct quarterly DR drills to ensure backup and recovery procedures are tested and staff are trained.', 'High uptime is excellent but DR readiness ensures business continuity under adverse conditions.', 'First drill by end of Q1 2026', 'Compliance', 'Medium', 'Department', 'Acknowledged', datetime('now', '-15 days'), datetime('now', '+45 days'), 1, 2, datetime('now', '-15 days'), NULL);

-- Organization-wide recommendation (not linked to specific report)
INSERT INTO Recommendations (Id, ReportId, IssuedById, TargetOrgUnitId, TargetUserId, Title, Description, Rationale, Timeline, Category, Priority, TargetScope, Status, EffectiveDate, DueDate, CascadeToSubUnits, AcknowledgmentCount, CreatedAt, UpdatedAt)
VALUES (3, NULL, 1, 1, NULL, 'Adopt standardized reporting metrics', 'All departments should align on common KPIs for reporting consistency across the organization.', 'Current reports use inconsistent metrics making cross-department comparison difficult.', 'Complete by Q2 2026', 'StrategicAlignment', 'High', 'OrganizationWide', 'Issued', datetime('now', '-10 days'), datetime('now', '+90 days'), 1, 0, datetime('now', '-10 days'), NULL);

INSERT INTO Recommendations (Id, ReportId, IssuedById, TargetOrgUnitId, TargetUserId, Title, Description, Rationale, Timeline, Category, Priority, TargetScope, Status, EffectiveDate, DueDate, CascadeToSubUnits, AcknowledgmentCount, CreatedAt, UpdatedAt)
VALUES (4, 3, 22, NULL, 26, 'Complete AWS certification training', 'Backend team lead should complete AWS Solutions Architect certification to support cloud migration efforts.', 'Cloud migration is strategic priority. Team lead needs certification for architecture decisions.', 'Complete by Q2 2026', 'SkillDevelopment', 'Medium', 'Individual', 'Acknowledged', datetime('now', '-5 days'), datetime('now', '+90 days'), 0, 1, datetime('now', '-5 days'), datetime('now', '-3 days'));

INSERT INTO Recommendations (Id, ReportId, IssuedById, TargetOrgUnitId, TargetUserId, Title, Description, Rationale, Timeline, Category, Priority, TargetScope, Status, EffectiveDate, DueDate, CascadeToSubUnits, AcknowledgmentCount, CreatedAt, UpdatedAt)
VALUES (5, NULL, 2, 4, NULL, 'Increase industry collaboration', 'Engineering faculty should establish at least 3 new industry partnerships for student internships and research collaboration.', 'Industry partnerships improve student employability and research relevance.', 'Establish by end of 2026', 'StrategicAlignment', 'Medium', 'Department', 'Issued', datetime('now', '-7 days'), datetime('now', '+300 days'), 1, 0, datetime('now', '-7 days'), NULL);


-- ---- Decisions ----
-- DecisionRequestType: SuggestedAction, ResourceRequest, SupportRequest
-- DecisionOutcome: Pending, Approved, ApprovedWithModifications, PartiallyApproved, Deferred, Rejected, Referred

-- Decisions on Suggested Actions
INSERT INTO Decisions (Id, ReportId, DecidedById, RequestType, SuggestedActionId, ResourceRequestId, SupportRequestId, Title, Outcome, Justification, Conditions, EffectiveDate, ApprovedAmount, Currency, Modifications, ReferredTo, IsAcknowledged, AcknowledgedAt, CreatedAt, UpdatedAt)
VALUES (1, 1, 3, 'SuggestedAction', 1, NULL, NULL, 'Decision: Automated code review tool', 'Approved', 'Automated code review aligns with quality improvement goals. SonarQube is approved.', 'Must coordinate with IT Infrastructure for licensing and integration.', datetime('now', '-18 days'), NULL, NULL, NULL, NULL, 1, datetime('now', '-17 days'), datetime('now', '-20 days'), datetime('now', '-17 days'));

INSERT INTO Decisions (Id, ReportId, DecidedById, RequestType, SuggestedActionId, ResourceRequestId, SupportRequestId, Title, Outcome, Justification, Conditions, EffectiveDate, ApprovedAmount, Currency, Modifications, ReferredTo, IsAcknowledged, AcknowledgedAt, CreatedAt, UpdatedAt)
VALUES (2, 4, 3, 'SuggestedAction', 6, NULL, NULL, 'Decision: Predictive monitoring with ML', 'ApprovedWithModifications', 'Predictive monitoring approved but scope reduced to pilot phase first.', 'Pilot with network monitoring only. Full implementation pending pilot results.', datetime('now', '-14 days'), 25000.00, 'EGP', 'Start with network segment only. Expand after 3-month pilot shows positive results.', NULL, 1, datetime('now', '-13 days'), datetime('now', '-15 days'), datetime('now', '-13 days'));

-- Decisions on Resource Requests
INSERT INTO Decisions (Id, ReportId, DecidedById, RequestType, SuggestedActionId, ResourceRequestId, SupportRequestId, Title, Outcome, Justification, Conditions, EffectiveDate, ApprovedAmount, Currency, Modifications, ReferredTo, IsAcknowledged, AcknowledgedAt, CreatedAt, UpdatedAt)
VALUES (3, 1, 20, 'ResourceRequest', NULL, 1, NULL, 'Decision: Senior Backend Developer Hire', 'Approved', 'Critical staffing gap must be addressed. Full budget approved for 2 senior developer positions.', 'Recruitment must be completed within 60 days.', datetime('now', '-21 days'), 480000.00, 'EGP', NULL, NULL, 1, datetime('now', '-20 days'), datetime('now', '-22 days'), datetime('now', '-20 days'));

INSERT INTO Decisions (Id, ReportId, DecidedById, RequestType, SuggestedActionId, ResourceRequestId, SupportRequestId, Title, Outcome, Justification, Conditions, EffectiveDate, ApprovedAmount, Currency, Modifications, ReferredTo, IsAcknowledged, AcknowledgedAt, CreatedAt, UpdatedAt)
VALUES (4, 1, 3, 'ResourceRequest', NULL, 3, NULL, 'Decision: Cloud Infrastructure Budget', 'PartiallyApproved', 'Budget increase approved at 60% of requested amount. Full increase requires Q2 review.', 'Utilization report required monthly. Full increase subject to Q2 budget review.', datetime('now', '-17 days'), 15000.00, 'EGP', 'Approved 15,000 EGP instead of requested 25,000 EGP. Remainder subject to Q2 review.', NULL, 1, datetime('now', '-16 days'), datetime('now', '-18 days'), datetime('now', '-16 days'));

-- Decision on Support Request
INSERT INTO Decisions (Id, ReportId, DecidedById, RequestType, SuggestedActionId, ResourceRequestId, SupportRequestId, Title, Outcome, Justification, Conditions, EffectiveDate, ApprovedAmount, Currency, Modifications, ReferredTo, IsAcknowledged, AcknowledgedAt, CreatedAt, UpdatedAt)
VALUES (5, 4, 3, 'SupportRequest', NULL, NULL, 6, 'Decision: Vendor negotiation support', 'Approved', 'Management will support vendor negotiation. Procurement team assigned to assist.', 'Procurement team lead will join negotiation meeting.', datetime('now', '-16 days'), NULL, NULL, NULL, NULL, 1, datetime('now', '-15 days'), datetime('now', '-17 days'), datetime('now', '-15 days'));

-- Pending decision
INSERT INTO Decisions (Id, ReportId, DecidedById, RequestType, SuggestedActionId, ResourceRequestId, SupportRequestId, Title, Outcome, Justification, Conditions, EffectiveDate, ApprovedAmount, Currency, Modifications, ReferredTo, IsAcknowledged, AcknowledgedAt, CreatedAt, UpdatedAt)
VALUES (6, 1, 3, 'SuggestedAction', 3, NULL, NULL, 'Decision: Containerized deployments', 'Deferred', 'Good proposal but requires more planning. Defer to Q3 2026 for proper resource allocation.', NULL, NULL, NULL, NULL, NULL, 'IT Architecture Committee for detailed planning', 0, NULL, datetime('now', '-18 days'), NULL)
