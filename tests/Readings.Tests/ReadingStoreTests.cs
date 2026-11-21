using Readings;

namespace Readings.Tests;

public class ReadingStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private static Reading At(int minutesAgo, double celsius = 20) =>
        At("boiler", minutesAgo, celsius);

    private static Reading At(string sensor, int minutesAgo, double celsius = 20) =>
        new(sensor, Now.AddMinutes(-minutesAgo), celsius);

    [Fact]
    public void StoresAPlausibleReading()
    {
        var store = new ReadingStore();
        Assert.True(store.TryAdd(At(0)));
        Assert.Single(store.For("boiler"));
    }

    [Fact]
    public void RefusesAReadingFromALooseSensor()
    {
        // -127 is what a disconnected sensor reports. A mean that includes it
        // is worse than a gap.
        var store = new ReadingStore();
        Assert.False(store.TryAdd(At(0, -127)));
        Assert.Empty(store.For("boiler"));
    }

    [Fact]
    public void TheSameReadingTwiceIsStoredOnce()
    {
        var store = new ReadingStore();
        store.TryAdd(At(5));
        store.TryAdd(At(5));
        Assert.Single(store.For("boiler"));
    }

    [Fact]
    public void ReturnsNewestFirst()
    {
        var store = new ReadingStore();
        store.TryAdd(At(30));
        store.TryAdd(At(1));
        Assert.Equal(Now.AddMinutes(-1), store.For("boiler")[0].At);
    }

    [Fact]
    public void LatestIsTheNewestReadingPerSensor()
    {
        var store = new ReadingStore();
        store.TryAdd(At("boiler", 30, 18));
        store.TryAdd(At("boiler", 2, 22));
        store.TryAdd(At("attic", 10));

        var latest = store.Latest();

        Assert.Equal(2, latest.Count);
        var boiler = Assert.Single(latest, r => r.Sensor == "boiler");
        Assert.Equal(Now.AddMinutes(-2), boiler.At);
        Assert.Equal(22, boiler.Celsius);
    }

    [Fact]
    public void LatestIsOrderedBySensorName()
    {
        var store = new ReadingStore();
        store.TryAdd(At("cellar", 1));
        store.TryAdd(At("attic", 2));
        store.TryAdd(At("boiler", 3));

        Assert.Equal(["attic", "boiler", "cellar"], store.Latest().Select(r => r.Sensor));
    }

    [Fact]
    public void LatestOmitsASensorWithNothingStored()
    {
        // A sensor whose only report was refused has no reading, so inventing
        // an entry for it would report a failed sensor as a working one.
        var store = new ReadingStore();
        Assert.False(store.TryAdd(At("shed", 0, -127)));
        Assert.Empty(store.Latest());
    }

    [Fact]
    public void MeanIsNullForAnEmptyWindowRatherThanZero()
    {
        // Zero degrees is a temperature, so zero cannot stand for "nothing".
        var store = new ReadingStore();
        store.TryAdd(At(120));
        Assert.Null(store.MeanCelsius("boiler", TimeSpan.FromMinutes(30), Now));
    }

    [Fact]
    public void MeanCoversOnlyTheWindow()
    {
        var store = new ReadingStore();
        store.TryAdd(At(10, 20));
        store.TryAdd(At(20, 30));
        store.TryAdd(At(300, 100 - 100));
        Assert.Equal(25, store.MeanCelsius("boiler", TimeSpan.FromMinutes(60), Now));
    }
}
