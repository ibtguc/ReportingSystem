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

        // Seed downward flow data (feedback, recommendations, decisions) if reports exist but no feedbacks
        if (await context.Reports.AnyAsync() && !await context.Feedbacks.AnyAsync())
        {
            await SeedDownwardFlowDataAsync(context);
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

    private static async Task SeedDownwardFlowDataAsync(ApplicationDbContext context)
    {
        // Get reports and users for seeding
        var reports = await context.Reports
            .Include(r => r.SubmittedBy)
            .Include(r => r.SuggestedActions)
            .Include(r => r.ResourceRequests)
            .Take(3)
            .ToListAsync();

        if (reports.Count == 0) return;

        var users = await context.Users.Take(5).ToListAsync();
        if (users.Count < 2) return;

        var report = reports[0];
        var manager = users[0];
        var reviewer = users.Count > 1 ? users[1] : users[0];

        // Seed Feedback
        var feedback1 = new Feedback
        {
            ReportId = report.Id,
            AuthorId = manager.Id,
            Subject = "Excellent Q4 Performance",
            Content = "The team has shown exceptional performance this quarter. The metrics demonstrate a clear improvement in efficiency.",
            Category = FeedbackCategory.PositiveRecognition,
            Visibility = FeedbackVisibility.TeamWide,
            Status = FeedbackStatus.Active,
            IsAcknowledged = true,
            AcknowledgedAt = DateTime.UtcNow.AddDays(-3),
            AcknowledgmentResponse = "Thank you for the recognition. The team worked very hard this quarter.",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        context.Feedbacks.Add(feedback1);

        var feedback2 = new Feedback
        {
            ReportId = report.Id,
            AuthorId = reviewer.Id,
            Subject = "Budget Clarification Needed",
            Content = "Please clarify the budget allocation in Section 3. Some figures need verification before approval.",
            Category = FeedbackCategory.Question,
            Visibility = FeedbackVisibility.Private,
            Status = FeedbackStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.Feedbacks.Add(feedback2);

        var feedback3 = new Feedback
        {
            ReportId = report.Id,
            AuthorId = manager.Id,
            Subject = "Timeline Concerns",
            Content = "The proposed timeline for the new initiative seems aggressive. Consider extending it by 2 weeks.",
            Category = FeedbackCategory.Concern,
            Visibility = FeedbackVisibility.Private,
            Status = FeedbackStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.Feedbacks.Add(feedback3);

        // Seed Recommendations
        var orgUnit = await context.OrganizationalUnits.FirstOrDefaultAsync();

        var recommendation1 = new Recommendation
        {
            ReportId = report.Id,
            IssuedById = manager.Id,
            TargetOrgUnitId = orgUnit?.Id,
            Title = "Implement Weekly Stand-ups",
            Description = "Based on the report findings, I recommend implementing weekly stand-up meetings to improve communication.",
            Rationale = "The report indicates communication gaps between team members that are affecting productivity.",
            Category = RecommendationCategory.ProcessChange,
            Priority = RecommendationPriority.High,
            TargetScope = RecommendationScope.Team,
            Status = RecommendationStatus.Issued,
            DueDate = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };
        context.Recommendations.Add(recommendation1);

        var recommendation2 = new Recommendation
        {
            ReportId = report.Id,
            IssuedById = reviewer.Id,
            TargetUserId = report.SubmittedById,
            Title = "Complete Project Management Training",
            Description = "Enroll in the advanced project management course to enhance leadership skills.",
            Category = RecommendationCategory.SkillDevelopment,
            Priority = RecommendationPriority.Medium,
            TargetScope = RecommendationScope.Individual,
            Status = RecommendationStatus.Acknowledged,
            AcknowledgmentCount = 1,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        context.Recommendations.Add(recommendation2);

        // Seed Decisions (linked to suggested actions or resource requests if available)
        var suggestedAction = report.SuggestedActions.FirstOrDefault();
        if (suggestedAction != null)
        {
            var decision1 = new Decision
            {
                ReportId = report.Id,
                DecidedById = manager.Id,
                RequestType = DecisionRequestType.SuggestedAction,
                SuggestedActionId = suggestedAction.Id,
                Title = $"Decision on: {suggestedAction.Title}",
                Outcome = DecisionOutcome.Approved,
                Justification = "The suggested action aligns with our strategic goals and has a clear ROI.",
                EffectiveDate = DateTime.UtcNow.AddDays(7),
                IsAcknowledged = true,
                AcknowledgedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            context.Decisions.Add(decision1);
        }

        var resourceRequest = report.ResourceRequests.FirstOrDefault();
        if (resourceRequest != null)
        {
            var decision2 = new Decision
            {
                ReportId = report.Id,
                DecidedById = manager.Id,
                RequestType = DecisionRequestType.ResourceRequest,
                ResourceRequestId = resourceRequest.Id,
                Title = $"Budget Decision: {resourceRequest.Title}",
                Outcome = DecisionOutcome.ApprovedWithModifications,
                Justification = "Approved with reduced scope. Initial phase funding approved.",
                ApprovedAmount = resourceRequest.EstimatedCost.HasValue ? resourceRequest.EstimatedCost.Value * 0.7m : 5000m,
                Currency = resourceRequest.Currency ?? "USD",
                Modifications = "Approved for Phase 1 only. Phase 2 funding will be reviewed after Q1 results.",
                Conditions = "Monthly progress reports required. Budget reallocation from training fund.",
                EffectiveDate = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            context.Decisions.Add(decision2);
        }

        // Add pending decision
        if (reports.Count > 1)
        {
            var report2 = reports[1];
            var decision3 = new Decision
            {
                ReportId = report2.Id,
                DecidedById = reviewer.Id,
                RequestType = DecisionRequestType.SuggestedAction,
                Title = "Process Improvement Initiative",
                Outcome = DecisionOutcome.Deferred,
                Justification = "Good proposal but requires more analysis. Deferred to next quarter planning cycle.",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            context.Decisions.Add(decision3);
        }

        await context.SaveChangesAsync();
    }
}
