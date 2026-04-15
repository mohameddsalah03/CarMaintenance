using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Domain.Specifications.Reviews;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services.Auth.Email;
using CarMaintenance.Core.Service.Abstraction.Services.Technicians;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.DTOs.Technicians.AI;
using CarMaintenance.Shared.Exceptions;
using CarMaintenance.Shared.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace CarMaintenance.Core.Service.Services.Technicians
{
    public class TechniciansService : ITechniciansService
    {
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

        public async Task<IEnumerable<TechniciansDto>> GetAllTechniciansAsync()
        {
            var spec = new TechnicianSpecification();
            var technicians = await _unitOfWork.GetRepo<Technician, string>()
                .GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<TechniciansDto>>(technicians);
        }

        public async Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync()
        {
            var spec = new TechnicianSpecification(isAvailable: true);
            var technicians = await _unitOfWork.GetRepo<Technician, string>()
                .GetAllWithSpecAsync(spec);
            return _mapper.Map<IEnumerable<TechniciansDto>>(technicians);
        }

        public async Task<TechniciansDto?> GetTechnicianByIdAsync(string id)
        {
            var spec = new TechnicianSpecification(id);
            var technician = await _unitOfWork.GetRepo<Technician, string>()
                .GetWithSpecAsync(spec);

            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            return _mapper.Map<TechniciansDto>(technician);
        }

        public async Task<TechniciansDto> CreateTechnicianAsync(CreateTechnicianDto createDto)
        {
            
            var displayNameTaken = await _userManager.Users.AnyAsync(u => u.DisplayName == createDto.DisplayName);

            if (displayNameTaken)
                throw new BadRequestException($"الاسم '{createDto.DisplayName}' مستخدم بالفعل من قِبَل مستخدم آخر. " + "يرجى اختيار اسم عرض مختلف.");

            var user = new ApplicationUser
            {
                DisplayName = createDto.DisplayName,
                Email = createDto.Email,
                UserName = createDto.UserName,
                PhoneNumber = createDto.PhoneNumber,
            };

            var result = await _userManager.CreateAsync(user, createDto.Password);
            if (!result.Succeeded)
                throw new ValidationException( "Failed to create technician account",result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, "Technician");

            var technician = new Technician
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Specialization = createDto.Specialization,
                ExperienceYears = createDto.ExperienceYears,
                Rating = 0,
                IsAvailable = true
            };

            await _unitOfWork.GetRepo<Technician, string>().AddAsync(technician);
            await _unitOfWork.SaveChangesAsync();

            //  Send set-password link instead of plain-text password
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
            var setPasswordUrl = $"{_appSettings.FrontendUrl.TrimEnd('/')}/set-password" +
                                 $"?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

            var emailBody = $@"
                <h2>مرحباً {user.DisplayName}</h2>
                <p>تم إنشاء حساب فني صيانة لك في نظام FIXORA.</p>
                <h3>بيانات الدخول:</h3>
                <p><strong>البريد الإلكتروني:</strong> {user.Email}</p>
                <p><strong>اسم المستخدم:</strong> {user.UserName}</p>
                <p><strong>التخصص:</strong> {createDto.Specialization}</p>
                <br>
                <p>يرجى تعيين كلمة المرور الخاصة بك عن طريق الرابط التالي (صالح لمدة 24 ساعة):</p>
                <a href='{setPasswordUrl}' style='background:#1d4ed8;color:#fff;padding:10px 20px;
                   border-radius:6px;text-decoration:none;display:inline-block;'>
                   تعيين كلمة المرور
                </a>
                <br><br>
                <p>رابط تسجيل الدخول بعد تعيين كلمة المرور: {_appSettings.FrontendUrl}/login</p>";

            await _emailService.SendEmailAsync(user.Email!, "حساب فني صيانة جديد - FIXORA",emailBody);

            var spec = new TechnicianSpecification(technician.Id);
            var created = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(spec);
            return _mapper.Map<TechniciansDto>(created!);
        }

        public async Task<TechniciansDto> UpdateTechnicianAsync(string id, TechnicianUpdateDto updateDto)
        {
            var spec = new TechnicianSpecification(id);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(spec);

            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            technician.Specialization = updateDto.Specialization;

            if (updateDto.IsAvailable.HasValue)
                technician.IsAvailable = updateDto.IsAvailable.Value;

            if (updateDto.ExperienceYears.HasValue)
                technician.ExperienceYears = updateDto.ExperienceYears.Value;

            var user = technician.User;

            if (!string.IsNullOrEmpty(updateDto.DisplayName) &&
                updateDto.DisplayName != user.DisplayName)
            {
                var displayNameTaken = await _userManager.Users.AnyAsync(u => u.DisplayName == updateDto.DisplayName && u.Id != user.Id);

                if (displayNameTaken)
                    throw new BadRequestException($"الاسم '{updateDto.DisplayName}' مستخدم بالفعل من قِبَل مستخدم آخر. " + "يرجى اختيار اسم عرض مختلف.");

                user.DisplayName = updateDto.DisplayName;
            }

            if (!string.IsNullOrEmpty(updateDto.UserName) && updateDto.UserName != user.UserName)
            {
                var existing = await _userManager.FindByNameAsync(updateDto.UserName);
                if (existing is not null && existing.Id != user.Id)
                    throw new BadRequestException($"اسم المستخدم '{updateDto.UserName}' موجود بالفعل");
                user.UserName = updateDto.UserName;
            }

            if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
            {
                var existing = await _userManager.FindByEmailAsync(updateDto.Email);
                if (existing is not null && existing.Id != user.Id)
                    throw new BadRequestException($"البريد الإلكتروني '{updateDto.Email}' موجود بالفعل");
                user.Email = updateDto.Email;
            }

            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new ValidationException( "Failed to update technician",updateResult.Errors.Select(e => e.Description));

            _unitOfWork.GetRepo<Technician, string>().Update(technician);
            await _unitOfWork.SaveChangesAsync();

            var updatedSpec = new TechnicianSpecification(id);
            var updated = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(updatedSpec);
            return _mapper.Map<TechniciansDto>(updated!);
        }

        //  Check for active bookings before deleting
        public async Task DeleteTechnicianAsync(string id)
        {
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(id);
            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            // Block deletion if technician has active/in-progress bookings
            var activeBookingSpec = new BookingByTechnicianActiveSpecification(id);
            var activeCount = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(activeBookingSpec);

            if (activeCount > 0)
                throw new BadRequestException(
                    $"لا يمكن حذف الفني — لديه {activeCount} حجز نشط حالياً. " +
                    "يرجى إعادة تعيين الحجوزات أو انتظار اكتمالها قبل الحذف.");

            _unitOfWork.GetRepo<Technician, string>().Delete(technician);
            await _unitOfWork.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(technician.UserId);
            if (user is not null)
                await _userManager.DeleteAsync(user);
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

        public async Task<TechnicianStatsDto> GetTechnicianStatsAsync(string id)
        {
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(id);
            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            var spec = new BookingByTechnicianSpecification(id);
            var bookings = (await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec)).ToList();

            int total = bookings.Count;
            int completed = bookings.Count(b => b.Status == BookingStatus.Completed);

            return new TechnicianStatsDto
            {
                TechnicianId = id,
                TotalBookings = total,
                CompletedBookings = completed,
                SuccessRate = total > 0 ? Math.Round((double)completed / total, 2) : 0
            };
        }

        public async Task<TechnicianWorkloadDto> GetTechnicianWorkloadAsync(string id)
        {
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(id);
            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            var spec = new BookingByTechnicianActiveSpecification(id);
            var active = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);

            return new TechnicianWorkloadDto
            {
                TechnicianId = id,
                CurrentWorkload = active.Count()
            };
        }

        public async Task<TechnicianRatingDto> GetTechnicianRatingAsync(string id)
        {
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(id);
            if (technician is null)
                throw new NotFoundException(nameof(Technician), id);

            var spec = new ReviewSpecification(id, byTechnician: true);
            var reviews = (await _unitOfWork.GetRepo<Review, int>().GetAllWithSpecAsync(spec)).ToList();

            return new TechnicianRatingDto
            {
                TechnicianId = id,
                AvgRating = reviews.Any()
                    ? Math.Round(reviews.Average(r => r.TechnicianRating), 2)
                    : Math.Round((double)technician.Rating, 2),
                TotalReviews = reviews.Count
            };
        }
    }
}