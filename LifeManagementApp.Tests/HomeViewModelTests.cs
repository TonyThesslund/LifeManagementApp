using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;
using LifeManagementApp.ViewModels;
using LifeManagementApp.Views;

namespace LifeManagementApp.Tests;

public class HomeViewModelTests
{
	private readonly FakeTodoRepository _repository = new();
	private readonly FakeNavigationService _navigation = new();
	private readonly FakeWeatherService _weather = new();
	private readonly FakeSettingsService _settings = new();

	// Friday 20 November 2026, 14:30
	private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 20, 14, 30, 0, TimeSpan.Zero));

	private static readonly WeatherLocation Oulu = new("Oulu", 65.01, 25.47, "North Ostrobothnia, Finland");

	private HomeViewModel CreateViewModel() =>
		new(_repository, _navigation, _weather, new KitesurfAdvisor(_settings), _settings, _time);

	[Fact]
	public async Task LoadTodos_AsksRepositoryForTopThree()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(3, _repository.LastTopCount);
	}

	[Fact]
	public async Task LoadTodos_ShowsWhatRepositoryReturns_InSameOrder()
	{
		_repository.Seed(new TodoItem { Title = "First" });
		_repository.Seed(new TodoItem { Title = "Second" });
		_repository.Seed(new TodoItem { Title = "Third" });
		_repository.Seed(new TodoItem { Title = "Fourth" });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Equal(["First", "Second", "Third"], viewModel.TopTodos.Select(t => t.Title).ToArray());
	}

	[Fact]
	public async Task LoadTodos_Twice_ReplacesListInsteadOfAppending()
	{
		_repository.Seed(new TodoItem { Title = "Only" });
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);
		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Single(viewModel.TopTodos);
	}

	[Fact]
	public async Task LoadTodos_WithNoTodos_LeavesListEmpty()
	{
		var viewModel = CreateViewModel();

		await viewModel.LoadTodosCommand.ExecuteAsync(null);

		Assert.Empty(viewModel.TopTodos);
	}

	[Fact]
	public async Task OpenTodo_NavigatesToDetailPage_WithId()
	{
		var item = _repository.Seed(new TodoItem { Title = "Call mum" });
		var viewModel = CreateViewModel();

		await viewModel.OpenTodoCommand.ExecuteAsync(item);

		Assert.Equal([nameof(TodoDetailPage)], _navigation.Routes);
		Assert.Equal(item.Id, _navigation.LastParameters![TodoDetailViewModel.IdParameter]);
	}

	[Fact]
	public async Task LoadWeather_Success_ShowsWeather_ForVaasa()
	{
		_weather.Result = new WeatherInfo { Temperature = -3.2, WindSpeed = 4.4 };
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.True(viewModel.HasWeather);
		Assert.Equal(-3.2, viewModel.Weather!.Temperature);
		Assert.Equal(4.4, viewModel.Weather.WindSpeed);
		Assert.Equal("Vaasa", viewModel.LocationName);
		Assert.Equal((63.10, 21.62), _weather.LastCoordinates);
		Assert.False(viewModel.HasWeatherError);
		Assert.False(viewModel.IsLoadingWeather);
	}

	[Fact]
	public async Task LoadWeather_ShowsLoading_UntilCallFinishes()
	{
		_weather.Gate = new TaskCompletionSource();
		var viewModel = CreateViewModel();

		var loading = viewModel.LoadWeatherCommand.ExecuteAsync(null);
		Assert.True(viewModel.IsLoadingWeather);

		_weather.Gate.SetResult();
		await loading;
		Assert.False(viewModel.IsLoadingWeather);
	}

	[Fact]
	public async Task LoadWeather_Failure_ShowsErrorMessage_AndNoWeather()
	{
		_weather.Error = new WeatherUnavailableException(WeatherUnavailableException.NoInternetMessage);
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.True(viewModel.HasWeatherError);
		Assert.Equal("No internet connection.", viewModel.WeatherError);
		Assert.False(viewModel.HasWeather);
		Assert.False(viewModel.IsLoadingWeather);
	}

	[Fact]
	public async Task LoadWeather_AfterFailure_SuccessClearsError()
	{
		_weather.Error = new WeatherUnavailableException(WeatherUnavailableException.UnreachableMessage);
		var viewModel = CreateViewModel();
		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		_weather.Error = null;
		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.False(viewModel.HasWeatherError);
		Assert.True(viewModel.HasWeather);
	}

	[Fact]
	public async Task LoadAsync_LoadsTodos_WhileWeatherIsStillLoading()
	{
		_repository.Seed(new TodoItem { Title = "Call mum" });
		_weather.Gate = new TaskCompletionSource();
		var viewModel = CreateViewModel();

		var loading = viewModel.LoadAsync();

		Assert.Single(viewModel.TopTodos);
		Assert.True(viewModel.IsLoadingWeather);

		_weather.Gate.SetResult();
		await loading;
	}

	[Fact]
	public async Task LoadAsync_LoadsTodos_EvenWhenWeatherFails()
	{
		_repository.Seed(new TodoItem { Title = "Call mum" });
		_weather.Error = new WeatherUnavailableException(WeatherUnavailableException.UnreachableMessage);
		var viewModel = CreateViewModel();

		await viewModel.LoadAsync();

		Assert.Single(viewModel.TopTodos);
		Assert.True(viewModel.HasWeatherError);
	}

	[Fact]
	public async Task LoadAsync_FetchesWeatherOnlyOnce_ButReloadsTodosEveryTime()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync();

		_repository.Seed(new TodoItem { Title = "Added later" });
		await viewModel.LoadAsync();

		Assert.Equal(1, _weather.CallCount);
		Assert.Single(viewModel.TopTodos);
	}

	[Fact]
	public async Task Refresh_FetchesWeatherAgain()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.Equal(2, _weather.CallCount);
	}

	[Theory]
	[InlineData(5.1)]
	[InlineData(9.0)]
	public async Task LoadWeather_WindOverLimit_ShowsKitesurfAlert(double windSpeed)
	{
		_weather.Result = new WeatherInfo { WindSpeed = windSpeed };
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.True(viewModel.IsKitesurfWeather);
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(4.9)]
	[InlineData(5.0)]
	public async Task LoadWeather_WindAtOrBelowLimit_HidesKitesurfAlert(double windSpeed)
	{
		_weather.Result = new WeatherInfo { WindSpeed = windSpeed };
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.False(viewModel.IsKitesurfWeather);
	}

	[Fact]
	public async Task LoadWeather_FailureAfterKitesurfWeather_HidesKitesurfAlert()
	{
		_weather.Result = new WeatherInfo { WindSpeed = 8.0 };
		var viewModel = CreateViewModel();
		await viewModel.LoadWeatherCommand.ExecuteAsync(null);
		Assert.True(viewModel.IsKitesurfWeather);

		_weather.Error = new WeatherUnavailableException(WeatherUnavailableException.UnreachableMessage);
		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.False(viewModel.IsKitesurfWeather);
	}

	[Fact]
	public void NewViewModel_HasNoKitesurfAlert()
	{
		Assert.False(CreateViewModel().IsKitesurfWeather);
	}

	[Fact]
	public async Task LoadWeather_UsesSavedLocation()
	{
		_settings.Location = Oulu;
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.Equal((65.01, 25.47), _weather.LastCoordinates);
		Assert.Equal("Oulu", viewModel.LocationName);
	}

	[Fact]
	public async Task LoadAsync_LocationChangedInSettings_FetchesWeatherAgain()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync();

		_settings.Location = Oulu;
		await viewModel.LoadAsync();

		Assert.Equal(2, _weather.CallCount);
		Assert.Equal((65.01, 25.47), _weather.LastCoordinates);
		Assert.Equal("Oulu", viewModel.LocationName);
	}

	[Fact]
	public async Task LoadAsync_LocationChanged_HidesOldWeatherWhileLoading()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync();

		_settings.Location = Oulu;
		_weather.Gate = new TaskCompletionSource();
		var loading = viewModel.LoadAsync();

		Assert.False(viewModel.HasWeather);
		Assert.Equal("Oulu", viewModel.LocationName);
		_weather.Gate.SetResult();
		await loading;
		Assert.True(viewModel.HasWeather);
	}

	[Fact]
	public async Task LoadAsync_SameLocation_DoesNotFetchAgain()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync();

		_settings.Location = WeatherLocation.Vaasa;
		await viewModel.LoadAsync();

		Assert.Equal(1, _weather.CallCount);
	}

	[Fact]
	public async Task Kitesurf_UsesSavedWindLimit()
	{
		_settings.WindLimit = 8.0;
		_weather.Result = new WeatherInfo { WindSpeed = 7.0 };
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.False(viewModel.IsKitesurfWeather);
	}

	[Fact]
	public async Task LoadAsync_WindLimitChanged_RechecksWithoutFetching()
	{
		_weather.Result = new WeatherInfo { WindSpeed = 7.0 };
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync();
		Assert.True(viewModel.IsKitesurfWeather);

		_settings.WindLimit = 8.0;
		await viewModel.LoadAsync();

		Assert.False(viewModel.IsKitesurfWeather);
		Assert.Null(viewModel.KitesurfReason);
		Assert.Equal(1, _weather.CallCount);
	}

	[Fact]
	public async Task KitesurfReason_NamesWindAndLimit()
	{
		_settings.WindLimit = 7.0;
		_weather.Result = new WeatherInfo { WindSpeed = 9.0 };
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.Equal($"Wind {9.0:0.0} m/s is above your 7 m/s limit", viewModel.KitesurfReason);
	}

	[Fact]
	public async Task WeatherTime_ShowsMeasurementTime_WithColon()
	{
		_weather.Result = new WeatherInfo { Time = new DateTime(2026, 11, 20, 13, 45, 0) };
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.Equal("13:45", viewModel.WeatherTime);
	}

	[Fact]
	public async Task WeatherTime_IsEmpty_WhenWeatherFails()
	{
		_weather.Error = new WeatherUnavailableException(WeatherUnavailableException.UnreachableMessage);
		var viewModel = CreateViewModel();

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.Null(viewModel.WeatherTime);
	}

	[Fact]
	public async Task Refresh_WhileWeatherIsLoading_IsIgnored()
	{
		_weather.Gate = new TaskCompletionSource();
		var viewModel = CreateViewModel();
		var first = viewModel.LoadWeatherCommand.ExecuteAsync(null);

		var second = viewModel.LoadWeatherCommand.ExecuteAsync(null);
		_weather.Gate.SetResult();
		await Task.WhenAll(first, second);

		Assert.Equal(1, _weather.CallCount);
		Assert.True(viewModel.HasWeather);
	}

	[Theory]
	[InlineData(0, "Good night")]
	[InlineData(4, "Good night")]
	[InlineData(5, "Good morning")]
	[InlineData(11, "Good morning")]
	[InlineData(12, "Good afternoon")]
	[InlineData(17, "Good afternoon")]
	[InlineData(18, "Good evening")]
	[InlineData(22, "Good evening")]
	[InlineData(23, "Good night")]
	public void GreetingFor_DependsOnHour(int hour, string expected)
	{
		Assert.Equal(expected, HomeViewModel.GreetingFor(hour));
	}

	[Fact]
	public void NewViewModel_ShowsGreetingAndDateForNow()
	{
		var viewModel = CreateViewModel();

		Assert.Equal("Good afternoon", viewModel.Greeting);
		Assert.Equal("Friday 20 November", viewModel.Today);
	}

	[Fact]
	public void Today_IsInEnglish_WhateverTheComputersLanguage()
	{
		var original = System.Globalization.CultureInfo.CurrentCulture;
		System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fi-FI");
		try
		{
			Assert.Equal("Friday 20 November", CreateViewModel().Today);
		}
		finally
		{
			System.Globalization.CultureInfo.CurrentCulture = original;
		}
	}

	[Fact]
	public async Task LoadAsync_UpdatesGreetingAndDate_WhenTimeHasPassed()
	{
		var viewModel = CreateViewModel();
		_time.Now = new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero);

		await viewModel.LoadAsync();

		Assert.Equal("Good morning", viewModel.Greeting);
		Assert.Equal("Saturday 21 November", viewModel.Today);
	}

	[Fact]
	public void NewViewModel_ShowsLocationDetails()
	{
		_settings.Location = Oulu;

		var viewModel = CreateViewModel();

		Assert.Equal("North Ostrobothnia, Finland", viewModel.LocationDetails);
	}

	[Fact]
	public async Task LoadWeather_AfterLocationChange_ShowsNewDetails()
	{
		var viewModel = CreateViewModel();
		_settings.Location = Oulu;

		await viewModel.LoadWeatherCommand.ExecuteAsync(null);

		Assert.Equal("Oulu", viewModel.LocationName);
		Assert.Equal("North Ostrobothnia, Finland", viewModel.LocationDetails);
	}

	[Fact]
	public async Task GoToTodos_OpensToDoTab()
	{
		var viewModel = CreateViewModel();

		await viewModel.GoToTodosCommand.ExecuteAsync(null);

		Assert.Equal(["//todos"], _navigation.Routes);
	}
}
