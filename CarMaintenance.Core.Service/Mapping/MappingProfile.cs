using AutoMapper;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Services;
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


        }
    }
}
