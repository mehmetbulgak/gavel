namespace Gavel.TestKit;

public sealed class TestClock : TimeProvider
{
    private long _offsetTicks;

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow().AddTicks(Interlocked.Read(ref _offsetTicks));

    public void Advance(TimeSpan by) => Interlocked.Add(ref _offsetTicks, by.Ticks);

    public void Reset() => Interlocked.Exchange(ref _offsetTicks, 0);
}
