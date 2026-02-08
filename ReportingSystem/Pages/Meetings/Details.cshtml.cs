using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace ReportingSystem.Pages.Meetings;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly MeetingService _meetingService;
    private readonly ConfidentialityService _confidentialityService;
    private readonly ApplicationDbContext _context;

    public DetailsModel(MeetingService meetingService, ConfidentialityService confidentialityService, ApplicationDbContext context)
    {
        _meetingService = meetingService;
        _confidentialityService = confidentialityService;
        _context = context;
    }

    public Meeting Meeting { get; set; } = null!;
    public bool IsModerator { get; set; }
    public bool IsAttendee { get; set; }
    public MeetingAttendee? CurrentAttendee { get; set; }

    // For adding attendees
    public List<SelectListItem> AvailableUsers { get; set; } = new();

    // For adding agenda items
    [BindProperty]
    public MeetingAgendaItem NewAgendaItem { get; set; } = new();

    // For adding decisions
    [BindProperty]
    public MeetingDecision NewDecision { get; set; } = new();

    // For adding action items
    [BindProperty]
    public ActionItem NewActionItem { get; set; } = new();

    [BindProperty]
    public int AddAttendeeUserId { get; set; }

    [BindProperty]
    public string? RsvpComment { get; set; }

    [BindProperty]
    public string? ConfirmationComment { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var meeting = await _meetingService.GetMeetingByIdAsync(id);
        if (meeting == null) return NotFound();

        // Confidentiality access check
        if (meeting.IsConfidential)
        {
            var checkUserId = GetUserId();
            if (!await _confidentialityService.CanUserAccessConfidentialItemAsync(
                ConfidentialItemType.Meeting, id, checkUserId))
            {
                TempData["ErrorMessage"] = "You do not have access to this confidential meeting.";
                return RedirectToPage("Index");
            }
        }

        Meeting = meeting;
        var userId = GetUserId();
        IsModerator = meeting.ModeratorId == userId;
        IsAttendee = meeting.Attendees.Any(a => a.UserId == userId);
        CurrentAttendee = meeting.Attendees.FirstOrDefault(a => a.UserId == userId);

        await LoadAvailableUsersAsync();
        return Page();
    }

    // ── Attendee Management ──

    public async Task<IActionResult> OnPostAddAttendeeAsync(int id)
    {
        await _meetingService.AddAttendeeAsync(id, AddAttendeeUserId);
        TempData["SuccessMessage"] = "Attendee added.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveAttendeeAsync(int id, int userId)
    {
        await _meetingService.RemoveAttendeeAsync(id, userId);
        TempData["SuccessMessage"] = "Attendee removed.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddCommitteeMembersAsync(int id)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null) return NotFound();

        await _meetingService.AddAttendeesFromCommitteeAsync(id, meeting.CommitteeId);
        TempData["SuccessMessage"] = "Committee members invited.";
        return RedirectToPage(new { id });
    }

    // ── RSVP ──

    public async Task<IActionResult> OnPostRsvpAcceptAsync(int id)
    {
        await _meetingService.UpdateRsvpAsync(id, GetUserId(), RsvpStatus.Accepted, RsvpComment);
        TempData["SuccessMessage"] = "RSVP accepted.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRsvpDeclineAsync(int id)
    {
        await _meetingService.UpdateRsvpAsync(id, GetUserId(), RsvpStatus.Declined, RsvpComment);
        TempData["SuccessMessage"] = "RSVP declined.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRsvpTentativeAsync(int id)
    {
        await _meetingService.UpdateRsvpAsync(id, GetUserId(), RsvpStatus.Tentative, RsvpComment);
        TempData["SuccessMessage"] = "RSVP marked tentative.";
        return RedirectToPage(new { id });
    }

    // ── Meeting Status Transitions ──

    public async Task<IActionResult> OnPostStartMeetingAsync(int id)
    {
        await _meetingService.StartMeetingAsync(id);
        TempData["SuccessMessage"] = "Meeting started.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostBeginMinutesAsync(int id)
    {
        await _meetingService.BeginMinutesEntryAsync(id);
        return RedirectToPage("/Meetings/Minutes", new { id });
    }

    public async Task<IActionResult> OnPostCancelMeetingAsync(int id)
    {
        await _meetingService.CancelMeetingAsync(id);
        TempData["SuccessMessage"] = "Meeting cancelled.";
        return RedirectToPage("Index");
    }

    // ── Agenda Items ──

    public async Task<IActionResult> OnPostAddAgendaItemAsync(int id)
    {
        ModelState.Remove("NewAgendaItem.Meeting");
        NewAgendaItem.MeetingId = id;

        if (string.IsNullOrWhiteSpace(NewAgendaItem.TopicTitle))
        {
            TempData["ErrorMessage"] = "Agenda item title is required.";
            return RedirectToPage(new { id });
        }

        await _meetingService.AddAgendaItemAsync(NewAgendaItem);
        TempData["SuccessMessage"] = "Agenda item added.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveAgendaItemAsync(int id, int agendaItemId)
    {
        await _meetingService.RemoveAgendaItemAsync(agendaItemId);
        TempData["SuccessMessage"] = "Agenda item removed.";
        return RedirectToPage(new { id });
    }

    // ── Decisions ──

    public async Task<IActionResult> OnPostAddDecisionAsync(int id)
    {
        ModelState.Remove("NewDecision.Meeting");
        NewDecision.MeetingId = id;

        if (string.IsNullOrWhiteSpace(NewDecision.DecisionText))
        {
            TempData["ErrorMessage"] = "Decision text is required.";
            return RedirectToPage(new { id });
        }

        await _meetingService.AddDecisionAsync(NewDecision);
        TempData["SuccessMessage"] = "Decision recorded.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveDecisionAsync(int id, int decisionId)
    {
        await _meetingService.RemoveDecisionAsync(decisionId);
        TempData["SuccessMessage"] = "Decision removed.";
        return RedirectToPage(new { id });
    }

    // ── Action Items ──

    public async Task<IActionResult> OnPostAddActionItemAsync(int id)
    {
        ModelState.Remove("NewActionItem.Meeting");
        ModelState.Remove("NewActionItem.AssignedTo");
        ModelState.Remove("NewActionItem.AssignedBy");

        NewActionItem.MeetingId = id;
        NewActionItem.AssignedById = GetUserId();

        if (string.IsNullOrWhiteSpace(NewActionItem.Title))
        {
            TempData["ErrorMessage"] = "Action item title is required.";
            return RedirectToPage(new { id });
        }

        await _meetingService.CreateActionItemAsync(NewActionItem);
        TempData["SuccessMessage"] = "Action item created.";
        return RedirectToPage(new { id });
    }

    // ── Minutes Confirmation ──

    public async Task<IActionResult> OnPostConfirmMinutesAsync(int id)
    {
        await _meetingService.UpdateConfirmationAsync(id, GetUserId(), ConfirmationStatus.Confirmed, ConfirmationComment);

        // Try to finalize if all confirmed
        await _meetingService.TryFinalizeMinutesAsync(id);

        TempData["SuccessMessage"] = "Minutes confirmed.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRequestRevisionAsync(int id)
    {
        await _meetingService.UpdateConfirmationAsync(id, GetUserId(), ConfirmationStatus.RevisionRequested, ConfirmationComment);
        TempData["SuccessMessage"] = "Revision requested.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAbstainAsync(int id)
    {
        await _meetingService.UpdateConfirmationAsync(id, GetUserId(), ConfirmationStatus.Abstained, ConfirmationComment);

        // Try to finalize if all responded
        await _meetingService.TryFinalizeMinutesAsync(id);

        TempData["SuccessMessage"] = "Abstention recorded.";
        return RedirectToPage(new { id });
    }

    private async Task LoadAvailableUsersAsync()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem(u.Name, u.Id.ToString()))
            .ToListAsync();
        AvailableUsers = users;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
