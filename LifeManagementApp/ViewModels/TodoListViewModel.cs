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
	private readonly IDialogService _dialogs;

	public TodoListViewModel(ITodoRepository repository, INavigationService navigation, IDialogService dialogs)
	{
		_repository = repository;
		_navigation = navigation;
		_dialogs = dialogs;
	}

	public ObservableCollection<TodoItem> Todos { get; } = [];

	public IReadOnlyList<TodoSortOrder> SortOrders { get; } = Enum.GetValues<TodoSortOrder>();

	[ObservableProperty]
	public partial TodoSortOrder SortOrder { get; set; } = TodoSortOrder.Priority;

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	/// <summary>Filter option that shows every to-do. Not stored in the database (Id 0).</summary>
	public static Category AllCategories { get; } = new() { Id = 0, Name = "All categories" };

	/// <summary>"All categories" first, then the categories from the database.</summary>
	public ObservableCollection<Category> Filters { get; } = [AllCategories];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(EmptyMessage))]
	public partial Category SelectedFilter { get; set; } = AllCategories;

	public string EmptyMessage => SelectedFilter == AllCategories
		? "No to-dos yet."
		: $"No to-dos in {SelectedFilter.Name}.";

	private bool _categoriesLoaded;

	partial void OnSortOrderChanged(TodoSortOrder value)
	{
		LoadTodosCommand.Execute(null);
	}

	partial void OnSelectedFilterChanged(Category value)
	{
		LoadTodosCommand.Execute(null);
	}

	[RelayCommand]
	private Task AddTodoAsync()
	{
		return _navigation.GoToAsync(nameof(TodoDetailPage));
	}

	[RelayCommand]
	private Task EditTodoAsync(TodoItem item)
	{
		return _navigation.GoToAsync(nameof(TodoDetailPage), new Dictionary<string, object>
		{
			[TodoDetailViewModel.IdParameter] = item.Id
		});
	}

	[RelayCommand]
	private async Task ToggleDoneAsync(TodoItem item)
	{
		item.IsDone = !item.IsDone;
		await _repository.UpdateAsync(item);
		await LoadTodosAsync();
	}

	[RelayCommand]
	private async Task DeleteTodoAsync(TodoItem item)
	{
		var confirmed = await _dialogs.ConfirmAsync(
			"Delete to-do",
			$"Delete \"{item.Title}\"? This can't be undone.",
			"Delete",
			"Cancel");
		if (!confirmed)
		{
			return;
		}

		await _repository.DeleteAsync(item.Id);
		await LoadTodosAsync();
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
			if (!_categoriesLoaded)
			{
				foreach (var category in await _repository.GetCategoriesAsync())
				{
					Filters.Add(category);
				}
				_categoriesLoaded = true;
			}

			int? categoryId = SelectedFilter == AllCategories ? null : SelectedFilter.Id;
			var items = await _repository.GetAllAsync(SortOrder, categoryId);

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
