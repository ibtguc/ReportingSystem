using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Seeds 200 users with human names across all organizational levels.
/// </summary>
public static class SeedUsers
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var users = GetUsers();
        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }

    private static List<User> GetUsers()
    {
        var now = DateTime.UtcNow;

        return new List<User>
        {
            // ========== EXECUTIVES (6) ==========
            new() { Id = 1, Email = "president@guc.edu.eg", Name = "Prof. Ahmed Hassan", Role = SystemRoles.Executive, OrganizationalUnitId = 1, JobTitle = "University President", IsActive = true, CreatedAt = now },
            new() { Id = 2, Email = "vp.academic@guc.edu.eg", Name = "Prof. Mona El-Said", Role = SystemRoles.Executive, OrganizationalUnitId = 1, JobTitle = "VP for Academic Affairs", IsActive = true, CreatedAt = now },
            new() { Id = 3, Email = "vp.admin@guc.edu.eg", Name = "Dr. Khaled Ibrahim", Role = SystemRoles.Executive, OrganizationalUnitId = 1, JobTitle = "VP for Administration", IsActive = true, CreatedAt = now },
            new() { Id = 4, Email = "dean.main@guc.edu.eg", Name = "Prof. Sara Mahmoud", Role = SystemRoles.Executive, OrganizationalUnitId = 2, JobTitle = "Dean - Main Campus", IsActive = true, CreatedAt = now },
            new() { Id = 5, Email = "dean.newcampus@guc.edu.eg", Name = "Prof. Omar Fathy", Role = SystemRoles.Executive, OrganizationalUnitId = 3, JobTitle = "Dean - New Campus", IsActive = true, CreatedAt = now },
            new() { Id = 61, Email = "head.admin@guc.edu.eg", Name = "Dr. Nadia Abdel-Rahman", Role = SystemRoles.Executive, OrganizationalUnitId = 37, JobTitle = "Director of Administrative Services", IsActive = true, CreatedAt = now },

            // ========== ADMINISTRATORS (3) ==========
            new() { Id = 6, Email = "admin@guc.edu.eg", Name = "Tarek Nabil", Role = SystemRoles.Administrator, OrganizationalUnitId = 9, JobTitle = "System Administrator", IsActive = true, CreatedAt = now },
            new() { Id = 7, Email = "admin2@guc.edu.eg", Name = "Yasmine Farouk", Role = SystemRoles.Administrator, OrganizationalUnitId = 22, JobTitle = "IT Systems Administrator", IsActive = true, CreatedAt = now },
            new() { Id = 8, Email = "admin3@guc.edu.eg", Name = "Mohamed Sherif", Role = SystemRoles.Administrator, OrganizationalUnitId = 23, JobTitle = "Infrastructure Administrator", IsActive = true, CreatedAt = now },

            // ========== DEPARTMENT HEADS (25) ==========
            // Engineering faculty
            new() { Id = 9, Email = "head.cse@guc.edu.eg", Name = "Prof. Nadia Kamel", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 12, JobTitle = "Head of CS & Engineering", IsActive = true, CreatedAt = now },
            new() { Id = 10, Email = "head.me@guc.edu.eg", Name = "Prof. Ayman Soliman", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 13, JobTitle = "Head of Mechanical Engineering", IsActive = true, CreatedAt = now },
            new() { Id = 11, Email = "head.ece@guc.edu.eg", Name = "Prof. Laila Abdel-Fattah", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 14, JobTitle = "Head of Electronics & Comm.", IsActive = true, CreatedAt = now },
            new() { Id = 12, Email = "head.arch@guc.edu.eg", Name = "Prof. Hisham Ragab", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 15, JobTitle = "Head of Architecture", IsActive = true, CreatedAt = now },
            // MET faculty
            new() { Id = 13, Email = "head.cs@guc.edu.eg", Name = "Prof. Fatma Zaki", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 16, JobTitle = "Head of Computer Science", IsActive = true, CreatedAt = now },
            new() { Id = 14, Email = "head.dm@guc.edu.eg", Name = "Dr. Ramy Shoukry", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 17, JobTitle = "Head of Digital Media", IsActive = true, CreatedAt = now },
            new() { Id = 15, Email = "head.nis@guc.edu.eg", Name = "Dr. Dina El-Masry", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 18, JobTitle = "Head of Networks & IS", IsActive = true, CreatedAt = now },
            // Management faculty
            new() { Id = 16, Email = "head.econ@guc.edu.eg", Name = "Prof. Sameh Attia", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 19, JobTitle = "Head of Economics", IsActive = true, CreatedAt = now },
            new() { Id = 17, Email = "head.fin@guc.edu.eg", Name = "Dr. Noha Salah", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 20, JobTitle = "Head of Finance & Accounting", IsActive = true, CreatedAt = now },
            // IT & Admin
            new() { Id = 18, Email = "head.sdev@guc.edu.eg", Name = "Eng. Mahmoud Adel", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 22, JobTitle = "Head of Software Development", IsActive = true, CreatedAt = now },
            new() { Id = 19, Email = "head.infra@guc.edu.eg", Name = "Eng. Heba Mostafa", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 23, JobTitle = "Head of IT Infrastructure", IsActive = true, CreatedAt = now },
            new() { Id = 20, Email = "head.hr@guc.edu.eg", Name = "Dr. Amira Youssef", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 24, JobTitle = "Head of Human Resources", IsActive = true, CreatedAt = now },
            new() { Id = 21, Email = "head.qa@guc.edu.eg", Name = "Dr. Sherif Hassan", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 25, JobTitle = "Head of QA & Audit", IsActive = true, CreatedAt = now },
            // Administrative Services departments
            new() { Id = 62, Email = "head.cfo@guc.edu.eg", Name = "Mr. Karim Mansour", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 38, JobTitle = "Chief Financial Officer", IsActive = true, CreatedAt = now },
            new() { Id = 63, Email = "head.legal@guc.edu.eg", Name = "Dr. Laila Ghanem", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 39, JobTitle = "Head of Legal & Compliance", IsActive = true, CreatedAt = now },
            new() { Id = 64, Email = "head.proc@guc.edu.eg", Name = "Mr. Ashraf El-Deeb", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 40, JobTitle = "Head of Procurement", IsActive = true, CreatedAt = now },
            new() { Id = 65, Email = "head.student@guc.edu.eg", Name = "Dr. Rania Fouad", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 41, JobTitle = "Dean of Students", IsActive = true, CreatedAt = now },
            new() { Id = 66, Email = "head.facilities@guc.edu.eg", Name = "Eng. Mohsen Abdallah", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 42, JobTitle = "Head of Facilities Management", IsActive = true, CreatedAt = now },
            new() { Id = 67, Email = "head.marketing@guc.edu.eg", Name = "Ms. Dina El-Sayed", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 43, JobTitle = "Head of Marketing & Communications", IsActive = true, CreatedAt = now },
            new() { Id = 68, Email = "head.research@guc.edu.eg", Name = "Prof. Tarek Zaki", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 44, JobTitle = "Director of Research & Innovation", IsActive = true, CreatedAt = now },
            new() { Id = 69, Email = "head.intl@guc.edu.eg", Name = "Dr. Yasmin Rashid", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 45, JobTitle = "Director of International Relations", IsActive = true, CreatedAt = now },
            new() { Id = 70, Email = "head.library@guc.edu.eg", Name = "Dr. Mahmoud Salem", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 46, JobTitle = "University Librarian", IsActive = true, CreatedAt = now },
            new() { Id = 71, Email = "head.security@guc.edu.eg", Name = "Col. Ahmed El-Mahdy", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 47, JobTitle = "Chief Security Officer", IsActive = true, CreatedAt = now },
            new() { Id = 72, Email = "head.registrar@guc.edu.eg", Name = "Ms. Fatma Nour", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 48, JobTitle = "University Registrar", IsActive = true, CreatedAt = now },
            new() { Id = 73, Email = "head.career@guc.edu.eg", Name = "Mr. Hany Ibrahim", Role = SystemRoles.DepartmentHead, OrganizationalUnitId = 49, JobTitle = "Director of Career Services", IsActive = true, CreatedAt = now },

            // ========== TEAM MANAGERS (28) ==========
            // IT Teams
            new() { Id = 22, Email = "mgr.web@guc.edu.eg", Name = "Eng. Ali Kamal", Role = SystemRoles.TeamManager, OrganizationalUnitId = 26, JobTitle = "Web Systems Section Lead", IsActive = true, CreatedAt = now },
            new() { Id = 23, Email = "mgr.mobile@guc.edu.eg", Name = "Eng. Salma Reda", Role = SystemRoles.TeamManager, OrganizationalUnitId = 27, JobTitle = "Mobile Development Lead", IsActive = true, CreatedAt = now },
            new() { Id = 24, Email = "mgr.netops@guc.edu.eg", Name = "Eng. Hassan Tawfik", Role = SystemRoles.TeamManager, OrganizationalUnitId = 28, JobTitle = "Network Operations Lead", IsActive = true, CreatedAt = now },
            new() { Id = 25, Email = "mgr.cloud@guc.edu.eg", Name = "Eng. Rana Mohamed", Role = SystemRoles.TeamManager, OrganizationalUnitId = 29, JobTitle = "Server & Cloud Lead", IsActive = true, CreatedAt = now },
            new() { Id = 26, Email = "mgr.backend@guc.edu.eg", Name = "Eng. Youssef Magdy", Role = SystemRoles.TeamManager, OrganizationalUnitId = 32, JobTitle = "Backend Team Lead", IsActive = true, CreatedAt = now },
            new() { Id = 27, Email = "mgr.frontend@guc.edu.eg", Name = "Eng. Nourhan Sayed", Role = SystemRoles.TeamManager, OrganizationalUnitId = 33, JobTitle = "Frontend Team Lead", IsActive = true, CreatedAt = now },
            new() { Id = 28, Email = "mgr.testing@guc.edu.eg", Name = "Eng. Karim Wael", Role = SystemRoles.TeamManager, OrganizationalUnitId = 34, JobTitle = "QA & Testing Lead", IsActive = true, CreatedAt = now },
            new() { Id = 29, Email = "mgr.ai@guc.edu.eg", Name = "Dr. Mariam Gamal", Role = SystemRoles.TeamManager, OrganizationalUnitId = 30, JobTitle = "AI & ML Section Lead", IsActive = true, CreatedAt = now },
            new() { Id = 30, Email = "mgr.se@guc.edu.eg", Name = "Dr. Wael Abdelrahman", Role = SystemRoles.TeamManager, OrganizationalUnitId = 31, JobTitle = "Software Engineering Lead", IsActive = true, CreatedAt = now },
            // Administrative section managers
            new() { Id = 74, Email = "mgr.admissions@guc.edu.eg", Name = "Ms. Reem Abdel-Aziz", Role = SystemRoles.TeamManager, OrganizationalUnitId = 50, JobTitle = "Admissions Manager", IsActive = true, CreatedAt = now },
            new() { Id = 75, Email = "mgr.counseling@guc.edu.eg", Name = "Dr. Noha Fathy", Role = SystemRoles.TeamManager, OrganizationalUnitId = 51, JobTitle = "Head Counselor", IsActive = true, CreatedAt = now },
            new() { Id = 76, Email = "mgr.activities@guc.edu.eg", Name = "Mr. Omar Tawfik", Role = SystemRoles.TeamManager, OrganizationalUnitId = 52, JobTitle = "Student Activities Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 77, Email = "mgr.maintenance@guc.edu.eg", Name = "Eng. Saeed Mostafa", Role = SystemRoles.TeamManager, OrganizationalUnitId = 53, JobTitle = "Maintenance Supervisor", IsActive = true, CreatedAt = now },
            new() { Id = 78, Email = "mgr.grounds@guc.edu.eg", Name = "Mr. Ibrahim Khalil", Role = SystemRoles.TeamManager, OrganizationalUnitId = 54, JobTitle = "Grounds Supervisor", IsActive = true, CreatedAt = now },
            new() { Id = 79, Email = "mgr.transport@guc.edu.eg", Name = "Mr. Mahmoud Ramzy", Role = SystemRoles.TeamManager, OrganizationalUnitId = 55, JobTitle = "Transportation Manager", IsActive = true, CreatedAt = now },
            new() { Id = 80, Email = "mgr.ap@guc.edu.eg", Name = "Ms. Heba Kamel", Role = SystemRoles.TeamManager, OrganizationalUnitId = 56, JobTitle = "Accounts Payable Manager", IsActive = true, CreatedAt = now },
            new() { Id = 81, Email = "mgr.ar@guc.edu.eg", Name = "Mr. Wael Hassan", Role = SystemRoles.TeamManager, OrganizationalUnitId = 57, JobTitle = "Accounts Receivable Manager", IsActive = true, CreatedAt = now },
            new() { Id = 82, Email = "mgr.budget@guc.edu.eg", Name = "Ms. Amira Yousry", Role = SystemRoles.TeamManager, OrganizationalUnitId = 58, JobTitle = "Budget & Planning Manager", IsActive = true, CreatedAt = now },
            new() { Id = 83, Email = "mgr.payroll@guc.edu.eg", Name = "Ms. Nevine Adel", Role = SystemRoles.TeamManager, OrganizationalUnitId = 59, JobTitle = "Payroll Manager", IsActive = true, CreatedAt = now },
            new() { Id = 84, Email = "mgr.recruitment@guc.edu.eg", Name = "Ms. Sara Farid", Role = SystemRoles.TeamManager, OrganizationalUnitId = 60, JobTitle = "Recruitment Manager", IsActive = true, CreatedAt = now },
            new() { Id = 85, Email = "mgr.training@guc.edu.eg", Name = "Mr. Khaled El-Masry", Role = SystemRoles.TeamManager, OrganizationalUnitId = 61, JobTitle = "Training Manager", IsActive = true, CreatedAt = now },
            new() { Id = 86, Email = "mgr.benefits@guc.edu.eg", Name = "Ms. Mona Sayed", Role = SystemRoles.TeamManager, OrganizationalUnitId = 62, JobTitle = "Benefits Administrator", IsActive = true, CreatedAt = now },
            new() { Id = 87, Email = "mgr.circulation@guc.edu.eg", Name = "Ms. Hanan Mostafa", Role = SystemRoles.TeamManager, OrganizationalUnitId = 63, JobTitle = "Circulation Manager", IsActive = true, CreatedAt = now },
            new() { Id = 88, Email = "mgr.digital@guc.edu.eg", Name = "Mr. Tamer El-Naggar", Role = SystemRoles.TeamManager, OrganizationalUnitId = 64, JobTitle = "Digital Resources Manager", IsActive = true, CreatedAt = now },
            new() { Id = 89, Email = "mgr.archives@guc.edu.eg", Name = "Ms. Azza Soliman", Role = SystemRoles.TeamManager, OrganizationalUnitId = 65, JobTitle = "Archives Manager", IsActive = true, CreatedAt = now },
            new() { Id = 90, Email = "mgr.digmktg@guc.edu.eg", Name = "Mr. Yasser Reda", Role = SystemRoles.TeamManager, OrganizationalUnitId = 66, JobTitle = "Digital Marketing Manager", IsActive = true, CreatedAt = now },
            new() { Id = 91, Email = "mgr.pr@guc.edu.eg", Name = "Ms. Eman Gamal", Role = SystemRoles.TeamManager, OrganizationalUnitId = 67, JobTitle = "Public Relations Manager", IsActive = true, CreatedAt = now },
            new() { Id = 92, Email = "mgr.events@guc.edu.eg", Name = "Ms. Layla Hamdy", Role = SystemRoles.TeamManager, OrganizationalUnitId = 68, JobTitle = "Events Manager", IsActive = true, CreatedAt = now },

            // ========== REPORT REVIEWERS (16) ==========
            new() { Id = 31, Email = "reviewer.cse1@guc.edu.eg", Name = "Dr. Hany Mourad", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 12, JobTitle = "Senior Lecturer - CSE", IsActive = true, CreatedAt = now },
            new() { Id = 32, Email = "reviewer.cse2@guc.edu.eg", Name = "Dr. Nesma Said", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 12, JobTitle = "Associate Professor - CSE", IsActive = true, CreatedAt = now },
            new() { Id = 33, Email = "reviewer.met@guc.edu.eg", Name = "Dr. Tamer Hosny", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 16, JobTitle = "Senior Lecturer - CS", IsActive = true, CreatedAt = now },
            new() { Id = 34, Email = "reviewer.sdev@guc.edu.eg", Name = "Eng. Waleed Emad", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 22, JobTitle = "Senior Developer", IsActive = true, CreatedAt = now },
            new() { Id = 35, Email = "reviewer.infra@guc.edu.eg", Name = "Eng. Maha Lotfy", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 23, JobTitle = "Senior Infrastructure Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 36, Email = "reviewer.mgt@guc.edu.eg", Name = "Dr. Azza Helmy", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 6, JobTitle = "Senior Lecturer - Management", IsActive = true, CreatedAt = now },
            new() { Id = 93, Email = "reviewer.finance@guc.edu.eg", Name = "Mr. Samir Abdel-Wahab", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 38, JobTitle = "Senior Financial Analyst", IsActive = true, CreatedAt = now },
            new() { Id = 94, Email = "reviewer.legal@guc.edu.eg", Name = "Ms. Mariam Shehata", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 39, JobTitle = "Senior Legal Counsel", IsActive = true, CreatedAt = now },
            new() { Id = 95, Email = "reviewer.student@guc.edu.eg", Name = "Dr. Ahmed Fouad", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 41, JobTitle = "Senior Student Advisor", IsActive = true, CreatedAt = now },
            new() { Id = 96, Email = "reviewer.facilities@guc.edu.eg", Name = "Eng. Hossam El-Din", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 42, JobTitle = "Senior Facilities Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 97, Email = "reviewer.mktg@guc.edu.eg", Name = "Ms. Nadia Kamal", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 43, JobTitle = "Senior Marketing Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 98, Email = "reviewer.research@guc.edu.eg", Name = "Dr. Adel El-Sayed", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 44, JobTitle = "Senior Research Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 99, Email = "reviewer.library@guc.edu.eg", Name = "Ms. Dalia Ibrahim", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 46, JobTitle = "Senior Librarian", IsActive = true, CreatedAt = now },
            new() { Id = 100, Email = "reviewer.eng1@guc.edu.eg", Name = "Dr. Hossam Shalaby", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 4, JobTitle = "Associate Professor - Engineering", IsActive = true, CreatedAt = now },
            new() { Id = 101, Email = "reviewer.eng2@guc.edu.eg", Name = "Dr. Iman Rasheed", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 4, JobTitle = "Assistant Professor - Engineering", IsActive = true, CreatedAt = now },
            new() { Id = 102, Email = "reviewer.mgt2@guc.edu.eg", Name = "Dr. Yehia Abdel-Fattah", Role = SystemRoles.ReportReviewer, OrganizationalUnitId = 6, JobTitle = "Associate Professor - Management", IsActive = true, CreatedAt = now },

            // ========== AUDITORS (5) ==========
            new() { Id = 57, Email = "auditor1@guc.edu.eg", Name = "Dr. Hazem Barakat", Role = SystemRoles.Auditor, OrganizationalUnitId = 25, JobTitle = "Internal Auditor", IsActive = true, CreatedAt = now },
            new() { Id = 58, Email = "auditor2@guc.edu.eg", Name = "Eng. Nevine Sami", Role = SystemRoles.Auditor, OrganizationalUnitId = 25, JobTitle = "Quality Auditor", IsActive = true, CreatedAt = now },
            new() { Id = 59, Email = "auditor3@guc.edu.eg", Name = "Dr. Tarek Mansour", Role = SystemRoles.Auditor, OrganizationalUnitId = 1, JobTitle = "External Audit Liaison", IsActive = true, CreatedAt = now },
            new() { Id = 199, Email = "auditor4@guc.edu.eg", Name = "Mr. Wael El-Shaarawy", Role = SystemRoles.Auditor, OrganizationalUnitId = 25, JobTitle = "Senior Internal Auditor", IsActive = true, CreatedAt = now },
            new() { Id = 200, Email = "auditor5@guc.edu.eg", Name = "Ms. Yasmine Fouad", Role = SystemRoles.Auditor, OrganizationalUnitId = 38, JobTitle = "Financial Auditor", IsActive = true, CreatedAt = now },

            // ========== REPORT ORIGINATORS (116) ==========
            // IT Development teams
            new() { Id = 37, Email = "dev.backend1@guc.edu.eg", Name = "Ahmed Samir", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 32, JobTitle = "Backend Developer", IsActive = true, CreatedAt = now },
            new() { Id = 38, Email = "dev.backend2@guc.edu.eg", Name = "Mohamed Ashraf", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 32, JobTitle = "Backend Developer", IsActive = true, CreatedAt = now },
            new() { Id = 39, Email = "dev.frontend1@guc.edu.eg", Name = "Farida Hassan", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 33, JobTitle = "Frontend Developer", IsActive = true, CreatedAt = now },
            new() { Id = 40, Email = "dev.frontend2@guc.edu.eg", Name = "Omar Khaled", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 33, JobTitle = "Frontend Developer", IsActive = true, CreatedAt = now },
            new() { Id = 41, Email = "qa.tester1@guc.edu.eg", Name = "Reem Adel", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 34, JobTitle = "QA Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 42, Email = "dev.mobile1@guc.edu.eg", Name = "Amr Fawzy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 35, JobTitle = "iOS Developer", IsActive = true, CreatedAt = now },
            new() { Id = 43, Email = "dev.mobile2@guc.edu.eg", Name = "Layla Mahmoud", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 36, JobTitle = "Android Developer", IsActive = true, CreatedAt = now },
            new() { Id = 44, Email = "netops1@guc.edu.eg", Name = "Mostafa Gamal", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 28, JobTitle = "Network Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 45, Email = "netops2@guc.edu.eg", Name = "Dalia Ayman", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 28, JobTitle = "Network Technician", IsActive = true, CreatedAt = now },
            new() { Id = 46, Email = "sysadmin1@guc.edu.eg", Name = "Kareem Hisham", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 29, JobTitle = "Systems Administrator", IsActive = true, CreatedAt = now },
            new() { Id = 47, Email = "cloud1@guc.edu.eg", Name = "Nada Sherif", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 29, JobTitle = "Cloud Engineer", IsActive = true, CreatedAt = now },
            // Academic faculty
            new() { Id = 48, Email = "faculty.cse1@guc.edu.eg", Name = "Dr. Bassem Aly", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 12, JobTitle = "Lecturer - CSE", IsActive = true, CreatedAt = now },
            new() { Id = 49, Email = "faculty.cse2@guc.edu.eg", Name = "Dr. Hoda Farid", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 12, JobTitle = "Assistant Lecturer - CSE", IsActive = true, CreatedAt = now },
            new() { Id = 50, Email = "faculty.me1@guc.edu.eg", Name = "Dr. Adel Ramadan", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 13, JobTitle = "Lecturer - Mechanical Eng.", IsActive = true, CreatedAt = now },
            new() { Id = 51, Email = "faculty.cs1@guc.edu.eg", Name = "Dr. Samar Nour", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 16, JobTitle = "Lecturer - Computer Science", IsActive = true, CreatedAt = now },
            new() { Id = 52, Email = "faculty.dm1@guc.edu.eg", Name = "Dr. Yara Essam", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 17, JobTitle = "Lecturer - Digital Media", IsActive = true, CreatedAt = now },
            new() { Id = 53, Email = "faculty.econ1@guc.edu.eg", Name = "Dr. Magdy Abbas", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 19, JobTitle = "Lecturer - Economics", IsActive = true, CreatedAt = now },
            new() { Id = 54, Email = "faculty.fin1@guc.edu.eg", Name = "Dr. Iman Khalil", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 20, JobTitle = "Lecturer - Finance", IsActive = true, CreatedAt = now },
            // HR staff
            new() { Id = 55, Email = "hr.staff1@guc.edu.eg", Name = "Marwa Elsayed", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 24, JobTitle = "HR Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 56, Email = "hr.staff2@guc.edu.eg", Name = "Nabil Tharwat", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 24, JobTitle = "HR Coordinator", IsActive = true, CreatedAt = now },
            // Finance Office staff
            new() { Id = 103, Email = "fin.ap1@guc.edu.eg", Name = "Ms. Sherine Mahmoud", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 56, JobTitle = "Accounts Payable Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 104, Email = "fin.ap2@guc.edu.eg", Name = "Mr. Ahmed Wagdy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 56, JobTitle = "Accounts Payable Clerk", IsActive = true, CreatedAt = now },
            new() { Id = 105, Email = "fin.ar1@guc.edu.eg", Name = "Ms. Manal Fawzy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 57, JobTitle = "Accounts Receivable Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 106, Email = "fin.ar2@guc.edu.eg", Name = "Mr. Hesham Nabil", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 57, JobTitle = "Collections Officer", IsActive = true, CreatedAt = now },
            new() { Id = 107, Email = "fin.budget1@guc.edu.eg", Name = "Ms. Salwa Ahmed", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 58, JobTitle = "Budget Analyst", IsActive = true, CreatedAt = now },
            new() { Id = 108, Email = "fin.payroll1@guc.edu.eg", Name = "Ms. Nagwa Farouk", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 59, JobTitle = "Payroll Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 109, Email = "fin.payroll2@guc.edu.eg", Name = "Mr. Mohamed Fathy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 59, JobTitle = "Payroll Clerk", IsActive = true, CreatedAt = now },
            // Legal staff
            new() { Id = 110, Email = "legal.counsel1@guc.edu.eg", Name = "Mr. Sherif El-Ghandour", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 39, JobTitle = "Legal Counsel", IsActive = true, CreatedAt = now },
            new() { Id = 111, Email = "legal.compliance@guc.edu.eg", Name = "Ms. Noura Hafez", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 39, JobTitle = "Compliance Officer", IsActive = true, CreatedAt = now },
            new() { Id = 112, Email = "legal.contracts@guc.edu.eg", Name = "Mr. Hazem Barakat", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 39, JobTitle = "Contracts Specialist", IsActive = true, CreatedAt = now },
            // Procurement staff
            new() { Id = 113, Email = "proc.buyer1@guc.edu.eg", Name = "Mr. Adel Shokry", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 40, JobTitle = "Senior Buyer", IsActive = true, CreatedAt = now },
            new() { Id = 114, Email = "proc.buyer2@guc.edu.eg", Name = "Ms. Ghada Lotfy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 40, JobTitle = "Procurement Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 115, Email = "proc.vendor@guc.edu.eg", Name = "Mr. Tariq El-Shafiey", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 40, JobTitle = "Vendor Manager", IsActive = true, CreatedAt = now },
            // Student Affairs staff
            new() { Id = 116, Email = "student.adm1@guc.edu.eg", Name = "Ms. Asmaa Ragab", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 50, JobTitle = "Admissions Officer", IsActive = true, CreatedAt = now },
            new() { Id = 117, Email = "student.adm2@guc.edu.eg", Name = "Mr. Ahmed El-Bakry", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 50, JobTitle = "Admissions Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 118, Email = "student.adm3@guc.edu.eg", Name = "Ms. Fatma Hassan", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 50, JobTitle = "Admissions Assistant", IsActive = true, CreatedAt = now },
            new() { Id = 119, Email = "student.couns1@guc.edu.eg", Name = "Dr. Ramy Shawky", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 51, JobTitle = "Student Counselor", IsActive = true, CreatedAt = now },
            new() { Id = 120, Email = "student.couns2@guc.edu.eg", Name = "Ms. Maha El-Sherif", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 51, JobTitle = "Wellness Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 121, Email = "student.act1@guc.edu.eg", Name = "Mr. Mostafa Gamal", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 52, JobTitle = "Activities Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 122, Email = "student.act2@guc.edu.eg", Name = "Ms. Yasmine Nour", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 52, JobTitle = "Clubs Administrator", IsActive = true, CreatedAt = now },
            // Facilities staff
            new() { Id = 123, Email = "fac.maint1@guc.edu.eg", Name = "Mr. Sobhy Mohamed", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 53, JobTitle = "Maintenance Technician", IsActive = true, CreatedAt = now },
            new() { Id = 124, Email = "fac.maint2@guc.edu.eg", Name = "Mr. Essam Abdel-Aziz", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 53, JobTitle = "HVAC Technician", IsActive = true, CreatedAt = now },
            new() { Id = 125, Email = "fac.maint3@guc.edu.eg", Name = "Mr. Ramadan Sayed", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 53, JobTitle = "Electrician", IsActive = true, CreatedAt = now },
            new() { Id = 126, Email = "fac.grounds1@guc.edu.eg", Name = "Mr. Abdel-Hakim Fathy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 54, JobTitle = "Groundskeeper", IsActive = true, CreatedAt = now },
            new() { Id = 127, Email = "fac.trans1@guc.edu.eg", Name = "Mr. Saber Hassan", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 55, JobTitle = "Fleet Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 128, Email = "fac.trans2@guc.edu.eg", Name = "Mr. Gamal Abdel-Nasser", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 55, JobTitle = "Transport Scheduler", IsActive = true, CreatedAt = now },
            // Marketing staff
            new() { Id = 129, Email = "mktg.digital1@guc.edu.eg", Name = "Ms. Rania El-Khouly", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 66, JobTitle = "Social Media Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 130, Email = "mktg.digital2@guc.edu.eg", Name = "Mr. Amr Shawky", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 66, JobTitle = "Content Creator", IsActive = true, CreatedAt = now },
            new() { Id = 131, Email = "mktg.pr1@guc.edu.eg", Name = "Ms. Sahar Mohamed", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 67, JobTitle = "PR Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 132, Email = "mktg.pr2@guc.edu.eg", Name = "Mr. Kareem Helal", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 67, JobTitle = "Media Relations Officer", IsActive = true, CreatedAt = now },
            new() { Id = 133, Email = "mktg.events1@guc.edu.eg", Name = "Ms. Marwa Tawfik", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 68, JobTitle = "Events Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 134, Email = "mktg.events2@guc.edu.eg", Name = "Mr. Tarek El-Guindy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 68, JobTitle = "Conference Planner", IsActive = true, CreatedAt = now },
            // Research staff
            new() { Id = 135, Email = "research.coord@guc.edu.eg", Name = "Dr. Samia El-Banna", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 44, JobTitle = "Research Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 136, Email = "research.grants@guc.edu.eg", Name = "Ms. Ola Abdel-Hamid", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 44, JobTitle = "Grants Administrator", IsActive = true, CreatedAt = now },
            new() { Id = 137, Email = "research.ip@guc.edu.eg", Name = "Mr. Hesham Khairy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 44, JobTitle = "IP & Technology Transfer Officer", IsActive = true, CreatedAt = now },
            // International Relations staff
            new() { Id = 138, Email = "intl.exchange@guc.edu.eg", Name = "Ms. Nermeen Samir", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 45, JobTitle = "Exchange Program Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 139, Email = "intl.partner@guc.edu.eg", Name = "Mr. Walid Helmy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 45, JobTitle = "Partnership Development Officer", IsActive = true, CreatedAt = now },
            new() { Id = 140, Email = "intl.visa@guc.edu.eg", Name = "Ms. Lobna Abdel-Azim", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 45, JobTitle = "International Student Advisor", IsActive = true, CreatedAt = now },
            // Library staff
            new() { Id = 141, Email = "lib.circ1@guc.edu.eg", Name = "Ms. Hala Mostafa", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 63, JobTitle = "Circulation Librarian", IsActive = true, CreatedAt = now },
            new() { Id = 142, Email = "lib.circ2@guc.edu.eg", Name = "Mr. Ramy El-Fiky", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 63, JobTitle = "Library Assistant", IsActive = true, CreatedAt = now },
            new() { Id = 143, Email = "lib.digital1@guc.edu.eg", Name = "Mr. Emad Khalifa", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 64, JobTitle = "Digital Resources Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 144, Email = "lib.digital2@guc.edu.eg", Name = "Ms. Aya Mohamed", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 64, JobTitle = "E-Resources Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 145, Email = "lib.archives@guc.edu.eg", Name = "Ms. Dina Abdel-Moneim", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 65, JobTitle = "Archivist", IsActive = true, CreatedAt = now },
            // Security staff
            new() { Id = 146, Email = "sec.ops1@guc.edu.eg", Name = "Mr. Mohamed El-Sharkawy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 47, JobTitle = "Security Operations Officer", IsActive = true, CreatedAt = now },
            new() { Id = 147, Email = "sec.safety@guc.edu.eg", Name = "Mr. Abdel-Rahman Youssef", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 47, JobTitle = "Safety Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 148, Email = "sec.emergency@guc.edu.eg", Name = "Mr. Hazem El-Badry", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 47, JobTitle = "Emergency Response Coordinator", IsActive = true, CreatedAt = now },
            // Registrar staff
            new() { Id = 149, Email = "reg.records@guc.edu.eg", Name = "Ms. Nadia Hassan", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 48, JobTitle = "Records Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 150, Email = "reg.schedule@guc.edu.eg", Name = "Mr. Ali Abdel-Ghani", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 48, JobTitle = "Academic Scheduler", IsActive = true, CreatedAt = now },
            new() { Id = 151, Email = "reg.transcript@guc.edu.eg", Name = "Ms. Soha Ramadan", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 48, JobTitle = "Transcripts Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 152, Email = "reg.graduation@guc.edu.eg", Name = "Ms. Heba El-Sawaf", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 48, JobTitle = "Graduation Coordinator", IsActive = true, CreatedAt = now },
            // Career Services staff
            new() { Id = 153, Email = "career.advisor1@guc.edu.eg", Name = "Ms. Rasha Abdel-Salam", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 49, JobTitle = "Career Advisor", IsActive = true, CreatedAt = now },
            new() { Id = 154, Email = "career.intern@guc.edu.eg", Name = "Mr. Hazem Taha", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 49, JobTitle = "Internship Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 155, Email = "career.alumni@guc.edu.eg", Name = "Ms. Mai El-Sherbiny", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 49, JobTitle = "Alumni Relations Officer", IsActive = true, CreatedAt = now },
            // HR section staff
            new() { Id = 156, Email = "hr.rec1@guc.edu.eg", Name = "Ms. Dalia Fawzy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 60, JobTitle = "Talent Acquisition Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 157, Email = "hr.rec2@guc.edu.eg", Name = "Mr. Karim Essam", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 60, JobTitle = "Recruitment Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 158, Email = "hr.training1@guc.edu.eg", Name = "Ms. Lamia Abdel-Fattah", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 61, JobTitle = "L&D Specialist", IsActive = true, CreatedAt = now },
            new() { Id = 159, Email = "hr.training2@guc.edu.eg", Name = "Mr. Bassem Shokry", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 61, JobTitle = "Training Coordinator", IsActive = true, CreatedAt = now },
            new() { Id = 160, Email = "hr.comp1@guc.edu.eg", Name = "Ms. Enas Mohamed", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 62, JobTitle = "Compensation Analyst", IsActive = true, CreatedAt = now },
            // Additional academic faculty
            new() { Id = 161, Email = "faculty.cse3@guc.edu.eg", Name = "Dr. Mohamed El-Menshawy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 12, JobTitle = "Senior Lecturer - CSE", IsActive = true, CreatedAt = now },
            new() { Id = 162, Email = "faculty.cse4@guc.edu.eg", Name = "Dr. Heba Abdel-Aal", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 12, JobTitle = "Assistant Professor - CSE", IsActive = true, CreatedAt = now },
            new() { Id = 163, Email = "faculty.cse5@guc.edu.eg", Name = "Eng. Tarek Fathy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 12, JobTitle = "Teaching Assistant - CSE", IsActive = true, CreatedAt = now },
            new() { Id = 164, Email = "faculty.me2@guc.edu.eg", Name = "Dr. Sherif El-Gohary", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 13, JobTitle = "Associate Professor - ME", IsActive = true, CreatedAt = now },
            new() { Id = 165, Email = "faculty.me3@guc.edu.eg", Name = "Dr. Amira Khairy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 13, JobTitle = "Assistant Professor - ME", IsActive = true, CreatedAt = now },
            new() { Id = 166, Email = "faculty.ece1@guc.edu.eg", Name = "Dr. Nabil El-Fishawy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 14, JobTitle = "Senior Lecturer - ECE", IsActive = true, CreatedAt = now },
            new() { Id = 167, Email = "faculty.ece2@guc.edu.eg", Name = "Dr. Reem Bahgat", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 14, JobTitle = "Assistant Professor - ECE", IsActive = true, CreatedAt = now },
            new() { Id = 168, Email = "faculty.arch1@guc.edu.eg", Name = "Dr. Mona El-Tayeb", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 15, JobTitle = "Senior Lecturer - Architecture", IsActive = true, CreatedAt = now },
            new() { Id = 169, Email = "faculty.arch2@guc.edu.eg", Name = "Dr. Yasser Wahba", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 15, JobTitle = "Assistant Professor - Architecture", IsActive = true, CreatedAt = now },
            new() { Id = 170, Email = "faculty.cs2@guc.edu.eg", Name = "Dr. Eman Helal", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 16, JobTitle = "Associate Professor - CS", IsActive = true, CreatedAt = now },
            new() { Id = 171, Email = "faculty.cs3@guc.edu.eg", Name = "Dr. Ahmed Fathy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 16, JobTitle = "Assistant Professor - CS", IsActive = true, CreatedAt = now },
            new() { Id = 172, Email = "faculty.dm2@guc.edu.eg", Name = "Dr. Sherine Fouad", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 17, JobTitle = "Senior Lecturer - Digital Media", IsActive = true, CreatedAt = now },
            new() { Id = 173, Email = "faculty.dm3@guc.edu.eg", Name = "Dr. Wessam Abdel-Wahab", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 17, JobTitle = "Assistant Professor - Digital Media", IsActive = true, CreatedAt = now },
            new() { Id = 174, Email = "faculty.nis1@guc.edu.eg", Name = "Dr. Karim El-Shabrawy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 18, JobTitle = "Senior Lecturer - Networks", IsActive = true, CreatedAt = now },
            new() { Id = 175, Email = "faculty.nis2@guc.edu.eg", Name = "Dr. Hoda Khalifa", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 18, JobTitle = "Assistant Professor - Networks", IsActive = true, CreatedAt = now },
            new() { Id = 176, Email = "faculty.econ2@guc.edu.eg", Name = "Dr. Amr El-Shafei", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 19, JobTitle = "Associate Professor - Economics", IsActive = true, CreatedAt = now },
            new() { Id = 177, Email = "faculty.econ3@guc.edu.eg", Name = "Dr. Nevine Salem", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 19, JobTitle = "Assistant Professor - Economics", IsActive = true, CreatedAt = now },
            new() { Id = 178, Email = "faculty.fin2@guc.edu.eg", Name = "Dr. Hazem El-Nahas", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 20, JobTitle = "Associate Professor - Finance", IsActive = true, CreatedAt = now },
            new() { Id = 179, Email = "faculty.fin3@guc.edu.eg", Name = "Dr. Salma Mostafa", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 20, JobTitle = "Assistant Professor - Accounting", IsActive = true, CreatedAt = now },
            new() { Id = 180, Email = "faculty.mgmt1@guc.edu.eg", Name = "Dr. Tamer Abdel-Ghaffar", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 21, JobTitle = "Senior Lecturer - Management", IsActive = true, CreatedAt = now },
            new() { Id = 181, Email = "faculty.mgmt2@guc.edu.eg", Name = "Dr. Nihal Fathy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 21, JobTitle = "Assistant Professor - Management", IsActive = true, CreatedAt = now },
            // Additional IT staff
            new() { Id = 182, Email = "dev.backend3@guc.edu.eg", Name = "Eng. Hany Abdel-Malak", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 32, JobTitle = "Senior Backend Developer", IsActive = true, CreatedAt = now },
            new() { Id = 183, Email = "dev.backend4@guc.edu.eg", Name = "Eng. Marwa Sobhy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 32, JobTitle = "Backend Developer", IsActive = true, CreatedAt = now },
            new() { Id = 184, Email = "dev.frontend3@guc.edu.eg", Name = "Eng. Maged El-Deeb", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 33, JobTitle = "Senior Frontend Developer", IsActive = true, CreatedAt = now },
            new() { Id = 185, Email = "dev.frontend4@guc.edu.eg", Name = "Eng. Nora Samy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 33, JobTitle = "UI Developer", IsActive = true, CreatedAt = now },
            new() { Id = 186, Email = "qa.tester2@guc.edu.eg", Name = "Eng. Ahmed El-Gammal", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 34, JobTitle = "Senior QA Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 187, Email = "qa.tester3@guc.edu.eg", Name = "Eng. Heba Saad", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 34, JobTitle = "Automation Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 188, Email = "dev.mobile3@guc.edu.eg", Name = "Eng. Youssef Abdel-Rahim", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 35, JobTitle = "Senior iOS Developer", IsActive = true, CreatedAt = now },
            new() { Id = 189, Email = "dev.mobile4@guc.edu.eg", Name = "Eng. Sara El-Kady", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 36, JobTitle = "Senior Android Developer", IsActive = true, CreatedAt = now },
            new() { Id = 190, Email = "netops3@guc.edu.eg", Name = "Eng. Ahmed Lotfy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 28, JobTitle = "Senior Network Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 191, Email = "cloud2@guc.edu.eg", Name = "Eng. Hazem Shawky", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 29, JobTitle = "Senior Cloud Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 192, Email = "sysadmin2@guc.edu.eg", Name = "Eng. Ola Magdy", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 29, JobTitle = "Systems Administrator", IsActive = true, CreatedAt = now },
            new() { Id = 193, Email = "ai.researcher1@guc.edu.eg", Name = "Dr. Khaled El-Ayat", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 30, JobTitle = "AI Researcher", IsActive = true, CreatedAt = now },
            new() { Id = 194, Email = "ai.researcher2@guc.edu.eg", Name = "Dr. Noha Ghanem", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 30, JobTitle = "ML Engineer", IsActive = true, CreatedAt = now },
            new() { Id = 195, Email = "se.researcher1@guc.edu.eg", Name = "Dr. Amr Abdel-Hamid", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 31, JobTitle = "SE Researcher", IsActive = true, CreatedAt = now },
            new() { Id = 196, Email = "se.researcher2@guc.edu.eg", Name = "Dr. Hana El-Sherif", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 31, JobTitle = "DevOps Researcher", IsActive = true, CreatedAt = now },
            new() { Id = 197, Email = "qa.analyst1@guc.edu.eg", Name = "Ms. Samar Raafat", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 25, JobTitle = "Quality Analyst", IsActive = true, CreatedAt = now },
            new() { Id = 198, Email = "qa.analyst2@guc.edu.eg", Name = "Mr. Mohamed Abdel-Aziz", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = 25, JobTitle = "Compliance Analyst", IsActive = true, CreatedAt = now },

            // ========== INACTIVE USER (1) ==========
            new() { Id = 60, Email = "former.staff@guc.edu.eg", Name = "Dr. Hamed Farouk", Role = SystemRoles.ReportOriginator, OrganizationalUnitId = null, JobTitle = "Former Faculty Member", IsActive = false, CreatedAt = now.AddDays(-365), LastLoginAt = now.AddDays(-180) },
        };
    }
}
