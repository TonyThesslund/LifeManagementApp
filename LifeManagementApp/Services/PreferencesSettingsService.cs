using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

/// <summary>
/// Saves the settings with MAUI Preferences, which keeps them between app restarts.
/// </summary>
public class PreferencesSettingsService(IPreferences preferences) : ISettingsService
{
	public const double DefaultWindLimit = 5.0;

	private const string LocationNameKey = "location_name";
	private const string LatitudeKey = "location_latitude";
	private const string LongitudeKey = "location_longitude";
	private const string WindLimitKey = "wind_limit";
	private const string ThemeKey = "theme";

	public WeatherLocation Location
	{
		get
		{
			if (!preferences.ContainsKey(LocationNameKey))
			{
				return WeatherLocation.Vaasa;
			}

			return new WeatherLocation(
				preferences.Get(LocationNameKey, WeatherLocation.Vaasa.Name),
				preferences.Get(LatitudeKey, WeatherLocation.Vaasa.Latitude),
				preferences.Get(LongitudeKey, WeatherLocation.Vaasa.Longitude));
		}
		set
		{
			preferences.Set(LocationNameKey, value.Name);
			preferences.Set(LatitudeKey, value.Latitude);
			preferences.Set(LongitudeKey, value.Longitude);
		}
	}

	public double WindLimit
	{
		get => preferences.Get(WindLimitKey, DefaultWindLimit);
		set => preferences.Set(WindLimitKey, value);
	}

	public ThemeSetting Theme
	{
		// Saved as text so the stored value stays readable and survives reordering the enum
		get => Enum.TryParse<ThemeSetting>(preferences.Get(ThemeKey, nameof(ThemeSetting.System)), out var theme)
			? theme
			: ThemeSetting.System;
		set => preferences.Set(ThemeKey, value.ToString());
	}
}
