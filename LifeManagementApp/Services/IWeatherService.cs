using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface IWeatherService
{
	Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude);
}
