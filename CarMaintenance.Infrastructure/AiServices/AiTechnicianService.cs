using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Shared.DTOs.AI.Request;
using CarMaintenance.Shared.DTOs.AI.Response;
using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CarMaintenance.Infrastructure.AiServices
{
    public class AiTechnicianService : IAiTechnicianService
    {
        private readonly HttpClient _httpClient;
        private readonly AISettings _aiSettings;

        public AiTechnicianService(
            HttpClient httpClient,
            IOptions<AISettings> aiSettings)
        {
            _httpClient = httpClient;
            _aiSettings = aiSettings.Value;

            _httpClient.Timeout = TimeSpan.FromSeconds(_aiSettings.TimeoutSeconds);
        }

        public async Task<AiAssignmentResponseDto?> GetTechnicianRecommendationAsync(AiAssignmentRequestDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _aiSettings.ApiKey);


                var response = await _httpClient.PostAsync(_aiSettings.TechnicianAssignmentUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AiAssignmentResponseDto>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}