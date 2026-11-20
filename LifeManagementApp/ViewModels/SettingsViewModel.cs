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
	private readonly IThemeService _themeService;
	private readonly ITodoRepository _repository;
	private readonly IDialogService _dialogs;

	public SettingsViewModel(
		ISettingsService settings,
		IGeocodingService geocoding,
		IThemeService themeService,
		ITodoRepository repository,
		IDialogService dialogs)
	{
		_settings = settings;
		_geocoding = geocoding;
		_themeService = themeService;
		_repository = repository;
		_dialogs = dialogs;
		Load();
	}

	public ObservableCollection<Category> Categories { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
	public partial string NewCategoryName { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasCategoryError))]
	public partial string? CategoryError { get; set; }

	public bool HasCategoryError => CategoryError is not null;

	public IReadOnlyList<ThemeSetting> Themes { get; } = Enum.GetValues<ThemeSetting>();

	[ObservableProperty]
	public partial ThemeSetting SelectedTheme { get; set; }

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
		SelectedTheme = _settings.Theme;
	}

	partial void OnSelectedThemeChanged(ThemeSetting value)
	{
		if (value == _settings.Theme)
		{
			return;
		}

		_settings.Theme = value;
		_themeService.Apply(value);
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

	[RelayCommand]
	private async Task LoadCategoriesAsync()
	{
		var categories = await _repository.GetCategoriesAsync();

		Categories.Clear();
		foreach (var category in categories)
		{
			Categories.Add(category);
		}
	}

	partial void OnNewCategoryNameChanged(string value)
	{
		// The message is about the name that was tried, so it goes away when the user changes it
		CategoryError = null;
	}

	private bool CanAddCategory => !string.IsNullOrWhiteSpace(NewCategoryName);

	[RelayCommand(CanExecute = nameof(CanAddCategory))]
	private async Task AddCategoryAsync()
	{
		var name = NewCategoryName.Trim();
		if (name.Length == 0)
		{
			CategoryError = "Enter a name for the category.";
			return;
		}

		if (name.Length > Category.MaxNameLength)
		{
			CategoryError = $"A category name can be at most {Category.MaxNameLength} characters.";
			return;
		}

		if (await _repository.CategoryExistsAsync(name))
		{
			CategoryError = $"A category called \"{name}\" already exists.";
			return;
		}

		await _repository.AddCategoryAsync(name);
		NewCategoryName = string.Empty;
		await LoadCategoriesAsync();
	}

	[RelayCommand]
	private async Task DeleteCategoryAsync(Category category)
	{
		var confirmed = await _dialogs.ConfirmAsync(
			"Delete category",
			$"Delete \"{category.Name}\"? Its to-dos are kept, without a category.",
			"Delete",
			"Cancel");
		if (!confirmed)
		{
			return;
		}

		await _repository.DeleteCategoryAsync(category.Id);
		await LoadCategoriesAsync();
	}
}
