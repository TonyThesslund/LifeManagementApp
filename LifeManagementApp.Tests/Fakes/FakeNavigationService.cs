using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Records navigation calls instead of navigating.
/// </summary>
public class FakeNavigationService : INavigationService
{
	public List<string> Routes { get; } = [];

	public IDictionary<string, object>? LastParameters { get; private set; }

	public int BackCount { get; private set; }

	public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
	{
		Routes.Add(route);
		LastParameters = parameters;
		return Task.CompletedTask;
	}

	public Task GoBackAsync()
	{
		BackCount++;
		return Task.CompletedTask;
	}
}
