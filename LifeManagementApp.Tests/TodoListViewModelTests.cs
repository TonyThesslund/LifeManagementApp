using LifeManagementApp.Models;
using LifeManagementApp.Tests.Fakes;
using LifeManagementApp.ViewModels;

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
}
