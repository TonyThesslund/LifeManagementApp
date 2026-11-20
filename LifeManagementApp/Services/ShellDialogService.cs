namespace LifeManagementApp.Services;

public class ShellDialogService : IDialogService
{
	public Task ShowAlertAsync(string title, string message)
	{
		return Shell.Current.DisplayAlert(title, message, "OK");
	}

	public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
	{
		return Shell.Current.DisplayAlert(title, message, accept, cancel);
	}
}
