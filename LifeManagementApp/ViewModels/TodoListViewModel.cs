using CommunityToolkit.Mvvm.ComponentModel;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoListViewModel : ObservableObject
{
	private readonly ITodoRepository _repository;

	public TodoListViewModel(ITodoRepository repository)
	{
		_repository = repository;
	}
}
