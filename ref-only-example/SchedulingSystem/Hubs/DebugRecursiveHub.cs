using Microsoft.AspNetCore.SignalR;

namespace SchedulingSystem.Hubs
{
    /// <summary>
    /// SignalR hub for real-time debug updates from the Recursive Conflict Resolution algorithm
    /// </summary>
    public class DebugRecursiveHub : Hub
    {
        /// <summary>
        /// Join a specific debug session group
        /// </summary>
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.SendAsync("Joined", sessionId);
        }

        /// <summary>
        /// Leave a debug session group
        /// </summary>
        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}
