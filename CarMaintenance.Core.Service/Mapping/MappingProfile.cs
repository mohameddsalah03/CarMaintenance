using AutoMapper;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.DTOs.Vehicles;

namespace CarMaintenance.Core.Service.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Services
            CreateMap<Domain.Models.Data.Service, ServiceDto>();
            CreateMap<CreateServiceDto, Domain.Models.Data.Service>();
            CreateMap<UpdateServiceDto, Domain.Models.Data.Service>();

            //Vehicles
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(d => d.OwnerName, options => options.MapFrom(src => src.Owner.DisplayName));
            CreateMap<CreateVehicleDto, Vehicle>();
            CreateMap<UpdateVehicleDto, Vehicle>();

            // Technicians
            CreateMap<Technician, TechniciansDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.Id))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber));

            CreateMap<TechniciansDto, Technician>();

            // Bookings 
            

            // Bookings
            CreateMap<Booking, BookingDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.User.Id))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Vehicle.Brand))
                .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Vehicle.Model))
                .ForMember(dest => dest.PlateNumber, opt => opt.MapFrom(src => src.Vehicle.PlateNumber))
                .ForMember(dest => dest.TechnicianName, opt => opt.MapFrom(src => src.AssignedTechnician != null ? src.AssignedTechnician.User.DisplayName : null))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
                .ForMember(dest => dest.BookingServiceDetailsDtos, opt => opt.MapFrom(src => src.BookingServices));

            CreateMap<Booking, BookingDetailsDto>()
                .IncludeBase<Booking, BookingDto>()
                .ForMember(dest => dest.AdditionalIssueDtos, opt => opt.MapFrom(src => src.AdditionalIssues));

            CreateMap<BookingService, BookingServiceDetailsDto>()
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
                .ForMember(dest => dest.ServicePrice, opt => opt.MapFrom(src => src.Service.BasePrice))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<AdditionalIssue, AdditionalIssueDto>();
        }
    }
}