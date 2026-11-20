using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Settings kept in memory, starting with the same defaults as the real service.
/// </summary>
public class FakeSettingsService : ISettingsService
{
	public WeatherLocation Location { get; set; } = WeatherLocation.Vaasa;

	public double WindLimit { get; set; } = 5.0;

	public ThemeSetting Theme { get; set; } = ThemeSetting.System;
}
