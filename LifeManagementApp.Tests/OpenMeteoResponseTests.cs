using System.Text.Json;
using LifeManagementApp.Models;

namespace LifeManagementApp.Tests;

public class OpenMeteoResponseTests
{
	// A real response from the Open-Meteo API for Vaasa.
	private const string SampleJson = """
		{"latitude":63.10396,"longitude":21.622711,"generationtime_ms":0.027,"utc_offset_seconds":0,
		 "timezone":"GMT","timezone_abbreviation":"GMT","elevation":18.0,
		 "current_units":{"time":"iso8601","interval":"seconds","temperature_2m":"°C","wind_speed_10m":"m/s"},
		 "current":{"time":"2026-11-20T10:00","interval":900,"temperature_2m":1.4,"wind_speed_10m":2.80}}
		""";

	[Fact]
	public void Deserialize_ReadsCurrentValues()
	{
		var response = JsonSerializer.Deserialize<OpenMeteoResponse>(SampleJson);

		Assert.NotNull(response?.Current);
		Assert.Equal(1.4, response.Current.Temperature);
		Assert.Equal(2.8, response.Current.WindSpeed);
		Assert.Equal(new DateTime(2026, 11, 20, 10, 0, 0), response.Current.Time);
	}

	[Fact]
	public void ToWeatherInfo_CopiesValues()
	{
		var current = new OpenMeteoCurrent
		{
			Temperature = -3.5,
			WindSpeed = 7.2,
			Time = new DateTime(2026, 11, 20, 12, 0, 0)
		};

		var weather = current.ToWeatherInfo();

		Assert.Equal(-3.5, weather.Temperature);
		Assert.Equal(7.2, weather.WindSpeed);
		Assert.Equal(new DateTime(2026, 11, 20, 12, 0, 0), weather.Time);
	}

	[Fact]
	public void Deserialize_WithoutCurrent_LeavesCurrentNull()
	{
		var response = JsonSerializer.Deserialize<OpenMeteoResponse>("""{"latitude":63.1}""");

		Assert.NotNull(response);
		Assert.Null(response.Current);
	}
}
