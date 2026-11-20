using System.Collections.ObjectModel;
using System.Globalization;
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
	private readonly ISettingsService _settings;

	// The location the shown weather is for, to notice when the user picks another one in Settings
	private WeatherLocation? _loadedLocation;

	public HomeViewModel(
		ITodoRepository repository,
		INavigationService navigation,
		IWeatherService weatherService,
		IKitesurfAdvisor kitesurfAdvisor,
		ISettingsService settings)
	{
		_repository = repository;
		_navigation = navigation;
		_weatherService = weatherService;
		_kitesurfAdvisor = kitesurfAdvisor;
		_settings = settings;
		LocationName = settings.Location.Name;
	}

	public ObservableCollection<TodoItem> TopTodos { get; } = [];

	[ObservableProperty]
	public partial string LocationName { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasWeather))]
	[NotifyPropertyChangedFor(nameof(WeatherTime))]
	public partial WeatherInfo? Weather { get; set; }

	/// <summary>
	/// When the shown weather was measured, in the location's own time zone, e.g. "13:45".
	/// Invariant culture so it's "13:45" and not "13.45" on a Finnish system.
	/// </summary>
	public string? WeatherTime => Weather?.Time.ToString("HH:mm", CultureInfo.InvariantCulture);

	[ObservableProperty]
	public partial bool IsKitesurfWeather { get; set; }

	/// <summary>Why it's kitesurf weather, e.g. "Wind 8.2 m/s is above your 7 m/s limit".</summary>
	[ObservableProperty]
	public partial string? KitesurfReason { get; set; }

	[ObservableProperty]
	public partial bool IsLoadingWeather { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasWeatherError))]
	public partial string? WeatherError { get; set; }

	public bool HasWeather => Weather is not null;

	public bool HasWeatherError => WeatherError is not null;

	partial void OnWeatherChanged(WeatherInfo? value)
	{
		UpdateKitesurf();
	}

	private void UpdateKitesurf()
	{
		IsKitesurfWeather = Weather is not null && _kitesurfAdvisor.IsKitesurfWeather(Weather);
		KitesurfReason = IsKitesurfWeather
			? $"Wind {Weather!.WindSpeed:0.0} m/s is above your {_kitesurfAdvisor.WindLimit:0.#} m/s limit"
			: null;
	}

	/// <summary>
	/// Called when the page appears. To-dos are always reloaded; the weather only if it hasn't been loaded yet
	/// or the location has changed in Settings. Both run at the same time, so a slow weather call doesn't hold
	/// up the to-do list. The kitesurf notice is re-checked in case the wind limit has changed.
	/// </summary>
	public Task LoadAsync()
	{
		var todos = LoadTodosCommand.ExecuteAsync(null);

		var needsWeather = !HasWeather || _settings.Location != _loadedLocation;
		if (!needsWeather)
		{
			UpdateKitesurf();
		}

		var weather = needsWeather ? LoadWeatherCommand.ExecuteAsync(null) : Task.CompletedTask;
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
			var location = _settings.Location;
			if (location != _loadedLocation)
			{
				// Don't show the old city's weather under the new city's name while loading
				Weather = null;
			}

			LocationName = location.Name;
			_loadedLocation = location;
			Weather = await _weatherService.GetCurrentWeatherAsync(location.Latitude, location.Longitude);
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
