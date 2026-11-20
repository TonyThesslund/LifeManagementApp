using Microsoft.Maui.Storage;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// In-memory IPreferences, so settings can be tested without MAUI storage.
/// </summary>
public class FakePreferences : IPreferences
{
	public Dictionary<string, object?> Values { get; } = [];

	public bool ContainsKey(string key, string? sharedName = null) => Values.ContainsKey(key);

	public void Remove(string key, string? sharedName = null) => Values.Remove(key);

	public void Clear(string? sharedName = null) => Values.Clear();

	public void Set<T>(string key, T value, string? sharedName = null) => Values[key] = value;

	public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
		Values.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
}
