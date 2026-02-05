# HORS Demo Guide

## Quick Start

### Running the Application
```bash
cd /home/user/ReportingSystem

# Delete existing database to get fresh seed data
rm -f ReportingSystem/db/reporting.db

# Build and run
dotnet build --no-restore ReportingSystem/ReportingSystem.csproj
dotnet run --project ReportingSystem/ReportingSystem.csproj

# Access at: http://localhost:5296
```

### Login Process
1. Go to http://localhost:5296
2. Click "Login" or navigate to /Auth/Login
3. Enter any seeded email (see accounts below)
4. In development mode, the magic link is logged to the console - copy the URL
5. Paste the URL in your browser to complete login

---

## Seeded Data Overview

The system is pre-seeded with:
- **68 Organizational Units** across 6 levels
- **200 Users** with realistic Egyptian names
- **5 Report Templates** with 22 fields
- **9 Report Periods** (active and closed)
- **4 Sample Reports** with field values
- **Workflow Data**: Comments, confirmations, feedback, recommendations, decisions

---

## Sample User Accounts by Role

### Executives (6 users)
| Email | Name | Position | Org Unit |
|-------|------|----------|----------|
| president@guc.edu.eg | Prof. Ahmed Hassan | University President | GUC (Root) |
| vp.academic@guc.edu.eg | Prof. Mona El-Said | VP for Academic Affairs | GUC |
| vp.admin@guc.edu.eg | Dr. Khaled Ibrahim | VP for Administration | GUC |
| dean.main@guc.edu.eg | Prof. Sara Mahmoud | Dean - Main Campus | Main Campus |
| dean.newcampus@guc.edu.eg | Prof. Omar Fathy | Dean - New Campus | New Campus |
| head.admin@guc.edu.eg | Dr. Nadia Abdel-Rahman | Director of Administrative Services | Admin Division |

### Administrators (3 users)
| Email | Name | Position |
|-------|------|----------|
| admin@guc.edu.eg | Tarek Nabil | System Administrator |
| admin2@guc.edu.eg | Yasmine Farouk | IT Systems Administrator |
| admin3@guc.edu.eg | Mohamed Sherif | Infrastructure Administrator |

### Department Heads (25 users)
**Academic Departments:**
| Email | Name | Department |
|-------|------|------------|
| head.cse@guc.edu.eg | Prof. Nadia Kamel | Computer Science & Engineering |
| head.me@guc.edu.eg | Prof. Ayman Soliman | Mechanical Engineering |
| head.ece@guc.edu.eg | Prof. Laila Abdel-Fattah | Electronics & Communications |
| head.arch@guc.edu.eg | Prof. Hisham Ragab | Architecture & Urban Design |
| head.cs@guc.edu.eg | Prof. Fatma Zaki | Computer Science (MET) |
| head.dm@guc.edu.eg | Dr. Ramy Shoukry | Digital Media |
| head.nis@guc.edu.eg | Dr. Dina El-Masry | Networks & Information Systems |
| head.econ@guc.edu.eg | Prof. Sameh Attia | Economics |
| head.fin@guc.edu.eg | Dr. Noha Salah | Finance & Accounting |

**IT & Admin Departments:**
| Email | Name | Department |
|-------|------|------------|
| head.sdev@guc.edu.eg | Eng. Mahmoud Adel | Software Development |
| head.infra@guc.edu.eg | Eng. Heba Mostafa | IT Infrastructure |
| head.hr@guc.edu.eg | Dr. Amira Youssef | Human Resources |
| head.qa@guc.edu.eg | Dr. Sherif Hassan | QA & Audit |

**Administrative Services Departments:**
| Email | Name | Department |
|-------|------|------------|
| head.cfo@guc.edu.eg | Mr. Karim Mansour | Finance Office (CFO) |
| head.legal@guc.edu.eg | Dr. Laila Ghanem | Legal & Compliance |
| head.proc@guc.edu.eg | Mr. Ashraf El-Deeb | Procurement & Contracts |
| head.student@guc.edu.eg | Dr. Rania Fouad | Student Affairs |
| head.facilities@guc.edu.eg | Eng. Mohsen Abdallah | Facilities Management |
| head.marketing@guc.edu.eg | Ms. Dina El-Sayed | Marketing & Communications |
| head.research@guc.edu.eg | Prof. Tarek Zaki | Research & Innovation |
| head.intl@guc.edu.eg | Dr. Yasmin Rashid | International Relations |
| head.library@guc.edu.eg | Dr. Mahmoud Salem | Library & Information Services |
| head.security@guc.edu.eg | Col. Ahmed El-Mahdy | Security & Safety |
| head.registrar@guc.edu.eg | Ms. Fatma Nour | Registrar Office |
| head.career@guc.edu.eg | Mr. Hany Ibrahim | Career Services |

### Team Managers (28 users)
**IT Section Leads:**
| Email | Name | Section |
|-------|------|---------|
| mgr.web@guc.edu.eg | Eng. Ali Kamal | Web Systems Section |
| mgr.mobile@guc.edu.eg | Eng. Salma Reda | Mobile Development |
| mgr.netops@guc.edu.eg | Eng. Hassan Tawfik | Network Operations |
| mgr.cloud@guc.edu.eg | Eng. Rana Mohamed | Server & Cloud |
| mgr.backend@guc.edu.eg | Eng. Youssef Magdy | Backend Team |
| mgr.frontend@guc.edu.eg | Eng. Nourhan Sayed | Frontend Team |
| mgr.testing@guc.edu.eg | Eng. Karim Wael | QA & Testing Team |

**Administrative Section Managers:**
| Email | Name | Section |
|-------|------|---------|
| mgr.admissions@guc.edu.eg | Ms. Reem Abdel-Aziz | Admissions Office |
| mgr.counseling@guc.edu.eg | Dr. Noha Fathy | Student Counseling |
| mgr.maintenance@guc.edu.eg | Eng. Saeed Mostafa | Building Maintenance |
| mgr.ap@guc.edu.eg | Ms. Heba Kamel | Accounts Payable |
| mgr.payroll@guc.edu.eg | Ms. Nevine Adel | Payroll |
| mgr.recruitment@guc.edu.eg | Ms. Sara Farid | Recruitment & Hiring |
| mgr.circulation@guc.edu.eg | Ms. Hanan Mostafa | Library Circulation |
| mgr.digmktg@guc.edu.eg | Mr. Yasser Reda | Digital Marketing |
| mgr.events@guc.edu.eg | Ms. Layla Hamdy | Events Management |

### Report Reviewers (16 users)
| Email | Name | Department |
|-------|------|------------|
| reviewer.cse1@guc.edu.eg | Dr. Hany Mourad | CSE - Senior Lecturer |
| reviewer.cse2@guc.edu.eg | Dr. Nesma Said | CSE - Associate Professor |
| reviewer.sdev@guc.edu.eg | Eng. Waleed Emad | Software Development |
| reviewer.finance@guc.edu.eg | Mr. Samir Abdel-Wahab | Finance Office |
| reviewer.legal@guc.edu.eg | Ms. Mariam Shehata | Legal & Compliance |
| reviewer.student@guc.edu.eg | Dr. Ahmed Fouad | Student Affairs |

### Report Originators (116 users)
Distributed across all departments. Examples:
| Email | Name | Position |
|-------|------|----------|
| dev.backend1@guc.edu.eg | Ahmed Samir | Backend Developer |
| dev.frontend1@guc.edu.eg | Farida Hassan | Frontend Developer |
| qa.tester1@guc.edu.eg | Reem Adel | QA Engineer |
| faculty.cse1@guc.edu.eg | Dr. Bassem Aly | Lecturer - CSE |
| hr.staff1@guc.edu.eg | Marwa Elsayed | HR Specialist |
| fin.ap1@guc.edu.eg | Ms. Sherine Mahmoud | Accounts Payable Specialist |
| student.adm1@guc.edu.eg | Ms. Asmaa Ragab | Admissions Officer |
| mktg.digital1@guc.edu.eg | Ms. Rania El-Khouly | Social Media Specialist |

### Auditors (5 users)
| Email | Name | Position |
|-------|------|----------|
| auditor1@guc.edu.eg | Dr. Hazem Barakat | Internal Auditor |
| auditor2@guc.edu.eg | Eng. Nevine Sami | Quality Auditor |
| auditor3@guc.edu.eg | Dr. Tarek Mansour | External Audit Liaison |
| auditor4@guc.edu.eg | Mr. Wael El-Shaarawy | Senior Internal Auditor |
| auditor5@guc.edu.eg | Ms. Yasmine Fouad | Financial Auditor |

---

## Organizational Structure

```
German University in Cairo (GUC) - Root
├── Main Campus
│   ├── Faculty of Engineering
│   │   ├── Computer Science & Engineering
│   │   │   ├── AI & Machine Learning Section
│   │   │   └── Software Engineering Section
│   │   ├── Mechanical Engineering
│   │   ├── Electronics & Communications Engineering
│   │   └── Architecture & Urban Design
│   ├── Faculty of Media Engineering and Technology (MET)
│   │   ├── Computer Science
│   │   ├── Digital Media
│   │   └── Networks & Information Systems
│   ├── Faculty of Management Technology
│   │   ├── Economics
│   │   ├── Finance & Accounting
│   │   └── Management
│   ├── Faculty of Pharmacy and Biotechnology
│   ├── Faculty of Applied Sciences and Arts
│   ├── IT & Administration Division
│   │   ├── Software Development
│   │   │   ├── Web Systems Section
│   │   │   │   ├── Backend Team
│   │   │   │   ├── Frontend Team
│   │   │   │   └── QA & Testing Team
│   │   │   └── Mobile Development Section
│   │   │       ├── iOS Team
│   │   │       └── Android Team
│   │   ├── IT Infrastructure
│   │   │   ├── Network Operations Section
│   │   │   └── Server & Cloud Section
│   │   ├── Human Resources
│   │   │   ├── Recruitment & Hiring
│   │   │   ├── Training & Development
│   │   │   └── Compensation & Benefits
│   │   └── Quality Assurance & Audit
│   └── Administrative Services Division
│       ├── Finance Office
│       │   ├── Accounts Payable
│       │   ├── Accounts Receivable
│       │   ├── Budget & Planning
│       │   └── Payroll
│       ├── Legal & Compliance
│       ├── Procurement & Contracts
│       ├── Student Affairs
│       │   ├── Admissions Office
│       │   ├── Student Counseling
│       │   └── Student Activities
│       ├── Facilities Management
│       │   ├── Building Maintenance
│       │   ├── Grounds & Landscaping
│       │   └── Transportation Services
│       ├── Marketing & Communications
│       │   ├── Digital Marketing
│       │   ├── Public Relations
│       │   └── Events Management
│       ├── Research & Innovation
│       ├── International Relations
│       ├── Library & Information Services
│       │   ├── Circulation Services
│       │   ├── Digital Resources
│       │   └── Archives & Special Collections
│       ├── Security & Safety
│       ├── Registrar Office
│       └── Career Services
└── New Campus
    ├── Faculty of Engineering - New Campus
    └── Faculty of MET - New Campus
```

---

## Report Templates

| Template | Schedule | Assigned To |
|----------|----------|-------------|
| Monthly Department Status Report | Monthly | All Department Heads |
| Weekly Team Progress Report | Weekly | All Team Managers, IT Division |
| Quarterly Academic Performance Report | Quarterly | Engineering & MET Faculties |
| Annual Executive Summary Report | Annual | All Executives |
| IT Infrastructure Health Report | Monthly | Head of IT Infrastructure |

---

## Demo Scenarios

### Scenario 1: Report Originator Creates a Report
1. Login as `dev.backend1@guc.edu.eg` (Ahmed Samir)
2. Go to Dashboard → see pending reports
3. Create a new Weekly Team Progress Report
4. Fill in the form with metrics
5. Submit for review

### Scenario 2: Team Manager Reviews and Approves
1. Login as `mgr.backend@guc.edu.eg` (Eng. Youssef Magdy)
2. Go to Reports → see submitted reports
3. Review the report from Ahmed Samir
4. Add comments, request confirmations
5. Approve or request amendments

### Scenario 3: Department Head Views Aggregated Data
1. Login as `head.sdev@guc.edu.eg` (Eng. Mahmoud Adel)
2. Go to Manager Dashboard
3. View charts showing team performance
4. Review upward flow items (suggested actions, resource requests)
5. Create recommendations for teams

### Scenario 4: Executive Reviews Organization
1. Login as `vp.admin@guc.edu.eg` (Dr. Khaled Ibrahim)
2. Go to Executive Dashboard
3. View organization-wide metrics
4. Drill down into specific departments
5. Export reports to CSV/Excel/PDF

### Scenario 5: Auditor Reviews Audit Trail
1. Login as `auditor1@guc.edu.eg` (Dr. Hazem Barakat)
2. Go to Audit Log
3. Filter by entity type, user, or date range
4. Export audit data for compliance review

---

## System Features

### Phase 1: Authentication
- Magic link passwordless login
- 15-minute token expiry
- 30-day sliding session cookie

### Phase 2: Organization
- 6-level hierarchy (Root → Campus → Faculty → Department → Sector → Team)
- User management with roles
- Delegation system for authority transfer

### Phase 3: Report Templates
- Configurable templates with custom fields
- Field types: Text, Numeric, Date, Dropdown, Checkbox, FileUpload, RichText, TableGrid
- Report periods with deadlines and grace periods
- Auto-save and pre-population

### Phase 4: Upward Flow
- Suggested Actions (process improvements, risk mitigation)
- Resource Requests (budget, personnel, equipment, training)
- Support Requests (management intervention, cross-dept coordination)

### Phase 5: Workflow
- Threaded comments with mentions
- Confirmation tags for data verification
- Report status tracking

### Phase 6: Downward Flow
- Management Feedback (recognition, concerns, questions)
- Recommendations (process changes, skill development)
- Decisions on upward flow items

### Phase 7: Aggregation
- Aggregation rules for metrics
- Automatic roll-up of data
- Manager amendments

### Phase 8: Analytics
- Role-based dashboards (Executive, Manager, Reviewer, Originator)
- Chart.js visualizations (line, bar, doughnut)
- Export to CSV, Excel, PDF
- Ad-hoc Report Builder

---

## Troubleshooting

### Magic Link Not Working
- Check console output for the logged magic link URL
- Ensure the token hasn't expired (15 minutes)
- Try deleting the database and restarting

### Missing Data
- Delete `ReportingSystem/db/reporting.db`
- Restart the application
- Seed data will be automatically created

### Build Errors
```bash
# Clean and rebuild
dotnet clean ReportingSystem/ReportingSystem.csproj
dotnet build --no-restore ReportingSystem/ReportingSystem.csproj
```
