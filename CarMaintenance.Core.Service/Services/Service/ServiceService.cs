using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.Exceptions;

namespace CarMaintenance.Core.Service
{
    public class ServiceService(IUnitOfWork unitOfWork , IMapper mapper) : IServiceService
    {

        public async Task<Pagination<ServiceDto>> GetServicesAsync(ServiceSpecParams specParams)
        {
            // Get data with specs
            var spec = new ServiceSpecifications(specParams);

            var services = await unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetAllWithSpecAsync(spec);

            var data = mapper.Map<IEnumerable<ServiceDto>>(services);

            // Get count
            var specCount = new ServiceWithFiltrationForCountSpecifications(specParams);
            var count = await unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetCountAsync(specCount);

            return new Pagination<ServiceDto>(specParams.PageIndex, specParams.PageSize, count)
            {
                Data = data
            };
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(int id)
        {
            var spec = new ServiceSpecifications(id);
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
            var spec = new ServiceSpecifications(id);
            var service = await unitOfWork.GetRepo<Domain.Models.Data.Service,int>().GetWithSpecAsync(spec);

            if (service is null)
                throw new NotFoundException(nameof(Domain.Models.Data.Service), id);

            var serviceDetails = mapper.Map<ServiceDetailsDto>(service);

            var specTech = new TechnicianSpecification(true);
            var allTechnicians = await unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(specTech);

            // 3.Filter technicians by specialization(flexible logic)
            var filteredTechnicians = new TechnicianByServiceCategorySpecification(service.Category, true);

            serviceDetails.AvailableTechnicians = mapper.Map<List<TechniciansDto>>(filteredTechnicians);
            return serviceDetails;
        }



        #endregion

    }
}
