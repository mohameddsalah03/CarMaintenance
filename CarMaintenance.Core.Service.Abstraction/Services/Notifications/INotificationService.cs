using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Notifications;

namespace CarMaintenance.Core.Service.Abstraction.Services.Notifications
{
    public interface INotificationService
    {
        Task SendAsync(string userId,string title,string message,NotificationType type,string? actionUrl = null);
        Task<Pagination<NotificationDto>> GetMyNotificationsAsync(string userId,NotificationSpecParams specParams);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}