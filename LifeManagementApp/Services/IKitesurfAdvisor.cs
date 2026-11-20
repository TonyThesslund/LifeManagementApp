using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface IKitesurfAdvisor
{
	/// <summary>Wind speed in m/s that must be exceeded for kitesurfing.</summary>
	double WindLimit { get; }

	bool IsKitesurfWeather(WeatherInfo weather);
}
