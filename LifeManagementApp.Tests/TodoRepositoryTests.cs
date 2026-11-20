using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests;

public sealed class TodoRepositoryTests : IDisposable
{
	private const int PersonalId = 1;
	private const int WorkId = 2;
	private const int ShoppingId = 3;

	private readonly TestDbContextFactory _factory = new();
	private readonly TodoRepository _repository;

	public TodoRepositoryTests()
	{
		_repository = new TodoRepository(_factory);
	}

	public void Dispose()
	{
		_factory.Dispose();
	}

	private static DateTime Day(int offset) => DateTime.Today.AddDays(offset);

	private async Task<TodoItem> AddAsync(string title, int priority, int dueInDays, bool isDone = false, int? categoryId = null)
	{
		var item = new TodoItem
		{
			Title = title,
			Priority = priority,
			DueDate = Day(dueInDays),
			IsDone = isDone,
			CategoryId = categoryId
		};
		await _repository.AddAsync(item);
		return item;
	}

	private static string[] Titles(IEnumerable<TodoItem> items) => items.Select(t => t.Title).ToArray();

	[Fact]
	public async Task AddAsync_AssignsId_AndCanBeReadBack()
	{
		var item = await AddAsync("Buy milk", 4, 1, categoryId: ShoppingId);

		Assert.True(item.Id > 0);

		var loaded = await _repository.GetByIdAsync(item.Id);
		Assert.NotNull(loaded);
		Assert.Equal("Buy milk", loaded.Title);
		Assert.Equal(4, loaded.Priority);
		Assert.Equal(Day(1), loaded.DueDate);
		Assert.Equal(ShoppingId, loaded.CategoryId);
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsNull_WhenIdDoesNotExist()
	{
		var loaded = await _repository.GetByIdAsync(999);

		Assert.Null(loaded);
	}

	[Fact]
	public async Task UpdateAsync_SavesChanges()
	{
		var item = await AddAsync("Buy milk", 2, 1);
		var loaded = await _repository.GetByIdAsync(item.Id);

		loaded!.Title = "Buy oat milk";
		loaded.IsDone = true;
		await _repository.UpdateAsync(loaded);

		var updated = await _repository.GetByIdAsync(item.Id);
		Assert.Equal("Buy oat milk", updated!.Title);
		Assert.True(updated.IsDone);
	}

	[Fact]
	public async Task DeleteAsync_RemovesTodo()
	{
		var item = await AddAsync("Buy milk", 2, 1);

		await _repository.DeleteAsync(item.Id);

		Assert.Null(await _repository.GetByIdAsync(item.Id));
	}

	[Fact]
	public async Task GetTopAsync_ReturnsUnfinished_ByPriorityThenDueDate()
	{
		await AddAsync("Low", 1, 0);
		await AddAsync("High later", 5, 3);
		await AddAsync("High sooner", 5, 1);
		await AddAsync("Medium", 3, 0);
		await AddAsync("Highest but done", 5, 0, isDone: true);

		var top = await _repository.GetTopAsync(3);

		Assert.Equal(["High sooner", "High later", "Medium"], Titles(top));
	}

	[Fact]
	public async Task GetTopAsync_ReturnsAllUnfinished_WhenFewerThanCount()
	{
		await AddAsync("One", 2, 0);
		await AddAsync("Done", 5, 0, isDone: true);

		var top = await _repository.GetTopAsync(3);

		Assert.Equal(["One"], Titles(top));
	}

	[Fact]
	public async Task GetAllAsync_ByPriority_SortsHighestFirst_ThenEarliestDueDate()
	{
		await AddAsync("A", 5, 3);
		await AddAsync("B", 5, 1);
		await AddAsync("C", 2, 1);
		await AddAsync("D", 3, 0);

		var all = await _repository.GetAllAsync(TodoSortOrder.Priority);

		Assert.Equal(["B", "A", "D", "C"], Titles(all));
	}

	[Fact]
	public async Task GetAllAsync_ByDueDate_SortsEarliestFirst_ThenHighestPriority()
	{
		await AddAsync("A", 5, 3);
		await AddAsync("B", 5, 1);
		await AddAsync("C", 2, 1);
		await AddAsync("D", 3, 0);

		var all = await _repository.GetAllAsync(TodoSortOrder.DueDate);

		Assert.Equal(["D", "B", "C", "A"], Titles(all));
	}

	[Fact]
	public async Task GetAllAsync_ByPriority_PutsFinishedLast_KeepingOrderWithinEachGroup()
	{
		await AddAsync("Done high", 5, 0, isDone: true);
		await AddAsync("Open low", 1, 0);
		await AddAsync("Done low", 2, 0, isDone: true);
		await AddAsync("Open high", 4, 0);

		var all = await _repository.GetAllAsync(TodoSortOrder.Priority);

		Assert.Equal(["Open high", "Open low", "Done high", "Done low"], Titles(all));
	}

	[Fact]
	public async Task GetAllAsync_ByDueDate_PutsFinishedLast_KeepingOrderWithinEachGroup()
	{
		await AddAsync("Done early", 3, 0, isDone: true);
		await AddAsync("Open late", 3, 5);
		await AddAsync("Done late", 3, 4, isDone: true);
		await AddAsync("Open early", 3, 1);

		var all = await _repository.GetAllAsync(TodoSortOrder.DueDate);

		Assert.Equal(["Open early", "Open late", "Done early", "Done late"], Titles(all));
	}

	[Fact]
	public async Task GetAllAsync_IncludesFinishedTodos_AndLoadsCategory()
	{
		await AddAsync("Done", 1, 0, isDone: true, categoryId: WorkId);

		var all = await _repository.GetAllAsync(TodoSortOrder.Priority);

		var item = Assert.Single(all);
		Assert.Equal("Work", item.Category?.Name);
	}

	[Fact]
	public async Task GetAllAsync_WithCategory_ReturnsOnlyThatCategory()
	{
		await AddAsync("Work 1", 1, 0, categoryId: WorkId);
		await AddAsync("Personal", 5, 0, categoryId: PersonalId);
		await AddAsync("Work 2", 4, 0, categoryId: WorkId);
		await AddAsync("No category", 3, 0);

		var work = await _repository.GetAllAsync(TodoSortOrder.Priority, WorkId);

		Assert.Equal(["Work 2", "Work 1"], Titles(work));
	}

	[Fact]
	public async Task GetAllAsync_WithEmptyCategory_ReturnsEmptyList()
	{
		await AddAsync("Work", 1, 0, categoryId: WorkId);

		var shopping = await _repository.GetAllAsync(TodoSortOrder.Priority, ShoppingId);

		Assert.Empty(shopping);
	}

	[Fact]
	public async Task GetCategoriesAsync_ReturnsSeededCategories_OrderedByName()
	{
		var categories = await _repository.GetCategoriesAsync();

		Assert.Equal(["Personal", "Shopping", "Work"], categories.Select(c => c.Name).ToArray());
	}
}
