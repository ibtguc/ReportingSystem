using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.AuditLog;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly AuditService _auditService;

    public DetailsModel(AuditService auditService)
    {
        _auditService = auditService;
    }

    public Models.AuditLog AuditEntry { get; set; } = null!;
    public List<Models.AuditLog> RelatedEntries { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entry = await _auditService.GetAuditLogByIdAsync(id);
        if (entry == null) return NotFound();

        AuditEntry = entry;

        if (!string.IsNullOrEmpty(entry.ItemType) && entry.ItemId.HasValue)
        {
            RelatedEntries = await _auditService.GetItemHistoryAsync(entry.ItemType, entry.ItemId.Value);
        }

        return Page();
    }
}
