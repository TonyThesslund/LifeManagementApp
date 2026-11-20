using LifeManagementApp.Models;

namespace LifeManagementApp.Tests;

public class TodoItemTests
{
	[Fact]
	public void NewTodoItem_HasDefaultValues()
	{
		var item = new TodoItem();

		Assert.Equal(string.Empty, item.Title);
		Assert.Equal(3, item.Priority);
		Assert.Equal(DateTime.Today, item.DueDate);
		Assert.False(item.IsDone);
		Assert.Null(item.CategoryId);
	}
}
