using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Users;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public new User User { get; set; } = new();
    public List<MagicLink> RecentMagicLinks { get; set; } = new();

    /// <summary>
    /// Tracks which page the user came from so the back button returns correctly.
    /// Values: "tree" (Committee Tree), "committee" (Committee Details), default (Users list).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnTo { get; set; }

    /// <summary>
    /// When ReturnTo is "committee", this holds the committee ID to return to.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? CommitteeId { get; set; }

    public string BackUrl => ReturnTo switch
    {
        "tree" => "/Admin/Organization/CommitteeTree",
        "committee" when CommitteeId.HasValue => $"/Admin/Organization/Committees/Details/{CommitteeId.Value}",
        "committees" => "/Admin/Organization/Committees",
        "org" => "/Admin/Organization",
        "report" => "/Reports",
        "directive" => "/Directives",
        "meeting" => "/Meetings",
        "confidentiality" => "/Confidentiality",
        _ => "/Admin/Users"
    };

    public string BackLabel => ReturnTo switch
    {
        "tree" => "Back to Committee Tree",
        "committee" => "Back to Committee",
        "committees" => "Back to Committees",
        "org" => "Back to Org Tree",
        "report" => "Back to Reports",
        "directive" => "Back to Directives",
        "meeting" => "Back to Meetings",
        "confidentiality" => "Back",
        _ => "Back to Users"
    };

    public string BackIcon => ReturnTo switch
    {
        "tree" => "bi-diagram-2",
        "committee" => "bi-people",
        "committees" => "bi-people-fill",
        "org" => "bi-diagram-3",
        "report" => "bi-file-earmark-text",
        "directive" => "bi-signpost-split",
        "meeting" => "bi-calendar-event",
        _ => "bi-arrow-left"
    };

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .Include(u => u.MagicLinks)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        User = user;
        RecentMagicLinks = user.MagicLinks
            .OrderByDescending(ml => ml.CreatedAt)
            .Take(10)
            .ToList();

        return Page();
    }
}
