using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SchedulingSystem.Hubs
{
    /// <summary>
    /// SignalR hub for real-time timetable generation progress updates
    /// </summary>
    public class TimetableGenerationHub : Hub
    {
        /// <summary>
        /// Send progress update to all clients listening to a specific generation session
        /// </summary>
        public async Task SendProgress(string sessionId, int optionsGenerated, int combinationsExplored, string statusMessage)
        {
            await Clients.Group(sessionId).SendAsync("ProgressUpdate", optionsGenerated, combinationsExplored, statusMessage);
        }

        /// <summary>
        /// Join a generation session group
        /// </summary>
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        /// <summary>
        /// Leave a generation session group
        /// </summary>
        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}
