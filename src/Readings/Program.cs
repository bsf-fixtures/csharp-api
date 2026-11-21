using Readings;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ReadingStore>();

var app = builder.Build();

app.MapPost("/readings", (Reading reading, ReadingStore store) =>
    store.TryAdd(reading)
        ? Results.Accepted()
        : Results.BadRequest(new { error = $"{reading.Celsius}C is outside the plausible range" }));

app.MapGet("/sensors/{sensor}/readings", (string sensor, ReadingStore store) =>
    Results.Ok(store.For(sensor)));

app.MapGet("/sensors/{sensor}/mean", (string sensor, int minutes, ReadingStore store) =>
{
    var mean = store.MeanCelsius(sensor, TimeSpan.FromMinutes(minutes), DateTimeOffset.UtcNow);
    return mean is null ? Results.NoContent() : Results.Ok(new { sensor, minutes, mean });
});

app.Run();

/// <summary>Named so the test project can reference it.</summary>
public partial class Program;
