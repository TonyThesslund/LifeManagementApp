using LifeManagementApp.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Tests;

/// <summary>
/// Creates AppDbContexts that share one in-memory SQLite database.
/// The database lives as long as the connection is open, so each test gets a fresh, empty one.
/// </summary>
public sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>, IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<AppDbContext> _options;

	public TestDbContextFactory()
	{
		_connection = new SqliteConnection("Data Source=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(_connection)
			.Options;

		using var db = CreateDbContext();
		db.Database.EnsureCreated();
	}

	public AppDbContext CreateDbContext()
	{
		return new AppDbContext(_options);
	}

	public void Dispose()
	{
		_connection.Dispose();
	}
}
