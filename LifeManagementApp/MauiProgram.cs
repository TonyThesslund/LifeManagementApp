using LifeManagementApp.Data;
using LifeManagementApp.Services;
using LifeManagementApp.ViewModels;
using LifeManagementApp.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeManagementApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lifemanagement.db");
		builder.Services.AddDbContextFactory<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		builder.Services.AddSingleton<ITodoRepository, TodoRepository>();

		builder.Services.AddSingleton<HomeViewModel>();
		builder.Services.AddSingleton<HomePage>();

		builder.Services.AddSingleton<TodoListViewModel>();
		builder.Services.AddSingleton<TodoListPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		CreateDatabase(app.Services);

		return app;
	}

	private static void CreateDatabase(IServiceProvider services)
	{
		var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
		using var db = factory.CreateDbContext();
		db.Database.EnsureCreated();
	}
}
