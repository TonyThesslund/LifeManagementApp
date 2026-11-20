using System.Globalization;
using System.Net;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;

namespace LifeManagementApp.Tests;

public class OpenMeteoWeatherServiceTests
{
	private const string ValidJson = """
		{"current":{"time":"2026-11-20T12:00","interval":900,"temperature_2m":1.4,"wind_speed_10m":6.3}}
		""";

	private static (OpenMeteoWeatherService Service, FakeHttpMessageHandler Handler) CreateService(
		HttpStatusCode statusCode = HttpStatusCode.OK, string content = ValidJson)
	{
		var handler = new FakeHttpMessageHandler(statusCode, content);
		return (new OpenMeteoWeatherService(new HttpClient(handler)), handler);
	}

	[Fact]
	public async Task GetCurrentWeatherAsync_MapsResponseToWeatherInfo()
	{
		var (service, _) = CreateService();

		var weather = await service.GetCurrentWeatherAsync(WeatherLocation.Vaasa.Latitude, WeatherLocation.Vaasa.Longitude);

		Assert.Equal(1.4, weather.Temperature);
		Assert.Equal(6.3, weather.WindSpeed);
		Assert.Equal(new DateTime(2026, 11, 20, 12, 0, 0), weather.Time);
	}

	[Fact]
	public async Task GetCurrentWeatherAsync_RequestsWindSpeedInMetersPerSecond()
	{
		var (service, handler) = CreateService();

		await service.GetCurrentWeatherAsync(63.10, 21.62);

		var query = handler.LastRequest!.RequestUri!.Query;
		Assert.Contains("wind_speed_unit=ms", query);
		Assert.Contains("current=temperature_2m,wind_speed_10m", query);
	}

	[Fact]
	public async Task GetCurrentWeatherAsync_SendsCoordinates()
	{
		var (service, handler) = CreateService();

		await service.GetCurrentWeatherAsync(63.10, 21.62);

		var uri = handler.LastRequest!.RequestUri!;
		Assert.Equal("api.open-meteo.com", uri.Host);
		Assert.Contains("latitude=63.1&", uri.Query);
		Assert.Contains("longitude=21.62&", uri.Query);
	}

	[Fact]
	public void BuildUrl_UsesDotDecimals_OnFinnishCulture()
	{
		var original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("fi-FI");

			var url = OpenMeteoWeatherService.BuildUrl(63.10, 21.62);

			Assert.Contains("latitude=63.1&", url);
			Assert.Contains("longitude=21.62&", url);
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}
}
