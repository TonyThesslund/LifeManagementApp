using LifeManagementApp.Data;
using LifeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Tests;

/// <summary>
/// Tests the rules set up with Code First in <see cref="AppDbContext"/>, against a real (in-memory) SQLite database.
/// </summary>
public sealed class AppDbContextTests : IDisposable
{
	private readonly TestDbContextFactory _factory = new();

	public void Dispose()
	{
		_factory.Dispose();
	}

	private async Task<int> AddTodoAsync(string title, int? categoryId)
	{
		await using var db = _factory.CreateDbContext();
		var item = new TodoItem { Title = title, CategoryId = categoryId };
		db.Todos.Add(item);
		await db.SaveChangesAsync();
		return item.Id;
	}

	[Fact]
	public async Task Database_HasSeededCategories()
	{
		await using var db = _factory.CreateDbContext();

		var categories = await db.Categories.OrderBy(c => c.Id).Select(c => new { c.Id, c.Name }).ToListAsync();

		Assert.Equal(
			[new { Id = 1, Name = "Personal" }, new { Id = 2, Name = "Work" }, new { Id = 3, Name = "Shopping" }],
			categories);
	}

	[Fact]
	public async Task TodoWithoutTitle_CannotBeSaved()
	{
		await using var db = _factory.CreateDbContext();
		db.Todos.Add(new TodoItem { Title = null! });

		await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
	}

	[Fact]
	public async Task CategoryWithoutName_CannotBeSaved()
	{
		await using var db = _factory.CreateDbContext();
		db.Categories.Add(new Category { Name = null! });

		await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
	}

	// SQLite doesn't enforce text length, so the limits are checked in the EF Core model instead of by saving long text.
	[Theory]
	[InlineData(typeof(TodoItem), nameof(TodoItem.Title), 200)]
	[InlineData(typeof(TodoItem), nameof(TodoItem.Notes), 1000)]
	[InlineData(typeof(Category), nameof(Category.Name), 100)]
	public void MaxLengths_AreConfigured(Type entity, string property, int expected)
	{
		using var db = _factory.CreateDbContext();

		var maxLength = db.Model.FindEntityType(entity)!.FindProperty(property)!.GetMaxLength();

		Assert.Equal(expected, maxLength);
	}

	[Theory]
	[InlineData(nameof(TodoItem.Title), false)]
	[InlineData(nameof(TodoItem.Notes), true)]
	[InlineData(nameof(TodoItem.CategoryId), true)]
	public void TodoColumns_HaveExpectedNullability(string property, bool nullable)
	{
		using var db = _factory.CreateDbContext();

		var isNullable = db.Model.FindEntityType(typeof(TodoItem))!.FindProperty(property)!.IsNullable;

		Assert.Equal(nullable, isNullable);
	}

	[Fact]
	public async Task DeletingCategory_KeepsItsTodos_WithoutCategory()
	{
		var todoId = await AddTodoAsync("Finish report", categoryId: 2);

		// Delete the category in a separate context where the to-do isn't loaded,
		// so it's the database's ON DELETE SET NULL rule that updates the to-do.
		await using (var db = _factory.CreateDbContext())
		{
			await db.Categories.Where(c => c.Id == 2).ExecuteDeleteAsync();
		}

		await using var check = _factory.CreateDbContext();
		var todo = await check.Todos.SingleAsync(t => t.Id == todoId);
		Assert.Null(todo.CategoryId);
	}

	[Fact]
	public async Task DeletingCategory_WithTodosLoaded_ClearsTheirCategory()
	{
		var todoId = await AddTodoAsync("Finish report", categoryId: 2);

		await using (var db = _factory.CreateDbContext())
		{
			var work = await db.Categories.Include(c => c.Todos).SingleAsync(c => c.Id == 2);
			db.Categories.Remove(work);
			await db.SaveChangesAsync();
		}

		await using var check = _factory.CreateDbContext();
		Assert.Null((await check.Todos.SingleAsync(t => t.Id == todoId)).CategoryId);
	}

	[Fact]
	public async Task TodoWithUnknownCategory_CannotBeSaved()
	{
		await Assert.ThrowsAsync<DbUpdateException>(() => AddTodoAsync("Orphan", categoryId: 99));
	}

	[Fact]
	public async Task Relationship_LoadsBothWays()
	{
		await AddTodoAsync("Buy milk", categoryId: 3);
		await AddTodoAsync("Buy bread", categoryId: 3);

		await using var db = _factory.CreateDbContext();
		var todo = await db.Todos.Include(t => t.Category).SingleAsync(t => t.Title == "Buy milk");
		var shopping = await db.Categories.Include(c => c.Todos).SingleAsync(c => c.Id == 3);

		Assert.Equal("Shopping", todo.Category!.Name);
		Assert.Equal(["Buy bread", "Buy milk"], shopping.Todos.Select(t => t.Title).Order());
	}
}
