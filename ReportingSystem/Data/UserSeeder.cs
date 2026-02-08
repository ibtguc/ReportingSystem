using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public static class UserSeeder
{
    public static async Task SeedAdminUsersAsync(ApplicationDbContext context)
    {
        // Skip if OrganizationSeeder already ran (it creates its own SystemAdmin)
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // Fallback admin user only if no organization data is seeded
        var admin = new User
        {
            Email = "admin@org.edu",
            Name = "System Administrator",
            SystemRole = SystemRole.SystemAdmin,
            Title = "System Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
