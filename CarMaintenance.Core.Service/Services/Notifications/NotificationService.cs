using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Notifications;
using CarMaintenance.Core.Service.Abstraction.Services.Notifications;
using CarMaintenance.Core.Service.Hubs;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Notifications;
using CarMaintenance.Shared.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace CarMaintenance.Core.Service.Services.Notifications
{
    public class NotificationService(
        IUnitOfWork _unitOfWork,
        IMapper _mapper,
        IHubContext<NotificationHub> _hubContext
    ) : INotificationService
    {
        public async Task SendAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            string? actionUrl = null)
        {
            // Step 1: Save to database (persists for offline users)
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepo<Notification, int>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            // Step 2: Broadcast via SignalR (only reaches online users)
            var realTimeDto = new RealTimeNotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                ActionUrl = notification.ActionUrl,
                CreatedAt = notification.CreatedAt
            };

            // Clients.User() sends to ALL devices of this specific user
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", realTimeDto);
        }

        //  Returns paginated notifications
        public async Task<Pagination<NotificationDto>> GetMyNotificationsAsync( string userId,NotificationSpecParams specParams)
        {
            // Get paginated data
            var spec = new NotificationSpecification(userId, specParams);
            var notifications = await _unitOfWork.GetRepo<Notification, int>().GetAllWithSpecAsync(spec);

            // Get total count for pagination metadata
            var countSpec = new NotificationCountSpecification(userId);
            var totalCount = await _unitOfWork.GetRepo<Notification, int>().GetCountAsync(countSpec);

            var data = _mapper.Map<IEnumerable<NotificationDto>>(notifications);

            return new Pagination<NotificationDto>(specParams.PageIndex,specParams.PageSize, totalCount)
            {
                Data = data
            };
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var spec = new UnreadNotificationCountSpecification(userId);
            return await _unitOfWork.GetRepo<Notification, int>().GetCountAsync(spec);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _unitOfWork.GetRepo<Notification, int>().GetByIdAsync(notificationId);

            if (notification == null)
                throw new NotFoundException(nameof(Notification), notificationId);

            if (notification.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لتعديل هذا الإشعار");

            if (notification.IsRead)
                return; // Already read, do nothing

            notification.IsRead = true;
            _unitOfWork.GetRepo<Notification, int>().Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var spec = new UnreadNotificationSpecification(userId);
            var unread = (await _unitOfWork.GetRepo<Notification, int>().GetAllWithSpecAsync(spec, withTracking: true)).ToList();

            if (!unread.Any()) return;

            foreach (var notification in unread)
                notification.IsRead = true;

            await _unitOfWork.SaveChangesAsync();
        }
    }
}