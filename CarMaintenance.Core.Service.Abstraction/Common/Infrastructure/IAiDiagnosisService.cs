using CarMaintenance.Shared.DTOs.AI.Request;
using CarMaintenance.Shared.DTOs.AI.Response;

namespace CarMaintenance.Core.Service.Abstraction.Common.Infrastructure
{
   
    public interface IAiDiagnosisService
    {
        Task<AiDiagnosisResponseDto?> AnalyzeProblemAsync(AiDiagnosisRequestDto request);
    }
}