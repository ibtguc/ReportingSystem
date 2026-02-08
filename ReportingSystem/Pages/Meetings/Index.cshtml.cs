using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Meetings;

[Authorize]
public class IndexModel : PageModel
{
    private readonly MeetingService _meetingService;
    private readonly ConfidentialityService _confidentialityService;

    public IndexModel(MeetingService meetingService, ConfidentialityService confidentialityService)
    {
        _meetingService = meetingService;
        _confidentialityService = confidentialityService;
    }

    public List<Meeting> Meetings { get; set; } = new();
    public bool CanSchedule { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CommitteeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public MeetingStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowMine { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludePast { get; set; }

    // Stats
    public int ScheduledCount { get; set; }
    public int InProgressCount { get; set; }
    public int MinutesReviewCount { get; set; }
    public int FinalizedCount { get; set; }

    public async Task OnGetAsync()
    {
        var userId = GetUserId();
        CanSchedule = await _meetingService.CanUserScheduleMeetingAsync(userId);

        if (ShowMine)
        {
            Meetings = await _meetingService.GetMeetingsForUserAsync(userId, IncludePast);
        }
        else
        {
            Meetings = await _meetingService.GetMeetingsAsync(
                committeeId: CommitteeId,
                status: Status,
                includePast: IncludePast);
        }

        if (ShowMine && Status.HasValue)
            Meetings = Meetings.Where(m => m.Status == Status.Value).ToList();

        // Filter out confidential items the user cannot access
        Meetings = await _confidentialityService.FilterAccessibleMeetingsAsync(Meetings, userId);

        var stats = await _meetingService.GetMeetingStatsAsync();
        ScheduledCount = stats.scheduled;
        InProgressCount = stats.inProgress;
        MinutesReviewCount = stats.minutesReview;
        FinalizedCount = stats.finalized;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
