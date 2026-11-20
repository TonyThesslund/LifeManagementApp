using System.Text.Json.Serialization;

namespace LifeManagementApp.Models;

/// <summary>
/// One place in the Open-Meteo geocoding response.
/// </summary>
public class GeocodingResult
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("latitude")]
	public double Latitude { get; set; }

	[JsonPropertyName("longitude")]
	public double Longitude { get; set; }

	/// <summary>Region, e.g. "Ostrobothnia".</summary>
	[JsonPropertyName("admin1")]
	public string? Admin1 { get; set; }

	[JsonPropertyName("country")]
	public string? Country { get; set; }

	public WeatherLocation ToWeatherLocation()
	{
		var details = string.Join(", ", new[] { Admin1, Country }.Where(part => !string.IsNullOrWhiteSpace(part)));
		return new WeatherLocation(Name, Latitude, Longitude, details.Length > 0 ? details : null);
	}
}
