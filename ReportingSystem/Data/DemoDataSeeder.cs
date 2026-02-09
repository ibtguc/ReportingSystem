using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Seeds realistic demo/transactional data across all tables for the Q4 2025
/// Institutional Reporting Cycle scenario. Depends on OrganizationSeeder having
/// already created users, committees, and memberships.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Only seed if no reports exist yet (idempotent)
        if (await context.Reports.AnyAsync())
            return;

        // ── Helper: look up users and committees by name/email ──
        var allUsers = await context.Users.ToListAsync();
        var allCommittees = await context.Committees
            .Include(c => c.Memberships)
            .ToListAsync();

        User? UserByEmail(string email) => allUsers.FirstOrDefault(u => u.Email == email);
        User? UserByName(string namePart) => allUsers.FirstOrDefault(u => u.Name.Contains(namePart));
        Committee? Comm(string namePart) => allCommittees.FirstOrDefault(c => c.Name.Contains(namePart));

        // Key users for the demo scenario
        var chairman   = UserByEmail("h.elsayed@org.edu")!;
        var chiefStaff = UserByEmail("n.kamel@org.edu")!;
        var co2        = UserByEmail("t.mansour@org.edu")!;
        var admin      = UserByEmail("admin@org.edu")!;

        // L0 General Secretaries
        var gsAcademic = UserByName("Amira Shalaby")!;
        var gsAdmin    = UserByName("Khaled Mostafa")!;
        var gsTech     = UserByName("Mariam Fawzy")!;
        var gsFinance  = UserByName("Adel Soliman")!;
        var gsStudent  = UserByName("Heba Nasser")!;

        // L1 Directors
        var dirAcadProg   = UserByName("Farid Zaki")!;
        var dirResearch   = UserByName("Ayman Tawfik")!;
        var dirQuality    = UserByName("Noha Mahmoud")!;
        var dirFacilities = UserByName("Mahmoud Gabr")!;
        var dirHR         = UserByName("Ashraf Youssef")!;
        var dirSoftware   = UserByName("Ibrahim Hassan")!;
        var dirITInfra    = UserByName("Yasmin Farouk")!;
        var dirFinance    = UserByName("Hesham Farag")!;
        var dirLegal      = UserByName("Nermeen Khalil")!;
        var dirStudAffairs= UserByName("Amal Fathi")!;
        var dirCareer     = UserByName("Ghada Mohsen")!;

        // L2 Function members (authors for detailed reports)
        var lamiaRefaat   = UserByName("Lamia Refaat")!;   // Curriculum Dev
        var wissamKhoury  = UserByName("Wissam Khoury")!;  // Curriculum Dev
        var yasserNasr    = UserByName("Yasser Nasr")!;    // Teaching & Faculty
        var soniaGuirguis = UserByName("Sonia Guirguis")!; // Exam & Assessment
        var magedBotros   = UserByName("Maged Botros")!;   // E-Learning
        var manalRizk     = UserByName("Manal Rizk")!;     // QA
        var nabilaFikry   = UserByName("Nabila Fikry")!;   // Research Grants
        var walidDarwish  = UserByName("Walid Darwish")!;  // Graduate Programs
        var karimTawfik   = UserByName("Karim Tawfik")!;   // Cybersecurity (L1)
        var hazemRizk     = UserByName("Hazem Rizk")!;     // Tech Support (L1)
        var gamalReda     = UserByName("Gamal Reda")!;     // Internal Audit (L1)
        var mostafaSalem  = UserByName("Mostafa Salem")!;  // Strategic Planning (L1)
        var tamerElSisi   = UserByName("Tamer El-Sisi")!;  // Academic Support (L1)
        var halaShafik    = UserByName("Hala Shafik")!;    // Student Activities (L1)

        // Committees
        var topLevel       = Comm("Top Level Committee")!;
        var acadPrograms   = Comm("Academic Programs Directorate")!;
        var researchGrad   = Comm("Research & Graduate Studies")!;
        var acadQuality    = Comm("Academic Quality & Accreditation")!;
        var softwareDev    = Comm("Software Development Directorate")!;
        var itInfra        = Comm("IT Infrastructure & Networks")!;
        var financeAcct    = Comm("Finance & Accounting")!;
        var legalCompl     = Comm("Legal & Compliance")!;
        var studentAffairs = Comm("Student Affairs Directorate")!;
        var careerServices = Comm("Career Services & Alumni")!;
        var currDev        = Comm("Curriculum Development")!;
        var teachFaculty   = Comm("Teaching & Faculty Affairs")!;
        var examAssess     = Comm("Examination & Assessment")!;
        var eLearning      = Comm("E-Learning & Digital Education")!;
        var resGrants      = Comm("Research Grants & Funding")!;
        var gradPrograms   = Comm("Graduate Programs Administration")!;
        var qaStandards    = Comm("Quality Assurance & Standards")!;
        var cybersecurity  = Comm("Cybersecurity")!;
        var genAcct        = Comm("General Accounting & Reporting")!;
        var budgeting      = Comm("Budgeting & Financial Planning")!;
        var admissions     = Comm("Admissions & Registration")!;
        var studentWelfare = Comm("Student Welfare & Counseling")!;
        var clubs          = Comm("Clubs & Organizations")!;
        var careerCounsel  = Comm("Career Counseling & Placement")!;
        var facilities     = Comm("Facilities & Maintenance")!;
        var humanRes       = Comm("Human Resources Directorate")!;
        var internalAudit  = Comm("Internal Audit Directorate")!;
        var stratPlan      = Comm("Strategic Planning")!;

        // ── Timestamps ──
        var now = DateTime.UtcNow;
        DateTime D(int daysAgo) => now.AddDays(-daysAgo);

        // ════════════════════════════════════════════════════════════
        //  PHASE A: REPORTS
        // ════════════════════════════════════════════════════════════

        await SeedReportsAsync(context, now, D,
            chairman, chiefStaff, co2, admin,
            gsAcademic, gsAdmin, gsTech, gsFinance, gsStudent,
            dirAcadProg, dirResearch, dirQuality, dirSoftware, dirITInfra,
            dirFinance, dirLegal, dirStudAffairs, dirCareer, dirFacilities,
            lamiaRefaat, wissamKhoury, yasserNasr, soniaGuirguis, magedBotros,
            manalRizk, nabilaFikry, walidDarwish, karimTawfik, hazemRizk,
            gamalReda, mostafaSalem, tamerElSisi, halaShafik,
            topLevel, acadPrograms, researchGrad, acadQuality,
            softwareDev, itInfra, financeAcct, legalCompl,
            studentAffairs, careerServices,
            currDev, teachFaculty, examAssess, eLearning,
            resGrants, gradPrograms, qaStandards, cybersecurity,
            genAcct, budgeting, admissions, studentWelfare, clubs, careerCounsel,
            facilities, humanRes, internalAudit, stratPlan);

        // ════════════════════════════════════════════════════════════
        //  PHASE B: DIRECTIVES
        // ════════════════════════════════════════════════════════════

        await SeedDirectivesAsync(context, now, D,
            chairman, chiefStaff, co2,
            gsAcademic, gsAdmin, gsTech, gsFinance, gsStudent,
            dirAcadProg, dirResearch, dirSoftware, dirITInfra,
            dirFinance, dirStudAffairs, dirFacilities,
            karimTawfik, gamalReda,
            topLevel, acadPrograms, researchGrad,
            softwareDev, itInfra, financeAcct,
            studentAffairs, currDev, cybersecurity,
            facilities, humanRes, internalAudit);

        // ════════════════════════════════════════════════════════════
        //  PHASE C: MEETINGS
        // ════════════════════════════════════════════════════════════

        await SeedMeetingsAsync(context, now, D,
            chairman, chiefStaff, co2,
            gsAcademic, gsAdmin, gsTech, gsFinance, gsStudent,
            dirAcadProg, dirResearch, dirQuality, dirSoftware, dirITInfra,
            dirFinance, dirStudAffairs, dirFacilities,
            lamiaRefaat, yasserNasr, soniaGuirguis, magedBotros,
            manalRizk, karimTawfik, gamalReda, mostafaSalem,
            topLevel, acadPrograms, researchGrad, acadQuality,
            softwareDev, itInfra, financeAcct,
            studentAffairs, cybersecurity, facilities);

        // ════════════════════════════════════════════════════════════
        //  PHASE D: CONFIDENTIALITY, NOTIFICATIONS, AUDIT LOG
        // ════════════════════════════════════════════════════════════

        await SeedCrossCuttingAsync(context, now, D,
            chairman, chiefStaff, co2, admin,
            gsAcademic, gsAdmin, gsTech, gsFinance, gsStudent,
            dirAcadProg, dirITInfra, dirFinance, dirStudAffairs,
            karimTawfik, gamalReda,
            topLevel, acadPrograms, itInfra, financeAcct, internalAudit);
    }

    // ────────────────────────────────────────────────────────────────
    //  PHASE A: Reports + StatusHistory + SourceLinks
    // ────────────────────────────────────────────────────────────────
    private static async Task SeedReportsAsync(
        ApplicationDbContext context, DateTime now, Func<int, DateTime> D,
        User chairman, User chiefStaff, User co2, User admin,
        User gsAcademic, User gsAdmin, User gsTech, User gsFinance, User gsStudent,
        User dirAcadProg, User dirResearch, User dirQuality, User dirSoftware, User dirITInfra,
        User dirFinance, User dirLegal, User dirStudAffairs, User dirCareer, User dirFacilities,
        User lamiaRefaat, User wissamKhoury, User yasserNasr, User soniaGuirguis, User magedBotros,
        User manalRizk, User nabilaFikry, User walidDarwish, User karimTawfik, User hazemRizk,
        User gamalReda, User mostafaSalem, User tamerElSisi, User halaShafik,
        Committee topLevel, Committee acadPrograms, Committee researchGrad, Committee acadQuality,
        Committee softwareDev, Committee itInfra, Committee financeAcct, Committee legalCompl,
        Committee studentAffairs, Committee careerServices,
        Committee currDev, Committee teachFaculty, Committee examAssess, Committee eLearning,
        Committee resGrants, Committee gradPrograms, Committee qaStandards, Committee cybersecurity,
        Committee genAcct, Committee budgeting, Committee admissions, Committee studentWelfare,
        Committee clubs, Committee careerCounsel,
        Committee facilities, Committee humanRes, Committee internalAudit, Committee stratPlan)
    {
        var reports = new List<Report>();
        var histories = new List<ReportStatusHistory>();
        var sourceLinks = new List<ReportSourceLink>();

        // Helper: create a report and its status history trail
        int rid = 0; // track index for linking
        Report R(string title, ReportType type, ReportStatus status,
                 User author, Committee committee, int daysAgoCreated,
                 string body, string? action = null, string? resources = null,
                 string? support = null, string? remarks = null,
                 bool confidential = false, int version = 1)
        {
            var r = new Report
            {
                Title = title,
                ReportType = type,
                Status = status,
                AuthorId = author.Id,
                CommitteeId = committee.Id,
                BodyContent = body,
                SuggestedAction = action,
                NeededResources = resources,
                NeededSupport = support,
                SpecialRemarks = remarks,
                IsConfidential = confidential,
                Version = version,
                CreatedAt = D(daysAgoCreated),
                SubmittedAt = status >= ReportStatus.Submitted ? D(daysAgoCreated - 1) : null,
                UpdatedAt = status >= ReportStatus.Submitted ? D(daysAgoCreated - 2) : null
            };
            reports.Add(r);
            return r;
        }

        // Helper: add status history for a report (call after context.SaveChanges to get IDs)
        void H(Report r, ReportStatus from, ReportStatus to, User by, int daysAgo, string? comment = null)
        {
            histories.Add(new ReportStatusHistory
            {
                Report = r,
                OldStatus = from,
                NewStatus = to,
                ChangedBy = by,
                ChangedAt = D(daysAgo),
                Comments = comment
            });
        }

        // ── L3 Process Reports (Sector 1: Academic) ──

        var r1 = R("Q4 Course Design Review — Engineering Programs",
            ReportType.Detailed, ReportStatus.Approved,
            lamiaRefaat, currDev, 45,
            "<h3>Course Design Review</h3><p>Completed comprehensive review of 12 engineering program course syllabi for Q4 2025. Key findings include alignment gaps with ABET criteria in 3 courses (ME301, EE405, CE210) and successful integration of industry feedback in 9 courses.</p><p>Updated learning outcomes for all reviewed courses. New rubric templates distributed to faculty.</p>",
            "Schedule follow-up review for flagged courses within 30 days",
            "Access to updated ABET 2025 criteria documentation",
            "Faculty Development Center coordination for rubric workshops");
        H(r1, ReportStatus.Draft, ReportStatus.Submitted, lamiaRefaat, 44);
        H(r1, ReportStatus.Submitted, ReportStatus.Submitted, dirAcadProg, 42);
        H(r1, ReportStatus.Submitted, ReportStatus.Approved, dirAcadProg, 38, "Thorough review. Approved for summarization.");

        var r2 = R("Faculty Recruitment Progress — Fall 2025 Cohort",
            ReportType.Detailed, ReportStatus.Approved,
            yasserNasr, teachFaculty, 42,
            "<h3>Recruitment Status</h3><p>14 of 18 planned faculty positions filled for Fall 2025. Remaining 4 positions: 2 in Computer Science (AI/ML specialization), 1 in Biomedical Engineering, 1 in Data Analytics.</p><p>Candidate pipeline: 23 applications under review, 8 interviews scheduled for January.</p><h4>Compensation Benchmarking</h4><p>Salary offers aligned with regional median. Two candidates declined due to competing offers from Gulf universities — recommended 8% adjustment for critical specializations.</p>",
            "Expedite CS faculty search with expanded advertising budget",
            "Additional EGP 50,000 for international recruitment advertising",
            "HR Directorate fast-track processing for visa-required candidates",
            "Consider visiting professor arrangements as interim solution");
        H(r2, ReportStatus.Draft, ReportStatus.Submitted, yasserNasr, 41);
        H(r2, ReportStatus.Submitted, ReportStatus.Submitted, dirAcadProg, 40);
        H(r2, ReportStatus.Submitted, ReportStatus.Approved, dirAcadProg, 37, "Approved. Salary adjustment recommendation forwarded to Finance.");

        var r3 = R("Mid-Term Examination Logistics Report",
            ReportType.Detailed, ReportStatus.Approved,
            soniaGuirguis, examAssess, 40,
            "<h3>Mid-Term Exam Administration</h3><p>Successfully administered 342 mid-term examinations across 28 venues over 12 days. Zero security incidents reported.</p><h4>Highlights</h4><ul><li>Digital exam pilot: 45 exams conducted via LMS with remote proctoring</li><li>Average turnaround for grade submission: 5.2 days (target: 7 days)</li><li>Student accommodation requests processed: 67 (all fulfilled)</li></ul><h4>Issues</h4><p>Venue scheduling conflicts in Building C required last-minute relocations for 8 exams. Recommend dedicated exam scheduling system.</p>",
            "Procure dedicated exam scheduling software for Spring 2026",
            "Budget allocation for exam management system (est. EGP 120,000)");
        H(r3, ReportStatus.Draft, ReportStatus.Submitted, soniaGuirguis, 39);
        H(r3, ReportStatus.Submitted, ReportStatus.Submitted, dirAcadProg, 38);
        H(r3, ReportStatus.Submitted, ReportStatus.Approved, dirAcadProg, 35);

        var r4 = R("E-Learning Platform Utilization — Q4 2025",
            ReportType.Detailed, ReportStatus.Approved,
            magedBotros, eLearning, 38,
            "<h3>LMS Usage Statistics</h3><p>Moodle platform served 8,450 active students and 312 faculty members in Q4. Total logins: 1.2M (18% increase from Q3).</p><h4>Content Production</h4><ul><li>New video lectures uploaded: 234</li><li>Interactive assessments created: 189</li><li>Average student engagement time: 4.2 hours/week</li></ul><p>Mobile app adoption reached 62% of student body. Server uptime: 99.7% (SLA target: 99.5%).</p>",
            "Expand server capacity for Spring 2026 enrollment surge",
            "2 additional application servers, CDN subscription upgrade");
        H(r4, ReportStatus.Draft, ReportStatus.Submitted, magedBotros, 37);
        H(r4, ReportStatus.Submitted, ReportStatus.Submitted, dirAcadProg, 36);
        H(r4, ReportStatus.Submitted, ReportStatus.Approved, dirAcadProg, 33);

        // ── L3 Process Reports (Sector 3: Technology — feedback loop) ──

        var r5 = R("Cybersecurity Threat Assessment — Q4 2025",
            ReportType.Detailed, ReportStatus.Approved,
            karimTawfik, cybersecurity, 50,
            "<h3>Threat Landscape</h3><p>Blocked 12,847 intrusion attempts in Q4 (34% increase from Q3). Two targeted phishing campaigns detected and neutralized within 4 hours of detection.</p><h4>Vulnerability Assessment</h4><p>Completed penetration testing on 15 critical systems. 3 high-severity vulnerabilities identified and patched. Mean time to patch: 48 hours.</p><h4>Compliance</h4><p>ISO 27001 internal audit completed. 2 minor non-conformities addressed. External certification audit scheduled for February 2026.</p>",
            "Implement zero-trust network architecture for administrative systems",
            "Dedicated SIEM platform license (EGP 280,000/year)",
            "Coordination with all directorates for endpoint compliance",
            "Board-level briefing recommended for ransomware preparedness");
        H(r5, ReportStatus.Draft, ReportStatus.Submitted, karimTawfik, 49);
        H(r5, ReportStatus.Submitted, ReportStatus.Submitted, dirITInfra, 47);
        H(r5, ReportStatus.Submitted, ReportStatus.Approved, dirITInfra, 43);

        var r6 = R("Network Infrastructure Upgrade Progress",
            ReportType.Detailed, ReportStatus.FeedbackRequested,
            dirITInfra, itInfra, 30,
            "<h3>Infrastructure Upgrade</h3><p>Phase 2 of the campus-wide network upgrade is 65% complete. Core switches replaced in Buildings A, B, and D. WiFi 6E access points installed in 40% of planned locations.</p><p>Fiber backbone installation delayed by 3 weeks due to contractor scheduling conflicts.</p>",
            "Escalate contractor SLA enforcement",
            "Additional contractor crew for parallel installation",
            "Facilities Directorate coordination for conduit access");
        H(r6, ReportStatus.Draft, ReportStatus.Submitted, dirITInfra, 29);
        H(r6, ReportStatus.Submitted, ReportStatus.Submitted, gsTech, 28);
        H(r6, ReportStatus.Submitted, ReportStatus.FeedbackRequested, gsTech, 25,
            "Need more detail on budget impact of the 3-week delay. Please add cost variance analysis and revised timeline.");

        var r7 = R("Network Infrastructure Upgrade Progress (Revised)",
            ReportType.Detailed, ReportStatus.Approved,
            dirITInfra, itInfra, 22,
            "<h3>Infrastructure Upgrade — Revised</h3><p>Phase 2 is now 72% complete following contractor acceleration. Core switches replaced in Buildings A through E.</p><h4>Budget Impact of Delay</h4><p>3-week delay resulted in EGP 45,000 additional contractor overtime costs. Total project remains within 5% contingency (EGP 38,000 under contingency cap).</p><h4>Revised Timeline</h4><p>Phase 2 completion: Feb 15, 2026 (original: Jan 25). Phase 3 start shifted to March 1.</p>",
            "Approve revised timeline and contingency utilization",
            remarks: "Revised per GS Technology feedback — added cost variance analysis",
            version: 2);
        r7.OriginalReport = r6;
        H(r7, ReportStatus.Draft, ReportStatus.Submitted, dirITInfra, 21);
        H(r7, ReportStatus.Submitted, ReportStatus.Submitted, gsTech, 20);
        H(r7, ReportStatus.Submitted, ReportStatus.Approved, gsTech, 18, "Comprehensive revision. Approved.");

        // ── L3 Process Reports (Sector 4: Finance) ──

        var r8 = R("Q4 Financial Statements Preparation Status",
            ReportType.Detailed, ReportStatus.Submitted,
            dirFinance, genAcct, 12,
            "<h3>Financial Close Progress</h3><p>Q4 2025 financial close process initiated. 78% of journal entries posted. Pending: depreciation schedules (3 asset classes), inter-fund transfers, and year-end accruals.</p><p>External auditor pre-engagement letter received. Fieldwork scheduled for March 2026.</p>",
            "Expedite depreciation calculations for fixed assets",
            "Temporary accounting staff for year-end close (2 FTEs for 4 weeks)");
        H(r8, ReportStatus.Draft, ReportStatus.Submitted, dirFinance, 11);

        var r9 = R("FY2026 Budget Framework Proposal",
            ReportType.Detailed, ReportStatus.Submitted,
            dirFinance, budgeting, 15,
            "<h3>Budget Framework</h3><p>Proposed FY2026 operating budget: EGP 485M (7.2% increase from FY2025). Capital expenditure requests total EGP 62M across 23 projects.</p><h4>Key Assumptions</h4><ul><li>Enrollment growth: 4.5%</li><li>Tuition increase: 6% (aligned with CPI)</li><li>Staff cost inflation: 8%</li><li>Energy cost increase: 12%</li></ul><p>Deficit funding gap of EGP 18M identified — requires either revenue enhancement or scope reduction in capital projects.</p>",
            "Present budget options to Top Level Committee for strategic guidance",
            "Finance committee workshop sessions (3 half-days)");
        H(r9, ReportStatus.Draft, ReportStatus.Submitted, dirFinance, 14);
        H(r9, ReportStatus.Submitted, ReportStatus.Submitted, gsFinance, 13, "Under review by GS Finance & Governance.");

        var r10 = R("Internal Controls Assessment — Draft",
            ReportType.Detailed, ReportStatus.Draft,
            gamalReda, internalAudit, 5,
            "<h3>Internal Controls Assessment</h3><p>Preliminary findings from the Q4 internal controls review across procurement and payroll processes. Draft — pending field verification of 4 observations.</p>");

        // ── L3 Process Reports (Sector 5: Student Experience) ──

        var r11 = R("Fall 2025 Admissions Cycle Summary",
            ReportType.Detailed, ReportStatus.Approved,
            dirStudAffairs, admissions, 35,
            "<h3>Admissions Summary</h3><p>Fall 2025 cohort: 2,340 students enrolled (target: 2,200 — 106% achievement). International students: 187 (8% of cohort, up from 6.2%).</p><h4>Program Demand</h4><p>Highest demand: Computer Science (+22%), Business Analytics (+18%), Biomedical Engineering (+15%). Under-enrolled: Classical Languages (-12%), Philosophy (-8%).</p>",
            "Increase CS program capacity by 50 seats for Fall 2026",
            "Additional CS lab infrastructure and faculty hiring");
        H(r11, ReportStatus.Draft, ReportStatus.Submitted, dirStudAffairs, 34);
        H(r11, ReportStatus.Submitted, ReportStatus.Submitted, gsStudent, 33);
        H(r11, ReportStatus.Submitted, ReportStatus.Approved, gsStudent, 30);

        var r12 = R("Student Mental Health Services — Quarterly Report",
            ReportType.Detailed, ReportStatus.Approved,
            dirStudAffairs, studentWelfare, 33,
            "<h3>Counseling Services</h3><p>Q4 served 428 unique students across 1,847 sessions. Average wait time: 2.3 days (target: 3 days). Crisis interventions: 12 (all resolved safely).</p><p>New peer support program launched with 45 trained student volunteers. Group therapy sessions expanded to 3 weekly slots.</p>",
            "Hire additional licensed counselor for Spring 2026",
            "1 FTE clinical psychologist position (EGP 180,000/year)",
            "HR fast-track for clinical position recruitment");
        H(r12, ReportStatus.Draft, ReportStatus.Submitted, dirStudAffairs, 32);
        H(r12, ReportStatus.Submitted, ReportStatus.Approved, gsStudent, 28);

        var r13 = R("Career Fair 2025 Outcomes Report",
            ReportType.Detailed, ReportStatus.Submitted,
            dirCareer, careerCounsel, 18,
            "<h3>Annual Career Fair</h3><p>67 employers participated (up from 52 in 2024). 1,240 students attended. On-site interviews: 380. Preliminary job/internship offers: 94.</p><p>Top recruiting sectors: Technology (28%), Financial Services (22%), Healthcare (15%).</p>",
            "Establish year-round employer engagement program");
        H(r13, ReportStatus.Draft, ReportStatus.Submitted, dirCareer, 17);

        // ── L2 Function Summary Reports ──

        var r14 = R("Academic Programs — Q4 2025 Summary Report",
            ReportType.Summary, ReportStatus.Approved,
            dirAcadProg, acadPrograms, 28,
            "<h3>Directorate Summary</h3><p>Academic Programs Directorate achieved all Q4 targets. Key outcomes across 4 functions:</p><ul><li><b>Curriculum:</b> 12 course reviews completed, 3 flagged for ABET realignment</li><li><b>Faculty:</b> 14/18 positions filled, salary benchmark update recommended</li><li><b>Examinations:</b> 342 exams administered incident-free, digital pilot successful</li><li><b>E-Learning:</b> 18% usage growth, 99.7% uptime achieved</li></ul><p>Aggregate recommendation: prioritize CS faculty recruitment and exam scheduling system procurement.</p>",
            "Approve CS faculty recruitment budget increase and exam system procurement",
            "Combined budget request: EGP 170,000 (recruitment) + EGP 120,000 (exam system)");
        H(r14, ReportStatus.Draft, ReportStatus.Submitted, dirAcadProg, 27);
        H(r14, ReportStatus.Submitted, ReportStatus.Submitted, gsAcademic, 26);
        H(r14, ReportStatus.Submitted, ReportStatus.Approved, gsAcademic, 24, "Excellent summary. Forwarded budget requests to Finance.");

        var r15 = R("Research & Graduate Studies — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Approved,
            dirResearch, researchGrad, 27,
            "<h3>Directorate Summary</h3><p>Research output maintained strong trajectory. 23 new grant applications submitted (EGP 4.2M total). 8 grants awarded (EGP 2.1M). Patent portfolio: 3 new filings.</p><p>Graduate enrollment: 892 active students. 47 thesis defenses completed. Average time-to-completion: 2.4 years (Masters), 4.1 years (PhD).</p>",
            "Expand grant writing support services to increase success rate");
        H(r15, ReportStatus.Draft, ReportStatus.Submitted, dirResearch, 26);
        H(r15, ReportStatus.Submitted, ReportStatus.Approved, gsAcademic, 23);

        var r16 = R("IT Infrastructure & Networks — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Approved,
            dirITInfra, itInfra, 16,
            "<h3>Directorate Summary</h3><p>Network upgrade Phase 2 at 72% completion. Cybersecurity posture improved with zero breaches. ISO 27001 recertification on track.</p><p>Key metrics: 99.8% network uptime, 12,847 blocked intrusions, 48-hour mean time to patch.</p>",
            "Approve zero-trust architecture initiative for FY2026");
        H(r16, ReportStatus.Draft, ReportStatus.Submitted, dirITInfra, 15);
        H(r16, ReportStatus.Submitted, ReportStatus.Submitted, gsTech, 14);
        H(r16, ReportStatus.Submitted, ReportStatus.Approved, gsTech, 12);

        // ── L1 Directorate Summary Reports ──

        var r17 = R("Academic Affairs Sector — Q4 2025 Executive Summary",
            ReportType.Summary, ReportStatus.Approved,
            gsAcademic, topLevel, 18,
            "<h3>Sector Executive Summary</h3><p>The Academic Affairs sector delivered strong Q4 results across all 4 directorates. Student satisfaction index: 4.2/5.0 (up 0.3 from Q3).</p><h4>Strategic Highlights</h4><ul><li>Enrollment exceeded target by 6%</li><li>Research funding secured: EGP 2.1M in new grants</li><li>E-Learning adoption up 18%</li><li>Quality audit on track for ISO certification</li></ul><h4>Risks & Escalations</h4><p>CS faculty shortage is critical — competing with Gulf universities on compensation. Budget request of EGP 290,000 for combined recruitment and exam system needs.</p>",
            "Approve cross-directorate budget for CS recruitment and exam system",
            "EGP 290,000 aggregate from Academic Programs Directorate");
        H(r17, ReportStatus.Draft, ReportStatus.Submitted, gsAcademic, 17);
        H(r17, ReportStatus.Submitted, ReportStatus.Submitted, chairman, 16);
        H(r17, ReportStatus.Submitted, ReportStatus.Approved, chairman, 14, "Approved. CS recruitment is institutional priority.");

        var r18 = R("Technology & Innovation Sector — Q4 2025 Executive Summary",
            ReportType.Summary, ReportStatus.Approved,
            gsTech, topLevel, 14,
            "<h3>Sector Executive Summary</h3><p>Technology sector maintained high operational standards. Network upgrade progressing with minor delay (mitigated). Cybersecurity posture significantly strengthened.</p><h4>Key Achievements</h4><ul><li>99.8% infrastructure uptime</li><li>ISO 27001 audit readiness confirmed</li><li>Zero data breaches</li></ul><h4>Investment Needs</h4><p>Zero-trust architecture and SIEM platform represent EGP 280,000 annual investment with 3-year ROI in reduced incident response costs.</p>");
        H(r18, ReportStatus.Draft, ReportStatus.Submitted, gsTech, 13);
        H(r18, ReportStatus.Submitted, ReportStatus.Approved, chairman, 10);

        var r19 = R("Finance & Governance Sector — Q4 2025 Executive Summary",
            ReportType.Summary, ReportStatus.Submitted,
            gsFinance, topLevel, 10,
            "<h3>Sector Executive Summary</h3><p>FY2025 financial close in progress (78% complete). FY2026 budget framework proposes EGP 485M operating budget with EGP 18M funding gap requiring strategic decision.</p><p>Internal audit identified 2 minor process improvements in procurement. Legal compliance review: no material findings.</p>",
            "Schedule Top Level Committee budget workshop for FY2026 framework");
        H(r19, ReportStatus.Draft, ReportStatus.Submitted, gsFinance, 9);

        var r20 = R("Student Experience Sector — Q4 2025 Executive Summary",
            ReportType.Summary, ReportStatus.Submitted,
            gsStudent, topLevel, 8,
            "<h3>Sector Executive Summary</h3><p>Student Experience metrics exceeded targets. Enrollment at 106% of target. Mental health services demand growing — additional counselor needed. Career fair delivered 94 job offers.</p><h4>Critical Need</h4><p>Student-to-counselor ratio now 1:2,100 (recommended: 1:1,500). Requesting urgent clinical psychologist hire.</p>",
            "Approve clinical psychologist position for Student Welfare");
        H(r20, ReportStatus.Draft, ReportStatus.Submitted, gsStudent, 7);

        // ── L0 Executive Summary Reports ──

        var r21 = R("Q4 2025 Institutional Performance Report",
            ReportType.ExecutiveSummary, ReportStatus.Approved,
            chiefStaff, topLevel, 10,
            "<h3>Chairman's Office — Institutional Performance</h3><p>The institution demonstrated robust performance across all 5 sectors in Q4 2025. Key institutional metrics:</p><table><tr><th>Metric</th><th>Target</th><th>Actual</th></tr><tr><td>Student Enrollment</td><td>2,200</td><td>2,340</td></tr><tr><td>Research Grants Won</td><td>EGP 1.5M</td><td>EGP 2.1M</td></tr><tr><td>Infrastructure Uptime</td><td>99.5%</td><td>99.8%</td></tr><tr><td>Budget Utilization</td><td>95%</td><td>92%</td></tr></table><p>Three items require Chairman's decision: CS faculty budget, zero-trust security investment, and clinical psychologist hire.</p>",
            "Approve all three escalated budget requests totaling EGP 750,000");
        H(r21, ReportStatus.Draft, ReportStatus.Submitted, chiefStaff, 9);
        H(r21, ReportStatus.Submitted, ReportStatus.Submitted, chairman, 8);
        H(r21, ReportStatus.Submitted, ReportStatus.Approved, chairman, 6, "Approved. Schedule implementation reviews for each item.");

        var r22 = R("Administration & Operations Sector — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Submitted,
            gsAdmin, topLevel, 7,
            "<h3>Sector Executive Summary</h3><p>Facilities maintenance completed 94% of scheduled preventive tasks. HR processed 67 new hires. Procurement cycle time reduced by 12%. Food safety compliance at 100%.</p><p>Energy costs rose 15% — exceeding budget by EGP 1.2M. Solar panel project proposal attached for FY2026 consideration.</p>",
            "Evaluate solar panel ROI for campus energy independence");
        H(r22, ReportStatus.Draft, ReportStatus.Submitted, gsAdmin, 6);
        H(r22, ReportStatus.Submitted, ReportStatus.Submitted, chairman, 5);

        // ── Confidential Report ──

        var r23 = R("Confidential: Staff Disciplinary Investigation — Finance Dept",
            ReportType.Detailed, ReportStatus.Approved,
            gamalReda, internalAudit, 25,
            "<h3>Investigation Report</h3><p>Investigation into irregular procurement approvals in the Finance department (Case #IA-2025-017). Findings indicate procedural non-compliance by 2 staff members, not fraud.</p><p>Recommended: mandatory retraining and process controls enhancement.</p>",
            "Implement enhanced approval workflows for procurement above EGP 50,000",
            confidential: true);
        H(r23, ReportStatus.Draft, ReportStatus.Submitted, gamalReda, 24);
        H(r23, ReportStatus.Submitted, ReportStatus.Approved, gsFinance, 20);

        // ── QA Report ──

        var r24 = R("Quality Assurance Audit Results — Engineering Faculty",
            ReportType.Detailed, ReportStatus.Approved,
            manalRizk, qaStandards, 30,
            "<h3>QA Audit Report</h3><p>Completed comprehensive quality audit of Engineering Faculty programs. 18 programs reviewed against national and international standards.</p><h4>Results</h4><ul><li>Fully compliant: 14 programs</li><li>Minor findings: 3 programs (documentation gaps)</li><li>Major finding: 1 program (outdated lab equipment in Materials Science)</li></ul>",
            "Prioritize Materials Science lab equipment upgrade in FY2026 capital budget",
            "Equipment replacement budget: EGP 850,000");
        H(r24, ReportStatus.Draft, ReportStatus.Submitted, manalRizk, 29);
        H(r24, ReportStatus.Submitted, ReportStatus.Submitted, dirQuality, 28);
        H(r24, ReportStatus.Submitted, ReportStatus.Approved, dirQuality, 26);

        // ── Research Grants ──

        var r25 = R("Research Grant Portfolio — Q4 Update",
            ReportType.Detailed, ReportStatus.Approved,
            nabilaFikry, resGrants, 32,
            "<h3>Grant Portfolio</h3><p>Active grants: 34 (total value: EGP 12.4M). New submissions: 23. Awards received: 8 (EGP 2.1M). Rejection rate: 48% (sector average: 55%).</p><p>Top funding sources: STDF (45%), EU Horizon (22%), Industry partnerships (18%).</p>",
            "Increase grant writing workshops from quarterly to monthly");
        H(r25, ReportStatus.Draft, ReportStatus.Submitted, nabilaFikry, 31);
        H(r25, ReportStatus.Submitted, ReportStatus.Approved, dirResearch, 28);

        // ── Graduate Programs ──

        var r26 = R("Graduate Programs Enrollment & Completion — Q4 2025",
            ReportType.Detailed, ReportStatus.Approved,
            walidDarwish, gradPrograms, 30,
            "<h3>Graduate Education</h3><p>Active enrollment: 892 students (Masters: 645, PhD: 247). New admissions: 134. Defenses completed: 47 (Masters: 38, PhD: 9).</p><p>Time-to-completion improving: Masters 2.4 years (target: 2.5), PhD 4.1 years (target: 4.5).</p>",
            "Expand thesis writing support program to reduce completion time further");
        H(r26, ReportStatus.Draft, ReportStatus.Submitted, walidDarwish, 29);
        H(r26, ReportStatus.Submitted, ReportStatus.Approved, dirResearch, 26);

        // ── Archived Report ──

        var r27 = R("Q3 2025 Institutional Performance Report (Archived)",
            ReportType.ExecutiveSummary, ReportStatus.Summarized,
            chiefStaff, topLevel, 95,
            "<h3>Q3 2025 Institutional Summary</h3><p>Archived. This report covers the Q3 2025 institutional performance metrics. Superseded by Q4 2025 report.</p>");
        H(r27, ReportStatus.Draft, ReportStatus.Submitted, chiefStaff, 94);
        H(r27, ReportStatus.Submitted, ReportStatus.Approved, chairman, 90);
        H(r27, ReportStatus.Approved, ReportStatus.Summarized, admin, 60, "Archived per retention policy.");

        // ── Additional Draft Reports ──

        var r28 = R("Student Club Activity Report — Draft",
            ReportType.Detailed, ReportStatus.Draft,
            halaShafik, clubs, 3,
            "<h3>Club Activities</h3><p>Draft report on Q4 2025 student club activities. 12 new clubs registered, 8 major events hosted. Still gathering attendance data from 3 departments.</p>");

        var r29 = R("Facilities Preventive Maintenance — Q4 Summary",
            ReportType.Detailed, ReportStatus.Approved,
            dirFacilities, facilities, 20,
            "<h3>Maintenance Summary</h3><p>Completed 94% of 1,240 scheduled preventive maintenance tasks. Building condition index improved from 3.2 to 3.5 (scale: 1-5). Emergency repairs: 87 (down 15% from Q3).</p><p>HVAC system replacements in Buildings B and E completed on schedule.</p>",
            "Budget for elevator modernization in Building A (FY2026)");
        H(r29, ReportStatus.Draft, ReportStatus.Submitted, dirFacilities, 19);
        H(r29, ReportStatus.Submitted, ReportStatus.Approved, gsAdmin, 16);

        var r30 = R("Strategic KPI Dashboard — Year-End 2025",
            ReportType.Detailed, ReportStatus.Submitted,
            mostafaSalem, stratPlan, 6,
            "<h3>KPI Dashboard</h3><p>Year-end 2025 strategic KPI review. 82% of institutional KPIs met or exceeded targets. 12% partially met. 6% not met (primarily energy cost and CS faculty targets).</p><p>Detailed breakdown by strategic pillar attached.</p>",
            "Revise underperforming KPI targets for 2026 strategic plan");
        H(r30, ReportStatus.Draft, ReportStatus.Submitted, mostafaSalem, 5);

        // ── Save all reports ──
        context.Reports.AddRange(reports);
        await context.SaveChangesAsync();

        // ── Source Links (Summary → Source relationships) ──
        // r14 (Academic Programs Summary) links to r1, r2, r3, r4
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r14.Id, SourceReportId = r1.Id, Annotation = "Course design review findings", CreatedAt = r14.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r14.Id, SourceReportId = r2.Id, Annotation = "Faculty recruitment progress", CreatedAt = r14.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r14.Id, SourceReportId = r3.Id, Annotation = "Examination logistics outcomes", CreatedAt = r14.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r14.Id, SourceReportId = r4.Id, Annotation = "E-Learning utilization metrics", CreatedAt = r14.CreatedAt });

        // r15 (Research Summary) links to r25, r26
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r15.Id, SourceReportId = r25.Id, Annotation = "Grant portfolio update", CreatedAt = r15.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r15.Id, SourceReportId = r26.Id, Annotation = "Graduate enrollment and completion data", CreatedAt = r15.CreatedAt });

        // r16 (IT Summary) links to r5, r7
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r16.Id, SourceReportId = r5.Id, Annotation = "Cybersecurity threat assessment", CreatedAt = r16.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r16.Id, SourceReportId = r7.Id, Annotation = "Network upgrade revised status", CreatedAt = r16.CreatedAt });

        // r17 (Academic Affairs Sector) links to r14, r15, r24
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r17.Id, SourceReportId = r14.Id, Annotation = "Academic Programs directorate summary", CreatedAt = r17.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r17.Id, SourceReportId = r15.Id, Annotation = "Research & Graduate Studies summary", CreatedAt = r17.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r17.Id, SourceReportId = r24.Id, Annotation = "QA audit results", CreatedAt = r17.CreatedAt });

        // r21 (Institutional Performance) links to r17, r18
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r21.Id, SourceReportId = r17.Id, Annotation = "Academic Affairs sector report", CreatedAt = r21.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r21.Id, SourceReportId = r18.Id, Annotation = "Technology & Innovation sector report", CreatedAt = r21.CreatedAt });

        context.ReportSourceLinks.AddRange(sourceLinks);
        context.ReportStatusHistories.AddRange(histories);
        await context.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────────────
    //  PHASE B: Directives + StatusHistory + Chains
    // ────────────────────────────────────────────────────────────────
    private static async Task SeedDirectivesAsync(
        ApplicationDbContext context, DateTime now, Func<int, DateTime> D,
        User chairman, User chiefStaff, User co2,
        User gsAcademic, User gsAdmin, User gsTech, User gsFinance, User gsStudent,
        User dirAcadProg, User dirResearch, User dirSoftware, User dirITInfra,
        User dirFinance, User dirStudAffairs, User dirFacilities,
        User karimTawfik, User gamalReda,
        Committee topLevel, Committee acadPrograms, Committee researchGrad,
        Committee softwareDev, Committee itInfra, Committee financeAcct,
        Committee studentAffairs, Committee currDev, Committee cybersecurity,
        Committee facilities, Committee humanRes, Committee internalAudit)
    {
        var directives = new List<Directive>();
        var dHistories = new List<DirectiveStatusHistory>();

        Directive Dir(string title, DirectiveType type, DirectivePriority priority,
                      DirectiveStatus status, User issuer, Committee target,
                      int daysAgoCreated, string body, DateTime? deadline = null,
                      Directive? parent = null, string? annotation = null,
                      bool confidential = false, User? targetUser = null)
        {
            var d = new Directive
            {
                Title = title,
                DirectiveType = type,
                Priority = priority,
                Status = status,
                IssuerId = issuer.Id,
                TargetCommitteeId = target.Id,
                TargetUserId = targetUser?.Id,
                BodyContent = body,
                ForwardingAnnotation = annotation,
                Deadline = deadline,
                IsConfidential = confidential,
                ParentDirective = parent,
                CreatedAt = D(daysAgoCreated),
                UpdatedAt = status > DirectiveStatus.Issued ? D(daysAgoCreated - 1) : null,
                AcknowledgedAt = status >= DirectiveStatus.Acknowledged ? D(daysAgoCreated - 2) : null,
                ImplementedAt = status >= DirectiveStatus.Implemented ? D(daysAgoCreated - 5) : null
            };
            directives.Add(d);
            return d;
        }

        void DH(Directive d, DirectiveStatus from, DirectiveStatus to, User by, int daysAgo, string? comment = null)
        {
            dHistories.Add(new DirectiveStatusHistory
            {
                Directive = d,
                OldStatus = from,
                NewStatus = to,
                ChangedBy = by,
                ChangedAt = D(daysAgo),
                Comments = comment
            });
        }

        // ── D1: Chairman directive — Completed chain (D1 → D2 → D3 → D4) ──
        // Chairman instructs L0 to prioritize CS faculty recruitment

        var d1 = Dir("Prioritize Computer Science Faculty Recruitment",
            DirectiveType.Instruction, DirectivePriority.High, DirectiveStatus.Closed,
            chairman, topLevel, 40,
            "Following the Academic Affairs Q4 report identifying critical CS faculty shortage, all relevant directorates must prioritize recruitment of qualified CS faculty. Target: fill remaining 2 positions by March 2026.",
            D(-60)); // deadline 60 days from now
        DH(d1, DirectiveStatus.Issued, DirectiveStatus.Delivered, chiefStaff, 39, "Distributed via Chairman's Office");
        DH(d1, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, gsAcademic, 38);
        DH(d1, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, gsAcademic, 36);
        DH(d1, DirectiveStatus.InProgress, DirectiveStatus.Implemented, gsAcademic, 15);
        DH(d1, DirectiveStatus.Implemented, DirectiveStatus.Verified, chairman, 12);
        DH(d1, DirectiveStatus.Verified, DirectiveStatus.Closed, chairman, 10, "Both positions filled. Directive closed.");

        // D2: GS Academic forwards to Academic Programs Directorate
        var d2 = Dir("CS Faculty Recruitment — Academic Programs Action",
            DirectiveType.Instruction, DirectivePriority.High, DirectiveStatus.Closed,
            gsAcademic, acadPrograms, 37,
            "Forward Chairman's directive on CS faculty recruitment. Academic Programs Directorate to lead search with expanded international advertising.",
            D(-60), d1, "GS Academic Affairs: This is top institutional priority. Report weekly progress.");
        DH(d2, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsAcademic, 37);
        DH(d2, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, dirAcadProg, 36);
        DH(d2, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, dirAcadProg, 35);
        DH(d2, DirectiveStatus.InProgress, DirectiveStatus.Implemented, dirAcadProg, 16);
        DH(d2, DirectiveStatus.Implemented, DirectiveStatus.Verified, gsAcademic, 14);
        DH(d2, DirectiveStatus.Verified, DirectiveStatus.Closed, gsAcademic, 12);

        // D3: Director Academic Programs forwards to Curriculum Dev for job spec preparation
        var d3 = Dir("Prepare CS Faculty Job Specifications",
            DirectiveType.Instruction, DirectivePriority.High, DirectiveStatus.Implemented,
            dirAcadProg, currDev, 35,
            "Prepare detailed job specifications for 2 CS faculty positions (AI/ML specialization, Data Analytics). Include required qualifications, research expectations, and teaching load.",
            D(-50), d2, "Director Academic Programs: Coordinate with industry advisory board for relevance.");
        DH(d3, DirectiveStatus.Issued, DirectiveStatus.Acknowledged, dirAcadProg, 34);
        DH(d3, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, dirAcadProg, 33);
        DH(d3, DirectiveStatus.InProgress, DirectiveStatus.Implemented, dirAcadProg, 20);

        // ── D4: Corrective Action — triggered by internal audit finding ──
        var d4 = Dir("Corrective Action: Procurement Approval Workflow Enhancement",
            DirectiveType.CorrectiveAction, DirectivePriority.High, DirectiveStatus.InProgress,
            gsFinance, financeAcct, 20,
            "Following Internal Audit finding (Case #IA-2025-017), implement enhanced dual-approval workflow for all procurement transactions above EGP 50,000. Deploy updated approval matrix by end of February 2026.",
            D(-30));
        DH(d4, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsFinance, 19);
        DH(d4, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, dirFinance, 18);
        DH(d4, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, dirFinance, 16, "Implementation started — configuring ERP approval workflows.");

        // ── D5: Urgent — Chairman's urgent cybersecurity directive ──
        var d5 = Dir("Urgent: Ransomware Preparedness Assessment",
            DirectiveType.Instruction, DirectivePriority.Urgent, DirectiveStatus.Acknowledged,
            chairman, topLevel, 8,
            "In light of recent sector-wide ransomware incidents, conduct immediate assessment of institutional ransomware preparedness. Report findings within 14 days.",
            D(-14));
        DH(d5, DirectiveStatus.Issued, DirectiveStatus.Delivered, chiefStaff, 7, "Urgent — distributed immediately via Chairman's Office");
        DH(d5, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, gsTech, 6, "Acknowledged. Assessment team mobilized.");

        // D6: Child of D5 — forwarded to IT Infrastructure
        var d6 = Dir("Ransomware Preparedness — IT Infrastructure Assessment",
            DirectiveType.Instruction, DirectivePriority.Urgent, DirectiveStatus.InProgress,
            gsTech, itInfra, 6,
            "Conduct technical assessment of ransomware defenses: backup integrity, endpoint protection coverage, and incident response readiness. Report within 10 days.",
            D(-10), d5, "GS Technology: Coordinate with Cybersecurity team for penetration testing.");
        DH(d6, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsTech, 6);
        DH(d6, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, dirITInfra, 5);
        DH(d6, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, dirITInfra, 4);

        // ── D7: Chairman's Office feedback on sector report ──
        var d7 = Dir("Feedback: Administration Sector Q4 Report — Energy Cost Analysis",
            DirectiveType.Feedback, DirectivePriority.Normal, DirectiveStatus.Delivered,
            chiefStaff, topLevel, 5,
            "The Chairman requires additional analysis on the 15% energy cost overrun. Please provide: (1) root cause breakdown by building, (2) comparison with peer institutions, (3) projected FY2026 energy budget under current trajectory vs. solar panel scenario.",
            D(-15), targetUser: gsAdmin);
        DH(d7, DirectiveStatus.Issued, DirectiveStatus.Delivered, chiefStaff, 4);

        // ── D8-D9: Approval directives ──
        var d8 = Dir("Approval: Exam Scheduling System Procurement",
            DirectiveType.Approval, DirectivePriority.Normal, DirectiveStatus.Closed,
            gsAcademic, acadPrograms, 22,
            "Budget allocation of EGP 120,000 approved for exam scheduling system procurement. Academic Programs Directorate to lead vendor evaluation and selection.",
            D(-30));
        DH(d8, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsAcademic, 21);
        DH(d8, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, dirAcadProg, 20);
        DH(d8, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, dirAcadProg, 18);
        DH(d8, DirectiveStatus.InProgress, DirectiveStatus.Implemented, dirAcadProg, 8, "Vendor selected: ExamPro v4.2. Contract signed.");
        DH(d8, DirectiveStatus.Implemented, DirectiveStatus.Verified, gsAcademic, 6);
        DH(d8, DirectiveStatus.Verified, DirectiveStatus.Closed, gsAcademic, 5, "System deployment confirmed.");

        var d9 = Dir("Approval: Clinical Psychologist Position",
            DirectiveType.Approval, DirectivePriority.High, DirectiveStatus.Issued,
            chairman, studentAffairs, 3,
            "Position approved for 1 FTE clinical psychologist in Student Welfare & Counseling. HR to initiate recruitment immediately. Annual budget: EGP 180,000.",
            D(-45));

        // ── D10: Information Notice ──
        var d10 = Dir("Notice: FY2026 Budget Submission Deadline",
            DirectiveType.InformationNotice, DirectivePriority.Normal, DirectiveStatus.Closed,
            gsFinance, topLevel, 30,
            "All directorates are reminded that FY2026 budget proposals are due by January 31, 2026. Submissions must use the updated budget template (v3.1) available on the intranet. Late submissions will not be included in the first consolidation round.");
        DH(d10, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsFinance, 29);
        DH(d10, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, gsAcademic, 28);
        DH(d10, DirectiveStatus.Acknowledged, DirectiveStatus.Closed, gsFinance, 5, "Budget cycle complete. All submissions received.");

        // ── D11: Overdue directive for demo ──
        var d11 = Dir("Network Upgrade Phase 3 Planning",
            DirectiveType.Instruction, DirectivePriority.Normal, DirectiveStatus.InProgress,
            gsTech, softwareDev, 25,
            "Prepare Phase 3 network upgrade specifications for the Software Development building cluster. Include bandwidth requirements for CI/CD infrastructure and developer workstations.",
            D(5)); // deadline was 5 days ago — OVERDUE
        DH(d11, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsTech, 24);
        DH(d11, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, dirSoftware, 22);
        DH(d11, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, dirSoftware, 18);

        // ── D12-D13: Recent directives in early states ──
        var d12 = Dir("Implement Solar Panel Feasibility Study",
            DirectiveType.Instruction, DirectivePriority.Normal, DirectiveStatus.Delivered,
            gsAdmin, facilities, 4,
            "Commission a feasibility study for solar panel installation on Buildings A, C, and F rooftops. Engage at least 3 vendors for competitive proposals. Report findings by March 15, 2026.",
            D(-35));
        DH(d12, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsAdmin, 3);

        var d13 = Dir("Research Collaboration Framework with Industry Partners",
            DirectiveType.Instruction, DirectivePriority.Normal, DirectiveStatus.Issued,
            gsAcademic, researchGrad, 2,
            "Develop a standardized framework for research collaboration agreements with industry partners. Include IP sharing terms, student involvement guidelines, and funding allocation models.");

        // ── D14: Confidential directive ──
        var d14 = Dir("Confidential: Enhanced Financial Controls Implementation",
            DirectiveType.CorrectiveAction, DirectivePriority.High, DirectiveStatus.InProgress,
            chairman, internalAudit, 18,
            "Implement enhanced monitoring and controls for all financial transactions above EGP 100,000 pending completion of the procurement workflow enhancement. Monthly compliance reports required.",
            D(-30), confidential: true, targetUser: gamalReda);
        DH(d14, DirectiveStatus.Issued, DirectiveStatus.Delivered, chiefStaff, 17);
        DH(d14, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, gamalReda, 16);
        DH(d14, DirectiveStatus.Acknowledged, DirectiveStatus.InProgress, gamalReda, 14);

        // ── D15: HR directive ──
        var d15 = Dir("Staff Performance Review Cycle — Q1 2026 Preparation",
            DirectiveType.Instruction, DirectivePriority.Normal, DirectiveStatus.Acknowledged,
            gsAdmin, humanRes, 10,
            "Initiate preparation for the Q1 2026 annual staff performance review cycle. Distribute updated evaluation forms, schedule manager training sessions, and set completion deadline of April 30, 2026.",
            D(-75));
        DH(d15, DirectiveStatus.Issued, DirectiveStatus.Delivered, gsAdmin, 9);
        DH(d15, DirectiveStatus.Delivered, DirectiveStatus.Acknowledged, gsAdmin, 7);

        // ── Save all directives ──
        context.Directives.AddRange(directives);
        await context.SaveChangesAsync();

        context.DirectiveStatusHistories.AddRange(dHistories);
        await context.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────────────
    //  PHASE C: Meetings + Agenda + Attendees + Decisions + ActionItems
    // ────────────────────────────────────────────────────────────────
    private static async Task SeedMeetingsAsync(
        ApplicationDbContext context, DateTime now, Func<int, DateTime> D,
        User chairman, User chiefStaff, User co2,
        User gsAcademic, User gsAdmin, User gsTech, User gsFinance, User gsStudent,
        User dirAcadProg, User dirResearch, User dirQuality, User dirSoftware, User dirITInfra,
        User dirFinance, User dirStudAffairs, User dirFacilities,
        User lamiaRefaat, User yasserNasr, User soniaGuirguis, User magedBotros,
        User manalRizk, User karimTawfik, User gamalReda, User mostafaSalem,
        Committee topLevel, Committee acadPrograms, Committee researchGrad, Committee acadQuality,
        Committee softwareDev, Committee itInfra, Committee financeAcct,
        Committee studentAffairs, Committee cybersecurity, Committee facilities)
    {
        // ── M1: Top Level Committee — Q4 Quarterly Review (Finalized) ──
        var m1 = new Meeting
        {
            Title = "Q4 2025 Top Level Committee Quarterly Review",
            MeetingType = MeetingType.Regular,
            Status = MeetingStatus.Finalized,
            CommitteeId = topLevel.Id,
            ModeratorId = chairman.Id,
            Description = "Quarterly review of institutional performance across all 5 sectors. Review of Q4 sector reports, budget framework discussion, and strategic directives.",
            Location = "Main Board Room — Building A, Floor 5",
            ScheduledAt = D(12),
            DurationMinutes = 120,
            RecurrencePattern = RecurrencePattern.None,
            MinutesContent = "<h3>Meeting Minutes — Q4 2025 Quarterly Review</h3><p>The Chairman opened the meeting at 10:00 AM. All General Secretaries presented their Q4 sector summaries.</p><h4>Key Discussions</h4><ul><li>CS faculty recruitment: budget approved, positions filled</li><li>Network upgrade delay: revised timeline accepted</li><li>FY2026 budget gap: deferred to special budget workshop</li><li>Student mental health staffing: clinical psychologist approved</li></ul><h4>Chairman's Remarks</h4><p>The Chairman commended the overall institutional performance and emphasized the need for proactive cybersecurity investment.</p>",
            MinutesSubmittedAt = D(10),
            MinutesFinalizedAt = D(7),
            CreatedAt = D(20),
        };

        // M1 attendees
        var m1Attendees = new List<MeetingAttendee>
        {
            new() { Meeting = m1, UserId = chairman.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(18), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(7) },
            new() { Meeting = m1, UserId = chiefStaff.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(18), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(8) },
            new() { Meeting = m1, UserId = gsAcademic.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(17), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(7) },
            new() { Meeting = m1, UserId = gsAdmin.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(17), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(8) },
            new() { Meeting = m1, UserId = gsTech.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(16), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(7) },
            new() { Meeting = m1, UserId = gsFinance.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(16), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(8) },
            new() { Meeting = m1, UserId = gsStudent.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(17), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(7) },
        };

        // M1 agenda items
        var m1a1 = new MeetingAgendaItem { Meeting = m1, OrderIndex = 1, TopicTitle = "Academic Affairs Sector Q4 Review", Description = "Prof. Amira Shalaby presents Academic Affairs sector performance", AllocatedMinutes = 20, PresenterId = gsAcademic.Id, DiscussionNotes = "Strong results across all directorates. CS faculty shortage highlighted as critical. Budget request of EGP 290,000 approved." };
        var m1a2 = new MeetingAgendaItem { Meeting = m1, OrderIndex = 2, TopicTitle = "Technology & Innovation Sector Q4 Review", Description = "Dr. Mariam Fawzy presents Technology sector updates", AllocatedMinutes = 20, PresenterId = gsTech.Id, DiscussionNotes = "Network upgrade progress noted. Zero-trust architecture proposal discussed — approved in principle for FY2026." };
        var m1a3 = new MeetingAgendaItem { Meeting = m1, OrderIndex = 3, TopicTitle = "FY2026 Budget Framework Discussion", Description = "Mr. Adel Soliman presents budget proposals and funding gap analysis", AllocatedMinutes = 30, PresenterId = gsFinance.Id, DiscussionNotes = "EGP 18M funding gap identified. Deferred to special budget workshop. Revenue enhancement options to be explored." };
        var m1a4 = new MeetingAgendaItem { Meeting = m1, OrderIndex = 4, TopicTitle = "Student Experience — Counseling Capacity", Description = "Dr. Heba Nasser presents mental health staffing needs", AllocatedMinutes = 15, PresenterId = gsStudent.Id, DiscussionNotes = "Clinical psychologist position approved effective immediately. HR to fast-track recruitment." };

        // M1 decisions
        var m1d1 = new MeetingDecision { Meeting = m1, AgendaItem = m1a1, DecisionType = DecisionType.Approval, DecisionText = "Approved EGP 290,000 combined budget for CS faculty recruitment (EGP 170,000) and exam scheduling system (EGP 120,000).", Deadline = D(-30), CreatedAt = D(12) };
        var m1d2 = new MeetingDecision { Meeting = m1, AgendaItem = m1a2, DecisionType = DecisionType.Direction, DecisionText = "Directed Technology sector to submit detailed zero-trust architecture proposal with ROI analysis by February 28, 2026.", Deadline = D(-20), CreatedAt = D(12) };
        var m1d3 = new MeetingDecision { Meeting = m1, AgendaItem = m1a3, DecisionType = DecisionType.Deferral, DecisionText = "Deferred FY2026 budget finalization to special budget workshop scheduled for February 2026. All sectors to submit revised proposals.", CreatedAt = D(12) };
        var m1d4 = new MeetingDecision { Meeting = m1, AgendaItem = m1a4, DecisionType = DecisionType.Approval, DecisionText = "Approved immediate recruitment of 1 FTE clinical psychologist for Student Welfare (annual budget: EGP 180,000).", CreatedAt = D(12) };

        // M1 action items
        var m1ai1 = new ActionItem { Meeting = m1, MeetingDecision = m1d1, Title = "Initiate CS faculty international recruitment campaign", Description = "Academic Programs to launch expanded international advertising for 2 CS positions", AssignedToId = dirAcadProg.Id, AssignedById = chairman.Id, Status = ActionItemStatus.Completed, Deadline = D(-15), CompletedAt = D(5), CreatedAt = D(12) };
        var m1ai2 = new ActionItem { Meeting = m1, MeetingDecision = m1d2, Title = "Submit zero-trust architecture proposal", Description = "IT Infrastructure to prepare detailed proposal with cost-benefit analysis", AssignedToId = dirITInfra.Id, AssignedById = chairman.Id, Status = ActionItemStatus.InProgress, Deadline = D(-20), CreatedAt = D(12) };
        var m1ai3 = new ActionItem { Meeting = m1, MeetingDecision = m1d3, Title = "Submit revised FY2026 budget proposals", Description = "All sector GS to submit revised budget proposals for workshop", AssignedToId = gsFinance.Id, AssignedById = chairman.Id, Status = ActionItemStatus.Assigned, Deadline = D(-25), CreatedAt = D(12) };
        var m1ai4 = new ActionItem { Meeting = m1, MeetingDecision = m1d4, Title = "Fast-track clinical psychologist recruitment", Description = "HR to initiate recruitment with priority processing", AssignedToId = dirStudAffairs.Id, AssignedById = chairman.Id, Status = ActionItemStatus.InProgress, Deadline = D(-30), CreatedAt = D(12) };

        // ── M2: Academic Programs Directorate Review (Finalized) ──
        var m2 = new Meeting
        {
            Title = "Academic Programs Directorate — Q4 Review Meeting",
            MeetingType = MeetingType.Regular,
            Status = MeetingStatus.Finalized,
            CommitteeId = acadPrograms.Id,
            ModeratorId = dirAcadProg.Id,
            Description = "Q4 review of all Academic Programs functions: Curriculum, Teaching, Examinations, and E-Learning.",
            Location = "Conference Room B2-03",
            ScheduledAt = D(30),
            DurationMinutes = 90,
            MinutesContent = "<h3>Academic Programs Q4 Review</h3><p>All function heads presented Q4 progress. Key outcomes:</p><ul><li>Course design review complete — 3 ABET realignment actions needed</li><li>Faculty recruitment at 78% of target</li><li>Digital exam pilot successful — recommend expansion</li><li>LMS utilization growth exceeding targets</li></ul>",
            MinutesSubmittedAt = D(28),
            MinutesFinalizedAt = D(26),
            CreatedAt = D(35),
        };

        var m2Attendees = new List<MeetingAttendee>
        {
            new() { Meeting = m2, UserId = dirAcadProg.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(33), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(26) },
            new() { Meeting = m2, UserId = lamiaRefaat.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(33), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(26) },
            new() { Meeting = m2, UserId = yasserNasr.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(32), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(27) },
            new() { Meeting = m2, UserId = soniaGuirguis.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(33), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(26) },
            new() { Meeting = m2, UserId = magedBotros.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(32), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(27) },
        };

        var m2a1 = new MeetingAgendaItem { Meeting = m2, OrderIndex = 1, TopicTitle = "Curriculum Review Progress", AllocatedMinutes = 20, PresenterId = lamiaRefaat.Id, DiscussionNotes = "12 course syllabi reviewed. 3 flagged for ABET criteria gaps. Action plan created." };
        var m2a2 = new MeetingAgendaItem { Meeting = m2, OrderIndex = 2, TopicTitle = "Faculty Recruitment Status", AllocatedMinutes = 20, PresenterId = yasserNasr.Id, DiscussionNotes = "14/18 positions filled. CS positions remain open — salary competitiveness is key issue." };
        var m2a3 = new MeetingAgendaItem { Meeting = m2, OrderIndex = 3, TopicTitle = "Digital Examination Pilot Results", AllocatedMinutes = 15, PresenterId = soniaGuirguis.Id, DiscussionNotes = "45 digital exams conducted successfully. Student feedback positive. Recommend expanding to 30% of exams in Spring 2026." };

        var m2d1 = new MeetingDecision { Meeting = m2, AgendaItem = m2a1, DecisionType = DecisionType.Direction, DecisionText = "Curriculum heads to complete ABET realignment for flagged courses by end of January 2026.", Deadline = D(-40), CreatedAt = D(30) };
        var m2d2 = new MeetingDecision { Meeting = m2, AgendaItem = m2a3, DecisionType = DecisionType.Approval, DecisionText = "Approved expansion of digital examination to 30% of Spring 2026 exams.", CreatedAt = D(30) };

        var m2ai1 = new ActionItem { Meeting = m2, MeetingDecision = m2d1, Title = "Complete ABET realignment for ME301, EE405, CE210", Description = "Update course syllabi, learning outcomes, and assessment rubrics", AssignedToId = lamiaRefaat.Id, AssignedById = dirAcadProg.Id, Status = ActionItemStatus.Completed, Deadline = D(-40), CompletedAt = D(10), VerifiedAt = D(8), CreatedAt = D(30) };
        var m2ai2 = new ActionItem { Meeting = m2, MeetingDecision = m2d2, Title = "Prepare digital exam expansion plan for Spring 2026", AssignedToId = soniaGuirguis.Id, AssignedById = dirAcadProg.Id, Status = ActionItemStatus.Completed, Deadline = D(-20), CompletedAt = D(15), CreatedAt = D(30) };

        // ── M3: Emergency Security Meeting (MinutesReview) ──
        var m3 = new Meeting
        {
            Title = "Emergency: Campus Network Security Incident Response",
            MeetingType = MeetingType.Emergency,
            Status = MeetingStatus.MinutesReview,
            CommitteeId = itInfra.Id,
            ModeratorId = dirITInfra.Id,
            Description = "Emergency meeting convened to address suspicious network activity detected on January 28. Potential unauthorized access to administrative subnet.",
            Location = "IT Security Operations Center — Building D",
            ScheduledAt = D(6),
            DurationMinutes = 60,
            MinutesContent = "<h3>Emergency Security Incident Meeting</h3><p>Meeting convened at 2:00 PM following detection of anomalous traffic on the administrative VLAN.</p><h4>Findings</h4><p>SOC investigation confirmed: false positive triggered by misconfigured load balancer after network upgrade. No data breach. No unauthorized access confirmed.</p><h4>Actions Taken</h4><ul><li>Load balancer reconfigured</li><li>Enhanced monitoring rules deployed</li><li>Incident report drafted for compliance records</li></ul>",
            MinutesSubmittedAt = D(4),
            CreatedAt = D(6),
        };

        var m3Attendees = new List<MeetingAttendee>
        {
            new() { Meeting = m3, UserId = dirITInfra.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(6), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(3) },
            new() { Meeting = m3, UserId = karimTawfik.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(6), ConfirmationStatus = ConfirmationStatus.Pending },
            new() { Meeting = m3, UserId = gsTech.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(6), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(3) },
        };

        var m3a1 = new MeetingAgendaItem { Meeting = m3, OrderIndex = 1, TopicTitle = "Incident Timeline and Evidence Review", AllocatedMinutes = 20, PresenterId = karimTawfik.Id, DiscussionNotes = "SOC detected anomalous traffic at 11:47 AM. Isolated subnet at 12:15 PM. Root cause identified at 1:30 PM." };
        var m3a2 = new MeetingAgendaItem { Meeting = m3, OrderIndex = 2, TopicTitle = "Remediation Actions", AllocatedMinutes = 15, PresenterId = dirITInfra.Id, DiscussionNotes = "Load balancer configuration corrected. Additional monitoring rules deployed within 2 hours." };

        var m3d1 = new MeetingDecision { Meeting = m3, AgendaItem = m3a1, DecisionType = DecisionType.Resolution, DecisionText = "Incident classified as false positive. No data breach occurred. Load balancer misconfiguration identified as root cause.", CreatedAt = D(6) };
        var m3d2 = new MeetingDecision { Meeting = m3, AgendaItem = m3a2, DecisionType = DecisionType.Direction, DecisionText = "Network team to implement change management review process for all infrastructure configuration changes.", Deadline = D(-14), CreatedAt = D(6) };

        var m3ai1 = new ActionItem { Meeting = m3, MeetingDecision = m3d2, Title = "Implement configuration change management process", Description = "Create formal review and approval workflow for network infrastructure changes", AssignedToId = dirITInfra.Id, AssignedById = gsTech.Id, Status = ActionItemStatus.InProgress, Deadline = D(-14), CreatedAt = D(6) };

        // ── M4: Finance Directorate Meeting (MinutesEntry) ──
        var m4 = new Meeting
        {
            Title = "Finance & Accounting — Monthly Close Review",
            MeetingType = MeetingType.Regular,
            Status = MeetingStatus.MinutesEntry,
            CommitteeId = financeAcct.Id,
            ModeratorId = dirFinance.Id,
            Description = "Monthly review of financial close progress and FY2026 budget preparation status.",
            Location = "Finance Conference Room — Building E, Floor 3",
            ScheduledAt = D(2),
            DurationMinutes = 60,
            RecurrencePattern = RecurrencePattern.Monthly,
            CreatedAt = D(10),
        };

        var m4Attendees = new List<MeetingAttendee>
        {
            new() { Meeting = m4, UserId = dirFinance.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(8) },
            new() { Meeting = m4, UserId = gamalReda.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(7) },
            new() { Meeting = m4, UserId = gsFinance.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(8) },
        };

        var m4a1 = new MeetingAgendaItem { Meeting = m4, OrderIndex = 1, TopicTitle = "Q4 Financial Close Progress", AllocatedMinutes = 25, PresenterId = dirFinance.Id };
        var m4a2 = new MeetingAgendaItem { Meeting = m4, OrderIndex = 2, TopicTitle = "FY2026 Budget Consolidation Status", AllocatedMinutes = 20, PresenterId = gamalReda.Id };
        var m4a3 = new MeetingAgendaItem { Meeting = m4, OrderIndex = 3, TopicTitle = "Internal Controls Enhancement Update", AllocatedMinutes = 15, PresenterId = gamalReda.Id };

        // ── M5: Student Affairs — Upcoming (Scheduled) ──
        var m5 = new Meeting
        {
            Title = "Student Affairs Directorate — Spring 2026 Planning",
            MeetingType = MeetingType.Regular,
            Status = MeetingStatus.Scheduled,
            CommitteeId = studentAffairs.Id,
            ModeratorId = dirStudAffairs.Id,
            Description = "Planning session for Spring 2026 semester. Agenda: orientation program, counseling capacity, new student club proposals.",
            Location = "Student Center — Multi-Purpose Hall",
            ScheduledAt = now.AddDays(7),
            DurationMinutes = 90,
            CreatedAt = D(5),
        };

        var m5Attendees = new List<MeetingAttendee>
        {
            new() { Meeting = m5, UserId = dirStudAffairs.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(3) },
            new() { Meeting = m5, UserId = gsStudent.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(4) },
            new() { Meeting = m5, UserId = mostafaSalem.Id, RsvpStatus = RsvpStatus.Tentative, RsvpAt = D(2), RsvpComment = "May have scheduling conflict with Strategic Planning session" },
        };

        var m5a1 = new MeetingAgendaItem { Meeting = m5, OrderIndex = 1, TopicTitle = "Spring 2026 Student Orientation Program", AllocatedMinutes = 25 };
        var m5a2 = new MeetingAgendaItem { Meeting = m5, OrderIndex = 2, TopicTitle = "Counseling Capacity — New Psychologist Onboarding", AllocatedMinutes = 20 };
        var m5a3 = new MeetingAgendaItem { Meeting = m5, OrderIndex = 3, TopicTitle = "New Student Club Proposals Review", AllocatedMinutes = 20 };

        // ── M6: Top Level Special Session — Budget Workshop (Scheduled) ──
        var m6 = new Meeting
        {
            Title = "Special Session: FY2026 Budget Workshop",
            MeetingType = MeetingType.SpecialSession,
            Status = MeetingStatus.Scheduled,
            CommitteeId = topLevel.Id,
            ModeratorId = chairman.Id,
            Description = "Special budget workshop to resolve FY2026 funding gap (EGP 18M). All sector heads to present revised budget proposals with priority rankings.",
            Location = "Main Board Room — Building A, Floor 5",
            ScheduledAt = now.AddDays(14),
            DurationMinutes = 180,
            CreatedAt = D(8),
        };

        var m6Attendees = new List<MeetingAttendee>
        {
            new() { Meeting = m6, UserId = chairman.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(7) },
            new() { Meeting = m6, UserId = chiefStaff.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(7) },
            new() { Meeting = m6, UserId = gsAcademic.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(6) },
            new() { Meeting = m6, UserId = gsAdmin.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(5) },
            new() { Meeting = m6, UserId = gsTech.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(6) },
            new() { Meeting = m6, UserId = gsFinance.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(7) },
            new() { Meeting = m6, UserId = gsStudent.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(5) },
            new() { Meeting = m6, UserId = dirFinance.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(4) },
        };

        var m6a1 = new MeetingAgendaItem { Meeting = m6, OrderIndex = 1, TopicTitle = "FY2026 Revenue Projections", AllocatedMinutes = 30, PresenterId = dirFinance.Id };
        var m6a2 = new MeetingAgendaItem { Meeting = m6, OrderIndex = 2, TopicTitle = "Sector Budget Proposals — Priority Rankings", AllocatedMinutes = 60, PresenterId = gsFinance.Id };
        var m6a3 = new MeetingAgendaItem { Meeting = m6, OrderIndex = 3, TopicTitle = "Funding Gap Resolution Options", AllocatedMinutes = 45, PresenterId = chairman.Id };

        // ── M7: QA Directorate — Regular (Finalized) ──
        var m7 = new Meeting
        {
            Title = "Academic Quality — Q4 Audit Planning & Results",
            MeetingType = MeetingType.Regular,
            Status = MeetingStatus.Finalized,
            CommitteeId = acadQuality.Id,
            ModeratorId = dirQuality.Id,
            Description = "Review of Q4 QA audit results and planning for Spring 2026 accreditation cycle.",
            Location = "Quality Assurance Office — Building C, Floor 2",
            ScheduledAt = D(22),
            DurationMinutes = 75,
            MinutesContent = "<h3>Academic Quality Q4 Meeting</h3><p>Dr. Noha Mahmoud chaired the meeting. Q4 audit results reviewed: 14/18 Engineering programs fully compliant. Materials Science lab equipment flagged as critical.</p>",
            MinutesSubmittedAt = D(20),
            MinutesFinalizedAt = D(18),
            CreatedAt = D(28),
        };

        var m7Attendees = new List<MeetingAttendee>
        {
            new() { Meeting = m7, UserId = dirQuality.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(26), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(18) },
            new() { Meeting = m7, UserId = manalRizk.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(25), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(18) },
            new() { Meeting = m7, UserId = gsAcademic.Id, RsvpStatus = RsvpStatus.Accepted, RsvpAt = D(25), ConfirmationStatus = ConfirmationStatus.Confirmed, ConfirmedAt = D(19) },
        };

        var m7a1 = new MeetingAgendaItem { Meeting = m7, OrderIndex = 1, TopicTitle = "Engineering Programs Audit Results", AllocatedMinutes = 30, PresenterId = manalRizk.Id, DiscussionNotes = "14/18 programs compliant. 3 minor documentation gaps. 1 major finding: Materials Science lab." };
        var m7a2 = new MeetingAgendaItem { Meeting = m7, OrderIndex = 2, TopicTitle = "Spring 2026 Accreditation Preparation", AllocatedMinutes = 25, PresenterId = dirQuality.Id, DiscussionNotes = "Self-study document preparation on track. External reviewer visit scheduled for April." };

        var m7d1 = new MeetingDecision { Meeting = m7, AgendaItem = m7a1, DecisionType = DecisionType.Direction, DecisionText = "Materials Science lab equipment upgrade to be included in FY2026 capital budget with highest priority (EGP 850,000).", Deadline = D(-30), CreatedAt = D(22) };

        var m7ai1 = new ActionItem { Meeting = m7, MeetingDecision = m7d1, Title = "Submit Materials Science lab upgrade capital request", Description = "Include detailed equipment list and vendor quotes in FY2026 capital budget submission", AssignedToId = manalRizk.Id, AssignedById = dirQuality.Id, Status = ActionItemStatus.Completed, Deadline = D(-30), CompletedAt = D(12), VerifiedAt = D(10), CreatedAt = D(22) };

        // ── M8: Cancelled meeting ──
        var m8 = new Meeting
        {
            Title = "Cybersecurity Team — Biweekly Standup (Cancelled)",
            MeetingType = MeetingType.Regular,
            Status = MeetingStatus.Cancelled,
            CommitteeId = cybersecurity.Id,
            ModeratorId = karimTawfik.Id,
            Description = "Regular biweekly standup cancelled due to emergency security incident meeting (M3) covering the same topics.",
            Location = "IT Security Lab — Building D",
            ScheduledAt = D(6),
            DurationMinutes = 30,
            RecurrencePattern = RecurrencePattern.Biweekly,
            CreatedAt = D(14),
            UpdatedAt = D(6),
        };

        // ── Save all meetings ──
        context.Meetings.AddRange(m1, m2, m3, m4, m5, m6, m7, m8);
        context.MeetingAttendees.AddRange(m1Attendees);
        context.MeetingAttendees.AddRange(m2Attendees);
        context.MeetingAttendees.AddRange(m3Attendees);
        context.MeetingAttendees.AddRange(m4Attendees);
        context.MeetingAttendees.AddRange(m5Attendees);
        context.MeetingAttendees.AddRange(m6Attendees);
        context.MeetingAttendees.AddRange(m7Attendees);

        context.MeetingAgendaItems.AddRange(m1a1, m1a2, m1a3, m1a4, m2a1, m2a2, m2a3, m3a1, m3a2, m4a1, m4a2, m4a3, m5a1, m5a2, m5a3, m6a1, m6a2, m6a3, m7a1, m7a2);
        context.MeetingDecisions.AddRange(m1d1, m1d2, m1d3, m1d4, m2d1, m2d2, m3d1, m3d2, m7d1);
        context.ActionItems.AddRange(m1ai1, m1ai2, m1ai3, m1ai4, m2ai1, m2ai2, m3ai1, m7ai1);

        await context.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────────────
    //  PHASE D: Confidentiality, Notifications, Audit Log
    // ────────────────────────────────────────────────────────────────
    private static async Task SeedCrossCuttingAsync(
        ApplicationDbContext context, DateTime now, Func<int, DateTime> D,
        User chairman, User chiefStaff, User co2, User admin,
        User gsAcademic, User gsAdmin, User gsTech, User gsFinance, User gsStudent,
        User dirAcadProg, User dirITInfra, User dirFinance, User dirStudAffairs,
        User karimTawfik, User gamalReda,
        Committee topLevel, Committee acadPrograms, Committee itInfra,
        Committee financeAcct, Committee internalAudit)
    {
        // ── Confidentiality Markings ──
        // The confidential report (r23) and directive (d14) already have IsConfidential=true.
        // Now create the corresponding ConfidentialityMarking records.

        // Find the confidential report and directive by title
        var confidentialReport = await context.Reports.FirstOrDefaultAsync(r => r.IsConfidential && r.Title.Contains("Disciplinary"));
        var confidentialDirective = await context.Directives.FirstOrDefaultAsync(d => d.IsConfidential && d.Title.Contains("Enhanced Financial Controls"));

        if (confidentialReport != null)
        {
            context.ConfidentialityMarkings.Add(new ConfidentialityMarking
            {
                ItemType = ConfidentialItemType.Report,
                ItemId = confidentialReport.Id,
                MarkedById = gamalReda.Id,
                MarkerCommitteeLevel = HierarchyLevel.Directors,
                MarkerCommitteeId = internalAudit.Id,
                MinChairmanOfficeRank = null,
                IsActive = true,
                Reason = "Staff disciplinary investigation — restricted to senior management",
                MarkedAt = D(24),
            });

            // Access grants for the confidential report
            context.AccessGrants.Add(new AccessGrant
            {
                ItemType = ConfidentialItemType.Report,
                ItemId = confidentialReport.Id,
                GrantedToUserId = gsFinance.Id,
                GrantedById = gamalReda.Id,
                Reason = "GS Finance & Governance requires access for oversight",
                IsActive = true,
                GrantedAt = D(23),
            });
            context.AccessGrants.Add(new AccessGrant
            {
                ItemType = ConfidentialItemType.Report,
                ItemId = confidentialReport.Id,
                GrantedToUserId = chairman.Id,
                GrantedById = gamalReda.Id,
                Reason = "Chairman briefing on investigation outcome",
                IsActive = true,
                GrantedAt = D(22),
            });
        }

        if (confidentialDirective != null)
        {
            context.ConfidentialityMarkings.Add(new ConfidentialityMarking
            {
                ItemType = ConfidentialItemType.Directive,
                ItemId = confidentialDirective.Id,
                MarkedById = chairman.Id,
                MarkerCommitteeLevel = HierarchyLevel.TopLevel,
                MarkerCommitteeId = topLevel.Id,
                MinChairmanOfficeRank = 2, // Only rank 1-2 CO members can see
                IsActive = true,
                Reason = "Sensitive financial controls directive — restricted access",
                MarkedAt = D(18),
            });
        }

        // ── Notifications ──
        var notifications = new List<Notification>();

        void N(string userId, NotificationType type, string title, string message,
               string? url = null, NotificationPriority priority = NotificationPriority.Normal,
               int daysAgo = 0, bool isRead = false, int? relatedId = null)
        {
            notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                ActionUrl = url,
                Priority = priority,
                CreatedAt = D(daysAgo),
                IsRead = isRead,
                ReadAt = isRead ? D(daysAgo > 0 ? daysAgo - 1 : 0) : null,
                RelatedEntityId = relatedId,
            });
        }

        // Notifications for Chairman
        N(chairman.Id.ToString(), NotificationType.ReportSubmitted, "Sector Report Submitted",
            "Finance & Governance Sector — Q4 2025 Executive Summary has been submitted for your review.",
            "/Reports/Details/19", NotificationPriority.Normal, 9, false);
        N(chairman.Id.ToString(), NotificationType.ReportSubmitted, "Sector Report Submitted",
            "Student Experience Sector — Q4 2025 Executive Summary has been submitted for your review.",
            "/Reports/Details/20", NotificationPriority.Normal, 7, false);
        N(chairman.Id.ToString(), NotificationType.ReportApproved, "Report Approved",
            "Q4 2025 Institutional Performance Report has been approved.",
            "/Reports/Details/21", NotificationPriority.Normal, 6, true);

        // Notifications for GS Academic
        N(gsAcademic.Id.ToString(), NotificationType.DirectiveIssued, "New Directive",
            "Research Collaboration Framework directive has been issued.",
            "/Directives/Details/13", NotificationPriority.Normal, 2, false);
        N(gsAcademic.Id.ToString(), NotificationType.ReportApproved, "Report Approved",
            "Academic Affairs Sector Q4 Executive Summary approved by Chairman.",
            "/Reports/Details/17", NotificationPriority.Normal, 14, true);
        N(gsAcademic.Id.ToString(), NotificationType.MeetingInvitation, "Budget Workshop",
            "You are invited to the Special Session: FY2026 Budget Workshop.",
            "/Meetings/Details/6", NotificationPriority.High, 8, false);

        // Notifications for GS Technology
        N(gsTech.Id.ToString(), NotificationType.DirectiveIssued, "Urgent Directive",
            "Chairman has issued urgent directive: Ransomware Preparedness Assessment.",
            "/Directives/Details/5", NotificationPriority.Urgent, 8, true);
        N(gsTech.Id.ToString(), NotificationType.ReportApproved, "Report Approved",
            "Technology sector Q4 summary approved.",
            "/Reports/Details/18", NotificationPriority.Normal, 10, true);

        // Notifications for Director IT Infrastructure
        N(dirITInfra.Id.ToString(), NotificationType.FeedbackReceived, "Feedback on Report",
            "GS Technology requested revisions to Network Infrastructure Upgrade report. Please add cost variance analysis.",
            "/Reports/Details/6", NotificationPriority.High, 25, true);
        N(dirITInfra.Id.ToString(), NotificationType.ActionItemAssigned, "Action Item Assigned",
            "Submit zero-trust architecture proposal — assigned from Q4 Quarterly Review meeting.",
            "/Meetings/ActionItems", NotificationPriority.Normal, 12, false);
        N(dirITInfra.Id.ToString(), NotificationType.DirectiveIssued, "Urgent Directive",
            "Ransomware Preparedness assessment assigned to IT Infrastructure.",
            "/Directives/Details/6", NotificationPriority.Urgent, 6, true);

        // Notifications for Director Academic Programs
        N(dirAcadProg.Id.ToString(), NotificationType.ActionItemAssigned, "Action Item Completed",
            "CS faculty recruitment campaign action item marked as completed.",
            "/Meetings/ActionItems", NotificationPriority.Normal, 5, true);
        N(dirAcadProg.Id.ToString(), NotificationType.DirectiveStatusChanged, "Directive Closed",
            "CS Faculty Recruitment directive has been closed — both positions filled.",
            "/Directives/Details/2", NotificationPriority.Normal, 12, true);

        // Notifications for Director Finance
        N(dirFinance.Id.ToString(), NotificationType.DirectiveIssued, "Corrective Action",
            "Corrective Action directive issued: Procurement Approval Workflow Enhancement.",
            "/Directives/Details/4", NotificationPriority.High, 20, true);
        N(dirFinance.Id.ToString(), NotificationType.MeetingInvitation, "Budget Workshop",
            "You are invited to the Special Session: FY2026 Budget Workshop.",
            "/Meetings/Details/6", NotificationPriority.Normal, 8, false);

        // Notifications for Director Student Affairs
        N(dirStudAffairs.Id.ToString(), NotificationType.DirectiveIssued, "Position Approved",
            "Chairman approved Clinical Psychologist position. Initiate recruitment.",
            "/Directives/Details/9", NotificationPriority.High, 3, false);
        N(dirStudAffairs.Id.ToString(), NotificationType.ActionItemAssigned, "Action Item",
            "Fast-track clinical psychologist recruitment — assigned from Q4 meeting.",
            "/Meetings/ActionItems", NotificationPriority.Normal, 12, false);

        // Notifications for GS Finance
        N(gsFinance.Id.ToString(), NotificationType.ReportSubmitted, "Report Submitted",
            "FY2026 Budget Framework Proposal submitted by Finance & Accounting.",
            "/Reports/Details/9", NotificationPriority.Normal, 14, true);
        N(gsFinance.Id.ToString(), NotificationType.DeadlineApproaching, "Budget Workshop Approaching",
            "FY2026 Budget Workshop is scheduled in 14 days. Ensure all sector proposals are collected.",
            "/Meetings/Details/6", NotificationPriority.High, 2, false);

        // Notifications for GS Admin
        N(gsAdmin.Id.ToString(), NotificationType.FeedbackReceived, "Feedback from Chairman's Office",
            "Chairman's Office requests additional energy cost analysis for Administration sector report.",
            "/Directives/Details/7", NotificationPriority.Normal, 5, false);

        // Notifications for Karim Tawfik (Cybersecurity)
        N(karimTawfik.Id.ToString(), NotificationType.MeetingInvitation, "Emergency Meeting",
            "Emergency security meeting convened — Campus Network Security Incident.",
            "/Meetings/Details/3", NotificationPriority.Urgent, 6, true);
        N(karimTawfik.Id.ToString(), NotificationType.ConfirmationRequested, "Minutes Confirmation",
            "Please confirm the minutes for the Emergency Security Incident meeting.",
            "/Meetings/Details/3", NotificationPriority.Normal, 4, false);

        // General announcement
        N(gsAcademic.Id.ToString(), NotificationType.General, "System Maintenance",
            "Scheduled system maintenance window: Saturday 2:00 AM - 6:00 AM.",
            priority: NotificationPriority.Low, daysAgo: 3, isRead: true);
        N(gsTech.Id.ToString(), NotificationType.General, "System Maintenance",
            "Scheduled system maintenance window: Saturday 2:00 AM - 6:00 AM.",
            priority: NotificationPriority.Low, daysAgo: 3, isRead: true);

        context.Notifications.AddRange(notifications);

        // ── Audit Log ──
        var auditLogs = new List<AuditLog>();

        void A(AuditActionType action, string itemType, int? itemId, string? itemTitle,
               int? userId, string? userName, int daysAgo, string? details = null,
               int? committeeId = null)
        {
            auditLogs.Add(new AuditLog
            {
                ActionType = action,
                ItemType = itemType,
                ItemId = itemId,
                ItemTitle = itemTitle,
                UserId = userId,
                UserName = userName,
                Timestamp = D(daysAgo),
                Details = details,
                CommitteeId = committeeId,
                IpAddress = "10.0.1." + (userId ?? 1) % 254,
            });
        }

        // Logins
        A(AuditActionType.Login, "User", chairman.Id, null, chairman.Id, chairman.Name, 12, "Login via magic link");
        A(AuditActionType.Login, "User", chiefStaff.Id, null, chiefStaff.Id, chiefStaff.Name, 10, "Login via magic link");
        A(AuditActionType.Login, "User", gsAcademic.Id, null, gsAcademic.Id, gsAcademic.Name, 18);
        A(AuditActionType.Login, "User", gsTech.Id, null, gsTech.Id, gsTech.Name, 14);
        A(AuditActionType.Login, "User", gsFinance.Id, null, gsFinance.Id, gsFinance.Name, 10);
        A(AuditActionType.Login, "User", dirAcadProg.Id, null, dirAcadProg.Id, dirAcadProg.Name, 28);
        A(AuditActionType.Login, "User", dirITInfra.Id, null, dirITInfra.Id, dirITInfra.Name, 22);
        A(AuditActionType.Login, "User", admin.Id, null, admin.Id, admin.Name, 5, "System administration session");

        // Report lifecycle events
        A(AuditActionType.Create, "Report", null, "Q4 Course Design Review", dirAcadProg.Id, dirAcadProg.Name, 45, committeeId: acadPrograms.Id);
        A(AuditActionType.StatusChange, "Report", null, "Q4 Course Design Review", dirAcadProg.Id, dirAcadProg.Name, 38, "Draft → Approved", acadPrograms.Id);
        A(AuditActionType.Create, "Report", null, "Academic Programs Q4 Summary", dirAcadProg.Id, dirAcadProg.Name, 28, "Summary report created with 4 source reports", acadPrograms.Id);
        A(AuditActionType.StatusChange, "Report", null, "Academic Programs Q4 Summary", gsAcademic.Id, gsAcademic.Name, 24, "UnderReview → Approved");
        A(AuditActionType.Create, "Report", null, "Institutional Performance Report", chiefStaff.Id, chiefStaff.Name, 10, "Executive summary compiled", topLevel.Id);
        A(AuditActionType.StatusChange, "Report", null, "Institutional Performance Report", chairman.Id, chairman.Name, 6, "UnderReview → Approved", topLevel.Id);
        A(AuditActionType.StatusChange, "Report", null, "Network Infrastructure Upgrade", gsTech.Id, gsTech.Name, 25, "UnderReview → FeedbackRequested");
        A(AuditActionType.StatusChange, "Report", null, "Network Upgrade (Revised)", gsTech.Id, gsTech.Name, 18, "UnderReview → Approved");

        // Directive events
        A(AuditActionType.Create, "Directive", null, "CS Faculty Recruitment", chairman.Id, chairman.Name, 40, "Priority: High", topLevel.Id);
        A(AuditActionType.DirectiveForwarded, "Directive", null, "CS Faculty Recruitment — Academic Programs", gsAcademic.Id, gsAcademic.Name, 37, "Forwarded to Academic Programs Directorate");
        A(AuditActionType.DirectiveAcknowledged, "Directive", null, "CS Faculty Recruitment — Academic Programs", dirAcadProg.Id, dirAcadProg.Name, 36);
        A(AuditActionType.StatusChange, "Directive", null, "CS Faculty Recruitment", chairman.Id, chairman.Name, 10, "Verified → Closed");
        A(AuditActionType.Create, "Directive", null, "Ransomware Preparedness", chairman.Id, chairman.Name, 8, "Priority: Urgent", topLevel.Id);

        // Meeting events
        A(AuditActionType.MeetingStarted, "Meeting", null, "Q4 Top Level Quarterly Review", chairman.Id, chairman.Name, 12, committeeId: topLevel.Id);
        A(AuditActionType.MinutesSubmitted, "Meeting", null, "Q4 Top Level Quarterly Review", chiefStaff.Id, chiefStaff.Name, 10, committeeId: topLevel.Id);
        A(AuditActionType.MinutesFinalized, "Meeting", null, "Q4 Top Level Quarterly Review", chairman.Id, chairman.Name, 7, "All attendees confirmed", topLevel.Id);
        A(AuditActionType.MeetingStarted, "Meeting", null, "Emergency Security Incident", dirITInfra.Id, dirITInfra.Name, 6, committeeId: itInfra.Id);
        A(AuditActionType.MinutesSubmitted, "Meeting", null, "Emergency Security Incident", dirITInfra.Id, dirITInfra.Name, 4, committeeId: itInfra.Id);

        // Confidentiality events
        A(AuditActionType.ConfidentialityMarked, "Report", null, "Staff Disciplinary Investigation", gamalReda.Id, gamalReda.Name, 24, "Marked as confidential — disciplinary investigation", internalAudit.Id);
        A(AuditActionType.AccessGranted, "Report", null, "Staff Disciplinary Investigation", gamalReda.Id, gamalReda.Name, 23, "Access granted to GS Finance & Governance");
        A(AuditActionType.ConfidentialityMarked, "Directive", null, "Enhanced Financial Controls", chairman.Id, chairman.Name, 18, "Confidential directive — financial controls");

        // Search event
        A(AuditActionType.SearchPerformed, "Search", null, null, gsAcademic.Id, gsAcademic.Name, 5, "Query: 'faculty recruitment progress'");

        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();
    }
}
