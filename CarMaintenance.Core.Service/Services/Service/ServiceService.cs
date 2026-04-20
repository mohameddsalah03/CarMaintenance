using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Domain.Specifications.Vehicles;
using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.AI.Request;
using CarMaintenance.Shared.DTOs.AI.Response;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Services;
using CarMaintenance.Shared.DTOs.Services.AnalyzeProblem;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.Exceptions;

namespace CarMaintenance.Core.Service
{
    public class ServiceService(
        IUnitOfWork unitOfWork ,
        IMapper mapper,
        IAiDiagnosisService _aiDiagnosisService
        ) : IServiceService
    {

        public async Task<Pagination<ServiceDto>> GetServicesAsync(ServiceSpecParams specParams)
        {
            var spec = new ServiceSpecification(specParams);

            var services = await unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetAllWithSpecAsync(spec);

            var data = mapper.Map<IEnumerable<ServiceDto>>(services);

            var specCount = new ServiceWithFiltrationForCountSpecification(specParams);
            var count = await unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetCountAsync(specCount);

            return new Pagination<ServiceDto>(specParams.PageIndex, specParams.PageSize, count)
            {
                Data = data
            };
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(int id)
        {
            var spec = new ServiceSpecification(id);
            var service = await unitOfWork.GetRepo<Domain.Models.Data.Service,int>().GetWithSpecAsync(spec);
            if (service is null)
                throw new NotFoundException(nameof(Service), id);
            return mapper.Map<ServiceDto>(service);
        }

        #region Admin Endpoints

        public async Task<ServiceDto> UpdateServiceAsync(UpdateServiceDto updateDto)
        {
            var service = await unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetByIdAsync(updateDto.Id);

            if (service is null)
                throw new NotFoundException(nameof(Domain.Models.Data.Service), updateDto.Id);

            mapper.Map(updateDto, service);

            unitOfWork.GetRepo<Domain.Models.Data.Service, int>().Update(service);
            await unitOfWork.SaveChangesAsync();

            return mapper.Map<ServiceDto>(service);
        }
        public async Task<ServiceDto> CreateServiceAsync(CreateServiceDto createDto)
        {
            var service = mapper.Map<Domain.Models.Data.Service>(createDto);

            await unitOfWork.GetRepo<Domain.Models.Data.Service,int>().AddAsync(service);
            await unitOfWork.SaveChangesAsync();

            return mapper.Map<ServiceDto>(service);
        }

        public async Task DeleteServiceAsync(int id)
        {
            var service = await unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetByIdAsync(id);

            if (service is null)
                throw new NotFoundException(nameof(Service), id);

            unitOfWork.GetRepo<Domain.Models.Data.Service, int>().Delete(service);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<ServiceDetailsDto?> GetServiceDetailsAsync(int id)
        {
            var spec = new ServiceSpecification(id);
            var service = await unitOfWork.GetRepo<Domain.Models.Data.Service,int>().GetWithSpecAsync(spec);

            if (service is null)
                throw new NotFoundException(nameof(Domain.Models.Data.Service), id);

            var serviceDetails = mapper.Map<ServiceDetailsDto>(service);

            var techSpec = new TechnicianByServiceCategorySpecification(service.Category, onlyAvailable: true);

            var filteredTechnicians = await unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(techSpec);

            serviceDetails.AvailableTechnicians = mapper.Map<List<TechniciansDto>>(filteredTechnicians);

            return serviceDetails;
        }



        #endregion


        public async Task<AnalyzeProblemResponseDto> AnalyzeProblemAsync(AnalyzeProblemRequestDto requestDto, string? userId)
        {
            var aiRequest = new AiDiagnosisRequestDto
            {
                ProblemDescription = requestDto.ProblemDescription,
                VehicleContext = await BuildVehicleContextAsync(requestDto.VehicleId, userId)
            };

            var aiResult = await _aiDiagnosisService.AnalyzeProblemAsync(aiRequest);
            

            if (aiResult is null)
            {
                return new AnalyzeProblemResponseDto
                {
                    Status = "unknown",
                    Message = "التشخيص الذكي غير متاح حالياً، يمكنك اختيار الخدمة يدوياً"
                };
            }

            if (aiResult.Status == "unknown" || !aiResult.RecommendedServices.Any())
            {
                return new AnalyzeProblemResponseDto
                {
                    Status = aiResult.Status,
                    Message = aiResult.Message ?? "مش قدرنا نحدد المشكلة، ممكن توضح أكتر؟"
                };
            }

            var validatedSuggestions = await ValidateAndEnrichServiceIdsAsync(aiResult.RecommendedServices);

            if (!validatedSuggestions.Any())
            {
               
                return new AnalyzeProblemResponseDto
                {
                    Status = "unknown",
                    Message = "تعذّر تحديد الخدمة المناسبة، يمكنك اختيارها يدوياً"
                };
            }

            return new AnalyzeProblemResponseDto
            {
                Status = aiResult.Status,
                SuggestedServices = validatedSuggestions,
                Message = aiResult.Message
            };
        }


        //  Private helpers
        private async Task<List<ValidatedServiceSuggestionDto>> ValidateAndEnrichServiceIdsAsync(List<AiRecommendedServiceDto> aiServices)
        {
            var validated = new List<ValidatedServiceSuggestionDto>();

            foreach (var aiSvc in aiServices)
            {
                var dbService = await unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetByIdAsync(aiSvc.ServiceId);

                if (dbService is null)
                {
                    continue;
                }

                validated.Add(new ValidatedServiceSuggestionDto
                {
                    ServiceId = dbService.Id,
                    ServiceName = dbService.Name,              // authoritative name from DB
                    Category = dbService.Category,
                    BasePrice = dbService.BasePrice,          // pricing always from DB
                    EstimatedDurationMinutes = dbService.EstimatedDurationMinutes,
                    Confidence = aiSvc.Confidence
                });
            }

            return validated;
        }

        private async Task<AiVehicleContextDto?> BuildVehicleContextAsync(int? vehicleId, string? userId)
        {
            if (vehicleId is null || string.IsNullOrEmpty(userId))
                return null;

            var spec = new VehicleSpecification(vehicleId.Value, userId);
            var vehicle = await unitOfWork.GetRepo<Vehicle, int>().GetWithSpecAsync(spec);

            if (vehicle is null)
                return null;

            return new AiVehicleContextDto
            {
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
            };
        }

    }
}
