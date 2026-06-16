using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Bookings;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public class BookingWithFiltrationForCountSpecification : BaseSpecifications<Booking, int>
    {
        public BookingWithFiltrationForCountSpecification(BookingSpecParams specParams)
            : base(BookingCriteriaBuilder.Build(specParams))
        {
            // No includes عشان الـ COUNT query متحملش بيانات زيادة
        }


    }
}