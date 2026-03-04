using CarMaintenance.Shared.DTOs.AI.Request;
using CarMaintenance.Shared.DTOs.AI.Response;

namespace CarMaintenance.Core.Service.Abstraction.Common.Infrastructure
{
    public interface IAiTechnicianService
    {
        Task<AiAssignmentResponseDto?> GetTechnicianRecommendationAsync(AiAssignmentRequestDto request);
    }
}