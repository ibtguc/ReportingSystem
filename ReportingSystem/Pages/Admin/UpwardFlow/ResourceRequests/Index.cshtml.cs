using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.UpwardFlow.ResourceRequests;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<ResourceRequest> ResourceRequests { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalApprovedAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UrgencyFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.ResourceRequests
            .Include(r => r.Report)
                .ThenInclude(r => r.SubmittedBy)
            .Include(r => r.Report.ReportTemplate)
            .Include(r => r.ReviewedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(r => r.Status == StatusFilter);
        }

        if (!string.IsNullOrEmpty(CategoryFilter))
        {
            query = query.Where(r => r.Category == CategoryFilter);
        }

        if (!string.IsNullOrEmpty(UrgencyFilter))
        {
            query = query.Where(r => r.Urgency == UrgencyFilter);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(term) ||
                (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        TotalCount = await query.CountAsync();
        TotalEstimatedCost = await query.Where(r => r.EstimatedCost.HasValue).SumAsync(r => r.EstimatedCost!.Value);
        TotalApprovedAmount = await query.Where(r => r.ApprovedAmount.HasValue).SumAsync(r => r.ApprovedAmount!.Value);

        ResourceRequests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus, decimal? approvedAmount)
    {
        var request = await _context.ResourceRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;

        if (approvedAmount.HasValue)
        {
            request.ApprovedAmount = approvedAmount.Value;
        }

        if (newStatus == ResourceStatus.Approved ||
            newStatus == ResourceStatus.PartiallyApproved ||
            newStatus == ResourceStatus.Rejected ||
            newStatus == ResourceStatus.Fulfilled)
        {
            request.ReviewedAt = DateTime.UtcNow;
            var reviewerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (reviewerEmail != null)
            {
                var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Email == reviewerEmail);
                if (reviewer != null)
                {
                    request.ReviewedById = reviewer.Id;
                }
            }

            if (newStatus == ResourceStatus.Fulfilled)
            {
                request.FulfilledAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Status updated to {ResourceStatus.DisplayName(newStatus)}.";

        return RedirectToPage();
    }
}
