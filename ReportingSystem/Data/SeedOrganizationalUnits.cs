using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Seeds 68 organizational units across 6 levels.
/// </summary>
public static class SeedOrganizationalUnits
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var units = GetOrganizationalUnits();
        context.OrganizationalUnits.AddRange(units);
        await context.SaveChangesAsync();
    }

    private static List<OrganizationalUnit> GetOrganizationalUnits()
    {
        var now = DateTime.UtcNow;

        return new List<OrganizationalUnit>
        {
            // ========== ROOT (Level 0) ==========
            new() { Id = 1, Name = "German University in Cairo", Code = "GUC", Description = "The German University in Cairo - Root Organization", Level = OrgUnitLevel.Root, ParentId = null, SortOrder = 0, IsActive = true, CreatedAt = now },

            // ========== CAMPUSES (Level 1) ==========
            new() { Id = 2, Name = "Main Campus", Code = "MAIN", Description = "GUC Main Campus - Cairo", Level = OrgUnitLevel.Campus, ParentId = 1, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 3, Name = "New Campus", Code = "NEWC", Description = "GUC New Capital Campus", Level = OrgUnitLevel.Campus, ParentId = 1, SortOrder = 2, IsActive = true, CreatedAt = now },

            // ========== FACULTIES/DIVISIONS (Level 2) ==========
            new() { Id = 4, Name = "Faculty of Engineering", Code = "ENG", Description = "Engineering and Technology Faculty", Level = OrgUnitLevel.Faculty, ParentId = 2, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 5, Name = "Faculty of Media Engineering and Technology", Code = "MET", Description = "Media Engineering & Technology Faculty", Level = OrgUnitLevel.Faculty, ParentId = 2, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 6, Name = "Faculty of Management Technology", Code = "MGT", Description = "Business and Management Faculty", Level = OrgUnitLevel.Faculty, ParentId = 2, SortOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = 7, Name = "Faculty of Pharmacy and Biotechnology", Code = "PHAR", Description = "Pharmacy and Biotechnology Faculty", Level = OrgUnitLevel.Faculty, ParentId = 2, SortOrder = 4, IsActive = true, CreatedAt = now },
            new() { Id = 8, Name = "Faculty of Applied Sciences and Arts", Code = "ASA", Description = "Applied Sciences and Arts Faculty", Level = OrgUnitLevel.Faculty, ParentId = 2, SortOrder = 5, IsActive = true, CreatedAt = now },
            new() { Id = 9, Name = "IT & Administration Division", Code = "ITADM", Description = "Information Technology and Administration", Level = OrgUnitLevel.Faculty, ParentId = 2, SortOrder = 6, IsActive = true, CreatedAt = now },
            new() { Id = 10, Name = "Faculty of Engineering - New Campus", Code = "ENG-NC", Description = "Engineering Faculty at New Capital Campus", Level = OrgUnitLevel.Faculty, ParentId = 3, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 11, Name = "Faculty of MET - New Campus", Code = "MET-NC", Description = "MET Faculty at New Capital Campus", Level = OrgUnitLevel.Faculty, ParentId = 3, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 37, Name = "Administrative Services Division", Code = "ADMIN", Description = "Central Administrative Services", Level = OrgUnitLevel.Faculty, ParentId = 2, SortOrder = 7, IsActive = true, CreatedAt = now },

            // ========== DEPARTMENTS (Level 3) ==========
            // Engineering departments
            new() { Id = 12, Name = "Computer Science & Engineering", Code = "CSE", Description = "Computer Science and Engineering Department", Level = OrgUnitLevel.Department, ParentId = 4, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 13, Name = "Mechanical Engineering", Code = "ME", Description = "Mechanical Engineering Department", Level = OrgUnitLevel.Department, ParentId = 4, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 14, Name = "Electronics & Communications Engineering", Code = "ECE", Description = "Electronics and Communications Department", Level = OrgUnitLevel.Department, ParentId = 4, SortOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = 15, Name = "Architecture & Urban Design", Code = "ARCH", Description = "Architecture and Urban Design Department", Level = OrgUnitLevel.Department, ParentId = 4, SortOrder = 4, IsActive = true, CreatedAt = now },
            // MET departments
            new() { Id = 16, Name = "Computer Science", Code = "CS", Description = "Computer Science Department (MET)", Level = OrgUnitLevel.Department, ParentId = 5, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 17, Name = "Digital Media", Code = "DM", Description = "Digital Media Department", Level = OrgUnitLevel.Department, ParentId = 5, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 18, Name = "Networks & Information Systems", Code = "NIS", Description = "Networks and Information Systems Department", Level = OrgUnitLevel.Department, ParentId = 5, SortOrder = 3, IsActive = true, CreatedAt = now },
            // Management departments
            new() { Id = 19, Name = "Economics", Code = "ECON", Description = "Economics Department", Level = OrgUnitLevel.Department, ParentId = 6, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 20, Name = "Finance & Accounting", Code = "FIN", Description = "Finance and Accounting Department", Level = OrgUnitLevel.Department, ParentId = 6, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 21, Name = "Management", Code = "MGMT", Description = "Management Department", Level = OrgUnitLevel.Department, ParentId = 6, SortOrder = 3, IsActive = true, CreatedAt = now },
            // IT & Admin departments
            new() { Id = 22, Name = "Software Development", Code = "SDEV", Description = "Software Development Department", Level = OrgUnitLevel.Department, ParentId = 9, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 23, Name = "IT Infrastructure", Code = "INFRA", Description = "IT Infrastructure and Networks Department", Level = OrgUnitLevel.Department, ParentId = 9, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 24, Name = "Human Resources", Code = "HR", Description = "Human Resources Department", Level = OrgUnitLevel.Department, ParentId = 9, SortOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = 25, Name = "Quality Assurance & Audit", Code = "QA", Description = "Quality Assurance and Internal Audit", Level = OrgUnitLevel.Department, ParentId = 9, SortOrder = 4, IsActive = true, CreatedAt = now },
            // Administrative Services departments
            new() { Id = 38, Name = "Finance Office", Code = "CFO", Description = "Central Finance and Budget Office", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 39, Name = "Legal & Compliance", Code = "LEGAL", Description = "Legal Affairs and Regulatory Compliance", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 40, Name = "Procurement & Contracts", Code = "PROC", Description = "Procurement and Contract Management", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = 41, Name = "Student Affairs", Code = "STUD", Description = "Student Services and Support", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 4, IsActive = true, CreatedAt = now },
            new() { Id = 42, Name = "Facilities Management", Code = "FAC", Description = "Campus Facilities and Maintenance", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 5, IsActive = true, CreatedAt = now },
            new() { Id = 43, Name = "Marketing & Communications", Code = "MKTG", Description = "Marketing, PR, and External Communications", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 6, IsActive = true, CreatedAt = now },
            new() { Id = 44, Name = "Research & Innovation", Code = "RIO", Description = "Research Support and Innovation Office", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 7, IsActive = true, CreatedAt = now },
            new() { Id = 45, Name = "International Relations", Code = "INTL", Description = "International Partnerships and Exchange Programs", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 8, IsActive = true, CreatedAt = now },
            new() { Id = 46, Name = "Library & Information Services", Code = "LIB", Description = "University Library and Digital Resources", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 9, IsActive = true, CreatedAt = now },
            new() { Id = 47, Name = "Security & Safety", Code = "SEC", Description = "Campus Security and Safety Management", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 10, IsActive = true, CreatedAt = now },
            new() { Id = 48, Name = "Registrar Office", Code = "REG", Description = "Student Registration and Academic Records", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 11, IsActive = true, CreatedAt = now },
            new() { Id = 49, Name = "Career Services", Code = "CAREER", Description = "Career Development and Alumni Relations", Level = OrgUnitLevel.Department, ParentId = 37, SortOrder = 12, IsActive = true, CreatedAt = now },

            // ========== SECTORS/SECTIONS (Level 4) ==========
            // Software Development sections
            new() { Id = 26, Name = "Web Systems Section", Code = "SDEV-WEB", Description = "Web Application Development Section", Level = OrgUnitLevel.Sector, ParentId = 22, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 27, Name = "Mobile Development Section", Code = "SDEV-MOB", Description = "Mobile Application Development Section", Level = OrgUnitLevel.Sector, ParentId = 22, SortOrder = 2, IsActive = true, CreatedAt = now },
            // IT Infrastructure sections
            new() { Id = 28, Name = "Network Operations Section", Code = "INFRA-NET", Description = "Network Operations and Maintenance", Level = OrgUnitLevel.Sector, ParentId = 23, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 29, Name = "Server & Cloud Section", Code = "INFRA-SRV", Description = "Server Administration and Cloud Services", Level = OrgUnitLevel.Sector, ParentId = 23, SortOrder = 2, IsActive = true, CreatedAt = now },
            // CSE research sections
            new() { Id = 30, Name = "AI & Machine Learning Section", Code = "CSE-AI", Description = "Artificial Intelligence and ML Research", Level = OrgUnitLevel.Sector, ParentId = 12, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 31, Name = "Software Engineering Section", Code = "CSE-SE", Description = "Software Engineering Research and Teaching", Level = OrgUnitLevel.Sector, ParentId = 12, SortOrder = 2, IsActive = true, CreatedAt = now },
            // Student Affairs sections
            new() { Id = 50, Name = "Admissions Office", Code = "STUD-ADM", Description = "Student Admissions and Enrollment", Level = OrgUnitLevel.Sector, ParentId = 41, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 51, Name = "Student Counseling", Code = "STUD-COUNS", Description = "Student Counseling and Wellness", Level = OrgUnitLevel.Sector, ParentId = 41, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 52, Name = "Student Activities", Code = "STUD-ACT", Description = "Clubs, Events, and Student Life", Level = OrgUnitLevel.Sector, ParentId = 41, SortOrder = 3, IsActive = true, CreatedAt = now },
            // Facilities sections
            new() { Id = 53, Name = "Building Maintenance", Code = "FAC-MAINT", Description = "Building and Infrastructure Maintenance", Level = OrgUnitLevel.Sector, ParentId = 42, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 54, Name = "Grounds & Landscaping", Code = "FAC-GRND", Description = "Grounds and Landscaping Services", Level = OrgUnitLevel.Sector, ParentId = 42, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 55, Name = "Transportation Services", Code = "FAC-TRANS", Description = "Campus Transportation and Fleet", Level = OrgUnitLevel.Sector, ParentId = 42, SortOrder = 3, IsActive = true, CreatedAt = now },
            // Finance Office sections
            new() { Id = 56, Name = "Accounts Payable", Code = "CFO-AP", Description = "Accounts Payable and Vendor Payments", Level = OrgUnitLevel.Sector, ParentId = 38, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 57, Name = "Accounts Receivable", Code = "CFO-AR", Description = "Accounts Receivable and Collections", Level = OrgUnitLevel.Sector, ParentId = 38, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 58, Name = "Budget & Planning", Code = "CFO-BUD", Description = "Budget Planning and Analysis", Level = OrgUnitLevel.Sector, ParentId = 38, SortOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = 59, Name = "Payroll", Code = "CFO-PAY", Description = "Payroll Administration", Level = OrgUnitLevel.Sector, ParentId = 38, SortOrder = 4, IsActive = true, CreatedAt = now },
            // HR sections
            new() { Id = 60, Name = "Recruitment & Hiring", Code = "HR-REC", Description = "Talent Acquisition and Recruitment", Level = OrgUnitLevel.Sector, ParentId = 24, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 61, Name = "Training & Development", Code = "HR-TRN", Description = "Employee Training and Development", Level = OrgUnitLevel.Sector, ParentId = 24, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 62, Name = "Compensation & Benefits", Code = "HR-COMP", Description = "Compensation and Benefits Administration", Level = OrgUnitLevel.Sector, ParentId = 24, SortOrder = 3, IsActive = true, CreatedAt = now },
            // Library sections
            new() { Id = 63, Name = "Circulation Services", Code = "LIB-CIRC", Description = "Library Circulation and Lending", Level = OrgUnitLevel.Sector, ParentId = 46, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 64, Name = "Digital Resources", Code = "LIB-DIG", Description = "Digital Library and E-Resources", Level = OrgUnitLevel.Sector, ParentId = 46, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 65, Name = "Archives & Special Collections", Code = "LIB-ARCH", Description = "Archives and Special Collections", Level = OrgUnitLevel.Sector, ParentId = 46, SortOrder = 3, IsActive = true, CreatedAt = now },
            // Marketing sections
            new() { Id = 66, Name = "Digital Marketing", Code = "MKTG-DIG", Description = "Digital Marketing and Social Media", Level = OrgUnitLevel.Sector, ParentId = 43, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 67, Name = "Public Relations", Code = "MKTG-PR", Description = "Public Relations and Media", Level = OrgUnitLevel.Sector, ParentId = 43, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 68, Name = "Events Management", Code = "MKTG-EVT", Description = "University Events and Conferences", Level = OrgUnitLevel.Sector, ParentId = 43, SortOrder = 3, IsActive = true, CreatedAt = now },

            // ========== TEAMS (Level 5) ==========
            new() { Id = 32, Name = "Backend Team", Code = "WEB-BE", Description = "Backend Development Team", Level = OrgUnitLevel.Team, ParentId = 26, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 33, Name = "Frontend Team", Code = "WEB-FE", Description = "Frontend Development Team", Level = OrgUnitLevel.Team, ParentId = 26, SortOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = 34, Name = "QA & Testing Team", Code = "WEB-QA", Description = "Quality Assurance and Testing Team", Level = OrgUnitLevel.Team, ParentId = 26, SortOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = 35, Name = "iOS Team", Code = "MOB-IOS", Description = "iOS Mobile Development Team", Level = OrgUnitLevel.Team, ParentId = 27, SortOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = 36, Name = "Android Team", Code = "MOB-AND", Description = "Android Mobile Development Team", Level = OrgUnitLevel.Team, ParentId = 27, SortOrder = 2, IsActive = true, CreatedAt = now },
        };
    }
}
