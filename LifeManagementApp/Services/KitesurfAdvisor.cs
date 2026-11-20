using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public class KitesurfAdvisor : IKitesurfAdvisor
{
	/// <summary>Wind speed in m/s that must be exceeded for kitesurfing.</summary>
	public const double MinimumWindSpeed = 5.0;

	public bool IsKitesurfWeather(WeatherInfo weather)
	{
		return weather.WindSpeed > MinimumWindSpeed;
	}
}
