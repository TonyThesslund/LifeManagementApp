using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Returns chosen places or throws a chosen error. Set <see cref="Gate"/> to hold the search open
/// until the test completes it, to check what the page shows while searching.
/// </summary>
public class FakeGeocodingService : IGeocodingService
{
	public List<WeatherLocation> Results { get; set; } = [];

	public Exception? Error { get; set; }

	public TaskCompletionSource? Gate { get; set; }

	public int CallCount { get; private set; }

	public string? LastName { get; private set; }

	public async Task<IReadOnlyList<WeatherLocation>> SearchAsync(string name)
	{
		CallCount++;
		LastName = name;

		if (Gate is not null)
		{
			await Gate.Task;
		}

		if (Error is not null)
		{
			throw Error;
		}

		return Results;
	}
}
