using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CarMaintenance.Core.Service.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // This method is called automatically when a client connects
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            Console.WriteLine($"[SignalR] User connected: {userName} (ID: {userId}), " + $"ConnectionId: {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        // This method is called automatically when a client disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            Console.WriteLine(
                $"[SignalR] User disconnected: {userId}, " +
                $"ConnectionId: {Context.ConnectionId}");

            await base.OnDisconnectedAsync(exception);
        }
    }
}