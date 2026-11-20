using System.Net.Http.Json;
using System.Text.Json;
using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public class OpenMeteoGeocodingService(HttpClient httpClient, IConnectivity connectivity) : IGeocodingService
{
	private const string BaseUrl = "https://geocoding-api.open-meteo.com/v1/search";

	public const int MaxResults = 10;

	public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	public async Task<IReadOnlyList<WeatherLocation>> SearchAsync(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return [];
		}

		if (connectivity.NetworkAccess != NetworkAccess.Internet)
		{
			throw new LocationSearchException(LocationSearchException.NoInternetMessage);
		}

		GeocodingResponse? response;
		try
		{
			using var httpResponse = await httpClient.GetAsync(BuildUrl(name));

			if (!httpResponse.IsSuccessStatusCode)
			{
				throw new LocationSearchException(LocationSearchException.BadResponseMessage);
			}

			response = await httpResponse.Content.ReadFromJsonAsync<GeocodingResponse>();
		}
		catch (HttpRequestException ex)
		{
			throw new LocationSearchException(LocationSearchException.UnreachableMessage, ex);
		}
		catch (TaskCanceledException ex)
		{
			// HttpClient reports a timeout as a cancelled task.
			throw new LocationSearchException(LocationSearchException.UnreachableMessage, ex);
		}
		catch (JsonException ex)
		{
			throw new LocationSearchException(LocationSearchException.BadResponseMessage, ex);
		}

		if (response is null)
		{
			throw new LocationSearchException(LocationSearchException.BadResponseMessage);
		}

		return response.Results?.Select(r => r.ToWeatherLocation()).ToList() ?? [];
	}

	public static string BuildUrl(string name)
	{
		// Escaped so names with spaces or letters like "ä" are sent correctly
		return $"{BaseUrl}?name={Uri.EscapeDataString(name.Trim())}"
			+ $"&count={MaxResults}"
			+ "&language=en&format=json";
	}
}
