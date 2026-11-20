using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// In-memory ITodoRepository for ViewModel tests. Records what was added and updated.
/// </summary>
public class FakeTodoRepository : ITodoRepository
{
	private int _nextId = 1;

	public List<TodoItem> Items { get; } = [];

	public List<TodoItem> Added { get; } = [];

	public List<TodoItem> Updated { get; } = [];

	/// <summary>Same categories as the database seed.</summary>
	public List<Category> Categories { get; } =
	[
		new() { Id = 1, Name = "Personal" },
		new() { Id = 2, Name = "Work" },
		new() { Id = 3, Name = "Shopping" }
	];

	public int CategoriesCallCount { get; private set; }

	public TodoSortOrder? LastSortOrder { get; private set; }

	public int? LastCategoryId { get; private set; }

	public TodoItem Seed(TodoItem item)
	{
		item.Id = _nextId++;
		Items.Add(item);
		return item;
	}

	/// <summary>Set to hold GetAllAsync open until the test completes it, to check what the UI shows while loading.</summary>
	public TaskCompletionSource? Gate { get; set; }

	public int GetAllCallCount { get; private set; }

	public async Task<List<TodoItem>> GetAllAsync(TodoSortOrder sortOrder, int? categoryId = null)
	{
		GetAllCallCount++;
		LastSortOrder = sortOrder;
		LastCategoryId = categoryId;

		if (Gate is not null)
		{
			await Gate.Task;
		}

		return Items
			.Where(t => categoryId is null || t.CategoryId == categoryId)
			.Select(Copy)
			.ToList();
	}

	public int? LastTopCount { get; private set; }

	public Task<List<TodoItem>> GetTopAsync(int count)
	{
		LastTopCount = count;
		return Task.FromResult(Items.Take(count).Select(Copy).ToList());
	}

	public Task<TodoItem?> GetByIdAsync(int id)
	{
		var item = Items.FirstOrDefault(t => t.Id == id);
		return Task.FromResult(item is null ? null : Copy(item));
	}

	public Task AddAsync(TodoItem item)
	{
		Seed(item);
		Added.Add(item);
		return Task.CompletedTask;
	}

	public Task UpdateAsync(TodoItem item)
	{
		Items.RemoveAll(t => t.Id == item.Id);
		Items.Add(item);
		Updated.Add(item);
		return Task.CompletedTask;
	}

	public List<int> Deleted { get; } = [];

	public Task DeleteAsync(int id)
	{
		Deleted.Add(id);
		Items.RemoveAll(t => t.Id == id);
		return Task.CompletedTask;
	}

	public Task<List<Category>> GetCategoriesAsync()
	{
		CategoriesCallCount++;
		return Task.FromResult(Categories.ToList());
	}

	public List<int> DeletedCategories { get; } = [];

	public Task<bool> CategoryExistsAsync(string name)
	{
		return Task.FromResult(Categories.Any(c =>
			string.Equals(c.Name, name.Trim(), StringComparison.CurrentCultureIgnoreCase)));
	}

	public Task<Category> AddCategoryAsync(string name)
	{
		var category = new Category { Id = Categories.Max(c => c.Id) + 1, Name = name.Trim() };
		Categories.Add(category);
		return Task.FromResult(category);
	}

	public Task DeleteCategoryAsync(int id)
	{
		DeletedCategories.Add(id);
		Categories.RemoveAll(c => c.Id == id);
		foreach (var item in Items.Where(t => t.CategoryId == id))
		{
			item.CategoryId = null;
		}

		return Task.CompletedTask;
	}

	// The real repository returns a new object for every read, so the fake does too.
	private static TodoItem Copy(TodoItem item) => new()
	{
		Id = item.Id,
		Title = item.Title,
		Notes = item.Notes,
		Priority = item.Priority,
		DueDate = item.DueDate,
		IsDone = item.IsDone,
		CategoryId = item.CategoryId
	};
}
