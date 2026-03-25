using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Notifications;

namespace CarMaintenance.Core.Domain.Specifications.Notifications
{
    public class NotificationSpecification : BaseSpecifications<Notification, int>
    {
        public NotificationSpecification(string userId, NotificationSpecParams specParams)
            : base(n => n.UserId == userId &&
                        (!specParams.IsRead.HasValue || n.IsRead == specParams.IsRead.Value))
        {
            AddOrderByDesc(n => n.CreatedAt);
            AddPagination(
                specParams.PageSize * (specParams.PageIndex - 1),
                specParams.PageSize);
        }

        //  Simple overload without pagination (for internal use)
        public NotificationSpecification(string userId)
            : base(n => n.UserId == userId)
        {
            AddOrderByDesc(n => n.CreatedAt);
        }
    }


}