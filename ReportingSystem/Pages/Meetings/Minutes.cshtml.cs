using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Meetings;

[Authorize]
public class MinutesModel : PageModel
{
    private readonly MeetingService _meetingService;

    public MinutesModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    public Meeting Meeting { get; set; } = null!;

    [BindProperty]
    public string MinutesContent { get; set; } = string.Empty;

    [BindProperty]
    public List<AgendaItemNotes> AgendaItemNotesList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var meeting = await _meetingService.GetMeetingByIdAsync(id);
        if (meeting == null) return NotFound();

        var userId = GetUserId();
        if (meeting.ModeratorId != userId)
        {
            TempData["ErrorMessage"] = "Only the moderator can edit minutes.";
            return RedirectToPage("Details", new { id });
        }

        // Ensure meeting is in correct state
        if (meeting.Status != MeetingStatus.MinutesEntry && meeting.Status != MeetingStatus.MinutesReview
            && meeting.Status != MeetingStatus.InProgress && meeting.Status != MeetingStatus.Scheduled)
        {
            TempData["ErrorMessage"] = "Minutes cannot be edited in the current meeting state.";
            return RedirectToPage("Details", new { id });
        }

        Meeting = meeting;
        MinutesContent = meeting.MinutesContent ?? string.Empty;

        // Pre-populate agenda item notes
        AgendaItemNotesList = meeting.AgendaItems
            .OrderBy(a => a.OrderIndex)
            .Select(a => new AgendaItemNotes
            {
                AgendaItemId = a.Id,
                TopicTitle = a.TopicTitle,
                Notes = a.DiscussionNotes ?? string.Empty
            }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        var meeting = await _meetingService.GetMeetingByIdAsync(id);
        if (meeting == null) return NotFound();

        var userId = GetUserId();
        if (meeting.ModeratorId != userId)
        {
            TempData["ErrorMessage"] = "Only the moderator can edit minutes.";
            return RedirectToPage("Details", new { id });
        }

        // Save per-agenda-item discussion notes
        if (AgendaItemNotesList != null)
        {
            foreach (var notes in AgendaItemNotesList)
            {
                await _meetingService.UpdateAgendaDiscussionNotesAsync(notes.AgendaItemId, notes.Notes);
            }
        }

        // Transition to MinutesEntry if not already
        if (meeting.Status == MeetingStatus.Scheduled || meeting.Status == MeetingStatus.InProgress)
        {
            await _meetingService.BeginMinutesEntryAsync(id);
        }

        // Save overall minutes content (not submitting yet)
        meeting.MinutesContent = MinutesContent;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _meetingService.UpdateMeetingAsync(meeting);

        TempData["SuccessMessage"] = "Minutes saved as draft.";
        return RedirectToPage("Minutes", new { id });
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var meeting = await _meetingService.GetMeetingByIdAsync(id);
        if (meeting == null) return NotFound();

        var userId = GetUserId();
        if (meeting.ModeratorId != userId)
        {
            TempData["ErrorMessage"] = "Only the moderator can submit minutes.";
            return RedirectToPage("Details", new { id });
        }

        // Save per-agenda-item discussion notes first
        if (AgendaItemNotesList != null)
        {
            foreach (var notes in AgendaItemNotesList)
            {
                await _meetingService.UpdateAgendaDiscussionNotesAsync(notes.AgendaItemId, notes.Notes);
            }
        }

        await _meetingService.SubmitMinutesAsync(id, MinutesContent);

        TempData["SuccessMessage"] = "Minutes submitted for attendee confirmation.";
        return RedirectToPage("Details", new { id });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}

public class AgendaItemNotes
{
    public int AgendaItemId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
