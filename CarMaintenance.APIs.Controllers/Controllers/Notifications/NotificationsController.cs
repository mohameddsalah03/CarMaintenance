using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services.Notifications;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarMaintenance.APIs.Controllers.Controllers.Notifications
{
    [Authorize]
    public class NotificationsController(INotificationService _notificationService) : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<Pagination<NotificationDto>>> GetMyNotifications(
            [FromQuery] NotificationSpecParams specParams)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _notificationService.GetMyNotificationsAsync(userId, specParams);
            return Ok(result);
        }

        //  X-Unread-Count header added for frontend polling support
        [HttpGet("unread-count")]
        public async Task<ActionResult<object>> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = await _notificationService.GetUnreadCountAsync(userId);

            // Header allows frontend to read count without parsing JSON body
            Response.Headers.Append("X-Unread-Count", count.ToString());

            return Ok(new { unreadCount = count });
        }

        [HttpPatch("{id:int}/mark-read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(new { message = "تم تحديد الإشعار كمقروء" });
        }

        [HttpPatch("mark-all-read")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "تم تحديد جميع الإشعارات كمقروءة" });
        }
    }
}