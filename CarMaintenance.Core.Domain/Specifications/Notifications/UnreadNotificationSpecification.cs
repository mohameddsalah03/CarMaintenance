using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Notifications
{
    public class UnreadNotificationSpecification : BaseSpecifications<Notification, int>
    {
        public UnreadNotificationSpecification(string userId)
            : base(n => n.UserId == userId && !n.IsRead)
        {
        }
    }
}
