using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Domain.Specifications.Vehicles;
using CarMaintenance.Core.Service.Abstraction.Services.Vehicles;
using CarMaintenance.Shared.DTOs.Vehicles;
using CarMaintenance.Shared.Exceptions;
using CarMaintenance.Shared.Helpers;

namespace CarMaintenance.Core.Service.Services.Vehicles
{
    internal class VehicleService(IUnitOfWork unitOfWork, IMapper mapper) : IVehicleService
    {
        // Admin
        public async Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync()
        {
            var spec = new VehicleSpecification(); 
            var vehicles = await unitOfWork.GetRepo<Vehicle, int>().GetAllWithSpecAsync(spec);
            return mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        }

        // User 
        public async Task<IEnumerable<VehicleDto>> GetUserVehicleAsync(string userId)
        {
            var spec = new VehicleSpecification(userId);
            var vehicles = await unitOfWork.GetRepo<Vehicle, int>().GetAllWithSpecAsync(spec);
            return mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        }

        public async Task<VehicleDto?> GetVehicleByIdAsync(int id, string userId)
        {
            var spec = new VehicleSpecification(id, userId);
            var vehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            if (vehicle is null)
                throw new NotFoundException(nameof(Vehicle), id);

            return mapper.Map<VehicleDto>(vehicle);
        }

      
        public async Task<VehicleDto> AddVehicleAsync(CreateVehicleDto createDto, string userId)
        {
            var cleanedPlate = PlateNumberHelper.CleanAndNormalize(createDto.PlateNumber);

            if (!PlateNumberHelper.IsValidEgyptianPlate(cleanedPlate))
                throw new BadRequestException( "صيغة لوحة السيارة غير صحيحة. يجب أن تتكون من (1-3 حروف عربية) ثم مسافة ثم (1-4 أرقام).");

            var plateSpec = new VehicleByPlateNumberSpecification(cleanedPlate);
            var existingVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(plateSpec);

            if (existingVehicle is not null)
                throw new BadRequestException($"رقم اللوحة '{cleanedPlate}' مستخدم بالفعل");

            var vehicle = mapper.Map<Vehicle>(createDto);
            vehicle.UserId = userId;
            vehicle.PlateNumber = cleanedPlate; 

            await unitOfWork.GetRepo<Vehicle, int>().AddAsync(vehicle);
            await unitOfWork.SaveChangesAsync();

            var spec = new VehicleSpecification(vehicle.Id, userId);
            var createdVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            return mapper.Map<VehicleDto>(createdVehicle!);
        }


        public async Task<VehicleDto> UpdateVehicleAsync(UpdateVehicleDto updateDto, string userId)
        {
            var spec = new VehicleSpecification(updateDto.Id, userId);
            var vehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            if (vehicle is null)
                throw new NotFoundException(nameof(Vehicle), updateDto.Id);

            var cleanedPlate = PlateNumberHelper.CleanAndNormalize(updateDto.PlateNumber);

            if (!PlateNumberHelper.IsValidEgyptianPlate(cleanedPlate))
                throw new BadRequestException("صيغة لوحة السيارة غير صحيحة. يجب أن تتكون من (1-3 حروف عربية) ثم مسافة ثم (1-4 أرقام).");

            var plateSpec = new VehicleByPlateNumberSpecification(cleanedPlate,updateDto.Id);
            var existingPlateVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(plateSpec);

            if (existingPlateVehicle is not null)
                throw new BadRequestException($"رقم اللوحة '{cleanedPlate}' مستخدم بالفعل");

            mapper.Map(updateDto, vehicle);
            vehicle.PlateNumber = cleanedPlate;

            unitOfWork.GetRepo<Vehicle, int>().Update(vehicle);
            await unitOfWork.SaveChangesAsync();

            var updatedSpec = new VehicleSpecification(vehicle.Id, userId);
            var updatedVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(updatedSpec);

            return mapper.Map<VehicleDto>(updatedVehicle!);
        }

        public async Task DeleteVehicleAsync(int id, string userId)
        {
            var spec = new VehicleSpecification(id, userId);
            var vehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            if (vehicle is null)
                throw new NotFoundException(nameof(Vehicle), id);

            var activeBookingSpec = new BookingByVehicleActiveSpecification(id);
            var activeCount = await unitOfWork.GetRepo<Booking, int>().GetCountAsync(activeBookingSpec);

            if (activeCount > 0)
                throw new BadRequestException($"لا يمكن حذف السيارة — لديها {activeCount} حجز نشط حالياً. " + "يرجى إلغاء أو انتظار اكتمال جميع الحجوزات قبل الحذف.");

            unitOfWork.GetRepo<Vehicle, int>().Delete(vehicle);
            await unitOfWork.SaveChangesAsync();
        }
    }
}