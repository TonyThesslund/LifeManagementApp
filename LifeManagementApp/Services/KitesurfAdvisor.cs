using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public class KitesurfAdvisor(ISettingsService settings) : IKitesurfAdvisor
{
	/// <summary>Wind speed in m/s that must be exceeded, from the user's settings (default 5).</summary>
	public double WindLimit => settings.WindLimit;

	public bool IsKitesurfWeather(WeatherInfo weather)
	{
		return weather.WindSpeed > WindLimit;
	}
}
