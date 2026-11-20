using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoDetailViewModel : ObservableObject
{
	private readonly ITodoRepository _repository;
	private readonly INavigationService _navigation;

	public TodoDetailViewModel(ITodoRepository repository, INavigationService navigation)
	{
		_repository = repository;
		_navigation = navigation;
	}

	public IReadOnlyList<int> Priorities { get; } = [1, 2, 3, 4, 5];

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

	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task SaveAsync()
	{
		if (!CanSave)
		{
			return;
		}

		var item = new TodoItem
		{
			Title = Title.Trim(),
			Notes = Notes,
			Priority = Priority,
			DueDate = DueDate
		};

		await _repository.AddAsync(item);
		await _navigation.GoBackAsync();
	}
}
