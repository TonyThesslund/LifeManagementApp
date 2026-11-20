using LifeManagementApp.Models;
using LifeManagementApp.Tests.Fakes;
using LifeManagementApp.ViewModels;
using LifeManagementApp.Views;

namespace LifeManagementApp.Tests;

public class TodoListViewModelTests
{
	private readonly FakeTodoRepository _repository = new();
	private readonly FakeNavigationService _navigation = new();
	private readonly FakeDialogService _dialogs = new();

	private TodoListViewModel CreateViewModel() => new(_repository, _navigation, _dialogs);

	private async Task<(TodoListViewModel ViewModel, TodoItem Shown)> LoadWithAsync(TodoItem item)
	{
		_repository.Seed(item);
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		return (viewModel, viewModel.Todos.Single());
	}

	[Fact]
	public async Task ToggleDone_UnfinishedTodo_IsSavedAsDone()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum", IsDone = false });

		await viewModel.ToggleDoneCommand.ExecuteAsync(shown);

		var saved = Assert.Single(_repository.Updated);
		Assert.Equal(shown.Id, saved.Id);
		Assert.True(saved.IsDone);
	}

	[Fact]
	public async Task ToggleDone_FinishedTodo_IsSavedAsNotDone()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum", IsDone = true });

		await viewModel.ToggleDoneCommand.ExecuteAsync(shown);

		Assert.False(Assert.Single(_repository.Updated).IsDone);
	}

	[Fact]
	public async Task ToggleDone_ReloadsList_WithSavedState()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum", IsDone = false });

		await viewModel.ToggleDoneCommand.ExecuteAsync(shown);

		var reloaded = Assert.Single(viewModel.Todos);
		Assert.NotSame(shown, reloaded);
		Assert.True(reloaded.IsDone);
	}

	[Fact]
	public async Task ToggleDone_Twice_EndsUpNotDone()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum", IsDone = false });

		await viewModel.ToggleDoneCommand.ExecuteAsync(shown);
		await viewModel.ToggleDoneCommand.ExecuteAsync(viewModel.Todos.Single());

		Assert.False(viewModel.Todos.Single().IsDone);
		Assert.Equal(2, _repository.Updated.Count);
	}

	[Fact]
	public async Task DeleteTodo_Confirmed_DeletesAndReloads()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum" });
		_dialogs.ConfirmResult = true;

		await viewModel.DeleteTodoCommand.ExecuteAsync(shown);

		Assert.Equal([shown.Id], _repository.Deleted);
		Assert.Empty(viewModel.Todos);
	}

	[Fact]
	public async Task DeleteTodo_Cancelled_KeepsTodo()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum" });
		_dialogs.ConfirmResult = false;

		await viewModel.DeleteTodoCommand.ExecuteAsync(shown);

		Assert.Empty(_repository.Deleted);
		Assert.Single(viewModel.Todos);
	}

	[Fact]
	public async Task DeleteTodo_AsksOnce_NamingTheTodo()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum" });

		await viewModel.DeleteTodoCommand.ExecuteAsync(shown);

		var confirmation = Assert.Single(_dialogs.Confirmations);
		Assert.Contains("Call mum", confirmation.Message);
		Assert.Equal("Delete", confirmation.Accept);
	}

	[Fact]
	public async Task Load_GivesAllCategoriesFirst_ThenDatabaseCategories()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["All categories", "Personal", "Work", "Shopping"], viewModel.Filters.Select(c => c.Name));
		Assert.Same(TodoListViewModel.AllCategories, viewModel.SelectedFilter);
	}

	[Fact]
	public async Task Load_Twice_DoesNotDuplicateCategories()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["All categories", "Personal", "Work", "Shopping"], viewModel.Filters.Select(c => c.Name));
	}

	[Fact]
	public async Task Load_WithAllCategories_PassesNoCategory()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Null(_repository.LastCategoryId);
	}

	[Fact]
	public async Task ChoosingCategory_ReloadsWithItsId_AndShowsOnlyThose()
	{
		_repository.Seed(new TodoItem { Title = "Finish report", CategoryId = 2 });
		_repository.Seed(new TodoItem { Title = "Buy milk", CategoryId = 3 });
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Work");
		await viewModel.LoadTodosCommand.ExecutionTask!;

		Assert.Equal(2, _repository.LastCategoryId);
		Assert.Equal(["Finish report"], viewModel.Todos.Select(t => t.Title));
	}

	[Fact]
	public async Task ChoosingAllCategoriesAgain_PassesNoCategory()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Work");
		await viewModel.LoadTodosCommand.ExecutionTask!;

		viewModel.SelectedFilter = TodoListViewModel.AllCategories;
		await viewModel.LoadTodosCommand.ExecutionTask!;

		Assert.Null(_repository.LastCategoryId);
	}

	[Fact]
	public async Task Filtering_KeepsSortOrder()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		viewModel.SortOrder = TodoSortOrder.DueDate;
		await viewModel.LoadTodosCommand.ExecutionTask!;

		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Shopping");
		await viewModel.LoadTodosCommand.ExecutionTask!;

		Assert.Equal(TodoSortOrder.DueDate, _repository.LastSortOrder);
		Assert.Equal(3, _repository.LastCategoryId);
	}

	[Fact]
	public async Task EmptyMessage_NamesChosenCategory()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		Assert.Equal("No to-dos yet.", viewModel.EmptyMessage);

		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Work");

		Assert.Equal("No to-dos in Work.", viewModel.EmptyMessage);
	}

	[Fact]
	public async Task AddTodo_OpensDetailPage_WithoutId()
	{
		var viewModel = CreateViewModel();

		await viewModel.AddTodoCommand.ExecuteAsync(null);

		Assert.Equal([nameof(TodoDetailPage)], _navigation.Routes);
		Assert.Null(_navigation.LastParameters);
	}

	[Fact]
	public async Task EditTodo_OpensDetailPage_WithItsId()
	{
		var (viewModel, shown) = await LoadWithAsync(new TodoItem { Title = "Call mum" });

		await viewModel.EditTodoCommand.ExecuteAsync(shown);

		Assert.Equal([nameof(TodoDetailPage)], _navigation.Routes);
		Assert.Equal(shown.Id, _navigation.LastParameters![TodoDetailViewModel.IdParameter]);
	}

	[Fact]
	public async Task Load_UsesPrioritySortByDefault()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(TodoSortOrder.Priority, _repository.LastSortOrder);
	}

	[Fact]
	public async Task ChangingSortOrder_ReloadsWithThatOrder()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		viewModel.SortOrder = TodoSortOrder.DueDate;
		await viewModel.LoadTodosCommand.ExecutionTask!;

		Assert.Equal(TodoSortOrder.DueDate, _repository.LastSortOrder);
		Assert.Equal(2, _repository.GetAllCallCount);
	}

	[Fact]
	public async Task Load_ShowsLoadingIndicator_UntilDone()
	{
		_repository.Gate = new TaskCompletionSource();
		var viewModel = CreateViewModel();

		var loading = viewModel.LoadTodosCommand.ExecuteAsync(null);
		Assert.True(viewModel.IsBusy);

		_repository.Gate.SetResult();
		await loading;
		Assert.False(viewModel.IsBusy);
	}

	[Fact]
	public async Task Load_Twice_ReplacesListInsteadOfAppending()
	{
		_repository.Seed(new TodoItem { Title = "One" });
		_repository.Seed(new TodoItem { Title = "Two" });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["One", "Two"], viewModel.Todos.Select(t => t.Title));
	}

	[Fact]
	public async Task Load_WhileLoading_IsIgnored()
	{
		_repository.Gate = new TaskCompletionSource();
		var viewModel = CreateViewModel();
		var first = viewModel.LoadTodosCommand.ExecuteAsync(null);

		// Changing the sort order asks for a reload while the first one is still running
		viewModel.SortOrder = TodoSortOrder.DueDate;
		_repository.Gate.SetResult();
		await first;

		Assert.Equal(1, _repository.GetAllCallCount);
		Assert.False(viewModel.IsBusy);
	}

	[Fact]
	public async Task CategoryAddedElsewhere_AppearsInFilter_OnNextLoad()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		// Same order as the real repository: sorted by name
		_repository.Categories.Insert(0, new Category { Id = 4, Name = "Hobbies" });
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["All categories", "Hobbies", "Personal", "Work", "Shopping"], viewModel.Filters.Select(c => c.Name));
	}

	[Fact]
	public async Task CategoryDeletedElsewhere_DisappearsFromFilter_OnNextLoad()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		_repository.Categories.RemoveAll(c => c.Name == "Work");
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["All categories", "Personal", "Shopping"], viewModel.Filters.Select(c => c.Name));
	}

	[Fact]
	public async Task Reload_KeepsSameCategoryObjects_SoPickerKeepsSelection()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		var work = viewModel.Filters.Single(c => c.Name == "Work");
		viewModel.SelectedFilter = work;
		await viewModel.LoadTodosCommand.ExecutionTask!;

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Same(work, viewModel.SelectedFilter);
		Assert.Same(work, viewModel.Filters.Single(c => c.Name == "Work"));
		Assert.Equal(2, _repository.LastCategoryId);
	}

	[Fact]
	public async Task ChosenCategoryDeletedElsewhere_FilterResetsToAll()
	{
		_repository.Seed(new TodoItem { Title = "Finish report", CategoryId = 2 });
		_repository.Seed(new TodoItem { Title = "Call mum", CategoryId = 1 });
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Work");
		await viewModel.LoadTodosCommand.ExecutionTask!;

		await _repository.DeleteCategoryAsync(2);
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Same(TodoListViewModel.AllCategories, viewModel.SelectedFilter);
		Assert.Null(_repository.LastCategoryId);
		Assert.Equal(2, viewModel.Todos.Count);
		Assert.Equal("No to-dos yet.", viewModel.EmptyMessage);
	}

	[Fact]
	public async Task PickerReportingNoSelection_FallsBackToAllCategories()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		viewModel.SelectedFilter = null!;

		Assert.Same(TodoListViewModel.AllCategories, viewModel.SelectedFilter);
	}

	// Acts like the MAUI Picker: when the list changes, it keeps the selected position, not the item,
	// and sets the item now at that position back into the ViewModel.
	private static void ShiftSelectionLikePicker(TodoListViewModel viewModel)
	{
		var index = viewModel.Filters.IndexOf(viewModel.SelectedFilter);
		viewModel.Filters.CollectionChanged += (_, _) =>
		{
			var shifted = viewModel.Filters[Math.Min(index, viewModel.Filters.Count - 1)];
			if (shifted != viewModel.SelectedFilter)
			{
				viewModel.SelectedFilter = shifted;
			}
		};
	}

	[Fact]
	public async Task CategoryAddedBeforeChosenFilter_KeepsChosenFilter()
	{
		_repository.Seed(new TodoItem { Title = "Call mum", CategoryId = 1 });
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Personal");
		await viewModel.LoadTodosCommand.ExecutionTask!;
		ShiftSelectionLikePicker(viewModel);

		_repository.Categories.Insert(0, new Category { Id = 4, Name = "Hobbies" });
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal("Personal", viewModel.SelectedFilter.Name);
		Assert.Equal(1, _repository.LastCategoryId);
		Assert.Equal(["Call mum"], viewModel.Todos.Select(t => t.Title));
	}

	[Fact]
	public async Task CategoryDeletedBeforeChosenFilter_KeepsChosenFilter()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Shopping");
		await viewModel.LoadTodosCommand.ExecutionTask!;
		ShiftSelectionLikePicker(viewModel);

		_repository.Categories.RemoveAll(c => c.Name == "Personal");
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal("Shopping", viewModel.SelectedFilter.Name);
		Assert.Equal(3, _repository.LastCategoryId);
	}

	[Fact]
	public async Task Load_SplitsTodosIntoOpenAndDone_KeepingOrder()
	{
		_repository.Seed(new TodoItem { Title = "Write report", Priority = 5 });
		_repository.Seed(new TodoItem { Title = "Old task", Priority = 4, IsDone = true });
		_repository.Seed(new TodoItem { Title = "Buy milk", Priority = 2 });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["Write report", "Buy milk"], viewModel.OpenTodos.Select(t => t.Title));
		Assert.Equal(["Old task"], viewModel.DoneTodos.Select(t => t.Title));
		Assert.True(viewModel.HasOpenTodos);
		Assert.True(viewModel.HasDoneTodos);
		Assert.False(viewModel.IsEmpty);
	}

	[Fact]
	public async Task Load_WithNothingDone_HidesDoneSection()
	{
		_repository.Seed(new TodoItem { Title = "Write report" });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.False(viewModel.HasDoneTodos);
		Assert.Empty(viewModel.DoneTodos);
	}

	[Fact]
	public async Task Load_WithNoTodos_ShowsEmptyState()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.True(viewModel.IsEmpty);
		Assert.False(viewModel.HasOpenTodos);
		Assert.False(viewModel.HasDoneTodos);
	}

	[Fact]
	public async Task ToggleDone_MovesTodoToDoneSection()
	{
		var item = _repository.Seed(new TodoItem { Title = "Write report" });
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		await viewModel.ToggleDoneCommand.ExecuteAsync(viewModel.OpenTodos.Single());

		Assert.Empty(viewModel.OpenTodos);
		Assert.Equal(item.Id, Assert.Single(viewModel.DoneTodos).Id);
	}

	[Fact]
	public async Task Load_Twice_DoesNotDuplicateSections()
	{
		_repository.Seed(new TodoItem { Title = "Write report" });
		_repository.Seed(new TodoItem { Title = "Old task", IsDone = true });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Single(viewModel.OpenTodos);
		Assert.Single(viewModel.DoneTodos);
	}

	[Fact]
	public async Task EmptyHint_DependsOnChosenCategory()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		Assert.Equal("Add your first to-do with the Add to-do button.", viewModel.EmptyHint);

		viewModel.SelectedFilter = viewModel.Filters.Single(c => c.Name == "Work");

		Assert.Equal("Pick another category, or add a to-do to this one.", viewModel.EmptyHint);
	}
}
