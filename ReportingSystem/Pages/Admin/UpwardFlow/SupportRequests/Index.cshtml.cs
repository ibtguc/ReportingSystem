using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.UpwardFlow.SupportRequests;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<SupportRequest> SupportRequests { get; set; } = new();
    public int TotalCount { get; set; }
    public int OpenCount { get; set; }
    public SelectList? UserList { get; set; }

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
        var query = _context.SupportRequests
            .Include(s => s.Report)
                .ThenInclude(r => r.SubmittedBy)
            .Include(s => s.Report.ReportTemplate)
            .Include(s => s.AssignedTo)
            .Include(s => s.AcknowledgedBy)
            .Include(s => s.ResolvedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(s => s.Status == StatusFilter);
        }

        if (!string.IsNullOrEmpty(CategoryFilter))
        {
            query = query.Where(s => s.Category == CategoryFilter);
        }

        if (!string.IsNullOrEmpty(UrgencyFilter))
        {
            query = query.Where(s => s.Urgency == UrgencyFilter);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(s =>
                s.Title.ToLower().Contains(term) ||
                (s.Description != null && s.Description.ToLower().Contains(term)));
        }

        TotalCount = await query.CountAsync();
        OpenCount = await query.Where(s => s.IsOpen).CountAsync();

        SupportRequests = await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(100)
            .ToListAsync();

        // Load users for assignment
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();
        UserList = new SelectList(users, "Id", "Name");
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus, string? resolution)
    {
        var request = await _context.SupportRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(resolution))
        {
            request.Resolution = resolution;
        }

        var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        User? currentUser = null;
        if (currentUserEmail != null)
        {
            currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
        }

        if (newStatus == SupportStatus.Acknowledged && currentUser != null)
        {
            request.AcknowledgedById = currentUser.Id;
            request.AcknowledgedAt = DateTime.UtcNow;
        }

        if ((newStatus == SupportStatus.Resolved || newStatus == SupportStatus.Closed) && currentUser != null)
        {
            request.ResolvedById = currentUser.Id;
            request.ResolvedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Status updated to {SupportStatus.DisplayName(newStatus)}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, int assignedToId)
    {
        var request = await _context.SupportRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.AssignedToId = assignedToId;
        request.UpdatedAt = DateTime.UtcNow;

        if (request.Status == SupportStatus.Submitted)
        {
            request.Status = SupportStatus.Acknowledged;
            var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (currentUserEmail != null)
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                if (currentUser != null)
                {
                    request.AcknowledgedById = currentUser.Id;
                    request.AcknowledgedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();

        var assignee = await _context.Users.FindAsync(assignedToId);
        TempData["SuccessMessage"] = $"Request assigned to {assignee?.Name ?? "user"}.";

        return RedirectToPage();
    }
}
