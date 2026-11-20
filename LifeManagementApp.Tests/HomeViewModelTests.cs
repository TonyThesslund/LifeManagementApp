using LifeManagementApp.Models;
using LifeManagementApp.Tests.Fakes;
using LifeManagementApp.ViewModels;
using LifeManagementApp.Views;

namespace LifeManagementApp.Tests;

public class HomeViewModelTests
{
	private readonly FakeTodoRepository _repository = new();
	private readonly FakeNavigationService _navigation = new();

	private HomeViewModel CreateViewModel() => new(_repository, _navigation);

	[Fact]
	public async Task LoadTodos_AsksRepositoryForTopThree()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(3, _repository.LastTopCount);
	}

	[Fact]
	public async Task LoadTodos_ShowsWhatRepositoryReturns_InSameOrder()
	{
		_repository.Seed(new TodoItem { Title = "First" });
		_repository.Seed(new TodoItem { Title = "Second" });
		_repository.Seed(new TodoItem { Title = "Third" });
		_repository.Seed(new TodoItem { Title = "Fourth" });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["First", "Second", "Third"], viewModel.TopTodos.Select(t => t.Title).ToArray());
	}

	[Fact]
	public async Task LoadTodos_Twice_ReplacesListInsteadOfAppending()
	{
		_repository.Seed(new TodoItem { Title = "Only" });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Single(viewModel.TopTodos);
	}

	[Fact]
	public async Task LoadTodos_WithNoTodos_LeavesListEmpty()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Empty(viewModel.TopTodos);
	}

	[Fact]
	public async Task OpenTodo_NavigatesToDetailPage_WithId()
	{
		var item = _repository.Seed(new TodoItem { Title = "Call mum" });
		var viewModel = CreateViewModel();

		await viewModel.OpenTodoCommand.ExecuteAsync(item);

		Assert.Equal([nameof(TodoDetailPage)], _navigation.Routes);
		Assert.Equal(item.Id, _navigation.LastParameters![TodoDetailViewModel.IdParameter]);
	}
}
