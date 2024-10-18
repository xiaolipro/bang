using Fake.Helpers;
using Fake.SyncEx;
using Microsoft.Extensions.Options;

namespace Fake.IdGenerators.Snowflake;

public class SnowflakeIdGenerator : LongIdGeneratorBase
{
    private readonly long _workerId;

    private long _sequence; // Serial number used per unit time
    private readonly long _sequenceMask; // sequence max

    private readonly long _startTimestamp;
    private long _lastTimestamp = -1L;

    private readonly SnowflakeIdGeneratorOptions _options;

    public SnowflakeIdGenerator(IOptions<SnowflakeIdGeneratorOptions> options, IWorkerProvider workerProvider)
    {
        _options = options.Value;
        _workerId = SyncContext.Run(workerProvider.GetWorkerIdAsync);
        _sequenceMask = ~(-1 << _options.SequenceBits);
        _startTimestamp = new DateTimeOffset(_options.StartTime).ToTimestamp(_options.TimestampType);
        if (_workerId > _options.MaxWorkerId || _workerId < 0)
        {
            throw new ArgumentException($"Worker ID can't be greater than {_options.MaxWorkerId} or less than 0");
        }
    }

    public override long Generate()
    {
        lock (this)
        {
            long currentTimestamp = GetCurrentTimestamp();

            if (currentTimestamp < _lastTimestamp)
            {
                throw new FakeException(
                    $"InvalidSystemClock: Clock moved backwards. Refusing to generate id for {_lastTimestamp - currentTimestamp} milliseconds");
            }

            if (_lastTimestamp == currentTimestamp)
            {
                _sequence = (_sequence + 1) & _sequenceMask;
                if (_sequence == 0) // 单位时间的序列号发光了
                {
                    currentTimestamp = TilNextTimestamp(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0L;
            }

            _lastTimestamp = currentTimestamp;

            return NextId(currentTimestamp - _startTimestamp);
        }
    }

    protected virtual long TilNextTimestamp(long lastTimestamp)
    {
        var currentTimestamp = GetCurrentTimestamp();
        while (currentTimestamp <= lastTimestamp)
        {
            currentTimestamp = GetCurrentTimestamp();
        }

        return currentTimestamp;
    }

    protected virtual long GetCurrentTimestamp() =>
        new DateTimeOffset(DateTime.UtcNow).ToTimestamp(_options.TimestampType);

    protected virtual long NextId(long deltaSeconds)
    {
        return (deltaSeconds << _options.SequenceBits + _options.WorkerIdBits)
               | (_workerId << _options.SequenceBits)
               | _sequence;
    }
}