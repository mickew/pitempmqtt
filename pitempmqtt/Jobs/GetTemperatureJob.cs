using Microsoft.Extensions.Logging;
using Pitempmqtt.Services;
using Quartz;
using UnitsNet;

namespace Pitempmqtt.Jobs;

internal class GetTemperatureJob : IJob
{
    private readonly ILogger<GetTemperatureJob> _logger;
    private readonly ITemperatureService _temperatureService;
    private readonly IMqttClientService _mqttClientService;
    private static double _lastTemperature = double.NaN;

    public GetTemperatureJob(ILogger<GetTemperatureJob> logger, ITemperatureService temperatureService, IMqttClientService mqttClientService)
    {
        _logger = logger;
        _temperatureService = temperatureService;
        _mqttClientService = mqttClientService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Executing GetTemperatureJob at {Time}", DateTimeOffset.Now);
        var (lastReadingOk, temperature) = await GetTemperatureAsync();
        if (lastReadingOk)
        {
            if (await _mqttClientService.IsConnectedAsync)
            {
                await PublishTemperatureAsync(temperature);
            }
            else
            {
                _logger.LogWarning("MQTT client is not connected. Skipping temperature publish.");
            }
        }
    }

    private async Task<(bool lastReadingOk, double temperature)> GetTemperatureAsync()
    {
        try
        {
            double temperature = await _temperatureService.GetTemperature();
            _logger.LogDebug("Current temperature: {Temperature}°C", temperature);
            var diff = Math.Abs(temperature - _lastTemperature);
            if (double.IsNaN(_lastTemperature) || diff >= 0.5)
            {
                _logger.LogDebug("Temperature changed from {LastTemperature}°C to {Temperature}°C", _lastTemperature, temperature);
                _lastTemperature = temperature;
            }
            return (true, temperature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting temperature");
        }
        return (false, _lastTemperature);
    }

    private async Task PublishTemperatureAsync(double temperature)
    {
        await _mqttClientService.PublishTemeratureAsync(temperature);
    }

    private async Task PublishTemperatureChangeAsync(double temperature)
    {
        // Implementation for publishing temperature change to MQTT broker
        await Task.CompletedTask;
    }
}
