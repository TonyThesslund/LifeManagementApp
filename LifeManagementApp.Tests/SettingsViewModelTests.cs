using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;
using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Tests;

public class SettingsViewModelTests
{
	private static readonly WeatherLocation Helsinki = new("Helsinki", 60.17, 24.94, "Uusimaa, Finland");
	private static readonly WeatherLocation Oulu = new("Oulu", 65.01, 25.47, "North Ostrobothnia, Finland");

	private readonly FakeSettingsService _settings = new();
	private readonly FakeGeocodingService _geocoding = new();
	private readonly FakeThemeService _themes = new();
	private readonly FakeTodoRepository _repository = new();
	private readonly FakeDialogService _dialogs = new();

	private SettingsViewModel CreateViewModel() => new(_settings, _geocoding, _themes, _repository, _dialogs);

	private async Task<SettingsViewModel> CreateWithCategoriesAsync()
	{
		var viewModel = CreateViewModel();
		await viewModel.LoadCategoriesCommand.ExecuteAsync(null);
		return viewModel;
	}

	[Fact]
	public void Opens_WithSavedSettings()
	{
		_settings.Location = Helsinki;
		_settings.WindLimit = 7.5;

		var viewModel = CreateViewModel();

		Assert.Equal(Helsinki, viewModel.CurrentLocation);
		Assert.True(SettingsViewModel.TryParseWindLimit(viewModel.WindLimitText, out var shown));
		Assert.Equal(7.5, shown);
		Assert.False(viewModel.HasWindLimitError);
	}

	[Fact]
	public async Task Search_ShowsResults()
	{
		_geocoding.Results = [Helsinki, Oulu];
		var viewModel = CreateViewModel();
		viewModel.SearchText = "Hel";

		await viewModel.SearchCommand.ExecuteAsync(null);

		Assert.Equal("Hel", _geocoding.LastName);
		Assert.Equal([Helsinki, Oulu], viewModel.SearchResults);
		Assert.False(viewModel.HasSearchMessage);
	}

	[Fact]
	public async Task Search_NoResults_ShowsMessage()
	{
		var viewModel = CreateViewModel();
		viewModel.SearchText = " Xyzzy ";

		await viewModel.SearchCommand.ExecuteAsync(null);

		Assert.Empty(viewModel.SearchResults);
		Assert.Equal("No places found for \"Xyzzy\".", viewModel.SearchMessage);
	}

	[Fact]
	public async Task Search_Error_ShowsErrorMessage()
	{
		_geocoding.Error = new LocationSearchException(LocationSearchException.NoInternetMessage);
		var viewModel = CreateViewModel();
		viewModel.SearchText = "Helsinki";

		await viewModel.SearchCommand.ExecuteAsync(null);

		Assert.Equal(LocationSearchException.NoInternetMessage, viewModel.SearchMessage);
		Assert.False(viewModel.IsSearching);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Search_EmptyText_CannotExecute(string text)
	{
		var viewModel = CreateViewModel();

		viewModel.SearchText = text;

		Assert.False(viewModel.SearchCommand.CanExecute(null));
	}

	[Fact]
	public async Task Search_ShowsLoading_UntilDone()
	{
		_geocoding.Gate = new TaskCompletionSource();
		var viewModel = CreateViewModel();
		viewModel.SearchText = "Helsinki";

		var search = viewModel.SearchCommand.ExecuteAsync(null);
		Assert.True(viewModel.IsSearching);

		_geocoding.Gate.SetResult();
		await search;
		Assert.False(viewModel.IsSearching);
	}

	[Fact]
	public async Task NewSearch_ClearsOldResultsAndMessage()
	{
		_geocoding.Results = [Helsinki];
		var viewModel = CreateViewModel();
		viewModel.SearchText = "Helsinki";
		await viewModel.SearchCommand.ExecuteAsync(null);

		_geocoding.Results = [];
		viewModel.SearchText = "Xyzzy";
		await viewModel.SearchCommand.ExecuteAsync(null);
		_geocoding.Results = [Oulu];
		viewModel.SearchText = "Oulu";
		await viewModel.SearchCommand.ExecuteAsync(null);

		Assert.Equal([Oulu], viewModel.SearchResults);
		Assert.False(viewModel.HasSearchMessage);
	}

	[Fact]
	public async Task SelectLocation_SavesIt_AndClearsSearch()
	{
		_geocoding.Results = [Helsinki, Oulu];
		var viewModel = CreateViewModel();
		viewModel.SearchText = "Hel";
		await viewModel.SearchCommand.ExecuteAsync(null);

		viewModel.SelectLocationCommand.Execute(Oulu);

		Assert.Equal(Oulu, _settings.Location);
		Assert.Equal(Oulu, viewModel.CurrentLocation);
		Assert.Empty(viewModel.SearchResults);
		Assert.Equal(string.Empty, viewModel.SearchText);
	}

	[Theory]
	[InlineData("7", 7.0)]
	[InlineData("7.5", 7.5)]
	[InlineData("7,5", 7.5)]
	[InlineData(" 12 ", 12.0)]
	[InlineData("40", 40.0)]
	public void WindLimit_ValidText_IsSaved(string text, double expected)
	{
		var viewModel = CreateViewModel();

		viewModel.WindLimitText = text;

		Assert.Equal(expected, _settings.WindLimit);
		Assert.False(viewModel.HasWindLimitError);
	}

	[Theory]
	[InlineData("")]
	[InlineData("abc")]
	[InlineData("0")]
	[InlineData("-3")]
	[InlineData("40.5")]
	[InlineData("5.5.5")]
	public void WindLimit_InvalidText_IsRejected(string text)
	{
		var viewModel = CreateViewModel();

		viewModel.WindLimitText = text;

		Assert.Equal(5.0, _settings.WindLimit);
		Assert.True(viewModel.HasWindLimitError);
	}

	[Fact]
	public void WindLimit_FixingInvalidText_ClearsError()
	{
		var viewModel = CreateViewModel();
		viewModel.WindLimitText = "abc";

		viewModel.WindLimitText = "6";

		Assert.False(viewModel.HasWindLimitError);
		Assert.Equal(6.0, _settings.WindLimit);
	}

	[Fact]
	public void Load_ReadsSettingsChangedElsewhere()
	{
		var viewModel = CreateViewModel();
		_settings.Location = Oulu;

		viewModel.Load();

		Assert.Equal(Oulu, viewModel.CurrentLocation);
	}

	[Fact]
	public void Opens_WithSavedTheme_WithoutApplyingAgain()
	{
		_settings.Theme = ThemeSetting.Dark;

		var viewModel = CreateViewModel();

		Assert.Equal(ThemeSetting.Dark, viewModel.SelectedTheme);
		Assert.Empty(_themes.Applied);
	}

	[Theory]
	[InlineData(ThemeSetting.Light)]
	[InlineData(ThemeSetting.Dark)]
	public void ChoosingTheme_SavesAndAppliesIt(ThemeSetting theme)
	{
		var viewModel = CreateViewModel();

		viewModel.SelectedTheme = theme;

		Assert.Equal(theme, _settings.Theme);
		Assert.Equal([theme], _themes.Applied);
	}

	[Fact]
	public void ChoosingSystemAgain_SavesAndAppliesIt()
	{
		var viewModel = CreateViewModel();
		viewModel.SelectedTheme = ThemeSetting.Dark;

		viewModel.SelectedTheme = ThemeSetting.System;

		Assert.Equal(ThemeSetting.System, _settings.Theme);
		Assert.Equal([ThemeSetting.Dark, ThemeSetting.System], _themes.Applied);
	}

	[Fact]
	public void Themes_OfferSystemLightDark()
	{
		Assert.Equal([ThemeSetting.System, ThemeSetting.Light, ThemeSetting.Dark], CreateViewModel().Themes);
	}

	[Fact]
	public async Task Search_WhileSearching_IsIgnored()
	{
		_geocoding.Results = [Helsinki];
		_geocoding.Gate = new TaskCompletionSource();
		var viewModel = CreateViewModel();
		viewModel.SearchText = "Helsinki";
		var first = viewModel.SearchCommand.ExecuteAsync(null);

		var second = viewModel.SearchCommand.ExecuteAsync(null);
		_geocoding.Gate.SetResult();
		await Task.WhenAll(first, second);

		Assert.Equal(1, _geocoding.CallCount);
		Assert.Equal([Helsinki], viewModel.SearchResults);
	}

	[Fact]
	public async Task LoadCategories_ShowsCategoriesFromRepository()
	{
		var viewModel = await CreateWithCategoriesAsync();

		Assert.Equal(["Personal", "Work", "Shopping"], viewModel.Categories.Select(c => c.Name));
	}

	[Fact]
	public async Task LoadCategories_Twice_ReplacesListInsteadOfAppending()
	{
		var viewModel = await CreateWithCategoriesAsync();

		await viewModel.LoadCategoriesCommand.ExecuteAsync(null);

		Assert.Equal(3, viewModel.Categories.Count);
	}

	[Fact]
	public async Task AddCategory_ValidName_IsSaved_ShownAndFieldCleared()
	{
		var viewModel = await CreateWithCategoriesAsync();
		viewModel.NewCategoryName = "  Hobbies ";

		await viewModel.AddCategoryCommand.ExecuteAsync(null);

		Assert.Contains(_repository.Categories, c => c.Name == "Hobbies");
		Assert.Contains("Hobbies", viewModel.Categories.Select(c => c.Name));
		Assert.Equal(string.Empty, viewModel.NewCategoryName);
		Assert.False(viewModel.HasCategoryError);
	}

	[Theory]
	[InlineData("Work")]
	[InlineData("work")]
	[InlineData(" WORK ")]
	public async Task AddCategory_DuplicateName_ShowsMessage_AndIsNotSaved(string name)
	{
		var viewModel = await CreateWithCategoriesAsync();
		viewModel.NewCategoryName = name;

		await viewModel.AddCategoryCommand.ExecuteAsync(null);

		Assert.Equal(3, _repository.Categories.Count);
		Assert.Equal($"A category called \"{name.Trim()}\" already exists.", viewModel.CategoryError);
		Assert.Equal(name, viewModel.NewCategoryName);
	}

	[Fact]
	public async Task AddCategory_TooLongName_ShowsMessage_AndIsNotSaved()
	{
		var viewModel = await CreateWithCategoriesAsync();
		viewModel.NewCategoryName = new string('x', Category.MaxNameLength + 1);

		await viewModel.AddCategoryCommand.ExecuteAsync(null);

		Assert.Equal(3, _repository.Categories.Count);
		Assert.Equal($"A category name can be at most {Category.MaxNameLength} characters.", viewModel.CategoryError);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task AddCategory_EmptyName_CannotExecute_AndIsNotSaved(string name)
	{
		var viewModel = await CreateWithCategoriesAsync();
		viewModel.NewCategoryName = name;

		Assert.False(viewModel.AddCategoryCommand.CanExecute(null));
		await viewModel.AddCategoryCommand.ExecuteAsync(null);

		Assert.Equal(3, _repository.Categories.Count);
	}

	[Fact]
	public async Task ChangingName_ClearsCategoryError()
	{
		var viewModel = await CreateWithCategoriesAsync();
		viewModel.NewCategoryName = "Work";
		await viewModel.AddCategoryCommand.ExecuteAsync(null);
		Assert.True(viewModel.HasCategoryError);

		viewModel.NewCategoryName = "Workout";

		Assert.False(viewModel.HasCategoryError);
	}

	[Fact]
	public async Task DeleteCategory_Confirmed_DeletesAndRemovesFromList()
	{
		var viewModel = await CreateWithCategoriesAsync();
		var work = viewModel.Categories.Single(c => c.Name == "Work");

		await viewModel.DeleteCategoryCommand.ExecuteAsync(work);

		Assert.Equal([work.Id], _repository.DeletedCategories);
		Assert.Equal(["Personal", "Shopping"], viewModel.Categories.Select(c => c.Name));
	}

	[Fact]
	public async Task DeleteCategory_Cancelled_KeepsIt()
	{
		_dialogs.ConfirmResult = false;
		var viewModel = await CreateWithCategoriesAsync();

		await viewModel.DeleteCategoryCommand.ExecuteAsync(viewModel.Categories.Single(c => c.Name == "Work"));

		Assert.Empty(_repository.DeletedCategories);
		Assert.Equal(3, viewModel.Categories.Count);
	}

	[Fact]
	public async Task DeleteCategory_AsksOnce_NamingIt_AndSayingTodosAreKept()
	{
		var viewModel = await CreateWithCategoriesAsync();

		await viewModel.DeleteCategoryCommand.ExecuteAsync(viewModel.Categories.Single(c => c.Name == "Work"));

		var confirmation = Assert.Single(_dialogs.Confirmations);
		Assert.Contains("\"Work\"", confirmation.Message);
		Assert.Contains("to-dos are kept", confirmation.Message);
		Assert.Equal("Delete", confirmation.Accept);
	}
}
