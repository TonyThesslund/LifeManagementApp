using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Views;

public partial class TodoDetailPage : ContentPage
{
	public TodoDetailPage(TodoDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
