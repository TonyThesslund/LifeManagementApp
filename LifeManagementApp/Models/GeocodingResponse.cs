using System.Text.Json.Serialization;

namespace LifeManagementApp.Models;

/// <summary>
/// Matches the JSON returned by the Open-Meteo geocoding API. "results" is missing when nothing matches.
/// </summary>
public class GeocodingResponse
{
	[JsonPropertyName("results")]
	public List<GeocodingResult>? Results { get; set; }
}
