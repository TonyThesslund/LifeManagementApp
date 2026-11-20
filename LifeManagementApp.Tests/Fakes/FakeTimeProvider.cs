namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// A clock that shows a set time. The local time zone is UTC, so <see cref="Now"/> is also the local time.
/// </summary>
public class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
	public DateTimeOffset Now { get; set; } = now;

	public override DateTimeOffset GetUtcNow() => Now;

	public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
