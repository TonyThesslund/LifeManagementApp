using System.Globalization;
using System.Net.Http.Json;
using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public class OpenMeteoWeatherService(HttpClient httpClient) : IWeatherService
{
	private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

	public async Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude)
	{
		var url = BuildUrl(latitude, longitude);

		var response = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(url);

		return response?.Current?.ToWeatherInfo()
			?? throw new InvalidOperationException("The weather response did not contain current weather.");
	}

	public static string BuildUrl(double latitude, double longitude)
	{
		// InvariantCulture so coordinates always use a dot (63.1), even on a Finnish system that uses a comma.
		var lat = latitude.ToString(CultureInfo.InvariantCulture);
		var lon = longitude.ToString(CultureInfo.InvariantCulture);

		return $"{BaseUrl}?latitude={lat}&longitude={lon}"
			+ "&current=temperature_2m,wind_speed_10m"
			+ "&wind_speed_unit=ms"
			+ "&timezone=auto";
	}
}
