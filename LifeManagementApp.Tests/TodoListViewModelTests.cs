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
	public async Task Load_Twice_LoadsCategoriesOnce()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(1, _repository.CategoriesCallCount);
		Assert.Equal(4, viewModel.Filters.Count);
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
}
