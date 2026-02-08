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

        // Domain-specific seeding is handled by OrganizationSeeder.SeedAsync()
        // which creates the full organizational hierarchy from test data.

        await context.SaveChangesAsync();
    }
}
