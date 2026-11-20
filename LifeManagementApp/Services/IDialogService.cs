namespace LifeManagementApp.Services;

public interface IDialogService
{
	Task ShowAlertAsync(string title, string message);
}
