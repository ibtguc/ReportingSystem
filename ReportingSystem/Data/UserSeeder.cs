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
            Email = "ah.mn@org.edu",
            Name = "Ah Mn",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 1,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };
        var co2 = new User
        {
            Email = "ms.fd@org.edu",
            Name = "Ms Fd",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 2,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };
        var co3 = new User
        {
            Email = "mr.sf@org.edu",
            Name = "Mr Sf",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 3,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };
        var co4 = new User
        {
            Email = "sm.ash@org.edu",
            Name = "Sm Ash",
            SystemRole = SystemRole.ChairmanOffice,
            ChairmanOfficeRank = 4,
            Title = "Chairman Office",
            IsActive = true,
            CreatedAt = now
        };

        // ── Top Level Committee Members ──
        var tl1 = new User
        {
            Email = "m.mans@org.edu",
            Name = "M Mans",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl2 = new User
        {
            Email = "rd.sl@org.edu",
            Name = "Rd Sl",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl3 = new User
        {
            Email = "gh.ns@org.edu",
            Name = "Gh Ns",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl4 = new User
        {
            Email = "ng.gl@org.edu",
            Name = "Ng Gl",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl5 = new User
        {
            Email = "kr.s@org.edu",
            Name = "Kr S",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };
        var tl6 = new User
        {
            Email = "sh.kh@org.edu",
            Name = "Sh Kh",
            SystemRole = SystemRole.CommitteeUser,
            Title = "Top Level Committee",
            IsActive = true,
            CreatedAt = now
        };

        context.Users.AddRange(admin, chairman, co1, co2, co3, co4, tl1, tl2, tl3, tl4, tl5, tl6);
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
        };

        context.CommitteeMemberships.AddRange(memberships);
        await context.SaveChangesAsync();
    }
}
