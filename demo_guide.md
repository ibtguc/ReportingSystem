# ORS Demo Guide — Organizational Reporting System

This guide walks through the system's organizational structure and core workflows. The system starts with a fully seeded organization (users, committees, memberships) and empty transactional tables, ready for live data entry during the demo.

---

## Quick Start

1. Delete the database to re-seed: `rm -f ReportingSystem/db/reporting.db`
2. Run the application: `dotnet run --project ReportingSystem`
3. Open `https://localhost:5001` (or the configured URL)
4. Log in using any email listed below (magic link authentication — no password needed)

---

## Demo User Accounts (57 users)

### Chairman & Chairman Office

| User | Email | Role | Notes |
|------|-------|------|-------|
| AM | `am@org.edu` | Chairman | Top authority |
| Ahmed Mansour | `ahmed.mansour@org.edu` | Chairman Office (Rank 1) | Co-head: Marketing & Outreach |
| Moustafa Fouad | `moustafa.fouad@org.edu` | Chairman Office (Rank 2) | |
| Marwa El Serafy | `marwa.elserafy@org.edu` | Chairman Office (Rank 3) | |
| Samia El Ashiry | `samia.elashiry@org.edu` | Chairman Office (Rank 4) | Co-head: Marketing & Outreach |

### Top Level Committee Heads (L0)

| User | Email | Also Heads |
|------|-------|------------|
| Mohamed Ibrahim | `mohamed.ibrahim@org.edu` | Campus Administration (L1) |
| Radwa Selim | `radwa.selim@org.edu` | HR (L1) |
| Ghadir Nassar | `ghadir.nassar@org.edu` | Academic Quality & Accreditation (L1) |
| Engy Galal | `engy.galal@org.edu` | — |
| Karim Salme | `karim.salme@org.edu` | — |
| Sherine Khalil | `sherine.khalil@org.edu` | Admission (L1), Admission Office (L2) |
| Sherine Salamony | `sherine.salamony@org.edu` | Student Activities (L1) |

### Named L2 Heads

| User | Email | Heads |
|------|-------|-------|
| Ohoud Khadr | `ohoud.khadr@org.edu` | Music |
| Yehia Razzaz | `yehia.razzaz@org.edu` | Sports, AWG |
| Amr Baibars | `amr.baibars@org.edu` | Facility Management (co-head) |
| Gen. Ibrahim Khalil | `ibrahim.khalil@org.edu` | Facility Management (co-head) |
| Dalia El Mainouny | `dalia.elmainouny@org.edu` | Recruitment |
| Ayman Rahmou | `ayman.rahmou@org.edu` | Compensation & Benefits |
| Salma Ibrahim | `salma.ibrahim@org.edu` | Personnel |

### Generated L2 Heads

| User | Email | Heads |
|------|-------|-------|
| Hanan Mostafa | `hanan.mostafa@org.edu` | Curriculum |
| Tarek Abdel Fattah | `tarek.abdelfattah@org.edu` | Probation & Mentoring |
| Noha El Sayed | `noha.elsayed@org.edu` | Teaching & Evaluation Standard |
| Layla Hassan | `layla.hassan@org.edu` | Theater |
| Ramy Shawky | `ramy.shawky@org.edu` | Admission Services |
| Hossam Badawy | `hossam.badawy@org.edu` | Security |
| Mona Farid | `mona.farid@org.edu` | Agriculture |

### L2 Committee Members

| Committee | Members |
|-----------|---------|
| Curriculum | Amira Soliman (`amira.soliman@org.edu`), Bassem Youssef (`bassem.youssef@org.edu`) |
| Probation & Mentoring | Fatma El Zahraa (`fatma.elzahraa@org.edu`), Omar Hashem (`omar.hashem@org.edu`) |
| Teaching & Evaluation | Yasmin Abdel Rahman (`yasmin.abdelrahman@org.edu`), Khaled Mostafa (`khaled.mostafa@org.edu`) |
| Music | Nadia Fawzy (`nadia.fawzy@org.edu`), Wael Abdel Meguid (`wael.abdelmeguid@org.edu`) |
| Theater | Rania Samir (`rania.samir@org.edu`), Hazem Ashraf (`hazem.ashraf@org.edu`) |
| Sports | Tamer El Naggar (`tamer.elnaggar@org.edu`), Dina Raafat (`dina.raafat@org.edu`) |
| AWG | Sahar El Gendy (`sahar.elgendy@org.edu`), Hassan Mahmoud (`hassan.mahmoud@org.edu`) |
| Marketing & Outreach | Lamia Youssef (`lamia.youssef@org.edu`), Mahmoud Farouk (`mahmoud.farouk@org.edu`) |
| Admission Services | Heba Abdel Aziz (`heba.abdelaziz@org.edu`), Nermeen Sami (`nermeen.sami@org.edu`) |
| Admission Office | Waleed Tantawy (`waleed.tantawy@org.edu`), Rana El Kholy (`rana.elkholy@org.edu`) |
| Facility Management | Mostafa Ragab (`mostafa.ragab@org.edu`), Samiha Adel (`samiha.adel@org.edu`) |
| Security | Abdallah Ramzy (`abdallah.ramzy@org.edu`), Yasser Galal (`yasser.galal@org.edu`) |
| Agriculture | Nashwa Ahmed (`nashwa.ahmed@org.edu`), Adel Ismail (`adel.ismail@org.edu`) |
| Recruitment | Mohamed Abdel Wahab (`mohamed.abdelwahab@org.edu`), Hala Mahmoud (`hala.mahmoud@org.edu`) |
| Compensation & Benefits | Essam Hamdy (`essam.hamdy@org.edu`), Iman El Batouty (`iman.elbatouty@org.edu`) |
| Personnel | Sherif Naguib (`sherif.naguib@org.edu`), Amany Lotfy (`amany.lotfy@org.edu`) |

### System Admin

| User | Email |
|------|-------|
| System Administrator | `admin@org.edu` |

---

## Committee Hierarchy (22 committees)

```
Top Level Committee (L0) — 7 heads
├── Academic Quality & Accreditation (L1) — Head: Ghadir Nassar
│   ├── Curriculum (L2) — Head: Hanan Mostafa
│   ├── Probation & Mentoring (L2) — Head: Tarek Abdel Fattah
│   └── Teaching and Evaluation Standard (L2) — Head: Noha El Sayed
├── Student Activities (L1) — Head: Sherine Salamony
│   ├── Music (L2) — Head: Ohoud Khadr
│   ├── Theater (L2) — Head: Layla Hassan
│   ├── Sports (L2) — Head: Yehia Razzaz
│   └── AWG (L2) — Head: Yehia Razzaz
├── Admission (L1) — Head: Sherine Khalil
│   ├── Marketing & Outreach (L2) — Co-heads: Ahmed Mansour + Samia El Ashiry
│   ├── Admission Services (L2) — Head: Ramy Shawky
│   └── Admission Office (L2) — Head: Sherine Khalil
├── Campus Administration (L1) — Head: Mohamed Ibrahim
│   ├── Facility Management (L2) — Co-heads: Amr Baibars + Gen. Ibrahim Khalil
│   ├── Security (L2) — Head: Hossam Badawy
│   └── Agriculture (L2) — Head: Mona Farid
└── HR (L1) — Head: Radwa Selim
    ├── Recruitment (L2) — Head: Dalia El Mainouny
    ├── Compensation & Benefits (L2) — Head: Ayman Rahmou
    └── Personnel (L2) — Head: Salma Ibrahim
```

---

## What's Pre-Seeded

| Entity | Count | Description |
|--------|-------|-------------|
| Users | 57 | 1 Chairman, 4 Chairman Office, 7 TLC heads, 7 named L2 heads, 7 generated L2 heads, 30 L2 members, 1 admin |
| Committees | 22 | 1 Top Level (L0), 5 L1, 16 L2 |
| Memberships | 80+ | Heads + Members for each committee |

**Empty tables** (ready for live data entry): Reports, Directives, Meetings, Notifications, Audit Log, Confidentiality Markings, Access Grants, Knowledge Articles.

---

## Report Lifecycle — Collective Approval

Reports follow a collective approval workflow where **all committee members** (except the author) must approve before a report advances:

```
Draft → Submitted → Approved → Summarized
              ↕
       FeedbackRequested
```

- **Draft** — Author is writing the report
- **Submitted** — Report sent for committee review; all members can approve individually
- **FeedbackRequested** — Any member can request feedback, resetting all approvals; author revises and resubmits
- **Approved** — All required members have approved (or head finalized after 3 days)
- **Summarized** — Report has been included in a summary report by the committee head

**Head Override:** If 3 days have passed since submission and not all members have approved, the committee head can finalize (force-approve) the report.

---

## Step-by-Step Demo Walkthrough

### Act 1: Exploring the Organization Structure

**Login as: System Administrator** (`admin@org.edu`)

1. **Organization > Committee Tree** — Visual tree of the hierarchy:
   - 22 committees displayed as cards in horizontal layers (L0 → L1 → L2)
   - Each card shows the committee name, head name, and member count
   - Click the **person icon** on any card to toggle the member list
   - Use **View**, **Edit**, and **Delete** buttons for CRUD operations
2. **Organization > Org Tree** — Nested collapsible tree view:
   - Expand/Collapse All button
   - Chairman and Chairman's Office section at the top
   - Stats cards: committees, users, memberships
3. **Organization > Committees** — Filterable table/list view:
   - Filter by Hierarchy Level (L0–L2) and Sector
   - See head names, member counts, parent committees
4. **Click a committee** (e.g., "Curriculum") — Full details:
   - Head and member lists with roles
   - Sub-committees listing
   - Add/remove member functionality

**Key Talking Points:**
- Three views of the same data: visual tree, nested tree, filterable table
- 22 committees across 3 hierarchy levels
- L1 members are the heads of corresponding L2 sub-committees

---

### Act 2: Creating a Report (L2 Member Perspective)

**Login as: Amira Soliman** (`amira.soliman@org.edu`) — Curriculum committee member

1. **Dashboard** — Personal dashboard showing assigned committees
2. **Reports > New Report** — Create a new report:
   - Choose the **committee** (Curriculum)
   - Fill in the **title** and use the **Quill rich text editor** for the body
   - Add optional sections: Suggested Action, Needed Resources, Needed Support, Special Remarks
   - Save as **Draft** or **Submit** directly
3. **Reports > All Reports** — See the newly created report in the list
4. **Click the report** — View it with status badge showing Draft or Submitted

**Key Talking Points:**
- Rich text editing with Quill editor
- Reports flow bottom-up through the hierarchy
- Author can save as draft and submit later

---

### Act 3: Collective Approval (Committee Head Perspective)

**Login as: Hanan Mostafa** (`hanan.mostafa@org.edu`) — Head of Curriculum

1. **Reports > All Reports** — See the submitted report from Amira Soliman
2. **Open the report** — The **Collective Approval Panel** shows:
   - Progress bar (e.g., "0 / 2 approved")
   - List of pending approvers (Hanan Mostafa, Bassem Youssef)
   - **Approve** button with optional comments
3. **Approve the report** — Progress updates to "1 / 2 approved"
4. **Login as: Bassem Youssef** (`bassem.youssef@org.edu`) — Approve as second member
   - Status automatically transitions to **Approved** when all members approve

**Alternative flow — Request Feedback:**
1. Instead of approving, click **Request Feedback** with a comment
2. Status changes to **FeedbackRequested**, all prior approvals reset
3. **Login as: Amira Soliman** — See the feedback, click **Revise & Resubmit**
4. Edit the report and resubmit — status returns to **Submitted**, approval restarts

**Head Override (after 3 days):**
- If some members haven't approved after 3 days, the committee head sees a **Finalize** button
- Head can force-approve the report to move it forward

**Key Talking Points:**
- Every committee member (except author) must approve — true collective review
- Any member can request feedback, which resets all approvals
- Head override after 3-day deadline prevents bottlenecks
- Progress bar gives real-time visibility into approval status

---

### Act 4: Report Summarization (L1 Head Perspective)

**Login as: Ghadir Nassar** (`ghadir.nassar@org.edu`) — Head of AQA

1. **Reports > Create Summary** — Only available to committee heads
   - Select multiple **Approved** source reports from sub-committees
   - System creates a summary report linked to sources via ReportSourceLink
   - Source reports transition to **Summarized** status
2. **Show the chain** — Click into a summary to see linked source reports

**Key Talking Points:**
- Summarization: L1 heads aggregate L2 reports into sector summaries
- Source links create a traceable chain from detail to summary
- Only Approved reports can be included in summaries

---

### Act 5: Issuing Directives & Running Meetings

**Login as: AM** (`am@org.edu`) — Chairman, or any committee head

1. **Directives > Issue Directive** — Create a directive:
   - Select type (Instruction, Approval, CorrectiveAction, Feedback, InformationNotice)
   - Set priority (Normal, High, Urgent)
   - Choose target committee and optional target user
   - Set deadline
   - Optionally link to a parent directive (for forwarding/chain)
2. **Directives > All Directives** — View the directive with its status (Issued)
3. **Login as the target** — Acknowledge, progress through statuses
4. **Directives > Track Overdue** — Show overdue tracking view

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

### Act 6: Analytics, Search, Confidentiality & Administration

**Login as: AM** (`am@org.edu`)

1. **Analytics** — Dashboard with Chart.js visualizations:
   - Organization overview cards with 30-day trend comparison
   - Monthly activity trends (bar chart)
   - Status distribution (doughnut charts)
   - Compliance metrics (on-time rates)
   - Committee activity table
   > Note: Charts populate as reports, directives, and meetings are created
2. **Search** — Unified search across all content types
3. **Knowledge > Browse** — Knowledge base with categories, search, and tags

**Login as: System Administrator** (`admin@org.edu`)

4. **Administration > Users** — View all 57 users with roles and status
5. **Administration > Database Backups** — Backup management
6. **Administration > Audit Log** — Browse audit trail (populates as users take actions)

**Confidentiality Demo** (requires a report to exist):
1. Create or find a report, then navigate to **Confidentiality > Mark**
2. Mark it as confidential — access restricted by hierarchy
3. Grant explicit access to specific users
4. Login as different users to verify access control

**Key Talking Points:**
- Data-driven analytics dashboard for executive oversight
- Unified search spans reports, directives, meetings, and action items
- Full audit trail for compliance and accountability
- Confidentiality markings restrict access based on hierarchy level
- Chairman's Office access is rank-based (Rank 1 sees all)

---

## Tips for a Successful Demo

1. **Start with the Committee Tree** — The visual tree view immediately shows the organizational scope and hierarchy.

2. **Create data live** — Walk through report creation, approval, and summarization to demonstrate the collective approval workflow.

3. **Switch between user levels** — Show the same report from author, member, and head perspectives to demonstrate collective approval. Keep browser tabs open for each persona.

4. **Follow a complete workflow** — Create a report at L2, get it collectively approved, summarize it at L1, then issue a directive based on it.

5. **Show analytics after creating data** — The Analytics dashboard is most impressive after the audience has seen what data feeds into it.

6. **Confidentiality is a differentiator** — The access control model (hierarchy-based + explicit grants + Chairman's Office rank) is sophisticated.

7. **Demonstrate the head override** — The 3-day finalize deadline shows the system balances thoroughness with practicality.
