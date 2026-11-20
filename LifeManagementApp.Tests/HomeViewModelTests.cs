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

	private HomeViewModel CreateViewModel() => new(_repository, _navigation, _weather);

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
}
