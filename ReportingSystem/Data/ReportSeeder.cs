using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Seeds detailed reports across ALL committees (L0, L1, L2) with realistic content,
/// status histories, source links, and collective approval records.
/// </summary>
public static class ReportSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Reports.AnyAsync()) return;

        var users = await context.Users.ToListAsync();
        var committees = await context.Committees.Include(c => c.Memberships).ToListAsync();

        User? UByEmail(string email) => users.FirstOrDefault(u => u.Email == email);
        Committee? Comm(string namePart) => committees.FirstOrDefault(c => c.Name.Contains(namePart));

        // ── Key Users ──
        var chairman       = UByEmail("am@org.edu")!;
        var ahmedMansour   = UByEmail("ahmed.mansour@org.edu")!;
        var moustafaFouad  = UByEmail("moustafa.fouad@org.edu")!;
        var marwaElSerafy  = UByEmail("marwa.elserafy@org.edu")!;
        var samiaElAshiry  = UByEmail("samia.elashiry@org.edu")!;

        // L0 heads (also L1 heads)
        var mohamedIbrahim = UByEmail("mohamed.ibrahim@org.edu")!;
        var radwaSelim     = UByEmail("radwa.selim@org.edu")!;
        var ghadirNassar   = UByEmail("ghadir.nassar@org.edu")!;
        var sherineKhalil  = UByEmail("sherine.khalil@org.edu")!;
        var sherineSalamy  = UByEmail("sherine.salamony@org.edu")!;

        // L2 heads
        var hananMostafa     = UByEmail("hanan.mostafa@org.edu")!;
        var tarekAbdelFattah = UByEmail("tarek.abdelfattah@org.edu")!;
        var nohaElSayed      = UByEmail("noha.elsayed@org.edu")!;
        var ohoudKhadr       = UByEmail("ohoud.khadr@org.edu")!;
        var laylaHassan      = UByEmail("layla.hassan@org.edu")!;
        var yehiaRazzaz      = UByEmail("yehia.razzaz@org.edu")!;
        var ramyShawky       = UByEmail("ramy.shawky@org.edu")!;
        var amrBaibars       = UByEmail("amr.baibars@org.edu")!;
        var ibrahimKhalil    = UByEmail("ibrahim.khalil@org.edu")!;
        var hossamBadawy     = UByEmail("hossam.badawy@org.edu")!;
        var monaFarid        = UByEmail("mona.farid@org.edu")!;
        var daliaElMainouny  = UByEmail("dalia.elmainouny@org.edu")!;
        var aymanRahmou      = UByEmail("ayman.rahmou@org.edu")!;
        var salmaIbrahim     = UByEmail("salma.ibrahim@org.edu")!;

        // L2 members (report authors)
        var amiraSoliman     = UByEmail("amira.soliman@org.edu")!;
        var yasminAbdelR     = UByEmail("yasmin.abdelrahman@org.edu")!;
        var saharElGendy     = UByEmail("sahar.elgendy@org.edu")!;
        var lamiaYoussef     = UByEmail("lamia.youssef@org.edu")!;
        var waleedTantawy    = UByEmail("waleed.tantawy@org.edu")!;
        var abdallahRamzy    = UByEmail("abdallah.ramzy@org.edu")!;
        var mohamedAbdelWahab = UByEmail("mohamed.abdelwahab@org.edu")!;
        var mostafaRagab     = UByEmail("mostafa.ragab@org.edu")!;

        // ── Committees ──
        var topLevel         = Comm("Top Level Committee")!;
        var aqa              = Comm("Academic Quality")!;
        var studentActivities = Comm("Student Activities")!;
        var admission        = Comm("Admission")!; // L1
        var campusAdmin      = Comm("Campus Administration")!;
        var hr               = Comm("HR")!;
        var curriculum       = Comm("Curriculum")!;
        var probation        = Comm("Probation")!;
        var teaching         = Comm("Teaching")!;
        var music            = Comm("Music")!;
        var theater          = Comm("Theater")!;
        var sports           = Comm("Sports")!;
        var awg              = Comm("AWG")!;
        var marketing        = Comm("Marketing")!;
        var admServices      = Comm("Admission Services")!;
        var admOffice        = Comm("Admission Office")!;
        var facility         = Comm("Facility")!;
        var security         = Comm("Security")!;
        var agriculture      = Comm("Agriculture")!;
        var recruitment      = Comm("Recruitment")!;
        var compBenefits     = Comm("Compensation")!;
        var personnel        = Comm("Personnel")!;

        var now = DateTime.UtcNow;
        DateTime D(int daysAgo) => now.AddDays(-daysAgo);

        var reports = new List<Report>();
        var histories = new List<ReportStatusHistory>();
        var sourceLinks = new List<ReportSourceLink>();
        var approvals = new List<ReportApproval>();

        // Helper: create a report
        Report R(string title, ReportType type, ReportStatus status,
                 User author, Committee committee, int daysAgoCreated,
                 string body, string? action = null, string? resources = null,
                 string? support = null, string? remarks = null,
                 bool confidential = false, int version = 1, bool skipApprovals = false)
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
                SkipApprovals = skipApprovals,
                Version = version,
                CreatedAt = D(daysAgoCreated),
                SubmittedAt = status >= ReportStatus.Submitted ? D(daysAgoCreated - 1) : null,
                UpdatedAt = status >= ReportStatus.Submitted ? D(daysAgoCreated - 2) : null
            };
            reports.Add(r);
            return r;
        }

        // Helper: status history
        void SH(Report r, ReportStatus from, ReportStatus to, User by, int daysAgo, string? comment = null)
        {
            histories.Add(new ReportStatusHistory
            {
                Report = r, OldStatus = from, NewStatus = to,
                ChangedBy = by, ChangedAt = D(daysAgo), Comments = comment
            });
        }

        // Helper: approval
        void Approve(Report r, User by, int daysAgo, string? comment = null)
        {
            approvals.Add(new ReportApproval
            {
                Report = r, UserId = by.Id,
                ApprovedAt = D(daysAgo), Comments = comment
            });
        }

        // ════════════════════════════════════════════════════════════
        //  L2 DETAILED REPORTS — Academic Quality & Accreditation
        // ════════════════════════════════════════════════════════════

        // ── Curriculum ──
        var r1 = R("Semester 1 Curriculum Review — 2025/2026",
            ReportType.Detailed, ReportStatus.Approved,
            hananMostafa, curriculum, 40,
            "<h3>Curriculum Review</h3><p>Completed comprehensive review of 18 program curricula for Semester 1 2025/2026. Key findings include alignment gaps with national accreditation requirements in 4 courses (ENG201, BIO305, CS410, MATH102) and successful integration of industry advisory board recommendations in 14 courses.</p><p>Updated learning outcomes documented for all reviewed programs. New competency mapping templates distributed to all program coordinators.</p>",
            "Schedule remediation workshops for flagged courses within 30 days",
            "Updated national accreditation framework documentation (2025 edition)",
            "Program coordinators availability for 2-day workshop");
        SH(r1, ReportStatus.Draft, ReportStatus.Submitted, hananMostafa, 39);
        Approve(r1, amiraSoliman, 37, "Thorough review. The competency mapping is excellent.");
        // bassemYoussef also approved → triggers auto-approve
        SH(r1, ReportStatus.Submitted, ReportStatus.Approved, hananMostafa, 35, "All members approved.");

        var r2 = R("New Program Proposal: Data Science Minor",
            ReportType.Detailed, ReportStatus.Submitted,
            amiraSoliman, curriculum, 8,
            "<h3>Program Proposal</h3><p>Proposing a new Data Science minor consisting of 6 courses (18 credit hours) spanning statistics, machine learning, data visualization, and applied analytics. Market demand analysis shows 34% increase in data science job postings regionally.</p><h4>Proposed Courses</h4><ul><li>DS101 — Introduction to Data Science</li><li>DS201 — Statistical Methods for Data Analysis</li><li>DS301 — Machine Learning Fundamentals</li><li>DS302 — Data Visualization & Communication</li><li>DS401 — Applied Analytics Project</li><li>DS402 — Big Data Technologies</li></ul>",
            "Approve curriculum design phase and assign faculty workload",
            "2 additional faculty positions (Statistics, ML specialization)");
        SH(r2, ReportStatus.Draft, ReportStatus.Submitted, amiraSoliman, 7);

        // ── Probation & Mentoring ──
        var r3 = R("Academic Probation Cases — Q4 2025",
            ReportType.Detailed, ReportStatus.Approved,
            tarekAbdelFattah, probation, 35,
            "<h3>Probation Report</h3><p>Q4 2025 academic probation overview: 142 students on probation (down from 178 in Q3). Mentoring program paired 142 students with 45 faculty mentors.</p><h4>Outcomes</h4><ul><li>68 students improved GPA above probation threshold (48% success rate)</li><li>23 students recommended for extended probation</li><li>12 students recommended for dismissal review</li><li>39 continuing monitoring</li></ul><p>Peer tutoring program contributed significantly — students attending 4+ sessions showed 72% improvement rate vs. 31% for non-attendees.</p>",
            "Expand peer tutoring program with dedicated study spaces",
            "3 additional tutoring rooms in Library Building, 20 additional peer tutors",
            "Student Affairs coordination for tutor recruitment");
        SH(r3, ReportStatus.Draft, ReportStatus.Submitted, tarekAbdelFattah, 34);
        Approve(r3, UByEmail("fatma.elzahraa@org.edu")!, 33, "Comprehensive data. Agree with tutoring expansion.");
        Approve(r3, UByEmail("omar.hashem@org.edu")!, 32);
        SH(r3, ReportStatus.Submitted, ReportStatus.Approved, tarekAbdelFattah, 31, "All members approved.");

        // ── Teaching and Evaluation Standard ──
        var r4 = R("Faculty Evaluation Results — Fall 2025",
            ReportType.Detailed, ReportStatus.Approved,
            nohaElSayed, teaching, 38,
            "<h3>Faculty Evaluation</h3><p>Completed peer observation and student evaluation for 98 faculty members across 6 departments. Overall teaching quality index: 4.1/5.0 (target: 3.8).</p><h4>Distribution</h4><ul><li>Excellent (4.5+): 22 faculty (22%)</li><li>Good (3.8-4.5): 51 faculty (52%)</li><li>Satisfactory (3.0-3.8): 20 faculty (20%)</li><li>Needs improvement (<3.0): 5 faculty (5%)</li></ul><p>Professional development plans initiated for 5 faculty members scoring below 3.0. Peer mentoring pairs assigned.</p>",
            "Implement mandatory teaching workshop for faculty scoring below 3.0",
            "Faculty Development Center workshop slots (5 sessions)");
        SH(r4, ReportStatus.Draft, ReportStatus.Submitted, nohaElSayed, 37);
        Approve(r4, yasminAbdelR, 36, "Clear metrics. Support the development plan approach.");
        Approve(r4, UByEmail("khaled.mostafa@org.edu")!, 35);
        SH(r4, ReportStatus.Submitted, ReportStatus.Approved, nohaElSayed, 34, "All members approved.");

        var r5 = R("Teaching Workshop Outcomes — Fall 2025",
            ReportType.Detailed, ReportStatus.Draft,
            yasminAbdelR, teaching, 3,
            "<h3>Workshop Outcomes</h3><p>Draft report on 3 faculty development workshops conducted in Fall 2025. Attendance: 45 faculty members total. Post-workshop evaluation surveys in progress — awaiting responses from 12 participants.</p>");

        // ════════════════════════════════════════════════════════════
        //  L2 DETAILED REPORTS — Student Activities
        // ════════════════════════════════════════════════════════════

        // ── Music ──
        var r6 = R("Annual Music Concert — Planning & Budget Report",
            ReportType.Detailed, ReportStatus.Approved,
            ohoudKhadr, music, 30,
            "<h3>Annual Concert Planning</h3><p>The 12th Annual Music Concert is confirmed for March 15, 2026. Venue: Main Auditorium (capacity 800). Budget approved: EGP 85,000.</p><h4>Program Highlights</h4><ul><li>Student orchestra: 45 performers (Beethoven Symphony No. 5, selections from Umm Kulthum)</li><li>Guest artist: Dr. Amira El-Sayed (Oud master class & performance)</li><li>Student solo competition: 12 finalists selected from 47 auditions</li></ul><p>Ticket sales opened — 320 of 800 seats reserved within first week. Corporate sponsorship secured: EGP 25,000 from CIB Foundation.</p>",
            "Confirm sound system rental and additional rehearsal schedule",
            "Sound system rental: EGP 15,000, additional rehearsal hours: 30 hours",
            skipApprovals: true);
        SH(r6, ReportStatus.Draft, ReportStatus.Approved, ohoudKhadr, 29, "Report approved immediately (approval cycle skipped)");

        // ── Theater ──
        var r7 = R("Spring Theater Season — Production Schedule",
            ReportType.Detailed, ReportStatus.Submitted,
            laylaHassan, theater, 12,
            "<h3>Spring Season</h3><p>Three productions planned for Spring 2026:</p><ol><li><b>\"The Glass Menagerie\"</b> (Tennessee Williams) — Feb 28-Mar 2. Cast: 8, Crew: 15. Director: Rania Samir.</li><li><b>\"Masrah Al-Shams\"</b> (Original Arabic play by student playwright) — Apr 5-7. Cast: 12, Crew: 20.</li><li><b>Year-End Showcase</b> — May 20. Student-directed one-acts (6 pieces).</li></ol><p>Total budget request: EGP 120,000. Set construction starts Feb 1 for Production 1.</p>",
            "Approve budget allocation and workshop access schedule",
            "Workshop space in Building F, costume budget: EGP 30,000");
        SH(r7, ReportStatus.Draft, ReportStatus.Submitted, laylaHassan, 11);

        // ── Sports ──
        var r8 = R("Interuniversity Championship Results — Fall 2025",
            ReportType.Detailed, ReportStatus.Approved,
            yehiaRazzaz, sports, 25,
            "<h3>Championship Results</h3><p>University teams competed in 8 interuniversity championships in Fall 2025. Results:</p><table><tr><th>Sport</th><th>Result</th><th>Notable</th></tr><tr><td>Football</td><td>2nd Place</td><td>Lost final 1-2 to Cairo University</td></tr><tr><td>Basketball (M)</td><td>1st Place</td><td>Undefeated season (8-0)</td></tr><tr><td>Basketball (W)</td><td>3rd Place</td><td>Best result in 5 years</td></tr><tr><td>Swimming</td><td>1st Place</td><td>3 individual gold medals</td></tr><tr><td>Tennis</td><td>4th Place</td><td>First time qualifying</td></tr><tr><td>Athletics</td><td>2nd Place</td><td>2 new university records</td></tr><tr><td>Volleyball</td><td>3rd Place</td><td>—</td></tr><tr><td>Table Tennis</td><td>1st Place</td><td>Individual & team titles</td></tr></table><p>3 First Place finishes — best institutional result in a decade. Student-athlete GPA average: 3.2 (above university mean of 3.0).</p>",
            "Invest in expanded training facilities for basketball and swimming programs",
            "Pool lane expansion (EGP 200,000), new basketball court lighting (EGP 75,000)");
        SH(r8, ReportStatus.Draft, ReportStatus.Submitted, yehiaRazzaz, 24);
        Approve(r8, UByEmail("tamer.elnaggar@org.edu")!, 23, "Outstanding results. Facilities upgrade well justified.");
        Approve(r8, UByEmail("dina.raafat@org.edu")!, 22);
        SH(r8, ReportStatus.Submitted, ReportStatus.Approved, yehiaRazzaz, 21, "All members approved.");

        // ── AWG ──
        var r9 = R("Community Service Program — Q4 2025",
            ReportType.Detailed, ReportStatus.FeedbackRequested,
            saharElGendy, awg, 18,
            "<h3>Community Service Report</h3><p>AWG organized 7 community service events in Q4 2025, engaging 280 student volunteers (total 1,120 volunteer hours).</p><h4>Events</h4><ul><li>Village school renovation project (Fayoum) — 45 volunteers, 3 days</li><li>Blood donation drive — 180 units collected</li><li>Environmental cleanup at Wadi El-Rayan — 60 volunteers</li><li>Elderly home visits (monthly) — 4 sessions, 20 volunteers each</li></ul><p>Volunteer satisfaction survey: 4.6/5.0. Community partner feedback: excellent.</p>",
            "Establish permanent partnership with Fayoum Governorate for ongoing school projects");
        SH(r9, ReportStatus.Draft, ReportStatus.Submitted, saharElGendy, 17);
        SH(r9, ReportStatus.Submitted, ReportStatus.FeedbackRequested, yehiaRazzaz, 15,
            "Good report but needs budget breakdown per event and impact metrics (pre/post assessments for school renovation). Please revise.");

        // ════════════════════════════════════════════════════════════
        //  L2 DETAILED REPORTS — Admission
        // ════════════════════════════════════════════════════════════

        // ── Marketing & Outreach ──
        var r10 = R("Fall 2025 Recruitment Campaign Results",
            ReportType.Detailed, ReportStatus.Approved,
            lamiaYoussef, marketing, 32,
            "<h3>Recruitment Campaign</h3><p>The Fall 2025 multi-channel recruitment campaign reached an estimated 45,000 prospective students. Campaign budget: EGP 320,000.</p><h4>Channel Performance</h4><table><tr><th>Channel</th><th>Reach</th><th>Applications</th><th>Cost/App</th></tr><tr><td>Social Media</td><td>28,000</td><td>1,840</td><td>EGP 52</td></tr><tr><td>School Visits (42 schools)</td><td>8,500</td><td>620</td><td>EGP 145</td></tr><tr><td>University Open Day</td><td>3,200</td><td>480</td><td>EGP 125</td></tr><tr><td>Print & Radio</td><td>5,300</td><td>210</td><td>EGP 380</td></tr></table><p>Total applications generated: 3,150 (up 18% YoY). Social media ROI is 7x print. Recommend shifting 40% of print budget to digital for Spring cycle.</p>",
            "Reallocate 40% of print budget to digital channels for Spring 2026",
            "Social media management tool upgrade (EGP 18,000/year)");
        SH(r10, ReportStatus.Draft, ReportStatus.Submitted, lamiaYoussef, 31);
        Approve(r10, ahmedMansour, 30, "Excellent data-driven analysis. Approve budget reallocation.");
        Approve(r10, samiaElAshiry, 29);
        Approve(r10, UByEmail("mahmoud.farouk@org.edu")!, 28);
        SH(r10, ReportStatus.Submitted, ReportStatus.Approved, ahmedMansour, 27, "All members approved.");

        // ── Admission Services ──
        var r11 = R("Application Processing Metrics — Q4 2025",
            ReportType.Detailed, ReportStatus.Approved,
            ramyShawky, admServices, 28,
            "<h3>Processing Metrics</h3><p>Q4 2025 application cycle: 3,150 applications received, 2,890 processed (91.7%). Average processing time: 4.2 business days (target: 5 days).</p><h4>Breakdown</h4><ul><li>Complete applications (first submission): 2,340 (74%)</li><li>Incomplete requiring follow-up: 810 (26%)</li><li>Average document follow-up cycles: 1.4</li><li>Rejection rate: 18% (academic criteria not met)</li></ul><p>New online portal reduced phone inquiries by 35%. Applicant satisfaction score: 4.3/5.0.</p>",
            "Implement automated document verification for common credentials");
        SH(r11, ReportStatus.Draft, ReportStatus.Submitted, ramyShawky, 27);
        Approve(r11, UByEmail("heba.abdelaziz@org.edu")!, 26, "Good metrics. Online portal is clearly paying off.");
        Approve(r11, UByEmail("nermeen.sami@org.edu")!, 25);
        SH(r11, ReportStatus.Submitted, ReportStatus.Approved, ramyShawky, 24, "All members approved.");

        // ── Admission Office ──
        var r12 = R("Enrollment Verification & Registration Audit",
            ReportType.Detailed, ReportStatus.Submitted,
            waleedTantawy, admOffice, 10,
            "<h3>Registration Audit</h3><p>Completed annual audit of enrollment verification procedures. 2,340 student records verified against original documents.</p><h4>Findings</h4><ul><li>Records fully compliant: 2,298 (98.2%)</li><li>Minor discrepancies (name spelling, date formats): 38 (1.6%)</li><li>Significant issues requiring resolution: 4 (0.17%)</li></ul><p>All 4 significant issues involve international student credential equivalency — forwarded to Registrar for resolution.</p>",
            "Standardize international credential verification checklist");
        SH(r12, ReportStatus.Draft, ReportStatus.Submitted, waleedTantawy, 9);

        // ════════════════════════════════════════════════════════════
        //  L2 DETAILED REPORTS — Campus Administration
        // ════════════════════════════════════════════════════════════

        // ── Facility Management ──
        var r13 = R("HVAC System Upgrade — Progress Report",
            ReportType.Detailed, ReportStatus.Approved,
            amrBaibars, facility, 33,
            "<h3>HVAC Upgrade Progress</h3><p>Phase 1 of campus-wide HVAC modernization is 85% complete. Buildings A, B, and C upgraded to energy-efficient VRF systems.</p><h4>Progress by Building</h4><table><tr><th>Building</th><th>Status</th><th>Completion</th></tr><tr><td>Building A</td><td>Complete</td><td>100%</td></tr><tr><td>Building B</td><td>Complete</td><td>100%</td></tr><tr><td>Building C</td><td>Commissioning</td><td>90%</td></tr><tr><td>Building D</td><td>Installation</td><td>55%</td></tr><tr><td>Building E</td><td>Not started</td><td>0%</td></tr></table><p>Energy savings from completed buildings: 28% reduction in electricity consumption. Projected annual savings: EGP 450,000.</p>",
            "Approve Phase 2 budget for Buildings D and E completion",
            "Phase 2 budget: EGP 1.2M (Buildings D & E)",
            skipApprovals: true);
        SH(r13, ReportStatus.Draft, ReportStatus.Approved, amrBaibars, 32, "Report approved immediately (approval cycle skipped)");

        // ── Security ──
        var r14 = R("Campus Security Assessment — Q4 2025",
            ReportType.Detailed, ReportStatus.Approved,
            hossamBadawy, security, 30,
            "<h3>Security Assessment</h3><p>Comprehensive Q4 security review covering access control, incident response, and surveillance systems across campus.</p><h4>Key Metrics</h4><ul><li>Security incidents reported: 23 (down 15% from Q3)</li><li>Average response time: 3.2 minutes (target: 5 minutes)</li><li>CCTV coverage: 92% of campus areas (up from 85%)</li><li>Access card violations: 45 (unauthorized entry attempts)</li></ul><h4>Incident Breakdown</h4><p>Theft: 3, Vandalism: 2, Unauthorized access: 8, Medical emergencies: 6, Safety hazards: 4.</p>",
            "Install additional CCTV cameras in parking areas B and C",
            "12 HD cameras with night vision (EGP 96,000), installation labor (EGP 24,000)",
            skipApprovals: true);
        SH(r14, ReportStatus.Draft, ReportStatus.Approved, hossamBadawy, 29, "Report approved immediately (approval cycle skipped)");

        var r15 = R("Emergency Evacuation Drill Results — December 2025",
            ReportType.Detailed, ReportStatus.Draft,
            abdallahRamzy, security, 4,
            "<h3>Evacuation Drill</h3><p>Draft report on campus-wide emergency evacuation drill conducted December 15, 2025. Preliminary data: 2,450 students and 380 staff evacuated from 8 buildings. Draft — awaiting final timing data from Building E and F fire marshals.</p>");

        // ── Agriculture ──
        var r16 = R("Campus Grounds Maintenance & Landscaping Report — Q4",
            ReportType.Detailed, ReportStatus.Submitted,
            monaFarid, agriculture, 14,
            "<h3>Grounds Maintenance</h3><p>Q4 grounds maintenance completed on schedule. 85,000 sqm of green areas maintained. New plantings: 120 trees (native species), 2,500 sqm of seasonal flowers.</p><h4>Irrigation</h4><p>Smart irrigation system installed in Zone A (15,000 sqm). Water consumption reduced by 32% compared to traditional sprinkler system. Full campus rollout proposed for FY2026.</p><h4>Pest Management</h4><p>Integrated pest management program maintained. Zero pesticide use in student-accessible areas. Organic composting program produced 4.5 tonnes of compost from campus green waste.</p>",
            "Approve smart irrigation expansion to Zones B, C, and D",
            "Smart irrigation system expansion: EGP 180,000 (3 zones)");
        SH(r16, ReportStatus.Draft, ReportStatus.Submitted, monaFarid, 13);

        // ════════════════════════════════════════════════════════════
        //  L2 DETAILED REPORTS — HR
        // ════════════════════════════════════════════════════════════

        // ── Recruitment ──
        var r17 = R("Faculty & Staff Hiring Progress — Spring 2026 Cycle",
            ReportType.Detailed, ReportStatus.Approved,
            daliaElMainouny, recruitment, 28,
            "<h3>Hiring Progress</h3><p>Spring 2026 recruitment cycle: 32 positions advertised, 24 filled (75%). Remaining 8 positions: 3 faculty (CS, Engineering), 3 administrative, 2 technical.</p><h4>Pipeline</h4><ul><li>Total applications received: 487</li><li>Shortlisted candidates: 96</li><li>Interviews conducted: 72</li><li>Offers extended: 28</li><li>Offers accepted: 24 (86% acceptance rate)</li></ul><p>Average time-to-hire: 38 days (target: 45). Key challenge: competing with private sector for IT specialists.</p>",
            "Approve signing bonuses for critical IT positions",
            "Signing bonus pool: EGP 150,000 (3 positions × EGP 50,000)",
            skipApprovals: true);
        SH(r17, ReportStatus.Draft, ReportStatus.Approved, daliaElMainouny, 27, "Report approved immediately (approval cycle skipped)");

        var r18 = R("Job Fair Outcomes — January 2026",
            ReportType.Detailed, ReportStatus.Submitted,
            mohamedAbdelWahab, recruitment, 6,
            "<h3>Job Fair Report</h3><p>Annual university job fair held January 22-23, 2026. 52 employers participated (up from 43 last year). 890 students attended across both days.</p><h4>Results</h4><ul><li>On-site interviews: 245</li><li>Preliminary offers: 67</li><li>Internship placements: 134</li></ul><p>Top sectors: Technology (32%), Financial Services (24%), Healthcare (16%), Manufacturing (12%).</p>");
        SH(r18, ReportStatus.Draft, ReportStatus.Submitted, mohamedAbdelWahab, 5);

        // ── Compensation & Benefits ──
        var r19 = R("Salary Benchmarking Study — 2025",
            ReportType.Detailed, ReportStatus.FeedbackRequested,
            aymanRahmou, compBenefits, 22,
            "<h3>Benchmarking Study</h3><p>Completed salary benchmarking against 12 peer institutions and 8 private sector competitors. Findings:</p><ul><li>Faculty salaries: 5-8% below market median (varies by rank)</li><li>Administrative staff: within 3% of market median</li><li>IT staff: 12-18% below market (critical gap)</li><li>Senior leadership: within 2% of market median</li></ul><p>Recommended adjustment: 6% across-the-board increase for faculty, 15% targeted increase for IT roles.</p>",
            "Present salary adjustment proposal to Top Level Committee",
            "Annual budget impact: EGP 2.8M (faculty) + EGP 450,000 (IT targeted)");
        SH(r19, ReportStatus.Draft, ReportStatus.Submitted, aymanRahmou, 21);
        SH(r19, ReportStatus.Submitted, ReportStatus.FeedbackRequested, aymanRahmou, 18,
            "Need to add retention risk analysis — how many staff have we lost to competitors in the last 12 months? Also add comparison with government pay scales.");

        // ── Personnel ──
        var r20 = R("Staff Turnover Analysis — 2025",
            ReportType.Detailed, ReportStatus.Approved,
            salmaIbrahim, personnel, 30,
            "<h3>Turnover Analysis</h3><p>2025 staff turnover: 67 departures out of 850 total staff (7.9% turnover rate). Benchmark: 10% for education sector.</p><h4>Departure Reasons</h4><ul><li>Better compensation elsewhere: 28 (42%)</li><li>Career advancement: 15 (22%)</li><li>Relocation: 9 (13%)</li><li>Retirement: 8 (12%)</li><li>Other: 7 (11%)</li></ul><h4>Department Analysis</h4><p>Highest turnover: IT Department (15.2%), Administrative Services (9.8%). Lowest: Academic departments (4.3%), Senior management (2.1%).</p>",
            "Implement IT staff retention program with competitive pay adjustments",
            "IT retention bonus program: EGP 200,000/year");
        SH(r20, ReportStatus.Draft, ReportStatus.Submitted, salmaIbrahim, 29);
        Approve(r20, UByEmail("sherif.naguib@org.edu")!, 28, "Clear analysis. IT turnover is alarming.");
        Approve(r20, UByEmail("amany.lotfy@org.edu")!, 27);
        SH(r20, ReportStatus.Submitted, ReportStatus.Approved, salmaIbrahim, 26, "All members approved.");

        // ── Confidential Report ──
        var r21 = R("Confidential: Staff Disciplinary Investigation — Admin Dept",
            ReportType.Detailed, ReportStatus.Approved,
            salmaIbrahim, personnel, 25,
            "<h3>Investigation Report</h3><p>Investigation into procurement irregularities in the Administrative Services department (Case #HR-2025-042). Findings indicate procedural non-compliance by 2 staff members — no evidence of fraud or financial loss.</p><p>Root cause: inadequate training on updated procurement guidelines (revised Q2 2025). Both staff members had not attended mandatory compliance training.</p><p>Recommended actions: mandatory compliance retraining, updated procurement approval workflows, supervisor oversight enhancement.</p>",
            "Implement enhanced procurement approval workflows and mandatory compliance training",
            confidential: true, skipApprovals: true);
        SH(r21, ReportStatus.Draft, ReportStatus.Approved, salmaIbrahim, 24, "Report approved immediately (approval cycle skipped)");

        // ════════════════════════════════════════════════════════════
        //  L1 SUMMARY REPORTS
        // ════════════════════════════════════════════════════════════

        var r22 = R("Academic Quality & Accreditation — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Approved,
            ghadirNassar, aqa, 22,
            "<h3>AQA Directorate Summary</h3><p>The Academic Quality & Accreditation directorate delivered strong Q4 results across all 3 functions:</p><ul><li><b>Curriculum:</b> 18 program reviews completed, 4 courses flagged for remediation. New Data Science Minor proposal in review.</li><li><b>Probation & Mentoring:</b> Probation cases down 20% from Q3. Peer tutoring program showing 72% success rate.</li><li><b>Teaching & Evaluation:</b> Faculty evaluation completed — 74% rated Good or Excellent. Professional development plans for underperformers initiated.</li></ul><p>Aggregate recommendation: expand peer tutoring program and mandate teaching workshops for low-scoring faculty.</p>",
            "Approve peer tutoring expansion and mandatory teaching workshops",
            "Combined budget: EGP 85,000 (tutoring rooms) + EGP 25,000 (workshops)");
        SH(r22, ReportStatus.Draft, ReportStatus.Submitted, ghadirNassar, 21);
        Approve(r22, hananMostafa, 20, "Accurate summary of our committee work.");
        Approve(r22, tarekAbdelFattah, 19);
        Approve(r22, nohaElSayed, 18);
        SH(r22, ReportStatus.Submitted, ReportStatus.Approved, ghadirNassar, 17, "All members approved.");

        var r23 = R("Student Activities — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Approved,
            sherineSalamy, studentActivities, 18,
            "<h3>Student Activities Summary</h3><p>Q4 highlights across 4 committees:</p><ul><li><b>Music:</b> Annual concert planned (March 15). 320 tickets sold in first week. EGP 25K sponsorship secured.</li><li><b>Theater:</b> 3 productions scheduled for Spring season. Budget proposal under review.</li><li><b>Sports:</b> Outstanding championship results — 3 first-place finishes (best in a decade). Student-athlete GPA above university mean.</li><li><b>AWG:</b> 7 community service events, 280 volunteers, 1,120 hours contributed.</li></ul><p>Student engagement index: 67% participation rate (up from 58% in Q3).</p>",
            "Approve sports facility upgrade (pool + basketball court) and theater season budget");
        SH(r23, ReportStatus.Draft, ReportStatus.Submitted, sherineSalamy, 17);
        Approve(r23, ohoudKhadr, 16);
        Approve(r23, laylaHassan, 15);
        Approve(r23, yehiaRazzaz, 14);
        SH(r23, ReportStatus.Submitted, ReportStatus.Approved, sherineSalamy, 13, "All members approved.");

        var r24 = R("Admission — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Submitted,
            sherineKhalil, admission, 15,
            "<h3>Admission Directorate Summary</h3><p>Q4 admission cycle successfully concluded:</p><ul><li><b>Marketing & Outreach:</b> Campaign reached 45,000 prospects. Social media ROI 7x print. 3,150 applications generated (+18% YoY).</li><li><b>Admission Services:</b> 91.7% processing rate. Average 4.2 days per application. Online portal reduced phone inquiries 35%.</li><li><b>Admission Office:</b> Enrollment verification audit: 98.2% full compliance. 4 international credential cases pending.</li></ul><p>Overall admission yield: 74% (target: 70%). Recommend digital channel budget reallocation and automated document verification.</p>",
            "Approve digital budget shift and automated verification system");
        SH(r24, ReportStatus.Draft, ReportStatus.Submitted, sherineKhalil, 14);

        var r25 = R("Campus Administration — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Approved,
            mohamedIbrahim, campusAdmin, 20,
            "<h3>Campus Admin Summary</h3><p>Q4 operational highlights:</p><ul><li><b>Facility Management:</b> HVAC Phase 1 at 85%, energy savings of 28% in completed buildings. Phase 2 budget requested.</li><li><b>Security:</b> Incidents down 15%. Response time 3.2 min. CCTV coverage up to 92%.</li><li><b>Agriculture:</b> Smart irrigation pilot reduced water usage 32%. Full campus rollout proposed.</li></ul><p>Total deferred maintenance backlog reduced by EGP 1.8M this quarter. Campus condition index improved from 3.2 to 3.5.</p>",
            "Approve HVAC Phase 2, parking CCTV expansion, and smart irrigation rollout",
            "Combined CapEx: EGP 1.2M (HVAC) + EGP 120K (CCTV) + EGP 180K (irrigation)");
        SH(r25, ReportStatus.Draft, ReportStatus.Submitted, mohamedIbrahim, 19);
        Approve(r25, amrBaibars, 18);
        Approve(r25, ibrahimKhalil, 17);
        Approve(r25, hossamBadawy, 16);
        Approve(r25, monaFarid, 15);
        SH(r25, ReportStatus.Submitted, ReportStatus.Approved, mohamedIbrahim, 14, "All members approved.");

        var r26 = R("Human Resources — Q4 2025 Summary",
            ReportType.Summary, ReportStatus.Submitted,
            radwaSelim, hr, 12,
            "<h3>HR Directorate Summary</h3><p>Q4 HR performance:</p><ul><li><b>Recruitment:</b> 75% of positions filled (24/32). 86% offer acceptance rate. IT positions remain challenging.</li><li><b>Compensation:</b> Benchmarking study completed — faculty 5-8% below market, IT gap 12-18%. Adjustment proposal being revised per feedback.</li><li><b>Personnel:</b> 7.9% annual turnover (below 10% benchmark). IT turnover at critical 15.2% level.</li></ul><p>Strategic priority: address IT compensation gap to reduce turnover and support digital transformation initiatives.</p>",
            "Approve IT retention program and targeted salary adjustments",
            "IT retention: EGP 200K/year + signing bonuses: EGP 150K");
        SH(r26, ReportStatus.Draft, ReportStatus.Submitted, radwaSelim, 11);

        // ════════════════════════════════════════════════════════════
        //  L0 EXECUTIVE SUMMARIES — Top Level Committee
        // ════════════════════════════════════════════════════════════

        var r27 = R("Q4 2025 Institutional Performance Report",
            ReportType.ExecutiveSummary, ReportStatus.Submitted,
            ahmedMansour, topLevel, 8,
            "<h3>Institutional Performance — Q4 2025</h3><p>The institution demonstrated strong performance across all 5 directorates in Q4 2025.</p><h4>Key Metrics</h4><table><tr><th>Metric</th><th>Target</th><th>Actual</th></tr><tr><td>Student Enrollment</td><td>2,200</td><td>2,340 (106%)</td></tr><tr><td>Faculty Positions Filled</td><td>32</td><td>24 (75%)</td></tr><tr><td>Infrastructure Uptime</td><td>95%</td><td>97%</td></tr><tr><td>Student Engagement</td><td>60%</td><td>67%</td></tr><tr><td>Staff Turnover</td><td><10%</td><td>7.9%</td></tr></table><h4>Escalated Items Requiring Decision</h4><ol><li>IT compensation gap — 12-18% below market, driving 15.2% IT turnover</li><li>HVAC Phase 2 + sports facilities + irrigation — combined CapEx EGP 1.7M</li><li>Faculty salary adjustment — 6% across-the-board, annual impact EGP 2.8M</li><li>Sports championship facility upgrades — EGP 275K</li></ol>",
            "Schedule executive budget review for all escalated CapEx and compensation items");
        SH(r27, ReportStatus.Draft, ReportStatus.Submitted, ahmedMansour, 7);

        var r28 = R("Q3 2025 Institutional Report (Archived)",
            ReportType.ExecutiveSummary, ReportStatus.Summarized,
            moustafaFouad, topLevel, 95,
            "<h3>Q3 2025 Summary</h3><p>Archived. This report covers Q3 2025 institutional performance metrics across all directorates. Superseded by Q4 2025 report. Key Q3 metrics: enrollment at 98% target, infrastructure projects on schedule, positive external audit results.</p>");
        SH(r28, ReportStatus.Draft, ReportStatus.Submitted, moustafaFouad, 94);
        SH(r28, ReportStatus.Submitted, ReportStatus.Approved, chairman, 90, "Approved for archive.");
        SH(r28, ReportStatus.Approved, ReportStatus.Summarized, ahmedMansour, 60, "Archived per retention policy.");

        // ── Save all reports ──
        context.Reports.AddRange(reports);
        await context.SaveChangesAsync();

        // ── Source Links ──
        // r22 (AQA Summary) sources: r1 (Curriculum), r3 (Probation), r4 (Teaching)
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r22.Id, SourceReportId = r1.Id, Annotation = "Curriculum review findings", CreatedAt = r22.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r22.Id, SourceReportId = r3.Id, Annotation = "Probation and mentoring outcomes", CreatedAt = r22.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r22.Id, SourceReportId = r4.Id, Annotation = "Faculty evaluation results", CreatedAt = r22.CreatedAt });

        // r23 (Student Activities Summary) sources: r6 (Music), r8 (Sports)
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r23.Id, SourceReportId = r6.Id, Annotation = "Annual concert planning report", CreatedAt = r23.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r23.Id, SourceReportId = r8.Id, Annotation = "Championship results", CreatedAt = r23.CreatedAt });

        // r24 (Admission Summary) sources: r10 (Marketing), r11 (Adm Services)
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r24.Id, SourceReportId = r10.Id, Annotation = "Recruitment campaign results", CreatedAt = r24.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r24.Id, SourceReportId = r11.Id, Annotation = "Application processing metrics", CreatedAt = r24.CreatedAt });

        // r25 (Campus Admin Summary) sources: r13 (Facility), r14 (Security)
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r25.Id, SourceReportId = r13.Id, Annotation = "HVAC upgrade progress", CreatedAt = r25.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r25.Id, SourceReportId = r14.Id, Annotation = "Security assessment results", CreatedAt = r25.CreatedAt });

        // r26 (HR Summary) sources: r17 (Recruitment), r20 (Personnel)
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r26.Id, SourceReportId = r17.Id, Annotation = "Hiring progress data", CreatedAt = r26.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r26.Id, SourceReportId = r20.Id, Annotation = "Staff turnover analysis", CreatedAt = r26.CreatedAt });

        // r27 (Institutional Summary) sources: r22 (AQA), r23 (Student), r25 (Campus)
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r27.Id, SourceReportId = r22.Id, Annotation = "Academic Quality directorate summary", CreatedAt = r27.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r27.Id, SourceReportId = r23.Id, Annotation = "Student Activities directorate summary", CreatedAt = r27.CreatedAt });
        sourceLinks.Add(new ReportSourceLink { SummaryReportId = r27.Id, SourceReportId = r25.Id, Annotation = "Campus Administration directorate summary", CreatedAt = r27.CreatedAt });

        context.ReportSourceLinks.AddRange(sourceLinks);
        context.ReportStatusHistories.AddRange(histories);
        context.ReportApprovals.AddRange(approvals);
        await context.SaveChangesAsync();
    }
}
