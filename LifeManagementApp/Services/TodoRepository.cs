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

		query = sortOrder switch
		{
			TodoSortOrder.DueDate => query
				.OrderBy(t => t.DueDate)
				.ThenByDescending(t => t.Priority),
			_ => query
				.OrderByDescending(t => t.Priority)
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
}
