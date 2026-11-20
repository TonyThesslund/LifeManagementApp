using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Views;

namespace LifeManagementApp.ViewModels;

public partial class HomeViewModel : ObservableObject
{
	public const int TopTodoCount = 3;

	private readonly ITodoRepository _repository;
	private readonly INavigationService _navigation;

	public HomeViewModel(ITodoRepository repository, INavigationService navigation)
	{
		_repository = repository;
		_navigation = navigation;
	}

	public ObservableCollection<TodoItem> TopTodos { get; } = [];

	[RelayCommand]
	private async Task LoadTodosAsync()
	{
		var items = await _repository.GetTopAsync(TopTodoCount);

		TopTodos.Clear();
		foreach (var item in items)
		{
			TopTodos.Add(item);
		}
	}

	[RelayCommand]
	private Task OpenTodoAsync(TodoItem item)
	{
		return _navigation.GoToAsync(nameof(TodoDetailPage), new Dictionary<string, object>
		{
			[TodoDetailViewModel.IdParameter] = item.Id
		});
	}
}
