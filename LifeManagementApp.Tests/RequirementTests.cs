using System.Net;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;
using LifeManagementApp.ViewModels;
using Microsoft.Maui.Networking;

namespace LifeManagementApp.Tests;

/// <summary>
/// The assignment's main requirements, each checked through the real parts working together:
/// ViewModels with the real <see cref="TodoRepository"/> on an in-memory SQLite database,
/// the real <see cref="KitesurfAdvisor"/> and settings service. Only navigation, dialogs and the network are faked.
/// </summary>
public sealed class RequirementTests : IDisposable
{
	private readonly TestDbContextFactory _factory = new();
	private readonly TodoRepository _repository;
	private readonly FakeNavigationService _navigation = new();
	private readonly FakeDialogService _dialogs = new();
	private readonly FakeWeatherService _weather = new();
	private readonly PreferencesSettingsService _settings = new(new FakePreferences());

	public RequirementTests()
	{
		_repository = new TodoRepository(_factory);
	}

	public void Dispose()
	{
		_factory.Dispose();
	}

	private HomeViewModel CreateHome(IWeatherService? weather = null) =>
		new(_repository, _navigation, weather ?? _weather, new KitesurfAdvisor(_settings), _settings, TimeProvider.System);

	private TodoListViewModel CreateList() => new(_repository, _navigation, _dialogs);

	private TodoDetailViewModel CreateDetail() => new(_repository, _navigation, _dialogs);

	private async Task<TodoItem> AddAsync(string title, int priority, int dueInDays, bool isDone = false)
	{
		var item = new TodoItem
		{
			Title = title,
			Priority = priority,
			DueDate = DateTime.Today.AddDays(dueInDays),
			IsDone = isDone
		};
		await _repository.AddAsync(item);
		return item;
	}

	private static List<string> Titles(IEnumerable<TodoItem> items) => items.Select(t => t.Title).ToList();

	[Fact]
	public async Task Home_ShowsTopThreeUnfinished_ByPriorityThenEarliestDueDate()
	{
		await AddAsync("Low", 1, 0);
		await AddAsync("High later", 5, 3);
		await AddAsync("High sooner", 5, 1);
		await AddAsync("Done highest", 5, 0, isDone: true);
		await AddAsync("Medium", 3, 0);
		var home = CreateHome();

		await home.LoadAsync();

		Assert.Equal(["High sooner", "High later", "Medium"], Titles(home.TopTodos));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task AddPage_EmptyTitle_IsRejected(string title)
	{
		var detail = CreateDetail();
		detail.Title = title;

		Assert.False(detail.SaveCommand.CanExecute(null));
		await detail.SaveCommand.ExecuteAsync(null);

		Assert.Empty(await _repository.GetAllAsync(TodoSortOrder.Priority));
		Assert.Equal(0, _navigation.BackCount);
	}

	[Fact]
	public void AddPage_TypingOnlySpaces_ShowsTitleError()
	{
		var detail = CreateDetail();
		Assert.False(detail.HasTitleError);

		detail.Title = "   ";

		Assert.True(detail.HasTitleError);
		Assert.Equal("Title is required.", detail.TitleError);
	}

	[Fact]
	public async Task AddPage_WithTitle_SavesTodo_ShownOnList()
	{
		var detail = CreateDetail();
		detail.Title = "Buy milk";
		detail.Priority = 2;
		detail.DueDate = DateTime.Today.AddDays(1);

		await detail.SaveCommand.ExecuteAsync(null);
		var list = CreateList();
		await list.LoadTodosCommand.ExecuteAsync(null);

		var saved = Assert.Single(list.Todos);
		Assert.Equal("Buy milk", saved.Title);
		Assert.Equal(2, saved.Priority);
		Assert.Equal(DateTime.Today.AddDays(1), saved.DueDate);
		Assert.False(saved.IsDone);
		Assert.True(saved.Id > 0);
	}

	[Fact]
	public async Task EditPage_LoadsTodoById_AndSavesChanges()
	{
		var existing = await AddAsync("Call mum", 3, 2);
		var detail = CreateDetail();

		// Same as Shell passing the id parameter
		detail.ApplyQueryAttributes(new Dictionary<string, object> { [TodoDetailViewModel.IdParameter] = existing.Id });
		await detail.LoadCommand.ExecutionTask!;
		Assert.Equal("Call mum", detail.Title);

		detail.Title = "Call dad";
		detail.Priority = 5;
		await detail.SaveCommand.ExecuteAsync(null);

		var saved = await _repository.GetByIdAsync(existing.Id);
		Assert.Equal("Call dad", saved!.Title);
		Assert.Equal(5, saved.Priority);
	}

	[Fact]
	public async Task List_SortsByPriority_AndByDueDate()
	{
		await AddAsync("A", 2, 0);
		await AddAsync("B", 5, 3);
		await AddAsync("C", 3, 1);
		var list = CreateList();

		await list.LoadTodosCommand.ExecuteAsync(null);
		Assert.Equal(["B", "C", "A"], Titles(list.Todos));

		list.SortOrder = TodoSortOrder.DueDate;
		await list.LoadTodosCommand.ExecutionTask!;
		Assert.Equal(["A", "C", "B"], Titles(list.Todos));
	}

	[Fact]
	public async Task MarkingDone_RemovesTodoFromHomeTopThree()
	{
		await AddAsync("First", 5, 0);
		await AddAsync("Second", 4, 0);
		await AddAsync("Third", 3, 0);
		await AddAsync("Fourth", 2, 0);
		var list = CreateList();
		var home = CreateHome();
		await list.LoadTodosCommand.ExecuteAsync(null);

		await list.ToggleDoneCommand.ExecuteAsync(list.Todos.Single(t => t.Title == "First"));
		await home.LoadAsync();

		Assert.Equal(["Second", "Third", "Fourth"], Titles(home.TopTodos));
	}

	[Fact]
	public async Task DeletingFromList_RemovesTodo()
	{
		await AddAsync("Old", 3, 0);
		var list = CreateList();
		await list.LoadTodosCommand.ExecuteAsync(null);

		await list.DeleteTodoCommand.ExecuteAsync(list.Todos.Single());

		Assert.Empty(list.Todos);
		Assert.Empty(await _repository.GetAllAsync(TodoSortOrder.Priority));
	}

	[Theory]
	[InlineData(5.1, true)]
	[InlineData(5.0, false)]
	[InlineData(4.9, false)]
	public async Task Kitesurf_ShownOnlyWhenWindIsOverFiveMetersPerSecond(double windSpeed, bool expected)
	{
		_weather.Result = new WeatherInfo { WindSpeed = windSpeed };
		var home = CreateHome();

		await home.LoadAsync();

		Assert.Equal(expected, home.IsKitesurfWeather);
	}

	[Fact]
	public async Task Home_ShowsTemperatureAndWindInMetersPerSecond_FromOpenMeteo()
	{
		var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """
			{"current":{"time":"2026-11-20T12:00","interval":900,"temperature_2m":-3.2,"wind_speed_10m":7.4}}
			""");
		var weatherService = new OpenMeteoWeatherService(new HttpClient(handler), new FakeConnectivity());
		var home = CreateHome(weatherService);

		await home.LoadAsync();

		Assert.Contains("wind_speed_unit=ms", handler.LastRequest!.RequestUri!.Query);
		Assert.Equal(-3.2, home.Weather!.Temperature);
		Assert.Equal(7.4, home.Weather.WindSpeed);
		Assert.True(home.IsKitesurfWeather);
	}

	[Fact]
	public async Task Home_WithoutInternet_ShowsMessage_AndStillShowsTodos()
	{
		await AddAsync("Call mum", 5, 0);
		var weatherService = new OpenMeteoWeatherService(
			new HttpClient(new ThrowingHttpMessageHandler(new InvalidOperationException("Should not be called"))),
			new FakeConnectivity(NetworkAccess.None));
		var home = CreateHome(weatherService);

		await home.LoadAsync();

		Assert.Equal(WeatherUnavailableException.NoInternetMessage, home.WeatherError);
		Assert.False(home.HasWeather);
		Assert.Equal(["Call mum"], Titles(home.TopTodos));
	}
}
