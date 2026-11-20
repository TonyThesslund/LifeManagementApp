using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Views;

namespace LifeManagementApp.ViewModels;

public partial class TodoListViewModel : ObservableObject
{
	private readonly ITodoRepository _repository;
	private readonly INavigationService _navigation;

	public TodoListViewModel(ITodoRepository repository, INavigationService navigation)
	{
		_repository = repository;
		_navigation = navigation;
	}

	public ObservableCollection<TodoItem> Todos { get; } = [];

	public IReadOnlyList<TodoSortOrder> SortOrders { get; } = Enum.GetValues<TodoSortOrder>();

	[ObservableProperty]
	public partial TodoSortOrder SortOrder { get; set; } = TodoSortOrder.Priority;

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	partial void OnSortOrderChanged(TodoSortOrder value)
	{
		LoadTodosCommand.Execute(null);
	}

	[RelayCommand]
	private Task AddTodoAsync()
	{
		return _navigation.GoToAsync(nameof(TodoDetailPage));
	}

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
			var items = await _repository.GetAllAsync(SortOrder);

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
