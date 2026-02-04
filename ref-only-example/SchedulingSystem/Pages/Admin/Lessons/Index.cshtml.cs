using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchedulingSystem.Data;

namespace SchedulingSystem.Pages.Admin.Lessons
{
    /// <summary>
    /// Redirects to Dashboard page - Index page has been replaced by more comprehensive Dashboard
    /// </summary>
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect to Dashboard which has all the functionality of Index plus much more
            return RedirectToPage("/Admin/Lessons/Dashboard");
        }
    }
}
