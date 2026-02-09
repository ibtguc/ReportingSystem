# ORS Demo Guide — Q4 2025 Institutional Reporting Cycle

This guide walks through a complete system demo using the pre-seeded data. The demo covers the full reporting, summarization, feedback, directive, and meeting workflow across 6 user personas at different organizational levels.

---

## Quick Start

1. Delete the database to re-seed: `rm -f ReportingSystem/db/reporting.db`
2. Run the application: `dotnet run --project ReportingSystem`
3. Open `https://localhost:5001` (or the configured URL)
4. Log in using any email listed below (magic link authentication — no password needed)

---

## Demo User Accounts

| User | Email | Role | Level |
|------|-------|------|-------|
| Dr. Hassan El-Sayed | `h.elsayed@org.edu` | Chairman | L0 |
| Nadia Kamel | `n.kamel@org.edu` | Chief of Staff (CO Rank 1) | L0 |
| Prof. Amira Shalaby | `a.shalaby@org.edu` | GS — Academic Affairs | L0 |
| Dr. Mariam Fawzy | `m.fawzy@org.edu` | GS — Technology & Innovation | L0 |
| Dr. Farid Zaki | `f.zaki@org.edu` | Director, Academic Programs | L1 |
| Dr. Yasmin Farouk | `y.farouk@org.edu` | Director, IT Infrastructure | L1 |
| Hesham Farag | `h.farag@org.edu` | Director, Finance & Accounting | L1 |
| Dr. Amal Fathi | `a.fathi@org.edu` | Director, Student Affairs | L1 |
| Dr. Lamia Refaat | `l.refaat@org.edu` | Curriculum Development (L2 member) | L2 |
| Dr. Sonia Guirguis | `s.guirguis@org.edu` | Examination & Assessment (L2 member) | L2 |
| Gamal Reda | `g.reda@org.edu` | Internal Audit Director | L1 |
| System Administrator | `admin@org.edu` | SystemAdmin | — |

---

## Demo Scenario Overview

The demo data simulates a complete **Q4 2025 quarterly reporting cycle** across a 5-sector, 350+ person educational institution. The data includes:

- **30 Reports** at various stages (Draft through Archived), spanning L3 process reports up to L0 executive summaries
- **15 Directives** including a 4-level chain from Chairman to L2, corrective actions, urgent cybersecurity directive, and overdue items
- **8 Meetings** (finalized, in-progress, scheduled, cancelled) with agenda items, decisions, and action items
- **Confidentiality markings** on sensitive items with access grants
- **25+ Notifications** across multiple users
- **30+ Audit log entries** tracking key system events

---

## Step-by-Step Demo Walkthrough

### Act 1: The L2 Function Member's View (Report Author)

**Login as: Dr. Lamia Refaat** (`l.refaat@org.edu`)

This demonstrates the perspective of a committee member who writes detailed reports.

1. **Dashboard** — View personal dashboard showing assigned committees and activity
2. **Reports > All Reports** — See the list of reports. Note the "Q4 Course Design Review" report by Dr. Refaat in **Approved** status
3. **Click the report** — View the full report with:
   - Rich HTML body content describing the 12-course review
   - Suggested Action, Needed Resources, and Needed Support sections
   - **Status History tab** — See the full lifecycle: Draft → Submitted → UnderReview → Approved
   - Each status change shows who made it and when
4. **Reports > New Report** — Show the report creation form:
   - Template picker (if templates are seeded)
   - Rich text editor (Quill) for body content
   - Committee selector
   - Optional sections (Suggested Action, Resources, etc.)
5. **Meetings > Action Items** — Show that Dr. Refaat has a **completed** action item: "Complete ABET realignment for ME301, EE405, CE210" from the Academic Programs review meeting

**Key Talking Points:**
- Bottom-up reporting: L2 members create detailed reports that feed into summaries
- Rich text editing with Quill editor
- Full status audit trail for every report

---

### Act 2: The L1 Director's View (Summarizer & Meeting Chair)

**Login as: Dr. Farid Zaki** (`f.zaki@org.edu`)

This shows how a directorate head reviews reports, creates summaries, and runs meetings.

1. **Reports > All Reports** — Filter by Academic Programs committee. See both:
   - 4 detailed L3 reports (Course Design, Faculty Recruitment, Exam Logistics, E-Learning)
   - 1 summary report: "Academic Programs — Q4 2025 Summary Report" in **Approved** status
2. **Click the Summary Report** — Note:
   - Report type is "Summary" (not Detailed)
   - Body aggregates key findings from all 4 source reports
   - Combined budget request (EGP 290,000) escalates to next level
3. **Reports > Create Summary** — Show the summary creation workflow:
   - Select source reports to summarize
   - System links sources via ReportSourceLink
4. **Meetings > All Meetings** — See the "Academic Programs Directorate — Q4 Review Meeting" in **Finalized** status
5. **Click the meeting** — Explore:
   - **Agenda** — 3 items with allocated times and presenters
   - **Attendees** — 5 members, all confirmed
   - **Decisions** — 2 decisions (ABET realignment direction + digital exam expansion approval)
   - **Action Items** — 2 items, both completed and verified
   - **Minutes** — Full minutes content
6. **Directives > All Directives** — See the "CS Faculty Recruitment" directive chain:
   - D2: "CS Faculty Recruitment — Academic Programs Action" (Closed)
   - D3: "Prepare CS Faculty Job Specifications" (Implemented)
   - Note the parent-child chain and forwarding annotations

**Key Talking Points:**
- Summarization: L1 directors aggregate L3 reports into directorate summaries
- Meeting workflow: schedule → conduct → minutes → review → finalize
- Directive chains flow top-down with annotations at each level

---

### Act 3: The Feedback Loop (Report Revision)

**Login as: Dr. Yasmin Farouk** (`y.farouk@org.edu`)

This demonstrates the feedback and revision cycle.

1. **Reports > All Reports** — Find two related reports:
   - "Network Infrastructure Upgrade Progress" — Status: **FeedbackRequested**
   - "Network Infrastructure Upgrade Progress (Revised)" — Status: **Approved**
2. **Click the FeedbackRequested report** — View status history:
   - Draft → Submitted → UnderReview → **FeedbackRequested**
   - The feedback comment reads: "Need more detail on budget impact of the 3-week delay"
3. **Click the Revised report** — Note:
   - Version: 2 (shows it's a revision)
   - Links to original report
   - Status history shows it went through review and was approved
   - Special Remarks: "Revised per GS Technology feedback — added cost variance analysis"
4. **Notifications** — See unread notifications including:
   - Action item: "Submit zero-trust architecture proposal"
   - Urgent directive: "Ransomware Preparedness assessment"
5. **Directives > All Directives** — See the urgent ransomware directive (InProgress)

**Key Talking Points:**
- Reports can be sent back for revision with specific feedback
- Revised reports link to originals, preserving full history
- Version tracking maintains complete audit trail

---

### Act 4: The GS / Sector Head View (Cross-Directorate Oversight)

**Login as: Prof. Amira Shalaby** (`a.shalaby@org.edu`)

This shows the General Secretary's cross-directorate view.

1. **Dashboard** — High-level overview of Academic Affairs sector activity
2. **Reports > All Reports** — See reports from all Academic directorates:
   - Academic Programs summary (Approved)
   - Research & Graduate Studies summary (Approved)
   - QA audit results (Approved)
   - The sector executive summary she authored (Approved by Chairman)
3. **Click "Academic Affairs Sector — Q4 Executive Summary"** — This is the L1→L0 summary:
   - Aggregates data from 3 directorate summaries
   - Highlights risks and escalations (CS faculty shortage)
   - Budget request forwarded to Chairman
   - Source links show the 3 source reports
4. **Directives > All Directives** — See directives issued and received:
   - Issued D2 (CS Faculty Recruitment to Academic Programs) — now Closed
   - Issued D8 (Exam System Procurement approval) — Closed
   - Received D13 (Research Collaboration Framework) — just Issued
5. **Search** — Search for "faculty recruitment" to demonstrate cross-content search

**Key Talking Points:**
- GS sees all activity in their sector across multiple directorates
- Summary reports chain upward: L3 → L2 → L1 → L0
- Directives and feedback flow back down

---

### Act 5: The Chairman & Chairman's Office View (Top-Level Decisions)

**Login as: Dr. Hassan El-Sayed** (`h.elsayed@org.edu`)

This is the top-level executive view.

1. **Dashboard** — Institutional overview across all 5 sectors
2. **Analytics** — Show the Analytics dashboard:
   - **Organization Overview** cards — Total reports, directives, meetings, users, committees
   - **Monthly Activity Trends** — Bar chart showing 12-month activity
   - **Status Distribution** — Doughnut charts for reports, directives, and meetings
   - **Compliance Metrics** — Directive on-time rates, action item completion
   - **Committee Activity Table** — Per-committee metrics
3. **Reports > All Reports** — See all submitted sector executive summaries:
   - Academic Affairs (Approved)
   - Technology & Innovation (Approved)
   - Finance & Governance (Submitted — pending review)
   - Student Experience (Submitted — pending review)
   - Administration & Operations (UnderReview)
   - Institutional Performance Report (Approved)
4. **Click the Institutional Performance Report** — The comprehensive Q4 executive summary:
   - Performance table with targets vs. actuals
   - Three items requiring Chairman's decision
   - Source links to sector reports
5. **Directives > All Directives** — Chairman-issued directives:
   - D1: CS Faculty Recruitment (Closed — completed successfully)
   - D5: Ransomware Preparedness (Acknowledged — in progress)
   - D9: Clinical Psychologist Position (Just Issued)
6. **Directives > Track Overdue** — Show the overdue directive:
   - "Network Upgrade Phase 3 Planning" — deadline passed 5 days ago
7. **Meetings > All Meetings** — See:
   - Q4 Quarterly Review (Finalized with decisions)
   - FY2026 Budget Workshop (Scheduled for 14 days out)
8. **Notifications** — Show unread notifications for pending sector reports

**Now switch to: Nadia Kamel** (`n.kamel@org.edu`) — Chairman's Office

9. **Show the CO perspective** — Same top-level visibility
10. **Confidentiality > Navigate to a confidential item** — CO with Rank 1 can see all confidential items

**Key Talking Points:**
- Chairman sees the full institutional picture via executive summaries
- Analytics dashboard provides data-driven oversight
- Directive chains track compliance from issuance to closure
- Overdue tracking ensures accountability

---

### Act 6: Confidentiality & Administration

**Login as: System Administrator** (`admin@org.edu`)

This demonstrates confidentiality controls and administrative features.

1. **Administration > Audit Log** — Browse the full audit trail:
   - Logins, report status changes, directive events, meeting lifecycle
   - Filter by action type, user, or date range
2. **Administration > Users** — View all 351 users with roles and status
3. **Administration > Database Backups** — Show backup management
4. **Administration > Report Templates** — Show configured report templates
5. **Administration > Knowledge Base** — Show KB admin:
   - Categories management
   - Bulk Index Content button — indexes approved reports and closed directives
6. **Knowledge Base** (main nav) — Browse knowledge articles:
   - After bulk indexing, approved non-confidential reports appear as articles
   - Search, category filter, and view counts
7. **Confidentiality Demo:**
   - Find "Staff Disciplinary Investigation" report — it's marked **Confidential**
   - As SystemAdmin, full access is available
   - **Login as a regular L2 user** (e.g., `l.refaat@org.edu`) — the report should NOT appear in their report list
   - **Login as GS Finance** (`a.soliman@org.edu`) — the report IS visible because of the explicit access grant

**Key Talking Points:**
- Full audit trail for compliance and accountability
- Confidentiality markings restrict access based on hierarchy level
- Explicit access grants allow sharing with specific users
- Knowledge base auto-indexes approved organizational content

---

## Data Summary for Reference

### Reports by Status
| Status | Count | Examples |
|--------|-------|---------|
| Approved | 18 | Course Design Review, Faculty Recruitment, Sector Summaries |
| Submitted | 4 | Financial Statements, Career Fair, KPI Dashboard, Sector Reports |
| UnderReview | 2 | Budget Framework, Admin Sector Summary |
| FeedbackRequested | 1 | Network Upgrade (original) |
| Draft | 2 | Internal Controls, Student Clubs |
| Archived | 1 | Q3 Institutional Report |

### Directives by Status
| Status | Count | Examples |
|--------|-------|---------|
| Closed | 4 | CS Faculty chain (D1, D2), Exam System (D8), Budget Notice (D10) |
| InProgress | 4 | Procurement Controls (D4), Ransomware IT (D6), Network Phase 3 (D11), Financial Controls (D14) |
| Implemented | 1 | CS Job Specs (D3) |
| Acknowledged | 2 | Ransomware (D5), HR Performance Review (D15) |
| Delivered | 2 | Energy Feedback (D7), Solar Study (D12) |
| Issued | 2 | Clinical Psychologist (D9), Research Collaboration (D13) |

### Meetings by Status
| Status | Count | Examples |
|--------|-------|---------|
| Finalized | 3 | Q4 Quarterly Review, Academic Programs Review, QA Audit Review |
| MinutesReview | 1 | Emergency Security Meeting |
| MinutesEntry | 1 | Finance Monthly Close |
| Scheduled | 2 | Student Affairs Planning, Budget Workshop |
| Cancelled | 1 | Cybersecurity Standup |

### Key Directive Chain (D1 → D2 → D3)
```
Chairman → Top Level: "Prioritize CS Faculty Recruitment" (Closed)
  └→ GS Academic → Academic Programs: "CS Faculty — Action" (Closed)
      └→ Dir Academic Programs → Curriculum Dev: "Prepare Job Specs" (Implemented)
```

### Report Summarization Chain
```
L3 Process Reports:
  ├── Course Design Review (Approved)
  ├── Faculty Recruitment (Approved)
  ├── Exam Logistics (Approved)
  └── E-Learning Utilization (Approved)
      ↓ summarized into
L2 Function Summary:
  └── Academic Programs Q4 Summary (Approved)
      ↓ summarized into
L1 Sector Summary:
  └── Academic Affairs Sector Q4 Executive Summary (Approved)
      ↓ summarized into
L0 Institutional Report:
  └── Q4 2025 Institutional Performance Report (Approved)
```

---

## Tips for a Successful Demo

1. **Start from the bottom up** — Show L2 member creating reports first, then zoom out to L1 summaries, L0 executive view. This builds understanding of the reporting hierarchy.

2. **Use the feedback loop** — The Network Upgrade report pair (FeedbackRequested + Revised) is the best example of the quality assurance workflow.

3. **Follow the directive chain** — The CS Faculty Recruitment directive (D1→D2→D3) demonstrates how instructions propagate through the hierarchy with annotations.

4. **Highlight the meeting lifecycle** — The Q4 Quarterly Review meeting shows the complete flow: agenda → attendance → discussion notes → decisions → action items → minutes confirmation → finalization.

5. **Show analytics last** — The Analytics dashboard is most impressive after the audience understands what data feeds into it.

6. **Confidentiality is a differentiator** — The access control model (hierarchy-based + explicit grants) is sophisticated. Show it with the disciplinary report.

7. **Keep browser tabs open** — Pre-open tabs for each user persona to enable quick switching during the demo.
