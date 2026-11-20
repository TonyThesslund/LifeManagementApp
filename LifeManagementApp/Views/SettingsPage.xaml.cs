using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Views;

public partial class SettingsPage : ContentPage
{
	private readonly SettingsViewModel _viewModel;

	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.Load();
		await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);
	}
}
