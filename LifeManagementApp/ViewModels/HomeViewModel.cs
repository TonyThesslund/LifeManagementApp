using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Views;

namespace LifeManagementApp.ViewModels;

public partial class HomeViewModel : ObservableObject
{
	public const int TopTodoCount = 3;

	private readonly ITodoRepository _repository;
	private readonly INavigationService _navigation;
	private readonly IWeatherService _weatherService;
	private readonly IKitesurfAdvisor _kitesurfAdvisor;

	public HomeViewModel(
		ITodoRepository repository,
		INavigationService navigation,
		IWeatherService weatherService,
		IKitesurfAdvisor kitesurfAdvisor)
	{
		_repository = repository;
		_navigation = navigation;
		_weatherService = weatherService;
		_kitesurfAdvisor = kitesurfAdvisor;
	}

	public ObservableCollection<TodoItem> TopTodos { get; } = [];

	[ObservableProperty]
	public partial string LocationName { get; set; } = WeatherLocation.Vaasa.Name;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasWeather))]
	public partial WeatherInfo? Weather { get; set; }

	[ObservableProperty]
	public partial bool IsKitesurfWeather { get; set; }

	[ObservableProperty]
	public partial bool IsLoadingWeather { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasWeatherError))]
	public partial string? WeatherError { get; set; }

	public bool HasWeather => Weather is not null;

	public bool HasWeatherError => WeatherError is not null;

	partial void OnWeatherChanged(WeatherInfo? value)
	{
		IsKitesurfWeather = value is not null && _kitesurfAdvisor.IsKitesurfWeather(value);
	}

	/// <summary>
	/// Called when the page appears. To-dos are always reloaded; the weather only if it hasn't been loaded yet.
	/// Both run at the same time, so a slow weather call doesn't hold up the to-do list.
	/// </summary>
	public Task LoadAsync()
	{
		var todos = LoadTodosCommand.ExecuteAsync(null);
		var weather = HasWeather ? Task.CompletedTask : LoadWeatherCommand.ExecuteAsync(null);
		return Task.WhenAll(todos, weather);
	}

	[RelayCommand]
	private async Task LoadTodosAsync()
	{
		var items = await _repository.GetTopAsync(TopTodoCount);

		TopTodos.Clear();
		foreach (var item in items)
		{
			TopTodos.Add(item);
		}
	}

	[RelayCommand]
	private async Task LoadWeatherAsync()
	{
		if (IsLoadingWeather)
		{
			return;
		}

		IsLoadingWeather = true;
		WeatherError = null;
		try
		{
			var location = WeatherLocation.Vaasa;
			Weather = await _weatherService.GetCurrentWeatherAsync(location.Latitude, location.Longitude);
			LocationName = location.Name;
		}
		catch (WeatherUnavailableException ex)
		{
			Weather = null;
			WeatherError = ex.Message;
		}
		finally
		{
			IsLoadingWeather = false;
		}
	}

	[RelayCommand]
	private Task OpenTodoAsync(TodoItem item)
	{
		return _navigation.GoToAsync(nameof(TodoDetailPage), new Dictionary<string, object>
		{
			[TodoDetailViewModel.IdParameter] = item.Id
		});
	}
}
