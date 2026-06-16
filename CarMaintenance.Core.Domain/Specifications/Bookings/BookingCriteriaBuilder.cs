using System.Linq.Expressions;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Bookings;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public static class BookingCriteriaBuilder
    {
        //public static Expression<Func<Booking, bool>> Build(BookingSpecParams p)
        //{
        //    var todayUtc = DateTime.UtcNow.Date;
        //    var tomorrowUtc = todayUtc.AddDays(1);

        //    return b =>
        //        (string.IsNullOrEmpty(p.Status) || b.Status.ToString() == p.Status) &&
        //        (string.IsNullOrEmpty(p.UserId) || b.UserId == p.UserId) &&
        //        (string.IsNullOrEmpty(p.TechnicianId) || b.TechnicianId == p.TechnicianId) &&
        //        (!p.VehicleId.HasValue || b.VehicleId == p.VehicleId.Value) &&
        //        (!p.TodayOnly || (b.ScheduledDate >= todayUtc && b.ScheduledDate < tomorrowUtc)) &&
        //        (!p.FromDate.HasValue || b.ScheduledDate >= p.FromDate.Value) &&
        //        (!p.ToDate.HasValue || b.ScheduledDate <= p.ToDate.Value);
        //}

        public static Expression<Func<Booking, bool>> Build(BookingSpecParams p)
        {
            var cairoTz = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cairoTz);
            var todayCairoStart = TimeZoneInfo.ConvertTimeToUtc(nowCairo.Date, cairoTz);
            var todayCairoEnd = todayCairoStart.AddDays(1);

            return b =>
                (string.IsNullOrEmpty(p.Status) || b.Status.ToString() == p.Status) &&
                (string.IsNullOrEmpty(p.UserId) || b.UserId == p.UserId) &&
                (string.IsNullOrEmpty(p.TechnicianId) || b.TechnicianId == p.TechnicianId) &&
                (!p.VehicleId.HasValue || b.VehicleId == p.VehicleId.Value) &&
                (!p.TodayOnly || (b.ScheduledDate >= todayCairoStart && b.ScheduledDate < todayCairoEnd)) &&
                (!p.FromDate.HasValue || b.ScheduledDate >= p.FromDate.Value) &&
                (!p.ToDate.HasValue || b.ScheduledDate <= p.ToDate.Value);
        }
    }
}