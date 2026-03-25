using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Domain.Specifications.Reviews;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services.Reviews;
using CarMaintenance.Shared.DTOs.Reviews;
using CarMaintenance.Shared.Exceptions;

namespace CarMaintenance.Core.Service.Services.Reviews
{
    internal class ReviewService(IUnitOfWork _unitOfWork , IMapper _mapper) : IReviewService
    {

        public async Task<IEnumerable<ReviewDto>> GetAllReviewsAsync()
        {
            var spec = new ReviewSpecification();
            var reviews = await _unitOfWork.GetRepo<Review, int>().GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
        }


        public async Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, string userId)
        {
            var spec = new BookingSpecification(dto.BookingId);
            var booking = await _unitOfWork.GetRepo<Booking,int>().GetWithSpecAsync(spec);

            if (booking is null)
                throw new NotFoundException(nameof(Booking), dto.BookingId);

            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لتقييم هذا الحجز");

            if (booking.Status != BookingStatus.Completed)
                throw new BadRequestException("لا يمكن تقييم حجز غير مكتمل");

            if (string.IsNullOrEmpty(booking.TechnicianId))
                throw new BadRequestException("لا يوجد فني معين لهذا الحجز");

            var existingReview = new ReviewSpecification(dto.BookingId);
            var existing = await _unitOfWork.GetRepo<Review,int>().GetWithSpecAsync(existingReview);
            if (existing is not null)
                throw new BadRequestException("لقد قمت بتقييم هذا الحجز مسبقاً");


            var review = new Review
            {
                BookingId = dto.BookingId,
                ServiceRating = dto.ServiceRating,
                Comment = dto.Comment??"",
                TechnicianRating = dto.TechnicianRating,
                TechnicianId = booking.TechnicianId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.GetRepo<Review, int>().AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            await UpdateTechnicianRatingAsync(booking.TechnicianId);

            var reviewSpec = new ReviewSpecification(dto.BookingId);
            var created = await _unitOfWork.GetRepo<Review, int>().GetWithSpecAsync(reviewSpec);
            return _mapper.Map<ReviewDto>(created!);

        }

        public async Task<ReviewDto?> GetBookingReviewAsync(int bookingId, string userId)
        {

            var spec = new ReviewSpecification(bookingId);
            var review = await _unitOfWork.GetRepo<Review, int>().GetWithSpecAsync(spec);

            if (review is null) return null;

            if (review.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لعرض هذا التقييم");

            return _mapper.Map<ReviewDto>(review);
        }

        public async Task<IEnumerable<ReviewDto>> GetMyReceivedReviewsAsync(string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                throw new ForbiddenException("لست فنياً معتمداً في النظام");

            var spec = new ReviewSpecification(technician.Id, byTechnician: true);
            var reviews = await _unitOfWork.GetRepo<Review, int>().GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<ReviewDto>>(reviews);

        }



        #region Helper Methods 
        private async Task UpdateTechnicianRatingAsync(string technicianId)
        {
            var spec = new ReviewSpecification(technicianId, byTechnician: true);
            var reviews = (await _unitOfWork.GetRepo<Review, int>().GetAllWithSpecAsync(spec)).ToList();

            if (!reviews.Any()) return;

            var avg = reviews.Average(r => r.TechnicianRating);

            var technician = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(technicianId);
            if (technician is null) return;

            technician.Rating = (decimal)Math.Round(avg, 2);
            _unitOfWork.GetRepo<Technician, string>().Update(technician);
            await _unitOfWork.SaveChangesAsync();
        }

        #endregion


    }
}
