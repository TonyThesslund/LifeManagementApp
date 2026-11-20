using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Records alerts and confirmations instead of showing them. Confirmations are answered with <see cref="ConfirmResult"/>.
/// </summary>
public class FakeDialogService : IDialogService
{
	public List<(string Title, string Message)> Alerts { get; } = [];

	public List<(string Title, string Message, string Accept, string Cancel)> Confirmations { get; } = [];

	public bool ConfirmResult { get; set; } = true;

	public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
	{
		Confirmations.Add((title, message, accept, cancel));
		return Task.FromResult(ConfirmResult);
	}

	public Task ShowAlertAsync(string title, string message)
	{
		Alerts.Add((title, message));
		return Task.CompletedTask;
	}
}
