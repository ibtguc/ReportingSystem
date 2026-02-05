using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Reports;

public class ViewModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ViewModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Report Report { get; set; } = null!;
    public List<ReportField> Fields { get; set; } = new();
    public Dictionary<int, ReportFieldValue> FieldValues { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();

    // Upward flow items (Phase 4)
    public List<SuggestedAction> SuggestedActions { get; set; } = new();
    public List<ResourceRequest> ResourceRequests { get; set; } = new();
    public List<SupportRequest> SupportRequests { get; set; } = new();

    // Workflow & Tagging items (Phase 5)
    public List<Comment> Comments { get; set; } = new();
    public List<ConfirmationTag> ConfirmationTags { get; set; } = new();
    public List<User> AvailableUsers { get; set; } = new();

    // Downward flow items (Phase 6)
    public List<Feedback> Feedbacks { get; set; } = new();
    public List<Recommendation> Recommendations { get; set; } = new();
    public List<Decision> Decisions { get; set; } = new();

    [BindProperty]
    public string? ReviewComments { get; set; }

    [BindProperty]
    public string ReviewAction { get; set; } = "";

    [BindProperty]
    public string? NewCommentContent { get; set; }

    [BindProperty]
    public int? ReplyToCommentId { get; set; }

    [BindProperty]
    public int? TagUserId { get; set; }

    [BindProperty]
    public string? TagMessage { get; set; }

    [BindProperty]
    public string? TagSectionReference { get; set; }

    [BindProperty]
    public string? ConfirmationResponse { get; set; }

    [BindProperty]
    public string? ConfirmationAction { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadReportAsync(id);
    }

    public async Task<IActionResult> OnPostReviewAsync(int id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return NotFound();

        switch (ReviewAction)
        {
            case "approve":
                report.Status = ReportStatus.Approved;
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewComments = ReviewComments;
                report.IsLocked = true;
                TempData["SuccessMessage"] = "Report approved and locked.";
                break;

            case "reject":
                if (string.IsNullOrWhiteSpace(ReviewComments))
                {
                    ModelState.AddModelError("ReviewComments", "Comments are required when rejecting.");
                    return await LoadReportAsync(id);
                }
                report.Status = ReportStatus.Rejected;
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewComments = ReviewComments;
                TempData["SuccessMessage"] = "Report rejected.";
                break;

            case "amend":
                report.Status = ReportStatus.Amended;
                report.ReviewComments = ReviewComments;
                report.AmendmentCount++;
                report.IsLocked = false;
                TempData["SuccessMessage"] = "Report sent back for amendment.";
                break;

            case "review":
                report.Status = ReportStatus.UnderReview;
                // Set reviewer
                var reviewerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (reviewerEmail != null)
                {
                    var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Email == reviewerEmail);
                    if (reviewer != null)
                    {
                        report.AssignedReviewerId = reviewer.Id;
                    }
                }
                TempData["SuccessMessage"] = "Report marked as under review.";
                break;
        }

        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToPage("View", new { id });
    }

    private async Task<IActionResult> LoadReportAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
                .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .Include(r => r.FieldValues)
            .Include(r => r.Attachments)
            .Include(r => r.SuggestedActions)
                .ThenInclude(a => a.ReviewedBy)
            .Include(r => r.ResourceRequests)
                .ThenInclude(r => r.ReviewedBy)
            .Include(r => r.SupportRequests)
                .ThenInclude(s => s.AssignedTo)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();

        Report = report;

        Fields = await _context.ReportFields
            .Where(f => f.ReportTemplateId == report.ReportTemplateId && f.IsActive)
            .OrderBy(f => f.SectionOrder)
            .ThenBy(f => f.FieldOrder)
            .ToListAsync();

        FieldValues = report.FieldValues.ToDictionary(fv => fv.ReportFieldId);
        Attachments = report.Attachments.ToList();

        // Load upward flow items
        SuggestedActions = report.SuggestedActions.OrderBy(a => a.CreatedAt).ToList();
        ResourceRequests = report.ResourceRequests.OrderBy(r => r.CreatedAt).ToList();
        SupportRequests = report.SupportRequests.OrderBy(s => s.CreatedAt).ToList();

        // Load comments and confirmation tags (Phase 5)
        Comments = await _context.Comments
            .Where(c => c.ReportId == id && c.Status != CommentStatus.Deleted)
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => r.Status != CommentStatus.Deleted))
                .ThenInclude(r => r.Author)
            .Where(c => c.ParentCommentId == null) // Only top-level comments
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ConfirmationTags = await _context.ConfirmationTags
            .Where(ct => ct.ReportId == id)
            .Include(ct => ct.RequestedBy)
            .Include(ct => ct.TaggedUser)
            .OrderByDescending(ct => ct.CreatedAt)
            .ToListAsync();

        // Load available users for tagging
        AvailableUsers = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();

        // Load downward flow items (Phase 6)
        Feedbacks = await _context.Feedbacks
            .Where(f => f.ReportId == id && f.ParentFeedbackId == null)
            .Include(f => f.Author)
            .Include(f => f.Replies)
                .ThenInclude(r => r.Author)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        Recommendations = await _context.Recommendations
            .Where(r => r.ReportId == id)
            .Include(r => r.IssuedBy)
            .Include(r => r.TargetOrgUnit)
            .Include(r => r.TargetUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        Decisions = await _context.Decisions
            .Where(d => d.ReportId == id)
            .Include(d => d.DecidedBy)
            .Include(d => d.SuggestedAction)
            .Include(d => d.ResourceRequest)
            .Include(d => d.SupportRequest)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAddCommentAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(NewCommentContent))
        {
            TempData["ErrorMessage"] = "Comment content is required.";
            return RedirectToPage("View", new { id });
        }

        var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var currentUser = currentUserEmail != null
            ? await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail)
            : null;

        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Could not identify current user.";
            return RedirectToPage("View", new { id });
        }

        var comment = new Comment
        {
            ReportId = id,
            AuthorId = currentUser.Id,
            Content = NewCommentContent,
            ParentCommentId = ReplyToCommentId,
            Status = CommentStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = ReplyToCommentId.HasValue ? "Reply added." : "Comment added.";
        return RedirectToPage("View", new { id });
    }

    public async Task<IActionResult> OnPostDeleteCommentAsync(int id, int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null || comment.ReportId != id)
        {
            return NotFound();
        }

        comment.Status = CommentStatus.Deleted;
        comment.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Comment deleted.";
        return RedirectToPage("View", new { id });
    }

    public async Task<IActionResult> OnPostAddConfirmationTagAsync(int id)
    {
        if (!TagUserId.HasValue)
        {
            TempData["ErrorMessage"] = "Please select a user to tag.";
            return RedirectToPage("View", new { id });
        }

        var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var currentUser = currentUserEmail != null
            ? await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail)
            : null;

        if (currentUser == null)
        {
            TempData["ErrorMessage"] = "Could not identify current user.";
            return RedirectToPage("View", new { id });
        }

        var tag = new ConfirmationTag
        {
            ReportId = id,
            RequestedById = currentUser.Id,
            TaggedUserId = TagUserId.Value,
            Message = TagMessage,
            SectionReference = TagSectionReference,
            Status = ConfirmationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfirmationTags.Add(tag);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Confirmation request sent.";
        return RedirectToPage("View", new { id });
    }

    public async Task<IActionResult> OnPostRespondToConfirmationAsync(int id, int tagId)
    {
        var tag = await _context.ConfirmationTags.FindAsync(tagId);
        if (tag == null || tag.ReportId != id)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(ConfirmationAction))
        {
            TempData["ErrorMessage"] = "Please select a response action.";
            return RedirectToPage("View", new { id });
        }

        tag.Status = ConfirmationAction;
        tag.Response = ConfirmationResponse;
        tag.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Confirmation {ConfirmationStatus.DisplayName(ConfirmationAction).ToLower()}.";
        return RedirectToPage("View", new { id });
    }

    public async Task<IActionResult> OnPostCancelConfirmationAsync(int id, int tagId)
    {
        var tag = await _context.ConfirmationTags.FindAsync(tagId);
        if (tag == null || tag.ReportId != id)
        {
            return NotFound();
        }

        tag.Status = ConfirmationStatus.Cancelled;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Confirmation request cancelled.";
        return RedirectToPage("View", new { id });
    }
}
