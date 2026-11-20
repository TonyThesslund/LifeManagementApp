using LifeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<TodoItem> Todos => Set<TodoItem>();

	public DbSet<Category> Categories => Set<Category>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TodoItem>(todo =>
		{
			todo.Property(t => t.Title)
				.IsRequired()
				.HasMaxLength(200);

			todo.Property(t => t.Notes)
				.HasMaxLength(1000);
		});

		modelBuilder.Entity<Category>(category =>
		{
			category.Property(c => c.Name)
				.IsRequired()
				.HasMaxLength(100);

			category.HasMany(c => c.Todos)
				.WithOne(t => t.Category)
				.HasForeignKey(t => t.CategoryId)
				.OnDelete(DeleteBehavior.SetNull);

			category.HasData(
				new Category { Id = 1, Name = "Personal" },
				new Category { Id = 2, Name = "Work" },
				new Category { Id = 3, Name = "Shopping" });
		});
	}
}
