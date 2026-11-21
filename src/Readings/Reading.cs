namespace Readings;

/// <summary>A single sensor reading. A record, so two readings with the same
/// values are equal — the service deduplicates on that.</summary>
public sealed record Reading(string Sensor, DateTimeOffset At, double Celsius)
{
    /// <summary>Readings outside this range are rejected rather than stored.
    /// A sensor that has come loose reports -127, and a mean that includes it
    /// is worse than a gap.</summary>
    public const double MinCelsius = -80;
    public const double MaxCelsius = 80;

    public bool IsPlausible => Celsius is >= MinCelsius and <= MaxCelsius;
}
