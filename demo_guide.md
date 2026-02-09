# ORS Demo Guide — Organizational Reporting System

This guide walks through the system's organizational structure and core workflows. The system starts with a fully seeded organization (users, committees, memberships) and empty transactional tables, ready for live data entry during the demo.

---

## Quick Start

1. Delete the database to re-seed: `rm -f ReportingSystem/db/reporting.db`
2. Run the application: `dotnet run --project ReportingSystem`
3. Open `https://localhost:5001` (or the configured URL)
4. Log in using any email listed below (magic link authentication — no password needed)

> **Optional — Enable Pre-Seeded Demo Data:**
> To start with 30 reports, 15 directives, 8 meetings, notifications, and audit log entries already populated, uncomment these two lines in `Program.cs` (around line 140):
> ```csharp
> logger.LogInformation("Seeding demo data...");
> await DemoDataSeeder.SeedAsync(context);
> ```
> Then delete `db/reporting.db` and restart. See [Appendix: Full Demo Data Walkthrough](#appendix-full-demo-data-walkthrough) for the detailed scenario guide.

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

## What's Pre-Seeded

The database starts with a fully populated organizational structure:

| Entity | Count | Description |
|--------|-------|-------------|
| Users | ~158 | Chairman, Chairman's Office (4), 5 GS, 19 Directors, ~100 L2/L3 staff, 3 admins |
| Committees | ~185 | Full hierarchy: 1 Top Level, 5 L0, 19 L1, ~65 L2, ~100 L3 |
| Memberships | ~500+ | Heads + Members for each committee |
| Shadow Assignments | 5 | GS-level shadow assignments |
| Report Templates | 5 | Default templates (Quarterly, Monthly, etc.) |
| Knowledge Categories | Default | Category hierarchy for knowledge base |

**Empty tables** (ready for live data entry): Reports, Directives, Meetings, Notifications, Audit Log, Confidentiality Markings, Access Grants, Knowledge Articles.

---

## Step-by-Step Demo Walkthrough

### Act 1: Exploring the Organization Structure

**Login as: System Administrator** (`admin@org.edu`)

1. **Organization > Committee Tree** — The new top-down visual tree:
   - Committees displayed as cards in horizontal layers by level (L0 → L1 → L2 → L3)
   - Each card shows the committee name, sector badge, head name, and member count
   - Click the **person icon button** on any card to toggle the staff member list
   - Each member name is a clickable link to their user details page
   - Use the **View**, **Edit**, and **Delete** icon buttons for CRUD operations
2. **Organization > Org Tree** — The nested collapsible tree view:
   - Expand/Collapse All button
   - Chairman and Chairman's Office section at the top
   - Stats cards: committees, users, memberships, shadow assignments
3. **Organization > Committees** — Table/list view:
   - Filter by Hierarchy Level (L0–L4) and Sector
   - See head names, member counts, parent committees
4. **Click a committee** (e.g., "Academic Programs Directorate") — Full details:
   - Head and member lists with roles
   - Sub-committees listing
   - Add/remove member functionality

**Key Talking Points:**
- Three views of the same data: visual tree, nested tree, filterable table
- 5-sector hierarchy: Academic Affairs, Administration, Technology, Finance, Student Experience
- ~185 committees with ~500+ membership assignments

---

### Act 2: Creating a Report (L2 Member Perspective)

**Login as: Dr. Lamia Refaat** (`l.refaat@org.edu`)

1. **Dashboard** — Personal dashboard showing assigned committees
2. **Reports > New Report** — Create a new report:
   - Select a **template** (e.g., "Quarterly Departmental Report")
   - Choose the **committee** (Curriculum Development)
   - Fill in the **title** and use the **Quill rich text editor** for the body
   - Add optional sections: Suggested Action, Needed Resources, Needed Support, Special Remarks
   - Save as **Draft** or **Submit** directly
3. **Reports > All Reports** — See the newly created report in the list
4. **Click the report** — View it with status history showing Draft/Submitted

**Key Talking Points:**
- Template-driven reports with configurable required fields
- Rich text editing with Quill editor
- Reports flow bottom-up through the hierarchy

---

### Act 3: Report Review & Summarization (L1 Director Perspective)

**Login as: Dr. Farid Zaki** (`f.zaki@org.edu`)

1. **Reports > All Reports** — See submitted reports from L2 members in the Academic Programs directorate
2. **Review a report** — Open a submitted report and change status:
   - Submitted → UnderReview → Approved (or FeedbackRequested for revision)
3. **Reports > Create Summary** — Demonstrate the summarization workflow:
   - Select multiple approved source reports
   - System creates a summary report linked to sources via ReportSourceLink
   - Summary aggregates key findings from subordinate reports
4. **Show the chain** — Click into a summary to see linked source reports

**Key Talking Points:**
- Summarization: L1 directors aggregate L2/L3 reports into directorate summaries
- Reports can be sent back for revision with specific feedback
- Source links create a traceable chain from detail to summary

---

### Act 4: Issuing Directives & Running Meetings

**Login as: Dr. Hassan El-Sayed** (`h.elsayed@org.edu`) or any GS/Director

1. **Directives > Issue Directive** — Create a directive:
   - Select type (Instruction, Approval, CorrectiveAction, Feedback, InformationNotice)
   - Set priority (Normal, High, Urgent)
   - Choose target committee and optional target user
   - Set deadline
   - Optionally link to a parent directive (for forwarding/chain)
2. **Directives > All Directives** — View the directive with its status (Issued)
3. **Login as the target** — Acknowledge, progress through statuses
4. **Directives > Track Overdue** — Show overdue tracking view (empty until deadlines pass)

5. **Meetings > Schedule Meeting** — Create a meeting:
   - Set committee, moderator, date/time, location
   - Add agenda items with time allocations and presenters
   - Invite attendees (RSVP workflow)
6. **Run the meeting lifecycle:**
   - Scheduled → InProgress → MinutesEntry (add discussion notes, decisions, action items)
   - MinutesReview (attendees confirm/abstain) → Finalized
7. **Meetings > Action Items** — Track assigned action items across meetings

**Key Talking Points:**
- 5 directive types, 3 priority levels, 7-status workflow
- Directives chain top-down with forwarding annotations
- Full meeting lifecycle with structured minutes, decisions, and action items

---

### Act 5: Analytics, Search & Knowledge Base

**Login as: Dr. Hassan El-Sayed** (`h.elsayed@org.edu`)

1. **Analytics** — Dashboard with Chart.js visualizations:
   - Organization overview cards with 30-day trend comparison
   - Monthly activity trends (bar chart)
   - Status distribution (doughnut charts)
   - Compliance metrics (on-time rates)
   - Committee activity table
   > Note: Charts populate as reports, directives, and meetings are created
2. **Search** — Unified search across all content types
3. **Knowledge > Browse** — Knowledge base with categories, search, and tags
4. **Administration > Knowledge Base** — Bulk index approved reports and closed directives into articles

**Key Talking Points:**
- Data-driven analytics dashboard for executive oversight
- Unified search spans reports, directives, meetings, and action items
- Knowledge base auto-indexes approved organizational content

---

### Act 6: Confidentiality & Administration

**Login as: System Administrator** (`admin@org.edu`)

1. **Administration > Users** — View all ~158 users with roles and status
2. **Administration > Report Templates** — View/edit 5 default templates
3. **Administration > Database Backups** — Backup management
4. **Administration > Audit Log** — Browse audit trail (populates as users take actions)

**Confidentiality Demo** (requires a report to exist):
1. Create or find a report, then navigate to **Confidentiality > Mark**
2. Mark it as confidential — access restricted by hierarchy
3. Grant explicit access to specific users
4. Login as different users to verify access control

**Key Talking Points:**
- Full audit trail for compliance and accountability
- Confidentiality markings restrict access based on hierarchy level
- Chairman's Office access is rank-based (Rank 1 sees all)
- Explicit access grants allow sharing with specific users

---

## Tips for a Successful Demo

1. **Start with the Committee Tree** — The visual tree view immediately shows the organizational scope and hierarchy.

2. **Create data live** — Walk through report creation, directive issuance, and meeting scheduling to demonstrate actual workflows rather than just viewing static data.

3. **Switch between user levels** — Show the same data from L2, L1, and L0 perspectives to demonstrate the hierarchical reporting chain. Keep browser tabs open for each persona.

4. **Follow a complete workflow** — Create a report at L2, review it at L1, summarize it, then issue a directive based on it.

5. **Show analytics after creating data** — The Analytics dashboard is most impressive after the audience has seen what data feeds into it.

6. **Confidentiality is a differentiator** — The access control model (hierarchy-based + explicit grants + Chairman's Office rank) is sophisticated.

---

## Appendix: Full Demo Data Walkthrough

To enable the full pre-seeded demo data (30 reports, 15 directives, 8 meetings, etc.), uncomment `DemoDataSeeder.SeedAsync(context)` in `Program.cs`, delete `db/reporting.db`, and restart.

The pre-seeded data simulates a complete **Q4 2025 quarterly reporting cycle** including:

| Data | Count | Highlights |
|------|-------|------------|
| Reports | 30 | L3 process → L2 function → L1 sector → L0 institutional summaries |
| Report Status Histories | ~100+ | Full lifecycle trails for each report |
| Report Source Links | ~10 | Summary-to-source linking |
| Directives | 15 | 4-level chain (D1→D2→D3), corrective actions, urgent, overdue |
| Directive Status Histories | ~40+ | Full status trails |
| Meetings | 8 | Finalized, MinutesReview, MinutesEntry, Scheduled, Cancelled |
| Agenda Items | ~20 | With presenters and discussion notes |
| Attendees | ~28 | RSVP and confirmation statuses |
| Decisions | ~9 | Approval, Direction, Resolution, Deferral types |
| Action Items | ~8 | Various statuses including completed and overdue |
| Confidentiality Markings | 2 | On report + directive |
| Access Grants | 3 | Explicit sharing |
| Notifications | ~25 | Across 10 users, mixed read/unread |
| Audit Log Entries | ~30 | Logins, status changes, searches |

### Key Demo Data Chains

**Directive Chain (D1 → D2 → D3):**
```
Chairman → Top Level: "Prioritize CS Faculty Recruitment" (Closed)
  └→ GS Academic → Academic Programs: "CS Faculty — Action" (Closed)
      └→ Dir Academic Programs → Curriculum Dev: "Prepare Job Specs" (Implemented)
```

**Report Summarization Chain:**
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

### Seeded Reports by Status
| Status | Count | Examples |
|--------|-------|---------|
| Approved | 18 | Course Design Review, Faculty Recruitment, Sector Summaries |
| Submitted | 4 | Financial Statements, Career Fair, KPI Dashboard |
| UnderReview | 2 | Budget Framework, Admin Sector Summary |
| FeedbackRequested | 1 | Network Upgrade (original) |
| Draft | 2 | Internal Controls, Student Clubs |
| Archived | 1 | Q3 Institutional Report |

### Seeded Directives by Status
| Status | Count | Examples |
|--------|-------|---------|
| Closed | 4 | CS Faculty chain (D1, D2), Exam System (D8), Budget Notice (D10) |
| InProgress | 4 | Procurement Controls (D4), Ransomware IT (D6), Network Phase 3 (D11) |
| Implemented | 1 | CS Job Specs (D3) |
| Acknowledged | 2 | Ransomware (D5), HR Performance Review (D15) |
| Delivered | 2 | Energy Feedback (D7), Solar Study (D12) |
| Issued | 2 | Clinical Psychologist (D9), Research Collaboration (D13) |

### Seeded Meetings by Status
| Status | Count | Examples |
|--------|-------|---------|
| Finalized | 3 | Q4 Quarterly Review, Academic Programs Review, QA Audit Review |
| MinutesReview | 1 | Emergency Security Meeting |
| MinutesEntry | 1 | Finance Monthly Close |
| Scheduled | 2 | Student Affairs Planning, Budget Workshop |
| Cancelled | 1 | Cybersecurity Standup |
