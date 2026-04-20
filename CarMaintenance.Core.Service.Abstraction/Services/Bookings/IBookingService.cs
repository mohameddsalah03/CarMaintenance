using CarMaintenance.Shared.DTOs.Bookings;
using CarMaintenance.Shared.DTOs.Bookings.Additionallssues;
using CarMaintenance.Shared.DTOs.Bookings.CreateBooking;
using CarMaintenance.Shared.DTOs.Bookings.Invoice;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;
using CarMaintenance.Shared.DTOs.Common;

namespace CarMaintenance.Core.Service.Abstraction.Services.Bookings
{
    public interface IBookingService 
    {
        //Customer
        Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto , string userId);
        Task<Pagination<BookingDto>> GetMyBookingsAsync(BookingSpecParams specParams, string userId);
        Task<BookingDetailsDto> GetBookingDetailsAsync(int id, string userId);
        Task<InvoiceDto> GetBookingInvoiceAsync(int bookingId, string userId);  
        Task ApproveAdditionalIssueAsync(ApproveAdditionalIssueDto approveAdditionalIssue, string userId);
        Task CancelBookingAsync(int id, string userId);


        //Technicians
        Task<Pagination<BookingDto>> GetMyAssignedBookingsAsync(BookingSpecParams specParams, string userId);
        Task<BookingDto> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto statusDto, string userId);
        Task<AdditionalIssueDto> AddAdditionalIssueAsync(int bookingId, AddAdditionalIssueDto issueDto , string userId);
        Task<BookingDetailsDto> GetBookingDetailsForTechnicianAsync(int id, string userId);


        //Admin 
        Task CancelBookingByAdminAsync(int id);
        Task<Pagination<BookingDto>> GetAllBookingsAsync(BookingSpecParams specParams);
        Task<BookingDto> AssignTechnicianAsync(int bookingId);

    }
}
