using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Shared.DTOs.AI.Request;
using CarMaintenance.Shared.DTOs.AI.Response;
using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CarMaintenance.Infrastructure.AiServices
{
    public class AiDiagnosisService : IAiDiagnosisService
    {
        private readonly HttpClient _httpClient;
        private readonly AISettings _aiSettings;

        public AiDiagnosisService(
            HttpClient httpClient,
            IOptions<AISettings> aiSettings
            )
        {
            _httpClient = httpClient;
            _aiSettings = aiSettings.Value;

            _httpClient.Timeout = TimeSpan.FromSeconds(_aiSettings.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _aiSettings.ApiKey);
        }

        public async Task<AiDiagnosisResponseDto?> AnalyzeProblemAsync(AiDiagnosisRequestDto request)
        {
            try
            {
                var payload = new
                {
                    problemDescription = request.ProblemDescription,
                    vehicleContext = request.VehicleContext is null ? null : new
                    {
                        brand = request.VehicleContext.Brand,
                        model = request.VehicleContext.Model,
                        year = request.VehicleContext.Year,
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                
               
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_aiSettings.DiagnosisUrl, content);

                if (!response.IsSuccessStatusCode)
                    return null;

                var responseBody = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<AiDiagnosisResponseDto>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception )
            {
                return null;
            }
        }
    }
}