namespace LifeManagementApp.Services;

public interface IDialogService
{
	Task ShowAlertAsync(string title, string message);

	/// <summary>Asks a yes/no question. Returns true if the user chose <paramref name="accept"/>.</summary>
	Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
