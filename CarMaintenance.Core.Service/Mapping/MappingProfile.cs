using AutoMapper;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.DTOs.Vehicles;
using System.Text.Json;

namespace CarMaintenance.Core.Service.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Services Mapping
            CreateMap<Domain.Models.Data.Service, ServiceDto>()
                .ForMember(dest => dest.IncludedItems, opt => opt.MapFrom(src => DeserializeList(src.IncludedItems)))
                .ForMember(dest => dest.ExcludedItems, opt => opt.MapFrom(src => DeserializeList(src.ExcludedItems)))
                .ForMember(dest => dest.Requirements, opt => opt.MapFrom(src => DeserializeList(src.Requirements)));

            CreateMap<Domain.Models.Data.Service, ServiceDetailsDto>()
                .IncludeBase<Domain.Models.Data.Service, ServiceDto>()
                .ForMember(dest => dest.AvailableTechnicians, opt => opt.Ignore()); // Set manually in service

            CreateMap<CreateServiceDto, Domain.Models.Data.Service>()
                .ForMember(dest => dest.IncludedItems, opt => opt.MapFrom(src => SerializeList(src.IncludedItems)))
                .ForMember(dest => dest.ExcludedItems, opt => opt.MapFrom(src => SerializeList(src.ExcludedItems)))
                .ForMember(dest => dest.Requirements, opt => opt.MapFrom(src => SerializeList(src.Requirements)));

            CreateMap<UpdateServiceDto, Domain.Models.Data.Service>()
                .ForMember(dest => dest.IncludedItems, opt => opt.MapFrom(src => SerializeList(src.IncludedItems)))
                .ForMember(dest => dest.ExcludedItems, opt => opt.MapFrom(src => SerializeList(src.ExcludedItems)))
                .ForMember(dest => dest.Requirements, opt => opt.MapFrom(src => SerializeList(src.Requirements)));

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


        //  Helper Methods for JSON Serialization/Deserialization
        private static List<string> DeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static string? SerializeList(List<string>? list)
        {
            if (list == null || !list.Any())
                return null;

            try
            {
                return JsonSerializer.Serialize(list);
            }
            catch
            {
                return null;
            }
        }
    

    }
}