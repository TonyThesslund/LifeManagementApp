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
	public partial string Title { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string? Notes { get; set; }

	[ObservableProperty]
	public partial int Priority { get; set; } = 3;

	[ObservableProperty]
	public partial DateTime DueDate { get; set; } = DateTime.Today;

	[RelayCommand]
	private async Task SaveAsync()
	{
		var item = new TodoItem
		{
			Title = Title,
			Notes = Notes,
			Priority = Priority,
			DueDate = DueDate
		};

		await _repository.AddAsync(item);
		await _navigation.GoBackAsync();
	}
}
