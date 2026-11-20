using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface ITodoRepository
{
	Task<List<TodoItem>> GetAllAsync(TodoSortOrder sortOrder, int? categoryId = null);

	Task<List<TodoItem>> GetTopAsync(int count);

	Task<TodoItem?> GetByIdAsync(int id);

	Task AddAsync(TodoItem item);

	Task UpdateAsync(TodoItem item);

	Task DeleteAsync(int id);
}
