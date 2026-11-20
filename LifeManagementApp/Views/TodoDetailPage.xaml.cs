using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Views;

public partial class TodoDetailPage : ContentPage
{
	private readonly TodoDetailViewModel _viewModel;

	public TodoDetailPage(TodoDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.LoadCategoriesCommand.Execute(null);
	}
}
