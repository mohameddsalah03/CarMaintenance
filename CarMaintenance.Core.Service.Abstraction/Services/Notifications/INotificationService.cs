using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Notifications;

namespace CarMaintenance.Core.Service.Abstraction.Services.Notifications
{
    public interface INotificationService
    {
        // Send notification + broadcast via SignalR
        Task SendAsync(string userId,string title,string message,NotificationType type,string? actionUrl = null);

        Task<Pagination<NotificationDto>> GetMyNotificationsAsync(string userId,NotificationSpecParams specParams);

        // Get unread count for bell badge
        Task<int> GetUnreadCountAsync(string userId);

        // Mark single notification as read
        Task MarkAsReadAsync(int notificationId, string userId);

        // Mark all as read
        Task MarkAllAsReadAsync(string userId);
    }
}