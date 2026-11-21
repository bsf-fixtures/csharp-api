using System.Collections.Concurrent;

namespace Readings;

/// <summary>Readings in memory, newest first per sensor.</summary>
public sealed class ReadingStore
{
    private readonly ConcurrentDictionary<string, List<Reading>> _bySensor = new();

    /// <summary>Stores a reading, or returns false when it is implausible.
    /// The caller decides what to do about it; this type does not log and
    /// carry on, which would hide a failing sensor.</summary>
    public bool TryAdd(Reading reading)
    {
        if (!reading.IsPlausible)
        {
            return false;
        }

        var list = _bySensor.GetOrAdd(reading.Sensor, static _ => []);
        lock (list)
        {
            if (list.Contains(reading))
            {
                return true;
            }

            list.Add(reading);
            list.Sort(static (a, b) => b.At.CompareTo(a.At));
        }

        return true;
    }

    public IReadOnlyList<Reading> For(string sensor) =>
        _bySensor.TryGetValue(sensor, out var list) ? [.. list] : [];

    /// <summary>The mean over a window, or null when the window holds
    /// nothing. Null rather than zero: zero degrees is a temperature.</summary>
    public double? MeanCelsius(string sensor, TimeSpan window, DateTimeOffset now)
    {
        var cutoff = now - window;
        var values = For(sensor).Where(r => r.At >= cutoff).Select(r => r.Celsius).ToList();
        return values.Count == 0 ? null : values.Average();
    }
}
