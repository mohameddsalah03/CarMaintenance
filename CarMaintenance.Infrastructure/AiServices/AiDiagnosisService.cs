using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Shared.DTOs.AI.Request;
using CarMaintenance.Shared.DTOs.AI.Response;
using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CarMaintenance.Infrastructure.AiServices
{
    public class AiDiagnosisService : IAiDiagnosisService
    {
        private readonly HttpClient _httpClient;
        private readonly AISettings _aiSettings;
        private readonly ILogger<AiDiagnosisService> _logger;

        private const int MaxAttempts = 3;

        private static readonly JsonSerializerOptions _deserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public AiDiagnosisService(
            HttpClient httpClient,
            IOptions<AISettings> aiSettings,
            ILogger<AiDiagnosisService> logger)
        {
            _httpClient = httpClient;
            _aiSettings = aiSettings.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_aiSettings.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _aiSettings.ApiKey);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CarMaintenance-Backend/1.0");
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<AiDiagnosisResponseDto?> AnalyzeProblemAsync(AiDiagnosisRequestDto request)
        {
            var payload = new
            {
                problemDescription = request.ProblemDescription,
                vehicleContext = request.VehicleContext is null ? null : new
                {
                    brand = request.VehicleContext.Brand,
                    model = request.VehicleContext.Model,
                    year = request.VehicleContext.Year
                }
            };

            var json = JsonSerializer.Serialize(payload);

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post, _aiSettings.DiagnosisUrl)
                    {
                        Version = HttpVersion.Version11,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };

                    _logger.LogInformation(
                        "AI Request (attempt {Attempt}/{Max}) → URL: {Url}, Payload: {Payload}",
                        attempt, MaxAttempts, _aiSettings.DiagnosisUrl, json);

                    using var response = await _httpClient.SendAsync(req);
                    var body = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation(
                        "AI Response → Status: {Status} ({Code}), Body: {Body}",
                        response.StatusCode, (int)response.StatusCode, body);

                    if (response.IsSuccessStatusCode)
                        return JsonSerializer.Deserialize<AiDiagnosisResponseDto>(body, _deserializeOptions);

                    if ((int)response.StatusCode >= 500 && attempt < MaxAttempts)
                    {
                        _logger.LogWarning(
                            "AI returned {Code} (attempt {Attempt}). Retrying...",
                            (int)response.StatusCode, attempt);

                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                        continue;
                    }

                    _logger.LogWarning(
                        "AI API returned non-success status {Code}. Body: {Body}",
                        (int)response.StatusCode, body);
                    return null;
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex,
                        "AI API timeout after {Timeout}s (attempt {Attempt})",
                        _aiSettings.TimeoutSeconds, attempt);

                    if (attempt < MaxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                        continue;
                    }
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex,
                        "AI API request failed (HttpRequestException) (attempt {Attempt})", attempt);

                    if (attempt < MaxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                        continue;
                    }
                    return null;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "AI API returned invalid JSON");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in AI diagnosis");
                    return null;
                }
            }

            return null;
        }
    }
}