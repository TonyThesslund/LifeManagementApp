using LifeManagementApp.Data;
using LifeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Services;

public class TodoRepository(IDbContextFactory<AppDbContext> dbFactory) : ITodoRepository
{
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
}
