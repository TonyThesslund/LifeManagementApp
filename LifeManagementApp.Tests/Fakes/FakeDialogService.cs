using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Records alerts instead of showing them.
/// </summary>
public class FakeDialogService : IDialogService
{
	public List<(string Title, string Message)> Alerts { get; } = [];

	public Task ShowAlertAsync(string title, string message)
	{
		Alerts.Add((title, message));
		return Task.CompletedTask;
	}
}
