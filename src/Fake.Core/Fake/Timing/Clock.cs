using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Fake.Timing;

public class Clock(IOptions<FakeClockOptions> options) : IFakeClock
{
    private readonly FakeClockOptions _options = options.Value;

    private readonly AsyncLocal<Stopwatch?> _stopwatch = new();

    public virtual DateTime Now => _options.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;
    public virtual DateTimeKind Kind => _options.Kind;

    public virtual DateTime Normalize(DateTime dateTime)
    {
        if (Kind == DateTimeKind.Unspecified || Kind == dateTime.Kind)
        {
            return dateTime;
        }

        return Kind switch
        {
            DateTimeKind.Local when dateTime.Kind == DateTimeKind.Utc => dateTime.ToLocalTime(),
            DateTimeKind.Utc when dateTime.Kind == DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, Kind)
        };
    }

    public virtual string NormalizeAsString(DateTime datetime)
    {
        return Normalize(datetime).ToString(_options.DateTimeFormat);
    }

    public virtual TimeSpan MeasureExecutionTime(Action action)
    {
        _stopwatch.Value ??= new Stopwatch();
        _stopwatch.Value.Start();
        action();
        _stopwatch.Value.Stop();
        var elapsed = _stopwatch.Value.Elapsed;
        _stopwatch.Value = null;
        return elapsed;
    }

    public virtual async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> task)
    {
        _stopwatch.Value ??= new Stopwatch();
        _stopwatch.Value.Start();
        await task();
        _stopwatch.Value.Stop();
        var elapsed = _stopwatch.Value.Elapsed;
        _stopwatch.Value = null;
        return elapsed;
    }
}