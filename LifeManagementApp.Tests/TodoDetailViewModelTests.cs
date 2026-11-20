using LifeManagementApp.Models;
using LifeManagementApp.Tests.Fakes;
using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Tests;

public class TodoDetailViewModelTests
{
	private readonly FakeTodoRepository _repository = new();
	private readonly FakeNavigationService _navigation = new();
	private readonly FakeDialogService _dialogs = new();
	private readonly TodoDetailViewModel _viewModel;

	public TodoDetailViewModelTests()
	{
		_viewModel = new TodoDetailViewModel(_repository, _navigation, _dialogs);
	}

	// Opens the ViewModel the same way Shell does, then waits for the load to finish.
	private async Task OpenWithIdAsync(int id)
	{
		_viewModel.ApplyQueryAttributes(new Dictionary<string, object>
		{
			[TodoDetailViewModel.IdParameter] = id
		});
		await _viewModel.LoadCommand.ExecutionTask!;
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void IsTitleValid_ReturnsFalse_ForMissingTitle(string? title)
	{
		Assert.False(TodoDetailViewModel.IsTitleValid(title));
	}

	[Fact]
	public void IsTitleValid_ReturnsTrue_ForNormalTitle()
	{
		Assert.True(TodoDetailViewModel.IsTitleValid("Buy milk"));
	}

	[Fact]
	public void NewForm_CannotSave_AndShowsNoError()
	{
		Assert.False(_viewModel.SaveCommand.CanExecute(null));
		Assert.False(_viewModel.HasTitleError);
		Assert.Equal("New to-do", _viewModel.PageTitle);
	}

	[Fact]
	public void TitleWithOnlySpaces_CannotSave_AndShowsError()
	{
		_viewModel.Title = "   ";

		Assert.False(_viewModel.SaveCommand.CanExecute(null));
		Assert.True(_viewModel.HasTitleError);
		Assert.Equal("Title is required.", _viewModel.TitleError);
	}

	[Fact]
	public void ValidTitle_CanSave_AndClearsError()
	{
		_viewModel.Title = "   ";
		_viewModel.Title = "Buy milk";

		Assert.True(_viewModel.SaveCommand.CanExecute(null));
		Assert.False(_viewModel.HasTitleError);
		Assert.Null(_viewModel.TitleError);
	}

	[Fact]
	public async Task Save_WithEmptyTitle_DoesNothing()
	{
		await _viewModel.SaveCommand.ExecuteAsync(null);

		Assert.Empty(_repository.Added);
		Assert.Equal(0, _navigation.BackCount);
	}

	[Fact]
	public async Task Save_NewTodo_AddsTrimmedTodo_AndNavigatesBack()
	{
		_viewModel.Title = "  Buy milk  ";
		_viewModel.Notes = "Oat";
		_viewModel.Priority = 5;
		_viewModel.DueDate = DateTime.Today.AddDays(2);

		await _viewModel.SaveCommand.ExecuteAsync(null);

		var added = Assert.Single(_repository.Added);
		Assert.Equal("Buy milk", added.Title);
		Assert.Equal("Oat", added.Notes);
		Assert.Equal(5, added.Priority);
		Assert.Equal(DateTime.Today.AddDays(2), added.DueDate);
		Assert.Empty(_repository.Updated);
		Assert.Equal(1, _navigation.BackCount);
	}

	[Fact]
	public async Task OpenWithId_LoadsTodo_IntoForm()
	{
		var existing = _repository.Seed(new TodoItem
		{
			Title = "Call mum",
			Notes = "Sunday",
			Priority = 4,
			DueDate = DateTime.Today.AddDays(1)
		});

		await OpenWithIdAsync(existing.Id);

		Assert.Equal("Edit to-do", _viewModel.PageTitle);
		Assert.Equal("Call mum", _viewModel.Title);
		Assert.Equal("Sunday", _viewModel.Notes);
		Assert.Equal(4, _viewModel.Priority);
		Assert.Equal(DateTime.Today.AddDays(1), _viewModel.DueDate);
		Assert.True(_viewModel.SaveCommand.CanExecute(null));
	}

	[Fact]
	public async Task Save_EditedTodo_Updates_AndKeepsFieldsNotOnForm()
	{
		var existing = _repository.Seed(new TodoItem
		{
			Title = "Call mum",
			Priority = 3,
			IsDone = true,
			CategoryId = 1
		});
		await OpenWithIdAsync(existing.Id);

		_viewModel.Title = "Call dad";
		_viewModel.Priority = 5;
		await _viewModel.SaveCommand.ExecuteAsync(null);

		var updated = Assert.Single(_repository.Updated);
		Assert.Equal(existing.Id, updated.Id);
		Assert.Equal("Call dad", updated.Title);
		Assert.Equal(5, updated.Priority);
		Assert.True(updated.IsDone);
		Assert.Equal(1, updated.CategoryId);
		Assert.Empty(_repository.Added);
		Assert.Equal(1, _navigation.BackCount);
	}

	[Fact]
	public async Task OpenWithMissingId_ShowsAlert_AndNavigatesBack()
	{
		await OpenWithIdAsync(999);

		var alert = Assert.Single(_dialogs.Alerts);
		Assert.Equal("Not found", alert.Title);
		Assert.Equal(1, _navigation.BackCount);
		Assert.Equal("New to-do", _viewModel.PageTitle);
	}

	[Fact]
	public async Task LoadCategories_GivesNoCategoryFirst_ThenDatabaseCategories()
	{
		await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);

		Assert.Equal(["No category", "Personal", "Work", "Shopping"], _viewModel.Categories.Select(c => c.Name));
		Assert.Same(TodoDetailViewModel.NoCategory, _viewModel.SelectedCategory);
	}

	[Fact]
	public async Task LoadCategories_Twice_LoadsOnlyOnce()
	{
		await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);
		await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);

		Assert.Equal(1, _repository.CategoriesCallCount);
		Assert.Equal(4, _viewModel.Categories.Count);
	}

	[Fact]
	public async Task SaveNew_WithCategory_StoresCategoryId()
	{
		await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);
		_viewModel.Title = "Buy milk";
		_viewModel.SelectedCategory = _viewModel.Categories.Single(c => c.Name == "Shopping");

		await _viewModel.SaveCommand.ExecuteAsync(null);

		Assert.Equal(3, Assert.Single(_repository.Added).CategoryId);
	}

	[Fact]
	public async Task SaveNew_WithNoCategory_StoresNull()
	{
		await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);
		_viewModel.Title = "Buy milk";

		await _viewModel.SaveCommand.ExecuteAsync(null);

		Assert.Null(Assert.Single(_repository.Added).CategoryId);
	}

	[Fact]
	public async Task OpenExisting_PreselectsItsCategory()
	{
		var existing = _repository.Seed(new TodoItem { Title = "Finish report", CategoryId = 2 });

		await OpenWithIdAsync(existing.Id);

		Assert.Equal("Work", _viewModel.SelectedCategory.Name);
	}

	[Fact]
	public async Task OpenExisting_WithoutCategory_PreselectsNoCategory()
	{
		var existing = _repository.Seed(new TodoItem { Title = "Book dentist", CategoryId = null });

		await OpenWithIdAsync(existing.Id);

		Assert.Same(TodoDetailViewModel.NoCategory, _viewModel.SelectedCategory);
	}

	[Fact]
	public async Task OpenExisting_WhilePageAlsoLoadsCategories_LoadsThemOnce()
	{
		var existing = _repository.Seed(new TodoItem { Title = "Finish report", CategoryId = 2 });

		_viewModel.LoadCategoriesCommand.Execute(null);
		await OpenWithIdAsync(existing.Id);

		Assert.Equal(1, _repository.CategoriesCallCount);
		Assert.Equal(4, _viewModel.Categories.Count);
		Assert.Equal("Work", _viewModel.SelectedCategory.Name);
	}

	[Theory]
	[InlineData("Shopping", 3)]
	[InlineData("No category", null)]
	public async Task SaveExisting_ChangedCategory_IsStored(string chosen, int? expectedId)
	{
		var existing = _repository.Seed(new TodoItem { Title = "Finish report", CategoryId = 2 });
		await OpenWithIdAsync(existing.Id);

		_viewModel.SelectedCategory = _viewModel.Categories.Single(c => c.Name == chosen);
		await _viewModel.SaveCommand.ExecuteAsync(null);

		Assert.Equal(expectedId, Assert.Single(_repository.Updated).CategoryId);
	}

	[Fact]
	public async Task Cancel_GoesBackWithoutSaving()
	{
		_viewModel.Title = "Buy milk";

		await _viewModel.CancelCommand.ExecuteAsync(null);

		Assert.Empty(_repository.Added);
		Assert.Empty(_repository.Updated);
		Assert.Equal(1, _navigation.BackCount);
	}
}
