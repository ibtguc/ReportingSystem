using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Seeds report templates, fields, assignments, and periods.
/// </summary>
public static class SeedReportTemplates
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;

        // ========== REPORT TEMPLATES ==========
        var templates = new List<ReportTemplate>
        {
            new()
            {
                Id = 1,
                Name = "Monthly Department Status Report",
                Description = "Standard monthly report for department heads covering KPIs, activities, challenges, and resource needs.",
                Schedule = ReportSchedule.Monthly,
                Version = 1,
                VersionNotes = "Initial template version",
                IncludeSuggestedActions = true,
                IncludeNeededResources = true,
                IncludeNeededSupport = true,
                AutoSaveIntervalSeconds = 60,
                AllowPrePopulation = true,
                AllowBulkImport = false,
                MaxAttachmentSizeMb = 10,
                AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg",
                IsActive = true,
                CreatedAt = now,
                CreatedById = 6
            },
            new()
            {
                Id = 2,
                Name = "Weekly Team Progress Report",
                Description = "Weekly progress report for team leads covering sprint progress, blockers, and upcoming work.",
                Schedule = ReportSchedule.Weekly,
                Version = 1,
                VersionNotes = "Initial template version",
                IncludeSuggestedActions = true,
                IncludeNeededResources = false,
                IncludeNeededSupport = true,
                AutoSaveIntervalSeconds = 30,
                AllowPrePopulation = true,
                AllowBulkImport = false,
                MaxAttachmentSizeMb = 5,
                AllowedFileTypes = ".pdf,.doc,.docx,.png,.jpg,.jpeg",
                IsActive = true,
                CreatedAt = now,
                CreatedById = 6
            },
            new()
            {
                Id = 3,
                Name = "Quarterly Academic Performance Report",
                Description = "Quarterly report for faculty departments covering teaching effectiveness, research output, and student outcomes.",
                Schedule = ReportSchedule.Quarterly,
                Version = 1,
                VersionNotes = "Initial template version",
                IncludeSuggestedActions = true,
                IncludeNeededResources = true,
                IncludeNeededSupport = true,
                AutoSaveIntervalSeconds = 60,
                AllowPrePopulation = true,
                AllowBulkImport = true,
                MaxAttachmentSizeMb = 20,
                AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg,.pptx",
                IsActive = true,
                CreatedAt = now,
                CreatedById = 6
            },
            new()
            {
                Id = 4,
                Name = "Annual Executive Summary Report",
                Description = "Annual summary report prepared by campus deans and division heads for the university president.",
                Schedule = ReportSchedule.Annual,
                Version = 1,
                VersionNotes = "Initial template version",
                IncludeSuggestedActions = true,
                IncludeNeededResources = true,
                IncludeNeededSupport = true,
                AutoSaveIntervalSeconds = 60,
                AllowPrePopulation = true,
                AllowBulkImport = true,
                MaxAttachmentSizeMb = 50,
                AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg,.pptx",
                IsActive = true,
                CreatedAt = now,
                CreatedById = 6
            },
            new()
            {
                Id = 5,
                Name = "IT Infrastructure Health Report",
                Description = "Monthly report on IT infrastructure status, uptime, incidents, and capacity.",
                Schedule = ReportSchedule.Monthly,
                Version = 1,
                VersionNotes = "Initial template version",
                IncludeSuggestedActions = true,
                IncludeNeededResources = true,
                IncludeNeededSupport = false,
                AutoSaveIntervalSeconds = 60,
                AllowPrePopulation = true,
                AllowBulkImport = true,
                MaxAttachmentSizeMb = 10,
                AllowedFileTypes = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg",
                IsActive = true,
                CreatedAt = now,
                CreatedById = 6
            }
        };

        context.ReportTemplates.AddRange(templates);
        await context.SaveChangesAsync();

        // ========== REPORT FIELDS ==========
        var fields = GetReportFields(now);
        context.ReportFields.AddRange(fields);
        await context.SaveChangesAsync();

        // ========== TEMPLATE ASSIGNMENTS ==========
        var assignments = new List<ReportTemplateAssignment>
        {
            // Monthly Dept Report assigned to all department heads
            new() { ReportTemplateId = 1, AssignmentType = TemplateAssignmentType.Role, RoleValue = SystemRoles.DepartmentHead, IncludeSubUnits = false, CreatedAt = now },
            // Weekly Team Report assigned to IT & Admin division (includes sub-units)
            new() { ReportTemplateId = 2, AssignmentType = TemplateAssignmentType.OrgUnit, TargetId = 9, IncludeSubUnits = true, CreatedAt = now },
            // Weekly Team Report also assigned to team managers by role
            new() { ReportTemplateId = 2, AssignmentType = TemplateAssignmentType.Role, RoleValue = SystemRoles.TeamManager, IncludeSubUnits = false, CreatedAt = now },
            // Quarterly Academic Report assigned to Engineering Faculty
            new() { ReportTemplateId = 3, AssignmentType = TemplateAssignmentType.OrgUnit, TargetId = 4, IncludeSubUnits = true, CreatedAt = now },
            // Quarterly Academic Report assigned to MET Faculty
            new() { ReportTemplateId = 3, AssignmentType = TemplateAssignmentType.OrgUnit, TargetId = 5, IncludeSubUnits = true, CreatedAt = now },
            // Annual Executive Report assigned to executives
            new() { ReportTemplateId = 4, AssignmentType = TemplateAssignmentType.Role, RoleValue = SystemRoles.Executive, IncludeSubUnits = false, CreatedAt = now },
            // IT Infrastructure Report assigned to Head of Infrastructure
            new() { ReportTemplateId = 5, AssignmentType = TemplateAssignmentType.Individual, TargetId = 19, IncludeSubUnits = false, CreatedAt = now }
        };

        context.ReportTemplateAssignments.AddRange(assignments);
        await context.SaveChangesAsync();

        // ========== REPORT PERIODS ==========
        var today = DateTime.UtcNow.Date;
        var periods = new List<ReportPeriod>
        {
            // Monthly Dept Report periods (IDs 1-3)
            new() { Id = 1, ReportTemplateId = 1, Name = "January 2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31), SubmissionDeadline = new DateTime(2026, 2, 5), GracePeriodDays = 3, Status = PeriodStatus.Closed, IsActive = true, CreatedAt = now },
            new() { Id = 2, ReportTemplateId = 1, Name = "February 2026", StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 28), SubmissionDeadline = new DateTime(2026, 3, 5), GracePeriodDays = 3, Status = PeriodStatus.Open, IsActive = true, CreatedAt = now },
            new() { Id = 3, ReportTemplateId = 1, Name = "March 2026", StartDate = new DateTime(2026, 3, 1), EndDate = new DateTime(2026, 3, 31), SubmissionDeadline = new DateTime(2026, 4, 5), GracePeriodDays = 3, Status = PeriodStatus.Upcoming, IsActive = true, CreatedAt = now },
            // Weekly Team Report periods (IDs 4-5)
            new() { Id = 4, ReportTemplateId = 2, Name = "Week of Jan 27, 2026", StartDate = new DateTime(2026, 1, 27), EndDate = new DateTime(2026, 2, 2), SubmissionDeadline = new DateTime(2026, 2, 3), GracePeriodDays = 1, Status = PeriodStatus.Closed, IsActive = true, CreatedAt = now },
            new() { Id = 5, ReportTemplateId = 2, Name = "Week of Feb 03, 2026", StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 9), SubmissionDeadline = new DateTime(2026, 2, 10), GracePeriodDays = 1, Status = PeriodStatus.Open, IsActive = true, CreatedAt = now },
            // Quarterly Academic periods (IDs 6-7)
            new() { Id = 6, ReportTemplateId = 3, Name = "Q4 2025", StartDate = new DateTime(2025, 10, 1), EndDate = new DateTime(2025, 12, 31), SubmissionDeadline = new DateTime(2026, 1, 15), GracePeriodDays = 5, Status = PeriodStatus.Closed, IsActive = true, CreatedAt = now },
            new() { Id = 7, ReportTemplateId = 3, Name = "Q1 2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 3, 31), SubmissionDeadline = new DateTime(2026, 4, 15), GracePeriodDays = 5, Status = PeriodStatus.Open, IsActive = true, CreatedAt = now },
            // IT Infrastructure Health Report periods (IDs 8-9)
            new() { Id = 8, ReportTemplateId = 5, Name = "January 2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31), SubmissionDeadline = new DateTime(2026, 2, 5), GracePeriodDays = 3, Status = PeriodStatus.Closed, IsActive = true, CreatedAt = now },
            new() { Id = 9, ReportTemplateId = 5, Name = "February 2026", StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 28), SubmissionDeadline = new DateTime(2026, 3, 5), GracePeriodDays = 3, Status = PeriodStatus.Open, IsActive = true, CreatedAt = now }
        };

        context.ReportPeriods.AddRange(periods);
        await context.SaveChangesAsync();
    }

    private static List<ReportField> GetReportFields(DateTime now)
    {
        return new List<ReportField>
        {
            // ========== TEMPLATE 1: Monthly Department Status Report ==========
            new() { Id = 1, ReportTemplateId = 1, Label = "Department Summary", FieldKey = "dept_summary", HelpText = "Provide a brief overview of the department's status this month.", Type = FieldType.RichText, Section = "General", SectionOrder = 0, FieldOrder = 1, IsRequired = true, MinLength = 50, MaxLength = 2000, IsActive = true, CreatedAt = now },
            new() { Id = 2, ReportTemplateId = 1, Label = "Total Active Staff", FieldKey = "total_staff", HelpText = "Number of currently active staff in the department.", Type = FieldType.Numeric, Section = "Key Metrics", SectionOrder = 1, FieldOrder = 1, IsRequired = true, MinValue = 0, MaxValue = 500, PrePopulateFromPrevious = true, IsActive = true, CreatedAt = now },
            new() { Id = 3, ReportTemplateId = 1, Label = "Projects Completed", FieldKey = "projects_completed", HelpText = "Number of projects completed this month.", Type = FieldType.Numeric, Section = "Key Metrics", SectionOrder = 1, FieldOrder = 2, IsRequired = true, MinValue = 0, MaxValue = 100, IsActive = true, CreatedAt = now },
            new() { Id = 4, ReportTemplateId = 1, Label = "Projects In Progress", FieldKey = "projects_in_progress", HelpText = "Number of projects currently in progress.", Type = FieldType.Numeric, Section = "Key Metrics", SectionOrder = 1, FieldOrder = 3, IsRequired = true, MinValue = 0, MaxValue = 100, IsActive = true, CreatedAt = now },
            new() { Id = 5, ReportTemplateId = 1, Label = "Budget Utilization (%)", FieldKey = "budget_utilization", HelpText = "Percentage of allocated budget used this month.", Type = FieldType.Numeric, Section = "Key Metrics", SectionOrder = 1, FieldOrder = 4, IsRequired = true, MinValue = 0, MaxValue = 100, IsActive = true, CreatedAt = now },
            new() { Id = 6, ReportTemplateId = 1, Label = "Overall Status", FieldKey = "overall_status", HelpText = "Select the overall status of the department.", Type = FieldType.Dropdown, Section = "Key Metrics", SectionOrder = 1, FieldOrder = 5, IsRequired = true, OptionsJson = "[\"On Track\",\"At Risk\",\"Behind Schedule\",\"Exceeding Expectations\"]", IsActive = true, CreatedAt = now },
            new() { Id = 7, ReportTemplateId = 1, Label = "Key Achievements", FieldKey = "achievements", HelpText = "List the key achievements this month.", Type = FieldType.RichText, Section = "Activities", SectionOrder = 2, FieldOrder = 1, MaxLength = 3000, IsActive = true, CreatedAt = now },
            new() { Id = 8, ReportTemplateId = 1, Label = "Current Challenges", FieldKey = "challenges", HelpText = "Describe any challenges or blockers encountered.", Type = FieldType.RichText, Section = "Activities", SectionOrder = 2, FieldOrder = 2, MaxLength = 3000, IsActive = true, CreatedAt = now },
            new() { Id = 9, ReportTemplateId = 1, Label = "Plans for Next Month", FieldKey = "next_month_plans", HelpText = "Key plans and objectives for the upcoming month.", Type = FieldType.RichText, Section = "Planning", SectionOrder = 3, FieldOrder = 1, MaxLength = 3000, IsActive = true, CreatedAt = now },
            new() { Id = 10, ReportTemplateId = 1, Label = "Requires Executive Attention", FieldKey = "needs_attention", HelpText = "Check if this report requires immediate executive attention.", Type = FieldType.Checkbox, Section = "Planning", SectionOrder = 3, FieldOrder = 2, DefaultValue = "false", IsActive = true, CreatedAt = now },

            // ========== TEMPLATE 2: Weekly Team Progress Report ==========
            new() { Id = 11, ReportTemplateId = 2, Label = "Sprint/Week Summary", FieldKey = "week_summary", HelpText = "Brief overview of the week's progress.", Type = FieldType.Text, Section = "Summary", SectionOrder = 0, FieldOrder = 1, IsRequired = true, MinLength = 10, MaxLength = 500, IsActive = true, CreatedAt = now },
            new() { Id = 12, ReportTemplateId = 2, Label = "Tasks Completed", FieldKey = "tasks_completed", HelpText = "Number of tasks completed this week.", Type = FieldType.Numeric, Section = "Metrics", SectionOrder = 1, FieldOrder = 1, IsRequired = true, MinValue = 0, MaxValue = 200, IsActive = true, CreatedAt = now },
            new() { Id = 13, ReportTemplateId = 2, Label = "Tasks In Progress", FieldKey = "tasks_in_progress", HelpText = "Number of tasks currently being worked on.", Type = FieldType.Numeric, Section = "Metrics", SectionOrder = 1, FieldOrder = 2, IsRequired = true, MinValue = 0, MaxValue = 200, IsActive = true, CreatedAt = now },
            new() { Id = 14, ReportTemplateId = 2, Label = "Blockers", FieldKey = "blockers", HelpText = "Describe any current blockers preventing progress.", Type = FieldType.RichText, Section = "Issues", SectionOrder = 2, FieldOrder = 1, MaxLength = 2000, IsActive = true, CreatedAt = now },
            new() { Id = 15, ReportTemplateId = 2, Label = "Team Velocity", FieldKey = "velocity", HelpText = "Story points or tasks completed per team member.", Type = FieldType.Numeric, Section = "Metrics", SectionOrder = 1, FieldOrder = 3, MinValue = 0, MaxValue = 100, IsActive = true, CreatedAt = now },
            new() { Id = 16, ReportTemplateId = 2, Label = "Next Week Focus", FieldKey = "next_week", HelpText = "Key tasks and priorities for next week.", Type = FieldType.Text, Section = "Planning", SectionOrder = 3, FieldOrder = 1, IsRequired = true, MinLength = 10, MaxLength = 1000, IsActive = true, CreatedAt = now },

            // ========== TEMPLATE 5: IT Infrastructure Health Report ==========
            new() { Id = 17, ReportTemplateId = 5, Label = "System Uptime (%)", FieldKey = "uptime_pct", HelpText = "Overall system uptime percentage for the month.", Type = FieldType.Numeric, Section = "Availability", SectionOrder = 0, FieldOrder = 1, IsRequired = true, MinValue = 0, MaxValue = 100, IsActive = true, CreatedAt = now },
            new() { Id = 18, ReportTemplateId = 5, Label = "Total Incidents", FieldKey = "total_incidents", HelpText = "Total number of infrastructure incidents this month.", Type = FieldType.Numeric, Section = "Incidents", SectionOrder = 1, FieldOrder = 1, IsRequired = true, MinValue = 0, MaxValue = 500, IsActive = true, CreatedAt = now },
            new() { Id = 19, ReportTemplateId = 5, Label = "Critical Incidents", FieldKey = "critical_incidents", HelpText = "Number of critical (P1/P2) incidents.", Type = FieldType.Numeric, Section = "Incidents", SectionOrder = 1, FieldOrder = 2, IsRequired = true, MinValue = 0, MaxValue = 100, IsActive = true, CreatedAt = now },
            new() { Id = 20, ReportTemplateId = 5, Label = "Server Capacity Status", FieldKey = "server_capacity", HelpText = "Current server capacity utilization level.", Type = FieldType.Dropdown, Section = "Capacity", SectionOrder = 2, FieldOrder = 1, IsRequired = true, OptionsJson = "[\"Green (< 60%)\",\"Yellow (60-80%)\",\"Orange (80-90%)\",\"Red (> 90%)\"]", IsActive = true, CreatedAt = now },
            new() { Id = 21, ReportTemplateId = 5, Label = "Network Bandwidth Usage (%)", FieldKey = "network_usage", HelpText = "Average network bandwidth utilization.", Type = FieldType.Numeric, Section = "Capacity", SectionOrder = 2, FieldOrder = 2, IsRequired = true, MinValue = 0, MaxValue = 100, IsActive = true, CreatedAt = now },
            new() { Id = 22, ReportTemplateId = 5, Label = "Incident Summary", FieldKey = "incident_summary", HelpText = "Brief summary of major incidents and resolutions.", Type = FieldType.RichText, Section = "Incidents", SectionOrder = 1, FieldOrder = 3, MaxLength = 3000, IsActive = true, CreatedAt = now }
        };
    }
}
