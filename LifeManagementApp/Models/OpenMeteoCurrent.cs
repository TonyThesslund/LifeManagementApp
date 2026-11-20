using System.Text.Json.Serialization;

namespace LifeManagementApp.Models;

/// <summary>
/// The "current" object in the Open-Meteo response.
/// </summary>
public class OpenMeteoCurrent
{
	[JsonPropertyName("time")]
	public DateTime Time { get; set; }

	[JsonPropertyName("temperature_2m")]
	public double Temperature { get; set; }

	[JsonPropertyName("wind_speed_10m")]
	public double WindSpeed { get; set; }

	public WeatherInfo ToWeatherInfo() => new()
	{
		Temperature = Temperature,
		WindSpeed = WindSpeed,
		Time = Time
	};
}
