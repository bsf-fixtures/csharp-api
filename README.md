# csharp-api

Build Software Factory fixture: a minimal ASP.NET Core service with xUnit
tests, for the C# rule pack.

The subject is sensor readings, chosen so the rules worth enforcing are about
honesty with absent data rather than style: a mean over an empty window is
`null` and not zero, because zero degrees is a temperature; an implausible
reading is refused rather than logged and stored, because a mean that
includes a disconnected sensor's −127 is worse than a gap.

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Not verified where this was written

The container this fixture was written in has no .NET SDK, so these commands
have not been run against it here — the same limit that leaves the C# rule
pack marked `verification: documented_not_run`. Its CI workflow runs them on
a runner that does have one, which is where the claim is actually tested.

## What the factory should find here

- `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, so the
  compiler is already a gate and the rule pack's baseline has something to
  ratchet against.
- The tests state the two decisions a change could quietly reverse: null for
  an empty window, and refusing rather than storing an implausible reading.
