using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	public const double MaxWindLimit = 40;

	private readonly ISettingsService _settings;
	private readonly IGeocodingService _geocoding;

	public SettingsViewModel(ISettingsService settings, IGeocodingService geocoding)
	{
		_settings = settings;
		_geocoding = geocoding;
		Load();
	}

	public ObservableCollection<WeatherLocation> SearchResults { get; } = [];

	[ObservableProperty]
	public partial WeatherLocation CurrentLocation { get; set; } = WeatherLocation.Vaasa;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SearchCommand))]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsSearching { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasSearchMessage))]
	public partial string? SearchMessage { get; set; }

	public bool HasSearchMessage => SearchMessage is not null;

	[ObservableProperty]
	public partial string WindLimitText { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasWindLimitError))]
	public partial string? WindLimitError { get; set; }

	public bool HasWindLimitError => WindLimitError is not null;

	/// <summary>Reads the saved settings, e.g. when the page opens.</summary>
	public void Load()
	{
		CurrentLocation = _settings.Location;
		WindLimitText = _settings.WindLimit.ToString(CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Accepts both "5.5" and "5,5", whatever the Windows language, and only limits above 0 and up to <see cref="MaxWindLimit"/>.
	/// </summary>
	public static bool TryParseWindLimit(string? text, out double limit)
	{
		var normalized = text?.Trim().Replace(',', '.');
		return double.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out limit)
			&& limit > 0
			&& limit <= MaxWindLimit;
	}

	partial void OnWindLimitTextChanged(string value)
	{
		if (TryParseWindLimit(value, out var limit))
		{
			_settings.WindLimit = limit;
			WindLimitError = null;
		}
		else
		{
			WindLimitError = $"Enter a wind speed above 0 and up to {MaxWindLimit:0} m/s.";
		}
	}

	private bool CanSearch => !string.IsNullOrWhiteSpace(SearchText);

	[RelayCommand(CanExecute = nameof(CanSearch))]
	private async Task SearchAsync()
	{
		if (IsSearching || !CanSearch)
		{
			return;
		}

		IsSearching = true;
		SearchMessage = null;
		SearchResults.Clear();
		try
		{
			var results = await _geocoding.SearchAsync(SearchText);
			foreach (var result in results)
			{
				SearchResults.Add(result);
			}

			if (results.Count == 0)
			{
				SearchMessage = $"No places found for \"{SearchText.Trim()}\".";
			}
		}
		catch (LocationSearchException ex)
		{
			SearchMessage = ex.Message;
		}
		finally
		{
			IsSearching = false;
		}
	}

	[RelayCommand]
	private void SelectLocation(WeatherLocation location)
	{
		_settings.Location = location;
		CurrentLocation = location;
		SearchResults.Clear();
		SearchText = string.Empty;
		SearchMessage = null;
	}
}
