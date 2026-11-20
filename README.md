# LifeManagementApp

A .NET MAUI app that combines a to-do list with the current weather and tells you when it's kitesurfing weather.

## Goals

| Area | Goal |
|------|------|
| Home page | Top 3 unfinished to-dos (priority high→low, then due date earliest first), current weather (temperature + wind in m/s), and a "🪁 Kitesurf weather!" notice when wind > 5 m/s |
| To-do page | All to-dos in a `CollectionView`, sortable by priority or due date, button to add a new item |
| Add / edit | Form page; an empty title is rejected. Edit page gets the to-do id as a navigation parameter |
| Tech | MAUI + XAML, MVVM with CommunityToolkit.Mvvm, SQLite + EF Core Code First, HttpClient + Open-Meteo, dependency injection |
| Target | .NET 9 / MAUI 9, one class per file |

## Architecture

```
View (XAML)  →  ViewModel  →  Service (interface)  →  SQLite (EF Core) / Open-Meteo (HTTP)
```

- **Views** only bind to their ViewModel using compiled bindings (`x:DataType`). No logic in code-behind.
- **ViewModels** use `[ObservableProperty]` and `[RelayCommand]` and never reference UI controls.
- **Services** hold all data access and business rules behind interfaces, and are injected via DI in `MauiProgram`.

## Planned structure

```text
LifeManagementApp/
├── Models/
│   ├── TodoItem.cs
│   ├── Category.cs
│   └── WeatherInfo.cs
├── Data/
│   └── AppDbContext.cs
├── Services/
│   ├── ITodoRepository.cs
│   ├── TodoRepository.cs
│   ├── IWeatherService.cs
│   └── OpenMeteoWeatherService.cs
├── ViewModels/
│   ├── HomeViewModel.cs
│   ├── TodoListViewModel.cs
│   └── TodoDetailViewModel.cs
├── Views/
│   ├── HomePage.xaml
│   ├── TodoListPage.xaml
│   └── TodoDetailPage.xaml
├── Converters/
├── AppShell.xaml
├── MauiProgram.cs
└── README.md
```

## Work order

Each step is a GitHub issue with sub-issues, done in a feature branch (`<issue#>-short-name`) and merged to `main` with a pull request that closes the issues. From step 7 on, each step also adds xUnit tests for the logic it introduces.

| # | Step | Contents |
|---|------|----------|
| 1 | Project setup | MAUI project on .NET 9, folder structure, NuGet packages (CommunityToolkit.Mvvm, EF Core Sqlite), Shell with tabs |
| 2 | Models + database | `TodoItem`, `Category` (1–N), `AppDbContext`, database created on startup |
| 3 | Repository | `ITodoRepository` / `TodoRepository` with async add, query, update, delete, sorting and top 3 |
| 4 | Dependency injection | Services, pages and ViewModels registered in `MauiProgram` |
| 5 | To-do list page | `CollectionView`, sort by priority / due date |
| 6 | Add / edit page | `TodoDetailPage` with title validation, edit via id parameter |
| 7 | Test project | xUnit project, app gets a plain `net9.0` target, tests for the repository and `TodoDetailViewModel` |
| 8 | Weather service | Open-Meteo client, JSON → `WeatherInfo`, m/s unit, error handling, kitesurf rule, tests for JSON mapping, errors and wind limit |
| 9 | Home page | Top 3 to-dos, weather, kitesurf alert, loading state |
| 10 | Bindings + converters | `x:DataType` everywhere, converters (e.g. readable sort order, priority color) |
| 11 | Done, edit, delete *(bonus)* | Mark a to-do done, edit and delete it from the list with a checkbox and a delete button |
| 12 | Categories and filtering *(bonus)* | Category picker on the add/edit page, category filter on the to-do list |
| 13 | Settings page *(bonus)* | Settings tab: weather location (city search), kitesurf wind limit and light / dark theme, saved with `Preferences` |
| 14 | Remaining tests | Fill any gaps in test coverage |
| 15 | Documentation | Final README: description, how to run, class diagram, AI use |

## Requirements checklist

**Core**
- [ ] Home: top 3 unfinished to-dos (priority desc, due date asc)
- [ ] Home: temperature + wind speed (m/s)
- [ ] Home: kitesurf notice when wind > 5 m/s
- [ ] To-do list in `CollectionView`
- [ ] Sort by priority / due date
- [ ] Add new to-do, empty title rejected
- [ ] Fields: Id, Title, Priority 1–5, DueDate, IsDone

**Top levels**
- [ ] Shell navigation + parameter to edit page
- [ ] SQLite + EF Core Code First, Category 1–N TodoItem, async queries
- [ ] Model/ViewModel/Service, interfaces, no logic in code-behind
- [ ] `[ObservableProperty]`, `[RelayCommand]`, `ObservableCollection`, `x:DataType`, converter
- [ ] Interfaces + pages/VMs via DI, lifetimes justified
- [ ] Async HTTP, JSON → model, no-network message, loading indicator
- [ ] Regular commits, README per template

**Bonus**
- [ ] Issues + feature branches + PRs
- [ ] Two extra features
- [ ] xUnit tests
