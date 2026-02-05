using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Seed organizational units if none exist
        if (!await context.OrganizationalUnits.AnyAsync())
        {
            await SeedOrganizationalUnitsAsync(context);
        }

        // Seed workflow data (comments and confirmations) if reports exist but no comments
        if (await context.Reports.AnyAsync() && !await context.Comments.AnyAsync())
        {
            await SeedWorkflowDataAsync(context);
        }
    }

    private static async Task SeedOrganizationalUnitsAsync(ApplicationDbContext context)
    {
        // Root organization
        var root = new OrganizationalUnit
        {
            Name = "German University in Cairo",
            Code = "GUC",
            Level = OrgUnitLevel.Root,
            SortOrder = 0,
            Description = "Root organization"
        };
        context.OrganizationalUnits.Add(root);
        await context.SaveChangesAsync();

        // Campuses
        var mainCampus = new OrganizationalUnit
        {
            Name = "Main Campus",
            Code = "GUC-MC",
            Level = OrgUnitLevel.Campus,
            ParentId = root.Id,
            SortOrder = 1
        };
        var newCampus = new OrganizationalUnit
        {
            Name = "New Campus",
            Code = "GUC-NC",
            Level = OrgUnitLevel.Campus,
            ParentId = root.Id,
            SortOrder = 2
        };
        context.OrganizationalUnits.AddRange(mainCampus, newCampus);
        await context.SaveChangesAsync();

        // Faculties under Main Campus
        var facEngineering = new OrganizationalUnit
        {
            Name = "Faculty of Engineering",
            Code = "ENG",
            Level = OrgUnitLevel.Faculty,
            ParentId = mainCampus.Id,
            SortOrder = 1
        };
        var facMET = new OrganizationalUnit
        {
            Name = "Faculty of Management, Economics & Technology",
            Code = "MET",
            Level = OrgUnitLevel.Faculty,
            ParentId = mainCampus.Id,
            SortOrder = 2
        };
        var facITAdmin = new OrganizationalUnit
        {
            Name = "IT & Administration",
            Code = "ITA",
            Level = OrgUnitLevel.Faculty,
            ParentId = mainCampus.Id,
            SortOrder = 3
        };
        context.OrganizationalUnits.AddRange(facEngineering, facMET, facITAdmin);
        await context.SaveChangesAsync();

        // Departments under Engineering
        var deptCS = new OrganizationalUnit
        {
            Name = "Computer Science & Engineering",
            Code = "CS",
            Level = OrgUnitLevel.Department,
            ParentId = facEngineering.Id,
            SortOrder = 1
        };
        var deptMech = new OrganizationalUnit
        {
            Name = "Mechatronics Engineering",
            Code = "MECH",
            Level = OrgUnitLevel.Department,
            ParentId = facEngineering.Id,
            SortOrder = 2
        };
        context.OrganizationalUnits.AddRange(deptCS, deptMech);
        await context.SaveChangesAsync();

        // Departments under IT & Admin
        var deptSoftware = new OrganizationalUnit
        {
            Name = "Software Development",
            Code = "SWDEV",
            Level = OrgUnitLevel.Department,
            ParentId = facITAdmin.Id,
            SortOrder = 1
        };
        context.OrganizationalUnits.Add(deptSoftware);
        await context.SaveChangesAsync();

        // Teams under Software Development
        var teamBackend = new OrganizationalUnit
        {
            Name = "Backend Team",
            Code = "SWDEV-BE",
            Level = OrgUnitLevel.Team,
            ParentId = deptSoftware.Id,
            SortOrder = 1
        };
        var teamFrontend = new OrganizationalUnit
        {
            Name = "Frontend Team",
            Code = "SWDEV-FE",
            Level = OrgUnitLevel.Team,
            ParentId = deptSoftware.Id,
            SortOrder = 2
        };
        context.OrganizationalUnits.AddRange(teamBackend, teamFrontend);
        await context.SaveChangesAsync();
    }

    private static async Task SeedWorkflowDataAsync(ApplicationDbContext context)
    {
        // Get some reports and users for seeding workflow data
        var reports = await context.Reports
            .Include(r => r.SubmittedBy)
            .Take(3)
            .ToListAsync();

        if (reports.Count == 0) return;

        var users = await context.Users.Take(5).ToListAsync();
        if (users.Count < 2) return;

        var report = reports[0];
        var author = users[0];
        var reviewer = users.Count > 1 ? users[1] : users[0];
        var taggedUser = users.Count > 2 ? users[2] : reviewer;

        // Seed some comments
        var comment1 = new Comment
        {
            ReportId = report.Id,
            AuthorId = reviewer.Id,
            Content = "Please clarify the budget allocation in section 3. The numbers don't seem to add up correctly.",
            Status = CommentStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        context.Comments.Add(comment1);
        await context.SaveChangesAsync();

        // Add a reply to the first comment
        var reply1 = new Comment
        {
            ReportId = report.Id,
            ParentCommentId = comment1.Id,
            AuthorId = report.SubmittedById,
            Content = "Thanks for catching that. I've corrected the figures in the updated version.",
            Status = CommentStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };
        context.Comments.Add(reply1);

        var comment2 = new Comment
        {
            ReportId = report.Id,
            AuthorId = reviewer.Id,
            Content = "Great progress on the Q4 objectives. The team has exceeded expectations.",
            Status = CommentStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.Comments.Add(comment2);

        // Seed confirmation tags
        var confirmation1 = new ConfirmationTag
        {
            ReportId = report.Id,
            RequestedById = report.SubmittedById,
            TaggedUserId = reviewer.Id,
            Message = "Please confirm the financial data in this report is accurate.",
            Status = ConfirmationStatus.Confirmed,
            Response = "Verified and confirmed. All figures match our records.",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            RespondedAt = DateTime.UtcNow.AddDays(-6)
        };
        context.ConfirmationTags.Add(confirmation1);

        var confirmation2 = new ConfirmationTag
        {
            ReportId = report.Id,
            RequestedById = report.SubmittedById,
            TaggedUserId = taggedUser.Id,
            Message = "Please verify the operational metrics section.",
            Status = ConfirmationStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.ConfirmationTags.Add(confirmation2);

        // Add a second report's data if available
        if (reports.Count > 1)
        {
            var report2 = reports[1];
            var pendingConfirmation = new ConfirmationTag
            {
                ReportId = report2.Id,
                RequestedById = report2.SubmittedById,
                TaggedUserId = reviewer.Id,
                Message = "Need your approval on the proposed budget changes.",
                Status = ConfirmationStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            context.ConfirmationTags.Add(pendingConfirmation);

            var declinedConfirmation = new ConfirmationTag
            {
                ReportId = report2.Id,
                RequestedById = report2.SubmittedById,
                TaggedUserId = author.Id,
                SectionReference = "Section 2: Metrics",
                Message = "Please confirm these metrics are correct.",
                Status = ConfirmationStatus.RevisionRequested,
                Response = "Some metrics need to be recalculated. Please review the attached notes.",
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                RespondedAt = DateTime.UtcNow.AddDays(-3)
            };
            context.ConfirmationTags.Add(declinedConfirmation);

            var comment3 = new Comment
            {
                ReportId = report2.Id,
                AuthorId = author.Id,
                Content = "This report requires immediate attention from the management team.",
                Status = CommentStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            context.Comments.Add(comment3);
        }

        await context.SaveChangesAsync();
    }
}
