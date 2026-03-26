using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarMaintenance.Core.Service.Services.Technicians
{
    public class TechniciansService : ITechniciansService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TechniciansService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task AddTechnicianAsync(TechnicianCreateDto techniciansDto)
        {
            if (techniciansDto == null)
                throw new BadRequestException("Technician data is required");

            // Validate FK user exists
            var userRepo = _unitOfWork.GetRepo<ApplicationUser, string>();
            var user = await userRepo.GetByIdAsync(techniciansDto.UserId);
            if (user == null)
                throw new NotFoundException($"User with id {techniciansDto.UserId} not found");

            var technician = _mapper.Map<Technician>(techniciansDto);
            // Ensure Id generated on entity ctor
            if (string.IsNullOrWhiteSpace(technician.Id))
                technician.Id = Guid.NewGuid().ToString();

            await _unitOfWork.GetRepo<Technician, string>().AddAsync(technician);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task EditTechnicianAsync(string id, TechniciansDto techniciansDto)
        {
            if (string.IsNullOrWhiteSpace(id) || techniciansDto == null)
                throw new BadRequestException("Invalid input");

            var repo = _unitOfWork.GetRepo<Technician, string>();
            var technician = await repo.GetByIdAsync(id);
            if (technician == null)
                throw new NotFoundException($"Technician with id {id} not found");

            // Update allowed fields only
            technician.Specialization = techniciansDto.Specialization;
            technician.IsAvailable = techniciansDto.IsAvailable;
            technician.Rating = techniciansDto.Rating;

            // Validate UserId if changed
            if (!string.Equals(technician.UserId, techniciansDto.UserId, StringComparison.OrdinalIgnoreCase))
            {
                var userRepo = _unitOfWork.GetRepo<ApplicationUser, string>();
                var user = await userRepo.GetByIdAsync(techniciansDto.UserId);
                if (user == null)
                    throw new NotFoundException($"User with id {techniciansDto.UserId} not found");

                technician.UserId = techniciansDto.UserId;
            }

            repo.Update(technician);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync()
        {
            var spec = new TechnicianSpecification(true);
            var data = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<TechniciansDto>>(data);
        }

        public async Task<IEnumerable<TechniciansDto>> GetTechniciansAsync()
        {
            var spec = new TechnicianSpecification();
            var data = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<TechniciansDto>>(data);
        }

        public async Task RemoveTechnicianAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new BadRequestException("Id is required");

            var repo = _unitOfWork.GetRepo<Technician, string>();
            var tech = await repo.GetByIdAsync(id);
            if (tech == null)
                throw new NotFoundException($"Technician with id {id} not found");

            repo.Delete(tech);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
