using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface IKitesurfAdvisor
{
	bool IsKitesurfWeather(WeatherInfo weather);
}
