using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Workflow.Comments;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Comment> Comments { get; set; } = new();
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Comments
            .Include(c => c.Report)
                .ThenInclude(r => r.SubmittedBy)
            .Include(c => c.Report.ReportTemplate)
            .Include(c => c.Author)
            .Include(c => c.ParentComment)
            .AsQueryable();

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(c => c.Status == StatusFilter);
        }

        if (ReportId.HasValue)
        {
            query = query.Where(c => c.ReportId == ReportId.Value);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(c =>
                c.Content.ToLower().Contains(term) ||
                c.Author.Name.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        Comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        comment.Status = newStatus;
        comment.UpdatedAt = DateTime.UtcNow;

        if (newStatus == CommentStatus.Deleted)
        {
            comment.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Comment status updated to {CommentStatus.DisplayName(newStatus)}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        comment.Status = CommentStatus.Deleted;
        comment.DeletedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Comment deleted.";

        return RedirectToPage();
    }
}
