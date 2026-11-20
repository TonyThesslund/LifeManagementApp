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

	/// <summary>All shown to-dos, unfinished first.</summary>
	public ObservableCollection<TodoItem> Todos { get; } = [];

	/// <summary>The unfinished to-dos, shown under "To do".</summary>
	public ObservableCollection<TodoItem> OpenTodos { get; } = [];

	/// <summary>The finished to-dos, shown under "Done".</summary>
	public ObservableCollection<TodoItem> DoneTodos { get; } = [];

	[ObservableProperty]
	public partial bool HasOpenTodos { get; set; }

	[ObservableProperty]
	public partial bool HasDoneTodos { get; set; }

	/// <summary>True when a load has finished and found nothing; shows the empty state.</summary>
	[ObservableProperty]
	public partial bool IsEmpty { get; set; }

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
	[NotifyPropertyChangedFor(nameof(EmptyHint))]
	public partial Category SelectedFilter { get; set; } = AllCategories;

	public string EmptyMessage => SelectedFilter == AllCategories
		? "No to-dos yet."
		: $"No to-dos in {SelectedFilter.Name}.";

	/// <summary>What to do about an empty list, shown under <see cref="EmptyMessage"/>.</summary>
	public string EmptyHint => SelectedFilter == AllCategories
		? "Add your first to-do with the Add to-do button."
		: "Pick another category, or add a to-do to this one.";

	partial void OnSortOrderChanged(TodoSortOrder value)
	{
		LoadTodosCommand.Execute(null);
	}

	partial void OnSelectedFilterChanged(Category value)
	{
		// The picker can report "nothing selected" while its items change; treat that as all categories
		if (value is null)
		{
			SelectedFilter = AllCategories;
			return;
		}

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
			// Reloaded every time, so categories added or deleted in Settings show up here
			UpdateFilters(await _repository.GetCategoriesAsync());

			int? categoryId = SelectedFilter == AllCategories ? null : SelectedFilter.Id;
			var items = await _repository.GetAllAsync(SortOrder, categoryId);

			Todos.Clear();
			OpenTodos.Clear();
			DoneTodos.Clear();
			foreach (var item in items)
			{
				Todos.Add(item);
				(item.IsDone ? DoneTodos : OpenTodos).Add(item);
			}

			HasOpenTodos = OpenTodos.Count > 0;
			HasDoneTodos = DoneTodos.Count > 0;
			IsEmpty = Todos.Count == 0;
		}
		finally
		{
			IsBusy = false;
		}
	}

	/// <summary>
	/// Brings <see cref="Filters"/> in line with the categories in the database. Categories that are still there
	/// keep the same object; the list isn't cleared and refilled.
	/// </summary>
	private void UpdateFilters(List<Category> categories)
	{
		var ids = categories.Select(c => c.Id).ToHashSet();

		if (SelectedFilter != AllCategories && !ids.Contains(SelectedFilter.Id))
		{
			// The chosen category was deleted. Switching first keeps the picker from losing its selection.
			// The reload this asks for is skipped while busy; this load continues with "All categories".
			SelectedFilter = AllCategories;
		}

		var selected = SelectedFilter;

		for (var i = Filters.Count - 1; i >= 1; i--)
		{
			if (!ids.Contains(Filters[i].Id))
			{
				Filters.RemoveAt(i);
			}
		}

		// Categories come sorted by name; insert new ones at their place after "All categories"
		for (var i = 0; i < categories.Count; i++)
		{
			if (!Filters.Skip(1).Any(f => f.Id == categories[i].Id))
			{
				Filters.Insert(i + 1, categories[i]);
			}
		}

		// The picker keeps its selected position, not item, when items are added or removed before it,
		// and passes the item now at that position back here. Put the chosen filter back.
		SelectedFilter = selected;
	}
}
