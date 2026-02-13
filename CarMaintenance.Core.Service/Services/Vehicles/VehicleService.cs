using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications.Vehicles;
using CarMaintenance.Core.Service.Abstraction.Services.Vehicles;
using CarMaintenance.Shared.DTOs.Vehicles;
using CarMaintenance.Shared.Exceptions;

namespace CarMaintenance.Core.Service.Services.Vehicles
{
    internal class VehicleService(IUnitOfWork unitOfWork, IMapper mapper) : IVehicleService
    {
        // Admin
        public async Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync()
        {
            var spec = new VehicleSpecifications(); // no Filter
            var vehicles = await unitOfWork.GetRepo<Vehicle, int>().GetAllWithSpecAsync(spec);
            return mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        }

        // User 
        public async Task<IEnumerable<VehicleDto>> GetUserVehicleAsync(string userId)
        {
            var spec = new VehicleSpecifications(userId);
            var vehicles = await unitOfWork.GetRepo<Vehicle, int>().GetAllWithSpecAsync(spec);
            return mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        }

        public async Task<VehicleDto?> GetVehicleByIdAsync(int id, string userId)
        {
            var spec = new VehicleSpecifications(id, userId);
            var vehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            if (vehicle is null)
                throw new NotFoundException(nameof(Vehicle), id);

            return mapper.Map<VehicleDto>(vehicle);
        }

        public async Task<VehicleDto> AddVehicleAsync(CreateVehicleDto createDto, string userId)
        {
            //  تصحيح 1: استخدم PlateNumber مش userId
            var plateSpec = new VehicleByPlateNumberSpecifications(createDto.PlateNumber);
            var existingVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(plateSpec);

            if (existingVehicle is not null)
                throw new BadRequestException($"رقم اللوحة '{createDto.PlateNumber}' مستخدم بالفعل");

            var vehicle = mapper.Map<Vehicle>(createDto);
            vehicle.UserId = userId;

            await unitOfWork.GetRepo<Vehicle, int>().AddAsync(vehicle);
            await unitOfWork.SaveChangesAsync();

            // Get with Owner data
            var spec = new VehicleSpecifications(vehicle.Id, userId);
            var createdVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            //  تصحيح 2: استخدم createdVehicle مش vehicle
            return mapper.Map<VehicleDto>(createdVehicle!);
        }

        public async Task<VehicleDto> UpdateVehicleAsync(UpdateVehicleDto updateDto, string userId)
        {
            var spec = new VehicleSpecifications(updateDto.Id, userId);
            var vehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            if (vehicle is null)
                throw new NotFoundException(nameof(Vehicle), updateDto.Id);

            // ✅ تصحيح 3: استخدم PlateNumber + excludeId
            var plateSpec = new VehicleByPlateNumberSpecifications(updateDto.PlateNumber, updateDto.Id);
            var existingPlateVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(plateSpec);

            if (existingPlateVehicle is not null)
                throw new BadRequestException($"رقم اللوحة '{updateDto.PlateNumber}' مستخدم بالفعل");

            // ✅ تصحيح 4: Map على نفس الـ vehicle
            mapper.Map(updateDto, vehicle);

            unitOfWork.GetRepo<Vehicle, int>().Update(vehicle);
            await unitOfWork.SaveChangesAsync();

            // Get updated vehicle with Owner
            var updatedSpec = new VehicleSpecifications(vehicle.Id, userId);
            var updatedVehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(updatedSpec);

            return mapper.Map<VehicleDto>(updatedVehicle!);
        }

        public async Task DeleteVehicleAsync(int id, string userId)
        {
            var spec = new VehicleSpecifications(id, userId);
            var vehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            if (vehicle is null)
                throw new NotFoundException(nameof(Vehicle), id);

            unitOfWork.GetRepo<Vehicle, int>().Delete(vehicle);
            await unitOfWork.SaveChangesAsync();
        }
    }
}