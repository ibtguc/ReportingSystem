using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public static class UserSeeder
{
    public static async Task SeedAdminUsersAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        // ── System Admin ──
        var admin = new User
        {
            Email = "admin@org.edu",
            Name = "System Administrator",
            SystemRole = SystemRole.SystemAdmin,
            Title = "System Administrator",
            IsActive = true,
            CreatedAt = now
        };

        // ── Chairman ──
        var chairman = new User
        {
            Email = "am@org.edu",
            Name = "AM",
            SystemRole = SystemRole.Chairman,
            Title = "Chairman",
            IsActive = true,
            CreatedAt = now
        };

        // ── Chairman Office ──
        var co1 = new User
        {
            Email = "ahmed.mansour@org.edu",
            Name = "Ahmed Mansour",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 1,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };
        var co2 = new User
        {
            Email = "moustafa.fouad@org.edu",
            Name = "Moustafa Fouad",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 2,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };
        var co3 = new User
        {
            Email = "marwa.elserafy@org.edu",
            Name = "Marwa El Serafy",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 3,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };
        var co4 = new User
        {
            Email = "samia.elashiry@org.edu",
            Name = "Samia El Ashiry",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 4,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };

        // ── Top Level Committee Members ──
        var tl1 = new User
        {
            Email = "mohamed.ibrahim@org.edu",
            Name = "Mohamed Ibrahim",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl2 = new User
        {
            Email = "radwa.selim@org.edu",
            Name = "Radwa Selim",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl3 = new User
        {
            Email = "ghadir.nassar@org.edu",
            Name = "Ghadir Nassar",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl4 = new User
        {
            Email = "engy.galal@org.edu",
            Name = "Engy Galal",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl5 = new User
        {
            Email = "karim.salme@org.edu",
            Name = "Karim Salme",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl6 = new User
        {
            Email = "sherine.khalil@org.edu",
            Name = "Sherine Khalil",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };

        var tl7 = new User
        {
            Email = "sherine.salamony@org.edu",
            Name = "Sherine Salamony",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };

        context.Users.AddRange(admin, chairman, co1, co2, co3, co4, tl1, tl2, tl3, tl4, tl5, tl6, tl7);
        await context.SaveChangesAsync();

        // ── Top Level Committee ──
        var topLevel = new Committee
        {
            Name = "Top Level Committee",
            HierarchyLevel = HierarchyLevel.TopLevel,
            IsActive = true,
            CreatedAt = now
        };
        context.Committees.Add(topLevel);
        await context.SaveChangesAsync();

        // ── Memberships (all as heads of top level) ──
        var memberships = new List<CommitteeMembership>
        {
            new() { UserId = tl1.Id, CommitteeId = topLevel.Id, Role = CommitteeRole.Head, EffectiveFrom = now },
            new() { UserId = tl2.Id, CommitteeId = topLevel.Id, Role = CommitteeRole.Head, EffectiveFrom = now },
            new() { UserId = tl3.Id, CommitteeId = topLevel.Id, Role = CommitteeRole.Head, EffectiveFrom = now },
            new() { UserId = tl4.Id, CommitteeId = topLevel.Id, Role = CommitteeRole.Head, EffectiveFrom = now },
            new() { UserId = tl5.Id, CommitteeId = topLevel.Id, Role = CommitteeRole.Head, EffectiveFrom = now },
            new() { UserId = tl6.Id, CommitteeId = topLevel.Id, Role = CommitteeRole.Head, EffectiveFrom = now },
            new() { UserId = tl7.Id, CommitteeId = topLevel.Id, Role = CommitteeRole.Head, EffectiveFrom = now },
        };

        context.CommitteeMemberships.AddRange(memberships);
        await context.SaveChangesAsync();
    }
}
