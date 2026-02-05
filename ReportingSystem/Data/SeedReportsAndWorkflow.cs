using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Seeds sample reports, field values, upward/downward flow data, comments, and confirmations.
/// </summary>
public static class SeedReportsAndWorkflow
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;

        // ========== SAMPLE REPORTS ==========
        var reports = new List<Report>
        {
            // Report 1: Approved - Head of Software Dev Monthly Report Jan 2026
            new()
            {
                Id = 1,
                ReportTemplateId = 1,
                ReportPeriodId = 1, // January 2026
                SubmittedById = 18, // Eng. Mahmoud Adel
                AssignedReviewerId = 3, // VP Admin
                Status = ReportStatus.Approved,
                SubmittedAt = now.AddDays(-25),
                ReviewedAt = now.AddDays(-23),
                ReviewComments = "Good comprehensive report. Approved.",
                IsLocked = true,
                LastAutoSaveAt = now.AddDays(-25),
                AmendmentCount = 0,
                WasPrePopulated = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-23)
            },
            // Report 2: Draft - Head of Infrastructure Feb 2026
            new()
            {
                Id = 2,
                ReportTemplateId = 1,
                ReportPeriodId = 2, // February 2026
                SubmittedById = 19, // Eng. Heba Mostafa
                Status = ReportStatus.Draft,
                IsLocked = false,
                LastAutoSaveAt = now.AddDays(-1),
                AmendmentCount = 0,
                WasPrePopulated = false,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1)
            },
            // Report 3: Submitted - Backend Team Lead Weekly Report
            new()
            {
                Id = 3,
                ReportTemplateId = 2,
                ReportPeriodId = 5, // Week of Feb 03
                SubmittedById = 26, // Eng. Youssef Magdy
                AssignedReviewerId = 22, // Eng. Ali Kamal (Web Section Lead)
                Status = ReportStatus.Submitted,
                SubmittedAt = now.AddDays(-1),
                IsLocked = false,
                LastAutoSaveAt = now.AddDays(-1),
                AmendmentCount = 0,
                WasPrePopulated = false,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-1)
            },
            // Report 4: Approved - Head of Infrastructure IT Health Jan 2026
            new()
            {
                Id = 4,
                ReportTemplateId = 5,
                ReportPeriodId = 8, // January 2026 IT
                SubmittedById = 19, // Eng. Heba Mostafa
                AssignedReviewerId = 3, // VP Admin
                Status = ReportStatus.Approved,
                SubmittedAt = now.AddDays(-20),
                ReviewedAt = now.AddDays(-18),
                ReviewComments = "Excellent uptime numbers. Approved.",
                IsLocked = true,
                LastAutoSaveAt = now.AddDays(-20),
                AmendmentCount = 0,
                WasPrePopulated = false,
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddDays(-18)
            }
        };

        context.Reports.AddRange(reports);
        await context.SaveChangesAsync();

        // ========== REPORT FIELD VALUES ==========
        await SeedReportFieldValuesAsync(context, now);

        // ========== UPWARD FLOW DATA ==========
        await SeedSuggestedActionsAsync(context, now);
        await SeedResourceRequestsAsync(context, now);
        await SeedSupportRequestsAsync(context, now);

        // ========== WORKFLOW DATA ==========
        await SeedCommentsAsync(context, now);
        await SeedConfirmationTagsAsync(context, now);

        // ========== DOWNWARD FLOW DATA ==========
        await SeedFeedbacksAsync(context, now);
        await SeedRecommendationsAsync(context, now);
        await SeedDecisionsAsync(context, now);
    }

    private static async Task SeedReportFieldValuesAsync(ApplicationDbContext context, DateTime now)
    {
        var fieldValues = new List<ReportFieldValue>
        {
            // Report 1 field values (Software Dev Monthly)
            new() { ReportId = 1, ReportFieldId = 1, Value = "<p>The Software Development department had a productive January. We completed 3 major projects and are on track with our Q1 roadmap. Team morale is high after the successful launch of the Student Portal v2.</p>", CreatedAt = now.AddDays(-25) },
            new() { ReportId = 1, ReportFieldId = 2, Value = "24", NumericValue = 24, CreatedAt = now.AddDays(-25) },
            new() { ReportId = 1, ReportFieldId = 3, Value = "3", NumericValue = 3, CreatedAt = now.AddDays(-25) },
            new() { ReportId = 1, ReportFieldId = 4, Value = "5", NumericValue = 5, CreatedAt = now.AddDays(-25) },
            new() { ReportId = 1, ReportFieldId = 5, Value = "72", NumericValue = 72, CreatedAt = now.AddDays(-25) },
            new() { ReportId = 1, ReportFieldId = 6, Value = "On Track", CreatedAt = now.AddDays(-25) },
            new() { ReportId = 1, ReportFieldId = 7, Value = "<ul><li>Student Portal v2 launched successfully</li><li>API gateway migration completed</li><li>Automated testing coverage increased to 85%</li></ul>", CreatedAt = now.AddDays(-25) },
            new() { ReportId = 1, ReportFieldId = 8, Value = "<p>Two senior developers resigned, creating a knowledge gap in the mobile team. Recruitment is in progress.</p>", CreatedAt = now.AddDays(-25) },

            // Report 3 field values (Backend Team Weekly)
            new() { ReportId = 3, ReportFieldId = 11, Value = "Sprint 4 is progressing well. Completed migration of authentication service to new identity provider.", CreatedAt = now.AddDays(-1) },
            new() { ReportId = 3, ReportFieldId = 12, Value = "8", NumericValue = 8, CreatedAt = now.AddDays(-1) },
            new() { ReportId = 3, ReportFieldId = 13, Value = "4", NumericValue = 4, CreatedAt = now.AddDays(-1) },
            new() { ReportId = 3, ReportFieldId = 14, Value = "<p>Waiting for DevOps team to provision staging environment for new API.</p>", CreatedAt = now.AddDays(-1) },
            new() { ReportId = 3, ReportFieldId = 16, Value = "Complete API integration tests. Begin user acceptance testing for the reporting module.", CreatedAt = now.AddDays(-1) },

            // Report 4 field values (IT Infrastructure Health)
            new() { ReportId = 4, ReportFieldId = 17, Value = "99.7", NumericValue = 99.7m, CreatedAt = now.AddDays(-20) },
            new() { ReportId = 4, ReportFieldId = 18, Value = "12", NumericValue = 12, CreatedAt = now.AddDays(-20) },
            new() { ReportId = 4, ReportFieldId = 19, Value = "1", NumericValue = 1, CreatedAt = now.AddDays(-20) },
            new() { ReportId = 4, ReportFieldId = 20, Value = "Yellow (60-80%)", CreatedAt = now.AddDays(-20) },
            new() { ReportId = 4, ReportFieldId = 21, Value = "65", NumericValue = 65, CreatedAt = now.AddDays(-20) }
        };

        context.ReportFieldValues.AddRange(fieldValues);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSuggestedActionsAsync(ApplicationDbContext context, DateTime now)
    {
        var actions = new List<SuggestedAction>
        {
            new() { Id = 1, ReportId = 1, Title = "Implement automated code review tool", Description = "Integrate SonarQube or similar static analysis tool into our CI/CD pipeline to catch code quality issues early.", Justification = "Manual code reviews are time-consuming and inconsistent. Automated tools can catch 70% of common issues.", ExpectedOutcome = "Reduced code review time by 40%, improved code quality consistency.", Timeline = "Q2 2026", Category = ActionCategory.QualityEnhancement, Priority = ActionPriority.High, Status = ActionStatus.Approved, ReviewedById = 3, ReviewedAt = now.AddDays(-20), ReviewComments = "Approved. Please coordinate with IT Infrastructure for licensing.", CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-20) },
            new() { Id = 2, ReportId = 1, Title = "Establish developer mentorship program", Description = "Create a structured mentorship program pairing senior developers with junior staff for knowledge transfer.", Justification = "Recent resignations highlighted knowledge concentration risk. Need systematic knowledge sharing.", ExpectedOutcome = "Improved knowledge retention, faster onboarding of new hires, reduced bus factor risk.", Timeline = "March 2026", Category = ActionCategory.RiskMitigation, Priority = ActionPriority.Medium, Status = ActionStatus.Implemented, ReviewedById = 3, ReviewedAt = now.AddDays(-18), ReviewComments = "Great initiative. Already implementing.", CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-18) },
            new() { Id = 3, ReportId = 1, Title = "Migrate to containerized deployments", Description = "Move all applications to Docker/Kubernetes for improved scalability and deployment consistency.", Justification = "Current deployment process is manual and error-prone. Containers provide reproducibility.", ExpectedOutcome = "Faster deployments, reduced environment-related bugs, easier scaling.", Timeline = "Q3 2026", Category = ActionCategory.ProcessImprovement, Priority = ActionPriority.Medium, Status = ActionStatus.UnderReview, CreatedAt = now.AddDays(-25) },
            new() { Id = 4, ReportId = 3, Title = "Implement API rate limiting", Description = "Add rate limiting to public APIs to prevent abuse and ensure fair usage.", Justification = "Recent spike in API calls caused performance degradation for legitimate users.", ExpectedOutcome = "Improved API stability, protection against abuse, better resource allocation.", Timeline = "Feb 2026", Category = ActionCategory.RiskMitigation, Priority = ActionPriority.High, Status = ActionStatus.Submitted, CreatedAt = now.AddDays(-1) },
            new() { Id = 5, ReportId = 3, Title = "Reduce database query redundancy", Description = "Implement caching layer for frequently accessed but rarely changing data.", Justification = "Database profiling shows 40% of queries are redundant within same session.", ExpectedOutcome = "Reduced database load, improved response times, lower infrastructure costs.", Timeline = "March 2026", Category = ActionCategory.CostReduction, Priority = ActionPriority.Medium, Status = ActionStatus.Submitted, CreatedAt = now.AddDays(-1) },
            new() { Id = 6, ReportId = 4, Title = "Implement predictive monitoring with ML", Description = "Use machine learning to predict infrastructure failures before they occur based on metrics patterns.", Justification = "Reactive monitoring causes downtime. Predictive approach can prevent 60% of incidents.", ExpectedOutcome = "Reduced unplanned downtime, proactive maintenance, improved SLA compliance.", Timeline = "Q2 2026", Category = ActionCategory.Innovation, Priority = ActionPriority.High, Status = ActionStatus.Approved, ReviewedById = 3, ReviewedAt = now.AddDays(-15), ReviewComments = "Excellent proposal. Allocate resources from innovation budget.", CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-15) }
        };

        context.SuggestedActions.AddRange(actions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedResourceRequestsAsync(ApplicationDbContext context, DateTime now)
    {
        var requests = new List<ResourceRequest>
        {
            new() { Id = 1, ReportId = 1, Title = "Senior Backend Developer Hire", Description = "Request to hire 2 senior backend developers to fill positions vacated by resignations.", Quantity = 2, Justification = "Team is understaffed after 2 resignations. Current workload is unsustainable.", Category = ResourceCategory.Personnel, Urgency = ResourceUrgency.High, EstimatedCost = 480000m, Currency = "EGP", Status = ResourceStatus.Approved, ApprovedAmount = 480000m, ReviewedById = 20, ReviewedAt = now.AddDays(-22), CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-22) },
            new() { Id = 2, ReportId = 1, Title = "JetBrains Team Tools License", Description = "Annual license for JetBrains IDEs (IntelliJ, WebStorm, ReSharper) for the development team.", Quantity = 24, Justification = "Current licenses expiring. Tool is critical for developer productivity.", Category = ResourceCategory.Software, Urgency = ResourceUrgency.Medium, EstimatedCost = 45000m, Currency = "EGP", Status = ResourceStatus.Fulfilled, ApprovedAmount = 45000m, ReviewedById = 3, ReviewedAt = now.AddDays(-20), FulfilledAt = now.AddDays(-19), CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-19) },
            new() { Id = 3, ReportId = 1, Title = "Cloud Infrastructure Budget Increase", Description = "Request 25% increase to monthly cloud infrastructure budget for new projects.", Justification = "New projects requiring additional cloud resources. Current budget fully utilized.", Category = ResourceCategory.Budget, Urgency = ResourceUrgency.Medium, EstimatedCost = 25000m, Currency = "EGP", Status = ResourceStatus.PartiallyApproved, ApprovedAmount = 15000m, ReviewedById = 3, ReviewedAt = now.AddDays(-18), CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-18) },
            new() { Id = 4, ReportId = 3, Title = "Developer Training - AWS Certification", Description = "AWS Solutions Architect certification training for 3 team members.", Quantity = 3, Justification = "Team needs AWS skills for upcoming cloud migration project.", Category = ResourceCategory.Training, Urgency = ResourceUrgency.Medium, EstimatedCost = 15000m, Currency = "EGP", Status = ResourceStatus.Submitted, CreatedAt = now.AddDays(-1) },
            new() { Id = 5, ReportId = 3, Title = "High-Performance Development Workstations", Description = "Upgrade to M3 MacBook Pro for backend team members for improved build times.", Quantity = 4, Justification = "Current machines struggle with Docker and IDE simultaneously. Build times are 3x longer than needed.", Category = ResourceCategory.Equipment, Urgency = ResourceUrgency.Low, EstimatedCost = 320000m, Currency = "EGP", Status = ResourceStatus.Submitted, CreatedAt = now.AddDays(-1) },
            new() { Id = 6, ReportId = 4, Title = "Additional Server Rack", Description = "New server rack to accommodate growth in on-premise infrastructure.", Quantity = 1, Justification = "Current capacity at 75%. Expected growth will exceed capacity by Q3.", Category = ResourceCategory.Facilities, Urgency = ResourceUrgency.High, EstimatedCost = 150000m, Currency = "EGP", Status = ResourceStatus.Approved, ApprovedAmount = 150000m, ReviewedById = 3, ReviewedAt = now.AddDays(-17), CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-17) },
            new() { Id = 7, ReportId = 4, Title = "Network Monitoring Software", Description = "Enterprise license for PRTG Network Monitor for comprehensive monitoring.", Quantity = 1, Justification = "Current monitoring has gaps. Need unified view across all network segments.", Category = ResourceCategory.Software, Urgency = ResourceUrgency.Medium, EstimatedCost = 35000m, Currency = "EGP", Status = ResourceStatus.Fulfilled, ApprovedAmount = 35000m, ReviewedById = 3, ReviewedAt = now.AddDays(-18), FulfilledAt = now.AddDays(-16), CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-16) }
        };

        context.ResourceRequests.AddRange(requests);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSupportRequestsAsync(ApplicationDbContext context, DateTime now)
    {
        var requests = new List<SupportRequest>
        {
            new() { Id = 1, ReportId = 1, Title = "Expedite procurement for development tools", Description = "Need management intervention to speed up procurement process for critical development tools.", CurrentSituation = "Procurement request submitted 45 days ago still pending. Standard lead time is 21 days.", DesiredOutcome = "Approval within 1 week to prevent project delays.", Category = SupportCategory.ManagementIntervention, Urgency = SupportUrgency.High, Status = SupportStatus.Resolved, Resolution = "Escalated to VP Admin. Procurement approved within 3 days.", AssignedToId = 3, AcknowledgedById = 3, AcknowledgedAt = now.AddDays(-24), ResolvedById = 3, ResolvedAt = now.AddDays(-22), CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-22) },
            new() { Id = 2, ReportId = 1, Title = "Cross-team API integration coordination", Description = "Need coordination between Backend Team and IT Infrastructure for API gateway setup.", CurrentSituation = "Teams working in silos. API gateway configuration not aligned with backend requirements.", DesiredOutcome = "Joint planning session and aligned technical specifications.", Category = SupportCategory.CrossDeptCoordination, Urgency = SupportUrgency.Medium, Status = SupportStatus.Closed, Resolution = "Meeting held between teams. Technical specs aligned. Integration completed.", AssignedToId = 24, AcknowledgedById = 18, AcknowledgedAt = now.AddDays(-23), ResolvedById = 18, ResolvedAt = now.AddDays(-20), CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-20) },
            new() { Id = 3, ReportId = 3, Title = "Staging environment provisioning delay", Description = "DevOps team has not provisioned the requested staging environment for 2 weeks.", CurrentSituation = "Cannot proceed with integration testing. Sprint velocity affected.", DesiredOutcome = "Staging environment available within 3 days.", Category = SupportCategory.CrossDeptCoordination, Urgency = SupportUrgency.High, Status = SupportStatus.InProgress, AssignedToId = 25, AcknowledgedById = 22, AcknowledgedAt = now, CreatedAt = now.AddDays(-1), UpdatedAt = now },
            new() { Id = 4, ReportId = 3, Title = "Database performance optimization assistance", Description = "Need DBA assistance for query optimization in the reporting module.", CurrentSituation = "Complex queries taking 10+ seconds. Team lacks deep SQL optimization expertise.", DesiredOutcome = "Queries optimized to under 2 seconds response time.", Category = SupportCategory.TechnicalAssistance, Urgency = SupportUrgency.Medium, Status = SupportStatus.Submitted, CreatedAt = now.AddDays(-1) },
            new() { Id = 5, ReportId = 4, Title = "Security policy clarification for cloud resources", Description = "Need clarification on security policies for hybrid cloud deployments.", CurrentSituation = "Conflicting guidance from IT Security and Compliance departments.", DesiredOutcome = "Unified security policy document for hybrid cloud.", Category = SupportCategory.PolicyClarification, Urgency = SupportUrgency.Medium, Status = SupportStatus.Resolved, Resolution = "Met with IT Security and Compliance. Created unified policy document.", AssignedToId = 21, AcknowledgedById = 19, AcknowledgedAt = now.AddDays(-18), ResolvedById = 21, ResolvedAt = now.AddDays(-15), CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-15) },
            new() { Id = 6, ReportId = 4, Title = "Vendor negotiation support", Description = "Need management support for contract negotiation with network equipment vendor.", CurrentSituation = "Vendor offering 10% discount. Believe 20% is achievable based on competitor quotes.", DesiredOutcome = "Better pricing terms saving estimated 50,000 EGP annually.", Category = SupportCategory.ManagementIntervention, Urgency = SupportUrgency.Low, Status = SupportStatus.Acknowledged, AssignedToId = 3, AcknowledgedById = 3, AcknowledgedAt = now.AddDays(-17), CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-17) }
        };

        context.SupportRequests.AddRange(requests);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCommentsAsync(ApplicationDbContext context, DateTime now)
    {
        var comments = new List<Comment>
        {
            // Comments on Report 1
            new() { Id = 1, ReportId = 1, AuthorId = 3, Content = "Excellent progress on the Student Portal v2. The launch metrics look very promising. @head.sdev@guc.edu.eg can you share the user adoption numbers?", Status = CommentStatus.Active, SectionReference = "Key Achievements", MentionedUserIdsJson = "[18]", CreatedAt = now.AddDays(-24) },
            new() { Id = 2, ReportId = 1, AuthorId = 18, Content = "Thank you! First week shows 85% adoption rate among active students. Full metrics report will be shared next week.", Status = CommentStatus.Active, ParentCommentId = 1, CreatedAt = now.AddDays(-24).AddHours(2) },
            new() { Id = 3, ReportId = 1, AuthorId = 9, Content = "The automated testing coverage improvement is noteworthy. This aligns with our faculty-wide quality initiative.", Status = CommentStatus.Active, SectionReference = "Key Metrics", CreatedAt = now.AddDays(-23) },
            new() { Id = 4, ReportId = 1, AuthorId = 20, Content = "Regarding the two senior developer resignations - HR is prioritizing the recruitment. @mgr.backend@guc.edu.eg please coordinate with HR for technical interviews.", Status = CommentStatus.Active, SectionReference = "Current Challenges", MentionedUserIdsJson = "[26]", CreatedAt = now.AddDays(-22) },
            new() { Id = 5, ReportId = 1, AuthorId = 26, Content = "Confirmed. I have already sent the technical requirements to HR and am available for interviews next week.", Status = CommentStatus.Active, ParentCommentId = 4, CreatedAt = now.AddDays(-22).AddHours(3) },
            // Comments on Report 3
            new() { Id = 6, ReportId = 3, AuthorId = 22, Content = "Good progress on the authentication migration. However, the staging environment blocker needs immediate attention. @mgr.cloud@guc.edu.eg can you prioritize this?", Status = CommentStatus.Active, SectionReference = "Blockers", MentionedUserIdsJson = "[25]", CreatedAt = now.AddHours(-12) },
            new() { Id = 7, ReportId = 3, AuthorId = 25, Content = "Apologies for the delay. We had capacity issues. Environment will be ready by end of day tomorrow.", Status = CommentStatus.Active, ParentCommentId = 6, CreatedAt = now.AddHours(-10) },
            // Comment on Report 4
            new() { Id = 8, ReportId = 4, AuthorId = 3, Content = "The 99.7% uptime is excellent. Please prepare a brief for the next executive meeting on how we achieved this.", Status = CommentStatus.Active, SectionReference = "Availability", CreatedAt = now.AddDays(-17) }
        };

        context.Comments.AddRange(comments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedConfirmationTagsAsync(ApplicationDbContext context, DateTime now)
    {
        var tags = new List<ConfirmationTag>
        {
            // Report 1 confirmations
            new() { ReportId = 1, RequestedById = 18, TaggedUserId = 34, SectionReference = "Key Metrics", Message = "Please confirm the testing coverage numbers are accurate based on our CI/CD reports.", Status = ConfirmationStatus.Confirmed, Response = "Verified. The 85% coverage matches our SonarQube dashboard.", CreatedAt = now.AddDays(-26), RespondedAt = now.AddDays(-25), ExpiresAt = now.AddDays(-19) },
            new() { ReportId = 1, RequestedById = 18, TaggedUserId = 26, SectionReference = "Key Achievements", Message = "Please confirm the Student Portal launch details are complete.", Status = ConfirmationStatus.Confirmed, Response = "Confirmed. Launch date and features list is accurate.", CreatedAt = now.AddDays(-26), RespondedAt = now.AddDays(-26).AddHours(4), ExpiresAt = now.AddDays(-19) },
            // Report 3 confirmations
            new() { ReportId = 3, RequestedById = 26, TaggedUserId = 37, SectionReference = "Metrics", Message = "Can you verify the task completion numbers from Jira?", Status = ConfirmationStatus.Confirmed, Response = "Numbers match Jira sprint report.", CreatedAt = now.AddDays(-2), RespondedAt = now.AddDays(-1), ExpiresAt = now.AddDays(5) },
            new() { ReportId = 3, RequestedById = 26, TaggedUserId = 38, SectionReference = "Blockers", Message = "Please confirm the staging environment issue description is accurate.", Status = ConfirmationStatus.Pending, CreatedAt = now.AddDays(-1), ExpiresAt = now.AddDays(6) },
            // Report 4 confirmations
            new() { ReportId = 4, RequestedById = 19, TaggedUserId = 24, SectionReference = "Incidents", Message = "Please verify the incident counts match our ITSM records.", Status = ConfirmationStatus.Confirmed, Response = "Verified against ServiceNow. Numbers are correct.", CreatedAt = now.AddDays(-21), RespondedAt = now.AddDays(-20), ExpiresAt = now.AddDays(-14) },
            new() { ReportId = 4, RequestedById = 19, TaggedUserId = 25, SectionReference = "Capacity", Message = "Please confirm server capacity status is current.", Status = ConfirmationStatus.RevisionRequested, Response = "The capacity figure needs updating - we added 2 new servers last week. Please update to Green status.", CreatedAt = now.AddDays(-21), RespondedAt = now.AddDays(-20), ExpiresAt = now.AddDays(-14) }
        };

        context.ConfirmationTags.AddRange(tags);
        await context.SaveChangesAsync();
    }

    private static async Task SeedFeedbacksAsync(ApplicationDbContext context, DateTime now)
    {
        var feedbacks = new List<Feedback>
        {
            new() { ReportId = 1, AuthorId = 3, Subject = "Excellent team performance", Content = "The Software Development department has shown exceptional performance this month. The Student Portal v2 launch demonstrates strong execution capabilities.", Category = FeedbackCategory.PositiveRecognition, Visibility = FeedbackVisibility.DepartmentWide, SectionReference = "General", IsAcknowledged = true, AcknowledgedAt = now.AddDays(-22), AcknowledgmentResponse = "Thank you for the recognition. The team worked hard and we are proud of the outcome.", Status = FeedbackStatus.Active, CreatedAt = now.AddDays(-23), UpdatedAt = now.AddDays(-22) },
            new() { ReportId = 1, AuthorId = 3, Subject = "Staffing concern noted", Content = "The loss of two senior developers is concerning. Please ensure knowledge transfer documentation is prioritized and keep me updated on recruitment progress.", Category = FeedbackCategory.Concern, Visibility = FeedbackVisibility.Private, SectionReference = "Current Challenges", ReportFieldId = 8, IsAcknowledged = true, AcknowledgedAt = now.AddDays(-21), AcknowledgmentResponse = "Understood. We have initiated knowledge transfer sessions and HR is actively recruiting.", Status = FeedbackStatus.Resolved, CreatedAt = now.AddDays(-22), UpdatedAt = now.AddDays(-21) },
            new() { ReportId = 1, AuthorId = 2, Subject = "Alignment with academic goals", Content = "The automated testing improvements align well with our academic quality standards. Consider sharing this approach with other technical departments.", Category = FeedbackCategory.Observation, Visibility = FeedbackVisibility.OrganizationWide, SectionReference = "Key Metrics", Status = FeedbackStatus.Active, CreatedAt = now.AddDays(-20) },
            new() { ReportId = 3, AuthorId = 22, Subject = "Question about API rate limiting", Content = "What specific rate limits are you proposing? We need to ensure they don't affect legitimate high-volume API users.", Category = FeedbackCategory.Question, Visibility = FeedbackVisibility.TeamWide, SectionReference = "Issues", Status = FeedbackStatus.Active, CreatedAt = now.AddHours(-6) },
            new() { ReportId = 4, AuthorId = 3, Subject = "Outstanding uptime achievement", Content = "The 99.7% uptime is among the best in our sector. This sets a benchmark for reliability excellence.", Category = FeedbackCategory.PositiveRecognition, Visibility = FeedbackVisibility.OrganizationWide, SectionReference = "Availability", IsAcknowledged = true, AcknowledgedAt = now.AddDays(-16), AcknowledgmentResponse = "Thank you. The team has worked diligently on proactive monitoring and quick incident response.", Status = FeedbackStatus.Active, CreatedAt = now.AddDays(-17), UpdatedAt = now.AddDays(-16) },
            new() { ReportId = 4, AuthorId = 1, Subject = "Capacity planning observation", Content = "Yellow status on server capacity needs attention before it becomes critical. Please include a capacity expansion plan in next month's report.", Category = FeedbackCategory.Concern, Visibility = FeedbackVisibility.Private, SectionReference = "Capacity", IsAcknowledged = true, AcknowledgedAt = now.AddDays(-15), AcknowledgmentResponse = "Noted. We have already procured additional server rack (approved in this report). Expansion plan will be detailed next month.", Status = FeedbackStatus.Resolved, CreatedAt = now.AddDays(-16), UpdatedAt = now.AddDays(-15) }
        };

        context.Feedbacks.AddRange(feedbacks);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRecommendationsAsync(ApplicationDbContext context, DateTime now)
    {
        var recommendations = new List<Recommendation>
        {
            new() { ReportId = 1, IssuedById = 3, TargetOrgUnitId = 22, Title = "Implement mandatory code documentation standards", Description = "All code changes must include documentation updates. Establish documentation review as part of PR process.", Rationale = "Recent resignations highlighted knowledge concentration risk. Proper documentation ensures continuity.", Timeline = "Implement within 30 days", Category = RecommendationCategory.ProcessChange, Priority = RecommendationPriority.High, TargetScope = RecommendationScope.Department, Status = RecommendationStatus.InProgress, EffectiveDate = now.AddDays(-20), DueDate = now.AddDays(10), CascadeToSubUnits = true, AcknowledgmentCount = 3, CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-15) },
            new() { ReportId = 4, IssuedById = 3, TargetOrgUnitId = 23, Title = "Establish disaster recovery drills", Description = "Conduct quarterly DR drills to ensure backup and recovery procedures are tested and staff are trained.", Rationale = "High uptime is excellent but DR readiness ensures business continuity under adverse conditions.", Timeline = "First drill by end of Q1 2026", Category = RecommendationCategory.Compliance, Priority = RecommendationPriority.Medium, TargetScope = RecommendationScope.Department, Status = RecommendationStatus.Acknowledged, EffectiveDate = now.AddDays(-15), DueDate = now.AddDays(45), CascadeToSubUnits = true, AcknowledgmentCount = 2, CreatedAt = now.AddDays(-15) },
            new() { IssuedById = 1, TargetOrgUnitId = 1, Title = "Adopt standardized reporting metrics", Description = "All departments should align on common KPIs for reporting consistency across the organization.", Rationale = "Current reports use inconsistent metrics making cross-department comparison difficult.", Timeline = "Complete by Q2 2026", Category = RecommendationCategory.StrategicAlignment, Priority = RecommendationPriority.High, TargetScope = RecommendationScope.OrganizationWide, Status = RecommendationStatus.Issued, EffectiveDate = now.AddDays(-10), DueDate = now.AddDays(90), CascadeToSubUnits = true, CreatedAt = now.AddDays(-10) },
            new() { ReportId = 3, IssuedById = 22, TargetUserId = 26, Title = "Complete AWS certification training", Description = "Backend team lead should complete AWS Solutions Architect certification to support cloud migration efforts.", Rationale = "Cloud migration is strategic priority. Team lead needs certification for architecture decisions.", Timeline = "Complete by Q2 2026", Category = RecommendationCategory.SkillDevelopment, Priority = RecommendationPriority.Medium, TargetScope = RecommendationScope.Individual, Status = RecommendationStatus.Acknowledged, EffectiveDate = now.AddDays(-5), DueDate = now.AddDays(90), AcknowledgmentCount = 1, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-3) },
            new() { IssuedById = 2, TargetOrgUnitId = 4, Title = "Increase industry collaboration", Description = "Engineering faculty should establish at least 3 new industry partnerships for student internships and research collaboration.", Rationale = "Industry partnerships improve student employability and research relevance.", Timeline = "Establish by end of 2026", Category = RecommendationCategory.StrategicAlignment, Priority = RecommendationPriority.Medium, TargetScope = RecommendationScope.Department, Status = RecommendationStatus.Issued, EffectiveDate = now.AddDays(-7), DueDate = now.AddDays(300), CascadeToSubUnits = true, CreatedAt = now.AddDays(-7) }
        };

        context.Recommendations.AddRange(recommendations);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDecisionsAsync(ApplicationDbContext context, DateTime now)
    {
        var decisions = new List<Decision>
        {
            new() { ReportId = 1, DecidedById = 3, RequestType = DecisionRequestType.SuggestedAction, SuggestedActionId = 1, Title = "Decision: Automated code review tool", Outcome = DecisionOutcome.Approved, Justification = "Automated code review aligns with quality improvement goals. SonarQube is approved.", Conditions = "Must coordinate with IT Infrastructure for licensing and integration.", EffectiveDate = now.AddDays(-18), IsAcknowledged = true, AcknowledgedAt = now.AddDays(-17), CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-17) },
            new() { ReportId = 4, DecidedById = 3, RequestType = DecisionRequestType.SuggestedAction, SuggestedActionId = 6, Title = "Decision: Predictive monitoring with ML", Outcome = DecisionOutcome.ApprovedWithModifications, Justification = "Predictive monitoring approved but scope reduced to pilot phase first.", Conditions = "Pilot with network monitoring only. Full implementation pending pilot results.", EffectiveDate = now.AddDays(-14), ApprovedAmount = 25000m, Currency = "EGP", Modifications = "Start with network segment only. Expand after 3-month pilot shows positive results.", IsAcknowledged = true, AcknowledgedAt = now.AddDays(-13), CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-13) },
            new() { ReportId = 1, DecidedById = 20, RequestType = DecisionRequestType.ResourceRequest, ResourceRequestId = 1, Title = "Decision: Senior Backend Developer Hire", Outcome = DecisionOutcome.Approved, Justification = "Critical staffing gap must be addressed. Full budget approved for 2 senior developer positions.", Conditions = "Recruitment must be completed within 60 days.", EffectiveDate = now.AddDays(-21), ApprovedAmount = 480000m, Currency = "EGP", IsAcknowledged = true, AcknowledgedAt = now.AddDays(-20), CreatedAt = now.AddDays(-22), UpdatedAt = now.AddDays(-20) },
            new() { ReportId = 1, DecidedById = 3, RequestType = DecisionRequestType.ResourceRequest, ResourceRequestId = 3, Title = "Decision: Cloud Infrastructure Budget", Outcome = DecisionOutcome.PartiallyApproved, Justification = "Budget increase approved at 60% of requested amount. Full increase requires Q2 review.", Conditions = "Utilization report required monthly. Full increase subject to Q2 budget review.", EffectiveDate = now.AddDays(-17), ApprovedAmount = 15000m, Currency = "EGP", Modifications = "Approved 15,000 EGP instead of requested 25,000 EGP. Remainder subject to Q2 review.", IsAcknowledged = true, AcknowledgedAt = now.AddDays(-16), CreatedAt = now.AddDays(-18), UpdatedAt = now.AddDays(-16) },
            new() { ReportId = 4, DecidedById = 3, RequestType = DecisionRequestType.SupportRequest, SupportRequestId = 6, Title = "Decision: Vendor negotiation support", Outcome = DecisionOutcome.Approved, Justification = "Management will support vendor negotiation. Procurement team assigned to assist.", Conditions = "Procurement team lead will join negotiation meeting.", EffectiveDate = now.AddDays(-16), IsAcknowledged = true, AcknowledgedAt = now.AddDays(-15), CreatedAt = now.AddDays(-17), UpdatedAt = now.AddDays(-15) },
            new() { ReportId = 1, DecidedById = 3, RequestType = DecisionRequestType.SuggestedAction, SuggestedActionId = 3, Title = "Decision: Containerized deployments", Outcome = DecisionOutcome.Deferred, Justification = "Good proposal but requires more planning. Defer to Q3 2026 for proper resource allocation.", ReferredTo = "IT Architecture Committee for detailed planning", CreatedAt = now.AddDays(-18) }
        };

        context.Decisions.AddRange(decisions);
        await context.SaveChangesAsync();
    }
}
