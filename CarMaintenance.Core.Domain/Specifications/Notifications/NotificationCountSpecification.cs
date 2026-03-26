using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Notifications;

namespace CarMaintenance.Core.Domain.Specifications.Notifications
{
    public class NotificationCountSpecification : BaseSpecifications<Notification, int>
    {
        //  Matches the same filter as NotificationSpecification
        public NotificationCountSpecification(string userId, NotificationSpecParams specParams)
            : base(n => n.UserId == userId &&
                        (!specParams.IsRead.HasValue || n.IsRead == specParams.IsRead.Value))
        {
        }

        // Simple overload for total count
        public NotificationCountSpecification(string userId)
            : base(n => n.UserId == userId)
        {
        }
    }
}
