using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Meetings;

[Authorize]
public class CreateModel : PageModel
{
    private readonly MeetingService _meetingService;
    private readonly NotificationService _notificationService;

    public CreateModel(MeetingService meetingService, NotificationService notificationService)
    {
        _meetingService = meetingService;
        _notificationService = notificationService;
    }

    [BindProperty]
    public Meeting Meeting { get; set; } = new();

    [BindProperty]
    public bool AddCommitteeMembers { get; set; } = true;

    public List<SelectListItem> CommitteeOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (!await _meetingService.CanUserScheduleMeetingAsync(userId))
        {
            TempData["ErrorMessage"] = "You do not have permission to schedule meetings.";
            return RedirectToPage("Index");
        }

        await LoadCommitteesAsync(userId);
        Meeting.ScheduledAt = DateTime.UtcNow.AddDays(1).Date.AddHours(10); // Default: tomorrow 10am
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetUserId();
        if (!await _meetingService.CanUserScheduleMeetingAsync(userId))
        {
            TempData["ErrorMessage"] = "You do not have permission to schedule meetings.";
            return RedirectToPage("Index");
        }

        ModelState.Remove("Meeting.Committee");
        ModelState.Remove("Meeting.Moderator");

        if (!ModelState.IsValid)
        {
            await LoadCommitteesAsync(userId);
            return Page();
        }

        var meeting = await _meetingService.CreateMeetingAsync(Meeting, userId);

        if (AddCommitteeMembers)
        {
            await _meetingService.AddAttendeesFromCommitteeAsync(meeting.Id, meeting.CommitteeId);
        }

        // Notify all attendees
        var created = await _meetingService.GetMeetingByIdAsync(meeting.Id);
        if (created != null)
        {
            foreach (var attendee in created.Attendees.Where(a => a.UserId != userId))
            {
                await _notificationService.NotifyMeetingInvitationAsync(meeting.Id, meeting.Title, attendee.UserId);
            }
        }

        TempData["SuccessMessage"] = $"Meeting \"{meeting.Title}\" scheduled successfully.";
        return RedirectToPage("Details", new { id = meeting.Id });
    }

    private async Task LoadCommitteesAsync(int userId)
    {
        var committees = await _meetingService.GetSchedulableCommitteesAsync(userId);
        CommitteeOptions = committees.Select(c => new SelectListItem(
            $"{c.Name} ({c.HierarchyLevel})", c.Id.ToString())).ToList();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
