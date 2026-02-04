using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Models;

namespace SchedulingSystem.Data;

public static class UserSeeder
{
    public static async Task SeedAdminUsersAsync(ApplicationDbContext context)
    {
        // Check if any users exist
        if (await context.Users.AnyAsync())
        {
            return; // Users already seeded
        }

        var adminUsers = new List<User>
        {
            new User
            {
                Email = "admin@school.com",
                Name = "System Administrator",
                Role = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Email = "admin1@school.com",
                Name = "Administrator One",
                Role = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Email = "admin2@school.com",
                Name = "Administrator Two",
                Role = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(adminUsers);
        await context.SaveChangesAsync();
    }
}
