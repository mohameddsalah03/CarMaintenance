using CarMaintenance.Shared.DTOs.Bookings;
using CarMaintenance.Shared.DTOs.Bookings.Additionallssues;
using CarMaintenance.Shared.DTOs.Bookings.CreateBooking;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Common;

namespace CarMaintenance.Core.Service.Abstraction.Services.Bookings
{
    public interface IBookingService 
    {
        //Customer
        Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto , string userId);
        Task<Pagination<BookingDto>> GetMyBookingsAsync(BookingSpecParams specParams, string userId);
        Task<BookingDetailsDto> GetBookingDetailsAsync(int id, string userId);
        Task CancelBookingAsync(int id, string userId);

        //Technicians
        Task<Pagination<BookingDto>> GetMyAssignedBookingsAsync(BookingSpecParams specParams, string technicianId);
        Task<BookingDto> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto statusDto, string technicianId);
        Task<AdditionalIssueDto> AddAdditionalIssueAsync(int bookingId, AddAdditionalIssueDto issueDto);


        //Admin 
        Task<Pagination<BookingDto>> GetAllBookingsAsync(BookingSpecParams specParams);
        Task<BookingDto> AssignTechnicianAsync(int id, AssignTechnicianDto assignDto);


        // Additional Issues
        Task ApproveAdditionalIssueAsync(ApproveAdditionalIssueDto approveAdditionalIssue, string userId);

    }
}
