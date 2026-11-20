using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Returns a chosen weather or throws a chosen error. Set <see cref="Gate"/> to hold the call
/// open until the test completes it, to check what the UI shows while loading.
/// </summary>
public class FakeWeatherService : IWeatherService
{
	public WeatherInfo Result { get; set; } = new() { Temperature = 1.5, WindSpeed = 2.7 };

	public Exception? Error { get; set; }

	public TaskCompletionSource? Gate { get; set; }

	public int CallCount { get; private set; }

	public (double Latitude, double Longitude)? LastCoordinates { get; private set; }

	public async Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude)
	{
		CallCount++;
		LastCoordinates = (latitude, longitude);

		if (Gate is not null)
		{
			await Gate.Task;
		}

		if (Error is not null)
		{
			throw Error;
		}

		return Result;
	}
}
