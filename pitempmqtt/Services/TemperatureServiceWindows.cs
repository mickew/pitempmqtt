namespace Pitempmqtt.Services;

internal class TemperatureServiceWindows : ITemperatureService
{
    Task<double> ITemperatureService.GetTemperature()
    {
        Random rnd = new Random();
        double temp = rnd.Next(-10, 35) + rnd.NextDouble();
        return Task.FromResult(temp);
    }
}
