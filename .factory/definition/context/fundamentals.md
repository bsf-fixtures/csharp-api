<!--
  The conventions no check enforces. Everything here is something a script
  could not decide, which is why it is prose.

  The bar for a line in this file: it has to be a judgement. If a static rule
  could catch it, it belongs in .factory/guardrails/ instead, because a rule
  is reproducible and prose is something agents argue with.

  Each line carries where it came from, so a line nobody can justify can be
  removed later.
-->

# Fundamentals — csharp-api

## Dominant patterns

For each recurring concern, the pattern most of this codebase already uses.
Follow it rather than introducing a second way of doing the same thing.

| Concern | The pattern here | Source |
| --- | --- | --- |
| An operation that can legitimately be refused | A `bool TryX(...)` that returns `false` and leaves the decision to the caller. Not an exception, and explicitly not log-and-continue — the doc comment says so: "this type does not log and carry on, which would hide a failing sensor." | `src/Readings/ReadingStore.cs` (`TryAdd`) |
| Absent data | `null`, never a zero, an empty string or a sentinel. `MeanCelsius` returns `double?`; the route turns `null` into `204 No Content` rather than `200` with a number. The stated reason: zero degrees is a temperature. | `src/Readings/ReadingStore.cs` (`MeanCelsius`), `src/Readings/Program.cs` (`/sensors/{sensor}/mean`) |
| The HTTP surface | Minimal APIs mapped inline in `Program.cs` with a lambda per route, services resolved as lambda parameters. No controllers, no `IEndpointRouteBuilder` extension methods, no separate routing file. | `src/Readings/Program.cs` |
| Domain types | `sealed record` with positional parameters. Value equality is load-bearing, not incidental: deduplication in `TryAdd` is `list.Contains(reading)`, which only works because `Reading` is a record. | `src/Readings/Reading.cs`, `src/Readings/ReadingStore.cs` |
| Validation | A property on the type that owns the rule (`IsPlausible`), sitting next to the `const`s that define it. Not a validator class, not a data annotation, not a check at the route. | `src/Readings/Reading.cs` |
| Returning a collection | `IReadOnlyList<T>`, as a copy made with a collection expression (`[.. list]`), never the internal `List<T>`. An absent sensor yields `[]`, not `null`. | `src/Readings/ReadingStore.cs` (`For`) |
| Null arguments on public members | `ArgumentNullException.ThrowIfNull(x)` as the first statement. `.editorconfig` sets `CA1062` to `warning`, and `TreatWarningsAsErrors` makes that fail the build — so this is the one item in this table a rule does enforce; it is here because the *placement* convention is not checked. | `src/Readings/ReadingStore.cs`, `.editorconfig` |
| Time | Taken as a `DateTimeOffset now` parameter by anything that reasons about it. `DateTimeOffset.UtcNow` is read in exactly one place, the route, which is why the store's tests need no clock abstraction and no mocking library. | `src/Readings/ReadingStore.cs` (`MeanCelsius`), `src/Readings/Program.cs` |
| Concurrency | `ConcurrentDictionary` for the outer map, a plain `lock` on each per-sensor `List<T>` for mutation. One mechanism per level, no `SemaphoreSlim`, no `async` in the store at all. | `src/Readings/ReadingStore.cs` |
| Namespaces | File-scoped. `.editorconfig` sets `csharp_style_namespace_declarations = file_scoped:warning`, which `EnforceCodeStyleInBuild` turns into a build failure. | `.editorconfig`, every `.cs` file |
| Warnings | Every warning is an error. The csproj carries the reason as a comment: "A warning that is not an error is a warning nobody fixes." Do not suppress one to get a build green; fix it, or make the suppression a reviewed, commented `#pragma`. | `src/Readings/Readings.csproj` |
| Tests | Plain xUnit `[Fact]`s. No mocking library, no fluent assertion library, no fixtures, no `[Theory]` — the test project references only `xunit`, `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk`. A tiny private factory helper (`At(minutesAgo, celsius)`) builds the data. | `tests/Readings.Tests/Readings.Tests.csproj`, `tests/Readings.Tests/ReadingStoreTests.cs` |
| Test names | A sentence stating the decision, not the method: `MeanIsNullForAnEmptyWindowRatherThanZero`, `RefusesAReadingFromALooseSensor`. The name says what would be wrong if it failed. | `tests/Readings.Tests/ReadingStoreTests.cs` |
| Dependencies | The service project has **zero** `PackageReference` entries. Adding the first one to `src/Readings` is a real decision, not routine. | `src/Readings/Readings.csproj` |

## Product intent

This is a service for sensor readings, and the README states the point
plainly: the decisions worth protecting here are about **honesty with absent
or bad data**, not style.

Two of them are load-bearing, and both are stated in the code, in the tests
and in the README:

- **A mean over an empty window is `null`, not `0`.** Zero degrees is a
  temperature, so zero cannot stand for "nothing". Anything that would make
  the API return a number where it has no data — a default, a `?? 0`, a
  `Sum() / Max(count, 1)` — reverses this.
- **An implausible reading is refused, not stored.** A disconnected sensor
  reports −127C. A mean that quietly includes it is worse than a gap in the
  data, so `TryAdd` returns `false` and the route answers `400` with the
  value in the message. Logging it and storing it anyway is the failure mode
  being guarded against.

Where a change forces a choice between "always return something" and "say
you have nothing", this product says you have nothing. Where it forces a
choice between "accept dubious input" and "refuse it loudly", this product
refuses it.

The range itself (`MinCelsius = -80`, `MaxCelsius = 80`) is a judgement about
plausible air and water temperatures, not a hard physical limit. Changing it
is a product decision and needs saying out loud; it is not a tuning knob.

## Taste

Things a reviewer would ask for that no rule states.

- **Comments say why, never what.** Every comment in `src/` justifies a
  decision — why −127 is the number that matters, why a record and not a
  class, why `null` and not `0`. There is not one comment restating the line
  below it. A new comment that describes mechanics will read as noise here.
- **A constant that encodes a judgement carries the judgement with it.** The
  `MinCelsius`/`MaxCelsius` doc comment explains the loose-sensor case. A
  bare `const` with no reason would be out of place.
- **Public types and members that carry a decision get an XML doc comment**
  (`<summary>`). Obvious members (`For`) do not. Documenting everything and
  documenting nothing would both be wrong here.
- **A test comment states the consequence of the test failing**, not what the
  test does — "Zero degrees is a temperature, so zero cannot stand for
  'nothing'." Follow that when adding a test for a decision.
- **Nothing in the domain logs.** There is no logger injected anywhere, and
  `ReadingStore`'s refusal to log is deliberate and documented. Reaching for
  `ILogger<T>` inside `ReadingStore` would contradict a stated decision; if
  something needs recording, the route is where it would go.
- **No `async` where there is no I/O.** The store is entirely synchronous and
  the route lambdas are not `async`. Adding `Task`-returning methods that
  only ever complete synchronously would add ceremony for nothing.
- **Keep the service project dependency-free as long as it is honest to.**
  Three files with no packages is a property of this codebase worth noticing
  before adding one.

## Deliberate exceptions

Places that look like they break a rule and should stay as they are, with the
reason. Without this section, cleanup keeps rediscovering them.

| Place | Looks like | Why it stays |
| --- | --- | --- |
| `public partial class Program;` at the end of `src/Readings/Program.cs` | Dead code: an empty partial class nothing references. | It is there so a test project can name `Program` and host the app through `WebApplicationFactory<Program>`. The file says so: "Named so the test project can reference it." No such test exists yet, so a cleanup pass will find it unused — leave it; removing it closes off the HTTP tests this repo is missing. |
| `Reading.IsPlausible` appearing in the JSON response (`{"sensor":…,"celsius":21.5,"isPlausible":true}`) | An internal validation flag leaking onto the wire. | It is a public property of the record, so `System.Text.Json` serialises it; nothing in the repo asks for it or hides it, and no test asserts on it. Recorded because it is a fact of the current API shape that a change could break by accident — **not** because the repo states it is wanted. If it should not be on the wire, that is a product decision to take deliberately, not a tidy-up. |
| `store.TryAdd(At(300, 100 - 100));` in `MeanCoversOnlyTheWindow` | A literal that should just be `0`, or a leftover from editing. | It is the out-of-window reading, and `100 - 100` writes zero in a way the reader cannot skim past — the one value that must not be silently absorbed into the mean. No comment in the repo states this, so the intent is inferred from the surrounding test; treat it as load-bearing until someone who knows says otherwise, and do not "simplify" it without replacing it with a comment that makes the same point. |
| `.editorconfig` raising `CA1062` to `warning` when `TreatWarningsAsErrors` turns every warning into an error | A rule set to `warning` that is really an error — two files saying different things. | The pair is intentional: severity lives with the style config, escalation lives with the build config, so loosening either one is a visible, single-line change in a reviewable place. |
