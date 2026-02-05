using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ReportingSystem.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        // Redirect authenticated users to Dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Admin/Dashboard");
        }

        // Redirect anonymous users to login
        return RedirectToPage("/Auth/Login");
    }
}
