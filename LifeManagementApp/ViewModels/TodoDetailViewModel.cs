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

	public TodoDetailViewModel(ITodoRepository repository, INavigationService navigation, IDialogService dialogs)
	{
		_repository = repository;
		_navigation = navigation;
		_dialogs = dialogs;
	}

	public IReadOnlyList<int> Priorities { get; } = [1, 2, 3, 4, 5];

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
				DueDate = DueDate
			});
		}
		else
		{
			_existing.Title = Title.Trim();
			_existing.Notes = Notes;
			_existing.Priority = Priority;
			_existing.DueDate = DueDate;
			await _repository.UpdateAsync(_existing);
		}

		await _navigation.GoBackAsync();
	}
}
