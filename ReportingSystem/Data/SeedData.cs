using Microsoft.EntityFrameworkCore;

namespace ReportingSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Check if database already has data
        if (await context.Users.AnyAsync())
        {
            return; // Database has been seeded
        }

        // No domain-specific seed data for Phase 1
        // Domain entities (ReportTemplates, OrganizationalUnits, etc.) will be
        // seeded in later phases when those models are implemented.

        await context.SaveChangesAsync();
    }
}
