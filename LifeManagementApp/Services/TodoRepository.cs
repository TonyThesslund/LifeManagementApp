using LifeManagementApp.Data;
using LifeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Services;

public class TodoRepository(IDbContextFactory<AppDbContext> dbFactory) : ITodoRepository
{
	public async Task<List<TodoItem>> GetAllAsync(TodoSortOrder sortOrder, int? categoryId = null)
	{
		await using var db = await dbFactory.CreateDbContextAsync();

		var query = db.Todos
			.AsNoTracking()
			.Include(t => t.Category)
			.AsQueryable();

		if (categoryId is not null)
		{
			query = query.Where(t => t.CategoryId == categoryId);
		}

		// Unfinished to-dos first (false sorts before true), then the chosen order within each group
		var byDone = query.OrderBy(t => t.IsDone);

		query = sortOrder switch
		{
			TodoSortOrder.DueDate => byDone
				.ThenBy(t => t.DueDate)
				.ThenByDescending(t => t.Priority),
			_ => byDone
				.ThenByDescending(t => t.Priority)
				.ThenBy(t => t.DueDate)
		};

		return await query.ToListAsync();
	}

	public async Task<List<TodoItem>> GetTopAsync(int count)
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		return await db.Todos
			.AsNoTracking()
			.Include(t => t.Category)
			.Where(t => !t.IsDone)
			.OrderByDescending(t => t.Priority)
			.ThenBy(t => t.DueDate)
			.Take(count)
			.ToListAsync();
	}

	public async Task<TodoItem?> GetByIdAsync(int id)
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		return await db.Todos
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == id);
	}

	public async Task AddAsync(TodoItem item)
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		db.Todos.Add(item);
		await db.SaveChangesAsync();
	}

	public async Task UpdateAsync(TodoItem item)
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		db.Entry(item).State = EntityState.Modified;
		await db.SaveChangesAsync();
	}

	public async Task DeleteAsync(int id)
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		await db.Todos
			.Where(t => t.Id == id)
			.ExecuteDeleteAsync();
	}

	public async Task<List<Category>> GetCategoriesAsync()
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		return await db.Categories
			.AsNoTracking()
			.OrderBy(c => c.Name)
			.ToListAsync();
	}

	public async Task<bool> CategoryExistsAsync(string name)
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		return await CategoryExistsAsync(db, name);
	}

	public async Task<Category> AddCategoryAsync(string name)
	{
		var trimmed = name?.Trim() ?? string.Empty;
		if (trimmed.Length == 0 || trimmed.Length > Category.MaxNameLength)
		{
			throw new ArgumentException($"A category name must be 1-{Category.MaxNameLength} characters.", nameof(name));
		}

		await using var db = await dbFactory.CreateDbContextAsync();
		if (await CategoryExistsAsync(db, trimmed))
		{
			throw new InvalidOperationException($"A category called \"{trimmed}\" already exists.");
		}

		var category = new Category { Name = trimmed };
		db.Categories.Add(category);
		await db.SaveChangesAsync();
		return category;
	}

	public async Task DeleteCategoryAsync(int id)
	{
		await using var db = await dbFactory.CreateDbContextAsync();

		// The database's ON DELETE SET NULL rule keeps the category's to-dos, without a category
		await db.Categories
			.Where(c => c.Id == id)
			.ExecuteDeleteAsync();
	}

	// Compared in C#, not SQL: SQLite's lower() only handles A-Z, so "Äiti" and "äiti" would count as different.
	// There are only a few categories, so loading their names is cheap.
	private static async Task<bool> CategoryExistsAsync(AppDbContext db, string name)
	{
		var trimmed = name.Trim();
		var names = await db.Categories.Select(c => c.Name).ToListAsync();
		return names.Any(n => string.Equals(n, trimmed, StringComparison.CurrentCultureIgnoreCase));
	}
}
