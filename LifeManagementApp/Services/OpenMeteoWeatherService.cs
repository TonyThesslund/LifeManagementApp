using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public class OpenMeteoWeatherService(HttpClient httpClient, IConnectivity connectivity) : IWeatherService
{
	private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

	public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	public async Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude)
	{
		if (connectivity.NetworkAccess != NetworkAccess.Internet)
		{
			throw new WeatherUnavailableException(WeatherUnavailableException.NoInternetMessage);
		}

		OpenMeteoResponse? response;
		try
		{
			using var httpResponse = await httpClient.GetAsync(BuildUrl(latitude, longitude));

			if (!httpResponse.IsSuccessStatusCode)
			{
				throw new WeatherUnavailableException(WeatherUnavailableException.BadResponseMessage);
			}

			response = await httpResponse.Content.ReadFromJsonAsync<OpenMeteoResponse>();
		}
		catch (HttpRequestException ex)
		{
			// DNS failure, connection refused, connection dropped...
			throw new WeatherUnavailableException(WeatherUnavailableException.UnreachableMessage, ex);
		}
		catch (TaskCanceledException ex)
		{
			// HttpClient reports a timeout as a cancelled task.
			throw new WeatherUnavailableException(WeatherUnavailableException.UnreachableMessage, ex);
		}
		catch (JsonException ex)
		{
			throw new WeatherUnavailableException(WeatherUnavailableException.BadResponseMessage, ex);
		}

		return response?.Current?.ToWeatherInfo()
			?? throw new WeatherUnavailableException(WeatherUnavailableException.BadResponseMessage);
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
