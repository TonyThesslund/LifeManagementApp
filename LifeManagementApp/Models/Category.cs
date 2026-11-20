namespace LifeManagementApp.Models;

public class Category
{
	public const int MaxNameLength = 100;

	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public List<TodoItem> Todos { get; set; } = [];
}
