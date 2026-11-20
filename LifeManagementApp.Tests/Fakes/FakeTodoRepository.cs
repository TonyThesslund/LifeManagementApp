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

	public TodoItem Seed(TodoItem item)
	{
		item.Id = _nextId++;
		Items.Add(item);
		return item;
	}

	public Task<List<TodoItem>> GetAllAsync(TodoSortOrder sortOrder, int? categoryId = null)
	{
		return Task.FromResult(Items.Select(Copy).ToList());
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
		return Task.FromResult(new List<Category>());
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
