using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using Pitempmqtt.Models;

namespace Pitempmqtt.Services;

internal class MqttClientService : IMqttClientService
{
    private readonly ILogger<MqttClientService> _logger;
    private readonly IOptions<MqttSettings> _mqttSettings;
    private readonly MqttClientOptions _options;
    private readonly IMqttClient _mqttClient;

    public MqttClientService(ILogger<MqttClientService> logger, IOptions<MqttSettings> mqttSettings)
    {
        _logger = logger;
        _mqttSettings = mqttSettings;
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttSettings.Value.Server, _mqttSettings.Value.Port)
            .WithClientId(_mqttSettings.Value.ClientId)
            .Build();
        _mqttClient = new MqttClientFactory().CreateMqttClient();
        ConfigureMqttClient();
    }

    public Task<bool> IsConnectedAsync => _mqttClient.IsConnected ? Task.FromResult(true) : Task.FromResult(false);

    public async Task<bool> PublishTemeratureAsync(double temperature)
    {
        //var payload = $"{temperature:F2}";
        var payload = temperature.ToString("F2", CultureInfo.InvariantCulture);

        var result = await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"{_mqttSettings.Value.Topic}/{_mqttSettings.Value.ClientId}/status/temerature")
            .WithPayload(payload)
            .Build());
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to publish temperature message to MQTT broker");
        }
        return result.IsSuccess;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MqttClientService starting...");
        try
        {
            await _mqttClient.ConnectAsync(_options);
        }
        catch (Exception)
        {
        }
        _ = Task.Run(
           async () =>
           {
               while (!cancellationToken.IsCancellationRequested)
               {
                   try
                   {
                       // This code will also do the very first connect! So no call to _ConnectAsync_ is required in the first place.
                       if (!await _mqttClient.TryPingAsync(cancellationToken))
                       {
                           await _mqttClient.ConnectAsync(_mqttClient.Options, cancellationToken);

                           // Subscribe to topics when session is clean etc.
                           _logger.LogInformation("The MQTT client is connected.");
                       }
                   }
                   catch (Exception ex)
                   {
                       // Handle the exception properly (logging etc.).
                       _logger.LogError(ex, "The MQTT client  connection failed");
                   }
                   finally
                   {
                       // Check the connection state every 5 seconds and perform a reconnect if required.
                       await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                   }
               }
           });
        _logger.LogInformation("MqttClientService started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected)
        {
               await SendHartBeat(false);
        }
        _logger.LogInformation("MqttClientService stopping....");
        if (cancellationToken.IsCancellationRequested)
        {
            var disconnectOption = new MqttClientDisconnectOptions
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
                ReasonString = "NormalDiconnection"
            };
            await _mqttClient.DisconnectAsync(disconnectOption, cancellationToken);
        }
        if (_mqttClient.IsConnected)
        {
                await _mqttClient.DisconnectAsync();
        }
        _logger.LogInformation("MqttClientService stopped");
    }

    private void ConfigureMqttClient()
    {
        _mqttClient.ConnectedAsync += HandleConnectedAsync;
        _mqttClient.DisconnectedAsync += HandleDisconnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
    }

    private async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        await ParseTopic(args.ApplicationMessage.Topic, args.ApplicationMessage.ConvertPayloadToString());
    }

    private async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        _logger.LogInformation("MQTTClient disconnected from server");
        await Task.CompletedTask;
    }

    private async Task HandleConnectedAsync(MqttClientConnectedEventArgs args)
    {
        _logger.LogInformation("MQTTClient connected to server {host}", _mqttSettings.Value.Server);
        await SendHartBeat(true);
        await AnnounceAsync(new AnnouncePayload(_mqttSettings.Value.ClientId!, "DS18B20", "000000000000", _mqttSettings.Value.Server!));
        await _mqttClient.SubscribeAsync($"{_mqttSettings.Value.Topic}/command");
        await Task.CompletedTask;
    }

    private async Task ParseTopic(string topic, string payload)
    {
        _logger.LogDebug("Received message on topic {topic} with payload {payload}", topic, payload);
        if (topic == $"{_mqttSettings.Value.Topic}/command")
        {
            await ProcessCommand(payload);
            return;
        }
        await Task.CompletedTask;
    }

    private async Task ProcessCommand(string Payload)
    {
        _logger.LogDebug("Received command with payload {payload}", Payload);
        if (Payload == "announce")
        {
            await AnnounceAsync(new AnnouncePayload(_mqttSettings.Value.ClientId!, "DS18B20", "000000000000", _mqttSettings.Value.Server!));
        }
        await Task.CompletedTask;
    }

    private async Task SendHartBeat(bool onLine = true)
    {
        var payload = $"{onLine.ToString().ToLower()}";

        var result = await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"{_mqttSettings.Value.Topic}/{_mqttSettings.Value.ClientId}/online")
            .WithPayload(payload)
            .WithRetainFlag(true)
            .Build());
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to send heartbeat message to MQTT broker");
        }
    }

    private async Task AnnounceAsync(AnnouncePayload announce)
    {
        var payload = JsonSerializer.Serialize(announce);

        var result = await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"{_mqttSettings.Value.Topic}/announce")
            .WithPayload(payload)
            .Build());
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to send announce message to MQTT broker");
        }
    }
}
