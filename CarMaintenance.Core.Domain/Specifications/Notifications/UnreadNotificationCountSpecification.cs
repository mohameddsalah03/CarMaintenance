using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Notifications
{
    public class UnreadNotificationCountSpecification : BaseSpecifications<Notification, int>
    {
        public UnreadNotificationCountSpecification(string userId)
            : base(n => n.UserId == userId && !n.IsRead)
        {
        }
    }
}
