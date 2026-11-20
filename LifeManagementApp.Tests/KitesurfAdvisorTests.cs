using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;

namespace LifeManagementApp.Tests;

public class KitesurfAdvisorTests
{
	private readonly FakeSettingsService _settings = new();
	private readonly KitesurfAdvisor _advisor;

	public KitesurfAdvisorTests()
	{
		_advisor = new KitesurfAdvisor(_settings);
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(2.8)]
	[InlineData(4.9)]
	[InlineData(5.0)]
	public void IsKitesurfWeather_ReturnsFalse_AtOrBelowDefaultLimit(double windSpeed)
	{
		Assert.False(_advisor.IsKitesurfWeather(new WeatherInfo { WindSpeed = windSpeed }));
	}

	[Theory]
	[InlineData(5.1)]
	[InlineData(8.0)]
	[InlineData(15.0)]
	public void IsKitesurfWeather_ReturnsTrue_AboveDefaultLimit(double windSpeed)
	{
		Assert.True(_advisor.IsKitesurfWeather(new WeatherInfo { WindSpeed = windSpeed }));
	}

	[Fact]
	public void DefaultWindLimit_IsFiveMetersPerSecond()
	{
		Assert.Equal(5.0, PreferencesSettingsService.DefaultWindLimit);
	}

	[Theory]
	[InlineData(7.5, 7.5, false)]
	[InlineData(7.5, 7.6, true)]
	[InlineData(3.0, 4.0, true)]
	public void IsKitesurfWeather_UsesSavedLimit(double limit, double windSpeed, bool expected)
	{
		_settings.WindLimit = limit;

		Assert.Equal(limit, _advisor.WindLimit);
		Assert.Equal(expected, _advisor.IsKitesurfWeather(new WeatherInfo { WindSpeed = windSpeed }));
	}

	[Fact]
	public void LimitChange_AppliesImmediately()
	{
		var weather = new WeatherInfo { WindSpeed = 6.0 };
		Assert.True(_advisor.IsKitesurfWeather(weather));

		_settings.WindLimit = 6.5;

		Assert.False(_advisor.IsKitesurfWeather(weather));
	}
}
