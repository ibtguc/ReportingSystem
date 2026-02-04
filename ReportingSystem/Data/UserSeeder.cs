using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public static class UserSeeder
{
    public static async Task SeedAdminUsersAsync(ApplicationDbContext context)
    {
        // Check if any users exist
        if (await context.Users.AnyAsync())
        {
            return; // Users already seeded
        }

        // Try to find the root org unit to assign to admin users
        var rootOrgUnit = await context.OrganizationalUnits
            .FirstOrDefaultAsync(ou => ou.Level == OrgUnitLevel.Root);

        var adminUsers = new List<User>
        {
            new User
            {
                Email = "admin@reporting.com",
                Name = "System Administrator",
                Role = SystemRoles.Administrator,
                JobTitle = "System Administrator",
                OrganizationalUnitId = rootOrgUnit?.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Email = "admin1@reporting.com",
                Name = "Administrator One",
                Role = SystemRoles.Administrator,
                JobTitle = "IT Administrator",
                OrganizationalUnitId = rootOrgUnit?.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Email = "admin2@reporting.com",
                Name = "Administrator Two",
                Role = SystemRoles.Administrator,
                JobTitle = "IT Administrator",
                OrganizationalUnitId = rootOrgUnit?.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(adminUsers);
        await context.SaveChangesAsync();
    }
}
