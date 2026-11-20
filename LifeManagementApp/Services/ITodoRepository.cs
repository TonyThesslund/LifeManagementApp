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

	Task<List<Category>> GetCategoriesAsync();

	/// <summary>True if a category with this name exists, ignoring upper/lower case and surrounding spaces.</summary>
	Task<bool> CategoryExistsAsync(string name);

	/// <summary>Adds a category with the trimmed name.</summary>
	/// <exception cref="ArgumentException">The name is empty or longer than <see cref="Category.MaxNameLength"/>.</exception>
	/// <exception cref="InvalidOperationException">A category with this name already exists.</exception>
	Task<Category> AddCategoryAsync(string name);

	/// <summary>Deletes a category. Its to-dos are kept, without a category.</summary>
	Task DeleteCategoryAsync(int id);
}
