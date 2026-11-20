using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests;

public class KitesurfAdvisorTests
{
	private readonly KitesurfAdvisor _advisor = new();

	[Theory]
	[InlineData(0.0)]
	[InlineData(2.8)]
	[InlineData(4.9)]
	[InlineData(5.0)]
	public void IsKitesurfWeather_ReturnsFalse_AtOrBelowLimit(double windSpeed)
	{
		Assert.False(_advisor.IsKitesurfWeather(new WeatherInfo { WindSpeed = windSpeed }));
	}

	[Theory]
	[InlineData(5.1)]
	[InlineData(8.0)]
	[InlineData(15.0)]
	public void IsKitesurfWeather_ReturnsTrue_AboveLimit(double windSpeed)
	{
		Assert.True(_advisor.IsKitesurfWeather(new WeatherInfo { WindSpeed = windSpeed }));
	}

	[Fact]
	public void MinimumWindSpeed_IsFiveMetersPerSecond()
	{
		Assert.Equal(5.0, KitesurfAdvisor.MinimumWindSpeed);
	}
}
