namespace LifeManagementApp.Services;

public class ShellDialogService : IDialogService
{
	public Task ShowAlertAsync(string title, string message)
	{
		return Shell.Current.DisplayAlert(title, message, "OK");
	}
}
