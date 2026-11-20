using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

/// <summary>
/// The user's saved app settings. Changes are saved immediately.
/// </summary>
public interface ISettingsService
{
	/// <summary>Where the weather is shown for. Default: Vaasa.</summary>
	WeatherLocation Location { get; set; }

	/// <summary>Kitesurf weather when the wind is above this, in m/s. Default: 5.</summary>
	double WindLimit { get; set; }

	/// <summary>Light, dark or follow Windows. Default: System.</summary>
	ThemeSetting Theme { get; set; }
}
