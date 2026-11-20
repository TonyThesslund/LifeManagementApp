using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoListViewModel : ObservableObject
{
	private readonly ITodoRepository _repository;

	public TodoListViewModel(ITodoRepository repository)
	{
		_repository = repository;
	}

	public ObservableCollection<TodoItem> Todos { get; } = [];

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	[RelayCommand]
	private async Task LoadTodosAsync()
	{
		if (IsBusy)
		{
			return;
		}

		IsBusy = true;
		try
		{
			var items = await _repository.GetAllAsync(TodoSortOrder.Priority);

			Todos.Clear();
			foreach (var item in items)
			{
				Todos.Add(item);
			}
		}
		finally
		{
			IsBusy = false;
		}
	}
}
