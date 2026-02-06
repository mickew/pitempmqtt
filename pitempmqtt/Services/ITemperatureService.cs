namespace Pitempmqtt.Services;

internal interface ITemperatureService
{
    Task<double> GetTemperature();
}
