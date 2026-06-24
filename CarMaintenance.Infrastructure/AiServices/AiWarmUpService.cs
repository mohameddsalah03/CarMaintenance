using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CarMaintenance.Infrastructure.AiServices
{
    public class AiWarmUpService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AISettings _aiSettings;
        private readonly ILogger<AiWarmUpService> _logger;

        public AiWarmUpService(
            IHttpClientFactory factory,
            IOptions<AISettings> settings,
            ILogger<AiWarmUpService> logger)
        {
            _httpClientFactory = factory;
            _aiSettings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(120);
                    client.DefaultRequestHeaders.Add("X-API-Key", _aiSettings.ApiKey);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("CarMaintenance-WarmUp/1.0");
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    var payload = new { problemDescription = "تسخين النظام", vehicleContext = (object?)null };
                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(_aiSettings.DiagnosisUrl, content, stoppingToken);

                    _logger.LogInformation(
                        "AI Space warm-up POST → {Url} → {Status}",
                        _aiSettings.DiagnosisUrl, (int)response.StatusCode);
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI warm-up ping failed");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
                catch (TaskCanceledException) { break; }
            }
        }
    }
}