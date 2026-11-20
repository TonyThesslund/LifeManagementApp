using System.Text.Json.Serialization;

namespace LifeManagementApp.Models;

/// <summary>
/// Matches the JSON returned by the Open-Meteo forecast API. Only the fields the app uses are included.
/// </summary>
public class OpenMeteoResponse
{
	[JsonPropertyName("current")]
	public OpenMeteoCurrent? Current { get; set; }
}
