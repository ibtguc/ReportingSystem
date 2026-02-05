using Microsoft.EntityFrameworkCore;

namespace ReportingSystem.Data;

/// <summary>
/// Main seed data orchestrator - calls individual seeders in the correct order.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Seed in dependency order

        // 1. Organizational Units (no dependencies)
        if (!await context.OrganizationalUnits.AnyAsync())
        {
            await SeedOrganizationalUnits.SeedAsync(context);
        }

        // 2. Users (depends on org units)
        if (!await context.Users.AnyAsync())
        {
            await SeedUsers.SeedAsync(context);
        }

        // 3. Delegations (depends on users)
        if (!await context.Delegations.AnyAsync())
        {
            await SeedDelegationsAndNotifications.SeedDelegationsAsync(context);
        }

        // 4. Notifications (depends on users)
        if (!await context.Notifications.AnyAsync())
        {
            await SeedDelegationsAndNotifications.SeedNotificationsAsync(context);
        }

        // 5. Report Templates (depends on users for CreatedById)
        if (!await context.ReportTemplates.AnyAsync())
        {
            await SeedReportTemplates.SeedTemplatesAsync(context);
        }

        // 6. Report Fields (depends on templates)
        if (!await context.ReportFields.AnyAsync())
        {
            await SeedReportTemplates.SeedFieldsAsync(context);
        }

        // 7. Template Assignments (depends on templates, org units)
        if (!await context.ReportTemplateAssignments.AnyAsync())
        {
            await SeedReportTemplates.SeedAssignmentsAsync(context);
        }

        // 8. Report Periods (depends on templates)
        if (!await context.ReportPeriods.AnyAsync())
        {
            await SeedReportTemplates.SeedPeriodsAsync(context);
        }

        // 9. Reports and related data (depends on templates, periods, users)
        if (!await context.Reports.AnyAsync())
        {
            await SeedReportsAndWorkflow.SeedAsync(context);
        }
    }
}
