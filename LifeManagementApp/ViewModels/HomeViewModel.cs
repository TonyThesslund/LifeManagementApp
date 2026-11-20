using CommunityToolkit.Mvvm.ComponentModel;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class HomeViewModel : ObservableObject
{
	private readonly ITodoRepository _repository;

	public HomeViewModel(ITodoRepository repository)
	{
		_repository = repository;
	}
}
