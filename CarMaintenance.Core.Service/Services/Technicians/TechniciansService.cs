using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMaintenance.Core.Service.Services.Technicians
{
    public class TechniciansService(IUnitOfWork _unitOfWork, IMapper _mapper,UserManager<ApplicationUser> _userManager) : ITechniciansService
    {
        //public async Task<TechniciansDto> AddTechniciansAsync(TechnicianCreateDto techniciansDto)
        //{
        //    if (techniciansDto == null)
        //    {
        //        throw new BadRequestException();
        //    }

        //    // التحقق من وجود المستخدم
        //    var userExists = await _userManager.FindByIdAsync(techniciansDto.UserId);

        //    if (userExists == null)
        //    {
        //        throw new NotFoundException($"User with id {techniciansDto.UserId} not found");
        //    }

        //    var mapped = _mapper.Map<Technician>(techniciansDto);
        //    mapped.Id = techniciansDto.UserId; // استخدام UserId كـ Id

        //    await _unitOfWork.GetRepo<Technician, string>().AddAsync(mapped);
        //    await _unitOfWork.SaveChangesAsync();

        //    var mappedToReturn = _mapper.Map<TechniciansDto>(mapped);
        //    return mappedToReturn;
        //}

        public async Task<TechniciansDto> EditTechniciansAsync(string id, TechnicianUpdateDto techniciansDto)
        {
            if (techniciansDto == null)
            {
                throw new BadRequestException();
            }

            var tech = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(id);
            if (tech == null)
            {
                throw new NotFoundException($"Technician with id {id} Not Found");
            }

           
            
            tech.Specialization = techniciansDto.Specialization;
            

            _unitOfWork.GetRepo<Technician, string>().Update(tech);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TechniciansDto>(tech);
        }

        public async Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync()
        {
            var tech = new TechnicianSpecification(true);
            var data = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(tech);
            var Technicians = _mapper.Map<IEnumerable<TechniciansDto>>(data);

            return Technicians;
        }

        public async Task<IEnumerable<TechniciansDto>> GetTechniciansAsync()
        {
            var tech = new TechnicianSpecification();
            var data = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(tech);
            var Technicians = _mapper.Map<IEnumerable<TechniciansDto>>(data);

            return Technicians;
        }

        public async Task RemoveTechnicianAsync(string id)
        {
            if (id == null)
            {
                throw new BadRequestException();
            }

            var tech = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(id);
            if (tech == null)
            {
                throw new NotFoundException($"Technician with Id {id} not found ");
            }

            _unitOfWork.GetRepo<Technician, string>().Delete(tech);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}