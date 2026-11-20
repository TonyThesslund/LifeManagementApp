using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoDetailViewModel : ObservableObject, IQueryAttributable
{
	public const string IdParameter = "id";

	private readonly ITodoRepository _repository;
	private readonly INavigationService _navigation;
	private readonly IDialogService _dialogs;

	private TodoItem? _existing;
	private Task? _categoriesLoading;

	public TodoDetailViewModel(ITodoRepository repository, INavigationService navigation, IDialogService dialogs)
	{
		_repository = repository;
		_navigation = navigation;
		_dialogs = dialogs;
	}

	public IReadOnlyList<int> Priorities { get; } = [1, 2, 3, 4, 5];

	/// <summary>Picker option for "no category". Not stored in the database (Id 0).</summary>
	public static Category NoCategory { get; } = new() { Id = 0, Name = "No category" };

	/// <summary>"No category" first, then the categories from the database.</summary>
	public ObservableCollection<Category> Categories { get; } = [NoCategory];

	[ObservableProperty]
	public partial Category SelectedCategory { get; set; } = NoCategory;

	[ObservableProperty]
	public partial string PageTitle { get; set; } = "New to-do";

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	public partial string Title { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string? TitleError { get; set; }

	[ObservableProperty]
	public partial bool HasTitleError { get; set; }

	[ObservableProperty]
	public partial string? Notes { get; set; }

	[ObservableProperty]
	public partial int Priority { get; set; } = 3;

	[ObservableProperty]
	public partial DateTime DueDate { get; set; } = DateTime.Today;

	private bool CanSave => IsTitleValid(Title);

	private int? SelectedCategoryId => SelectedCategory.Id == NoCategory.Id ? null : SelectedCategory.Id;

	public static bool IsTitleValid(string? title)
	{
		return !string.IsNullOrWhiteSpace(title);
	}

	partial void OnTitleChanged(string value)
	{
		TitleError = IsTitleValid(value) ? null : "Title is required.";
		HasTitleError = TitleError is not null;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue(IdParameter, out var value) && value is int id)
		{
			LoadCommand.Execute(id);
		}
	}

	[RelayCommand]
	private Task LoadCategoriesAsync()
	{
		// Loaded once, even if both the page and LoadAsync ask for them
		return _categoriesLoading ??= FetchCategoriesAsync();
	}

	private async Task FetchCategoriesAsync()
	{
		foreach (var category in await _repository.GetCategoriesAsync())
		{
			Categories.Add(category);
		}
	}

	[RelayCommand]
	private async Task LoadAsync(int id)
	{
		var item = await _repository.GetByIdAsync(id);
		if (item is null)
		{
			await _dialogs.ShowAlertAsync("Not found", "This to-do no longer exists.");
			await _navigation.GoBackAsync();
			return;
		}

		_existing = item;
		PageTitle = "Edit to-do";
		Title = item.Title;
		Notes = item.Notes;
		Priority = item.Priority;
		DueDate = item.DueDate;

		await LoadCategoriesAsync();
		SelectedCategory = Categories.FirstOrDefault(c => c.Id == item.CategoryId) ?? NoCategory;
	}

	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task SaveAsync()
	{
		if (!CanSave)
		{
			return;
		}

		if (_existing is null)
		{
			await _repository.AddAsync(new TodoItem
			{
				Title = Title.Trim(),
				Notes = Notes,
				Priority = Priority,
				DueDate = DueDate,
				CategoryId = SelectedCategoryId
			});
		}
		else
		{
			_existing.Title = Title.Trim();
			_existing.Notes = Notes;
			_existing.Priority = Priority;
			_existing.DueDate = DueDate;
			_existing.CategoryId = SelectedCategoryId;
			await _repository.UpdateAsync(_existing);
		}

		await _navigation.GoBackAsync();
	}
}
