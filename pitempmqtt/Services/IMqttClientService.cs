using Microsoft.Extensions.Hosting;
using Pitempmqtt.Models;

namespace Pitempmqtt.Services;

internal interface IMqttClientService : IHostedService
{
    Task<bool> IsConnectedAsync { get; }

    Task<bool> PublishTemeratureAsync(double temperature);
}
