using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface ITodoRepository
{
	Task<TodoItem?> GetByIdAsync(int id);

	Task AddAsync(TodoItem item);

	Task UpdateAsync(TodoItem item);

	Task DeleteAsync(int id);
}
