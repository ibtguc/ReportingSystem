# Hierarchical Organizational Reporting System (HORS) - Demo Guide

## Overview

This guide walks you through a complete demonstration of the HORS system, showing the perspective of different user roles and the bidirectional information flow (upward and downward) through the organizational hierarchy.

**Demo Date Context**: The seed data is set for February 2026.

---

## Authentication - Magic Link Login

The system uses passwordless authentication via magic links.

### How to Login

1. Navigate to `/Auth/Login`
2. Enter any user email from the seed data
3. Click "Send Magic Link"
4. **In Development Mode**: The magic link is displayed directly on the screen (no actual email sent)
5. Click the link to complete authentication
6. The session lasts 30 days with sliding expiration

### How to Logout

1. Click your name in the top-right corner
2. Select "Logout"

---

## Demo Scenarios by User Role

### Key Users for Demo

| Email | Name | Role | Org Unit |
|-------|------|------|----------|
| `president@guc.edu.eg` | Prof. Ahmed Hassan | Executive | GUC Root |
| `vp.admin@guc.edu.eg` | Dr. Khaled Ibrahim | Executive | GUC Root |
| `admin@guc.edu.eg` | System Administrator | Administrator | IT & Admin Division |
| `head.sdev@guc.edu.eg` | Eng. Mahmoud Adel | DepartmentHead | Software Development |
| `head.infra@guc.edu.eg` | Eng. Heba Mostafa | DepartmentHead | IT Infrastructure |
| `mgr.backend@guc.edu.eg` | Eng. Youssef Magdy | TeamManager | Backend Team |
| `mgr.web@guc.edu.eg` | Eng. Ali Kamal | TeamManager | Web Systems Section |
| `dev.backend1@guc.edu.eg` | Ahmed Samir | ReportOriginator | Backend Team |
| `reviewer.sdev@guc.edu.eg` | Eng. Waleed Emad | ReportReviewer | Software Development |
| `auditor1@guc.edu.eg` | Dr. Hazem Barakat | Auditor | QA & Audit |

---

## Demo Walkthrough

### Part 1: System Administrator Perspective

**Login as**: `admin@guc.edu.eg`

#### 1.1 Dashboard Overview
- Navigate to **Home** (Dashboard)
- View system-wide statistics

#### 1.2 Organizational Structure
- Go to **Organization > Organizational Units**
- Explore the 6-level hierarchy:
  - Level 0: GUC Root
  - Level 1: Campuses (Main Campus, New Capital)
  - Level 2: Faculties (Engineering, MET, Management, etc.)
  - Level 3: Departments (Software Development, IT Infrastructure, etc.)
  - Level 4: Sections (Web Systems, Mobile Development, etc.)
  - Level 5: Teams (Backend Team, Frontend Team, etc.)
- Expand nodes to see children

#### 1.3 User Management
- Go to **Administration > Users**
- View all 60 users across different roles
- Filter by role to see:
  - 5 Executives
  - 3 Administrators
  - 13 Department Heads
  - 9 Team Managers
  - 6 Report Reviewers
  - 20 Report Originators
  - 3 Auditors
- Click on a user to view details

#### 1.4 Delegations
- Go to **Organization > Delegations**
- View active delegations:
  - Prof. Nadia Kamel (CSE Head) delegated to Dr. Hany Mourad for conference travel
  - Eng. Mahmoud Adel (SoftDev Head) delegated to Eng. Ali Kamal during annual leave
- Note different scopes: Full, ApprovalOnly, ReportingOnly

#### 1.5 Report Templates
- Go to **Reporting > Templates**
- View the 5 templates:
  1. Monthly Department Status Report (10 fields)
  2. Weekly Team Progress Report (6 fields)
  3. Quarterly Academic Performance Report
  4. Annual Executive Summary Report
  5. IT Infrastructure Health Report (6 fields)
- Click on a template to see field definitions

**Logout and proceed to next user**

---

### Part 2: Report Originator Perspective (Team Member)

**Login as**: `dev.backend1@guc.edu.eg` (Ahmed Samir - Backend Developer)

#### 2.1 View Dashboard
- See reports relevant to your team
- Note any pending tasks or notifications

#### 2.2 View Existing Reports
- Go to **Reporting > Reports**
- Filter to see reports from your team
- View Report #3 (Backend Team Weekly Report - Submitted status)
  - See the field values filled by the team lead
  - Note the blockers section mentioning staging environment delay

#### 2.3 View Support Requests (Upward Flow)
- Go to **Upward Flow > Support Requests**
- See the staging environment provisioning delay request (In Progress)
- Note the cross-department coordination category

**Logout and proceed to next user**

---

### Part 3: Team Manager Perspective

**Login as**: `mgr.backend@guc.edu.eg` (Eng. Youssef Magdy - Backend Team Lead)

#### 3.1 View/Edit Weekly Report
- Go to **Reporting > Reports**
- Find Report #3 (Weekly Team Progress - Week of Feb 03, 2026)
- View the submitted report with:
  - Sprint summary
  - 8 tasks completed, 4 in progress
  - Blockers (staging environment)
  - Next week focus

#### 3.2 Review Upward Flow Items
- Go to **Upward Flow > Suggested Actions**
  - View "Implement API rate limiting" suggestion
  - View "Reduce database query redundancy" suggestion
- Go to **Upward Flow > Resource Requests**
  - View "Developer Training - AWS Certification" request (Submitted)
  - View "High-Performance Development Workstations" request
- Go to **Upward Flow > Support Requests**
  - View "Staging environment provisioning delay" (In Progress)
  - See the acknowledgment from Eng. Rana Mohamed (Cloud Lead)

#### 3.3 Review Workflow Items
- Go to **Workflow > Comments**
  - See threaded discussion about staging environment
  - View response from mgr.cloud@guc.edu.eg about environment readiness
- Go to **Workflow > Confirmations**
  - See confirmation request to verify task completion numbers
  - Note the confirmed status from team member

#### 3.4 View Downward Feedback
- Go to **Downward Flow > Feedback**
  - View question about API rate limiting from section lead
- Go to **Downward Flow > Recommendations**
  - See individual recommendation for AWS certification

**Logout and proceed to next user**

---

### Part 4: Department Head Perspective

**Login as**: `head.sdev@guc.edu.eg` (Eng. Mahmoud Adel - Head of Software Development)

#### 4.1 View Department Reports
- Go to **Reporting > Reports**
- Find Report #1 (Monthly Dept Status - January 2026) - Your approved report
- View the comprehensive report with:
  - Department summary (Student Portal v2 success)
  - Key metrics: 24 staff, 3 projects completed, 5 in progress, 72% budget
  - Status: On Track
  - Achievements (Student Portal, API gateway, testing coverage)
  - Challenges (2 senior developer resignations)
  - Plans for next month

#### 4.2 Review Upward Flow (Your Submissions)
- Go to **Upward Flow > Suggested Actions**
  - "Implement automated code review tool" - Approved
  - "Establish developer mentorship program" - Implemented
  - "Migrate to containerized deployments" - Under Review
- Go to **Upward Flow > Resource Requests**
  - "Senior Backend Developer Hire" - Approved (480,000 EGP)
  - "JetBrains Team Tools License" - Fulfilled (45,000 EGP)
  - "Cloud Infrastructure Budget Increase" - Partially Approved (15,000 of 25,000 EGP)
- Go to **Upward Flow > Support Requests**
  - "Expedite procurement" - Resolved
  - "Cross-team API integration" - Closed

#### 4.3 View Workflow Discussions
- Go to **Workflow > Comments**
  - See VP Admin's comment on Student Portal success
  - See your response about adoption metrics
  - See HR's comment about recruitment coordination
- Go to **Workflow > Confirmations**
  - See confirmed testing coverage numbers
  - See confirmed Student Portal launch details

#### 4.4 Review Downward Communications
- Go to **Downward Flow > Feedback**
  - Positive recognition from VP Admin
  - Staffing concern acknowledgment
- Go to **Downward Flow > Recommendations**
  - "Implement mandatory code documentation standards" - In Progress
- Go to **Downward Flow > Decisions**
  - Review decisions on your suggested actions and resource requests

**Logout and proceed to next user**

---

### Part 5: Executive Perspective

**Login as**: `vp.admin@guc.edu.eg` (Dr. Khaled Ibrahim - VP for Administration)

#### 5.1 Executive Dashboard
- View organization-wide statistics
- See pending reviews and decisions

#### 5.2 Review Submitted Reports
- Go to **Reporting > Reports**
- Filter by status to find reports needing review
- View approved Report #1 (Software Development Monthly)
  - Read review comments: "Good comprehensive report. Approved."
- View approved Report #4 (IT Infrastructure Health)
  - Note the excellent 99.7% uptime

#### 5.3 Review Upward Flow Items
- Go to **Upward Flow > Suggested Actions**
  - See organization-wide suggestions
  - Filter by status to see items needing review
  - Note the "Predictive monitoring with ML" suggestion from IT Infrastructure
- Go to **Upward Flow > Resource Requests**
  - Review resource requests across departments
  - See approved server rack request
  - See fulfilled network monitoring software

#### 5.4 Provide Downward Feedback
- Go to **Downward Flow > Feedback**
  - View feedbacks you've provided:
    - Positive recognition for Software Dev team
    - Staffing concern noted
  - See acknowledgment responses from department heads
- Go to **Downward Flow > Recommendations**
  - View recommendations you've issued:
    - Code documentation standards
    - Disaster recovery drills
- Go to **Downward Flow > Decisions**
  - View your decisions on:
    - Automated code review tool - Approved
    - Predictive monitoring - Approved with Modifications
    - Cloud budget - Partially Approved

#### 5.5 Aggregation & Analytics
- Go to **Aggregation > Summary Data**
  - View aggregated values across org units
  - Use drill-down to see source reports
- Go to **Aggregation > Aggregation Rules**
  - View configured aggregation methods
- Go to **Aggregation > Audit Log**
  - View system-wide activity log
  - Filter by action type, entity, user, date
  - Export to CSV for compliance

**Logout and proceed to next user**

---

### Part 6: Auditor Perspective

**Login as**: `auditor1@guc.edu.eg` (Dr. Hazem Barakat - Internal Auditor)

#### 6.1 Audit Log Review
- Go to **Aggregation > Audit Log**
- View comprehensive change history:
  - All creates, updates, deletes
  - User actions with timestamps
  - IP addresses
- Use filters:
  - By Action (Create, Update, Delete, Approve, etc.)
  - By Entity Type
  - By User
  - By Date Range
  - Free text search
- Export filtered results to CSV

#### 6.2 Data Lineage
- Go to **Aggregation > Summary Data**
- Select an aggregated value
- Use drill-down to trace back to source reports
- Verify data integrity through the hierarchy

**Logout**

---

## Summary of Information Flow

### Upward Flow (Bottom-Up)
1. **Report Originators** create draft reports with field values
2. **Team Managers** submit reports with:
   - Suggested Actions (improvements, innovations)
   - Resource Requests (budget, equipment, personnel)
   - Support Requests (management help, coordination)
3. **Department Heads** review and add their own reports
4. **Executives** receive aggregated data and pending requests

### Downward Flow (Top-Down)
1. **Executives** provide:
   - Feedback (recognition, concerns, observations)
   - Recommendations (directives, guidance)
   - Decisions (approvals, rejections, modifications)
2. **Department Heads** receive and acknowledge
3. **Team Managers** implement decisions
4. **Report Originators** see outcomes

### Workflow Layer
- **Comments**: Threaded discussions on reports
- **Confirmations**: Request verification from specific users
- **Tags**: Mark sections needing attention

### Aggregation Layer
- Automatic rollup of numeric metrics
- Drill-down to source data
- Complete audit trail

---

## Key Demo Points to Highlight

1. **Passwordless Authentication**: Secure magic link login
2. **Hierarchical Organization**: 6 levels from root to team
3. **Delegation System**: Authority transfer with scope controls
4. **Flexible Templates**: Customizable report structures
5. **Bidirectional Flow**: Information flows both up and down
6. **Workflow Collaboration**: Comments, confirmations, tagging
7. **Decision Tracking**: Complete record of approvals/rejections
8. **Full Audit Trail**: Every change is logged
9. **Data Aggregation**: Automatic rollup with drill-down
10. **Role-Based Access**: Each user sees relevant data

---

## Quick Reference: Sample Data

### Reports in System
| ID | Template | Period | Submitted By | Status |
|----|----------|--------|--------------|--------|
| 1 | Monthly Dept Status | January 2026 | Eng. Mahmoud Adel (SoftDev Head) | Approved |
| 2 | Monthly Dept Status | February 2026 | Eng. Heba Mostafa (Infra Head) | Draft |
| 3 | Weekly Team Progress | Week of Feb 03 | Eng. Youssef Magdy (Backend Lead) | Submitted |
| 4 | IT Infrastructure Health | January 2026 | Eng. Heba Mostafa | Approved |

### Upward Flow Items
- 6 Suggested Actions (various statuses)
- 7 Resource Requests (totaling ~1M EGP)
- 6 Support Requests (cross-dept coordination)

### Downward Flow Items
- 6 Feedback items (recognition, concerns)
- 5 Recommendations (process, skills, strategy)
- 6 Decisions (approvals, rejections)

### Workflow Items
- 8 Comments (threaded discussions)
- 6 Confirmation Tags (verification requests)
