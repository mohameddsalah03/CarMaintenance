using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Core.Service.Abstraction.Services.Auth.Email;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.Exceptions;
using CarMaintenance.Shared.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CarMaintenance.Core.Service.Services.Technicians
{
    public class TechniciansService : ITechniciansService
    {

        #region DI

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly AppSettings _appSettings;
        public TechniciansService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IOptions<AppSettings> appSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _emailService = emailService;
            _appSettings = appSettings.Value;
        }

        #endregion

        public async Task<IEnumerable<TechniciansDto>> GetAllTechniciansAsync()
        {
            var spec = new TechnicianSpecification();
            var technicians = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<TechniciansDto>>(technicians);
        }

        public async Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync()
        {
            var spec = new TechnicianSpecification(isAvailable: true);
            var technicians = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<TechniciansDto>>(technicians);
        }

        public async Task<TechniciansDto?> GetTechnicianByIdAsync(string id)
        {
            var spec = new TechnicianSpecification(id);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(spec);

            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            return _mapper.Map<TechniciansDto>(technician);
        }

        public async Task<TechniciansDto> CreateTechnicianAsync(CreateTechnicianDto createDto)
        {
            // 1. Create User Account
            var user = new ApplicationUser()
            {
                DisplayName = createDto.DisplayName,
                Email = createDto.Email,
                UserName = createDto.UserName,
                PhoneNumber = createDto.PhoneNumber,
            };

            var result = await _userManager.CreateAsync(user, createDto.Password);

            if (!result.Succeeded)
            {
                throw new ValidationException(
                    "Failed to create technician account",
                    result.Errors.Select(e => e.Description)
                );
            }

            // 2. Assign Technician Role
            await _userManager.AddToRoleAsync(user, "Technician");

            // 3. Create Technician Record
            var technician = new Technician()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Specialization = createDto.Specialization,
                Rating = 0,
                IsAvailable = true
            };

            await _unitOfWork.GetRepo<Technician, string>().AddAsync(technician);
            await _unitOfWork.SaveChangesAsync();

            // 4. Send Email with Login Credentials
            var emailBody = $@"
                <h2>مرحباً {user.DisplayName}</h2>
                <p>تم إنشاء حساب فني صيانة لك في نظام إدارة صيانة السيارات.</p>
                <h3>بيانات الدخول:</h3>
                <p><strong>البريد الإلكتروني:</strong> {user.Email}</p>
                <p><strong>اسم المستخدم:</strong> {user.UserName}</p>
                <p><strong>كلمة المرور:</strong> {createDto.Password}</p>
                <p><strong>التخصص:</strong> {createDto.Specialization}</p>
                <br>
                <p>يرجى تسجيل الدخول وتغيير كلمة المرور من الإعدادات.</p>
                <p>رابط تسجيل الدخول: {_appSettings.FrontendUrl}/login</p>
            ";

            await _emailService.SendEmailAsync(user.Email!, "حساب فني صيانة جديد", emailBody);

            // 5. Get Full Technician with User Info
            var spec = new TechnicianSpecification(technician.Id);
            var createdTechnician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(spec);

            return _mapper.Map<TechniciansDto>(createdTechnician!);
        }

        public async Task<TechniciansDto> UpdateTechnicianAsync(string id, TechnicianUpdateDto updateDto)
        {
            var spec = new TechnicianSpecification(id);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(spec);

            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            // Update Technician Info
            technician.Specialization = updateDto.Specialization;

            if (updateDto.IsAvailable.HasValue)
                technician.IsAvailable = updateDto.IsAvailable.Value;

            // Update User Info (if provided)
            var user = technician.User;

            if (!string.IsNullOrEmpty(updateDto.DisplayName))
                user.DisplayName = updateDto.DisplayName;

            //  تحقق من UserName قبل التعديل
            if (!string.IsNullOrEmpty(updateDto.UserName) && updateDto.UserName != user.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(updateDto.UserName);
                if (existingUser is not null && existingUser.Id != user.Id)
                    throw new BadRequestException($"اسم المستخدم '{updateDto.UserName}' موجود بالفعل");

                user.UserName = updateDto.UserName;
            }

            //  تحقق من Email قبل التعديل
            if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(updateDto.Email);
                if (existingUser is not null && existingUser.Id != user.Id)
                    throw new BadRequestException($"البريد الإلكتروني '{updateDto.Email}' موجود بالفعل");

                user.Email = updateDto.Email;
            }

            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;

            // Save Changes
            var userUpdateResult = await _userManager.UpdateAsync(user);

            if (!userUpdateResult.Succeeded)
            {
                throw new ValidationException(
                    "Failed to update technician",
                    userUpdateResult.Errors.Select(e => e.Description)
                );
            }

            _unitOfWork.GetRepo<Technician, string>().Update(technician);
            await _unitOfWork.SaveChangesAsync();

            // Get Updated Technician
            var updatedSpec = new TechnicianSpecification(id);
            var updatedTechnician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(updatedSpec);

            return _mapper.Map<TechniciansDto>(updatedTechnician!);
        }
        public async Task DeleteTechnicianAsync(string id)
        {
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(id);

            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            // Delete Technician Record
            _unitOfWork.GetRepo<Technician, string>().Delete(technician);
            await _unitOfWork.SaveChangesAsync();

            //  Optional: Delete User Account too
            var user = await _userManager.FindByIdAsync(technician.UserId);
            if (user is not null)
            {
                await _userManager.DeleteAsync(user);
            }
        }

        public async Task<TechniciansDto> ToggleAvailabilityAsync(string id)
        {
            var spec = new TechnicianSpecification(id);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(spec);

            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            technician.IsAvailable = !technician.IsAvailable;

            _unitOfWork.GetRepo<Technician, string>().Update(technician);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TechniciansDto>(technician);
        }
    }
}