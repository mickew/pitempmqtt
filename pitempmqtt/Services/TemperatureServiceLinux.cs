using Iot.Device.OneWire;

namespace Pitempmqtt.Services;

internal class TemperatureServiceLinux : ITemperatureService
{
    public async Task<double> GetTemperature()
    {
        foreach (var dev in OneWireThermometerDevice.EnumerateDevices())
        {
            if (dev is OneWireThermometerDevice thermometer)
            {
                return (await thermometer.ReadTemperatureAsync()).DegreesCelsius;
            }
        }
        throw new Exception("No OneWireThermometerDevice found");
    }
}
