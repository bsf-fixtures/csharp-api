<!--
  What has gone wrong before, and what was learned. Seeded by setup from the
  mined corrections, then added to by the scheduled review.

  This file is for failures whose lesson could not be turned into a check. A
  correction that became a rule does not belong here: the rule is the record,
  and repeating it in prose means two things to keep in step.
-->

# History — csharp-api

This repo has one commit (`906a13d`, 2026-11-21, "Upgrade the factory to
build-software-factory ecee54e (#8)"), no merged pull requests behind it, and
no factory run has produced anything yet.
`.factory/guardrails/candidates.json` is empty, so no corrections have been
mined from anyone's sessions. There is therefore almost no history to record,
and the tables below say that rather than inventing entries.

## Lessons

| What happened | What to do instead | Where it came from |
| --- | --- | --- |
| The README documented `dotnet restore`, `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` while stating plainly that none of them had ever been run — the container the fixture was written in had no .NET SDK. A documented command nobody has executed is a guess. | Run a command before writing it down, and when you cannot, say which commands are unverified and why, where a reader will see it. The README did exactly that, which is why this is a lesson and not a failure. | `README.md`, section "Not verified where this was written"; resolved by setup's sandbox episode, 2026-11-21 |
| `dotnet restore` crashed with `System.IO.DirectoryNotFoundException: Could not find a part of the path '/run/user/1001'` inside NuGet's first-run migration. The cause was not the repo: the agent container exported `XDG_RUNTIME_DIR=/run/user/1001` without creating it, and a named mutex needs that directory. | Before blaming the project for a toolchain crash, check what the environment claims exists. Unset `XDG_RUNTIME_DIR` rather than trying to create it — the directory is root-owned. The sandbox never sets it, so this cannot happen on the runner. | Setup's sandbox episode, attempt 2, 2026-11-21; recorded in the runbook |

## Problem classes already eliminated

Kept so nobody proposes a rule that exists, and so a regression is
recognisable as one.

| Problem class | How it was eliminated | When |
| --- | --- | --- |
| A compiler or analyzer warning accumulating unfixed | `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild` and `AnalysisLevel latest-recommended` in `src/Readings/Readings.csproj`. The csproj carries the reason inline: "A warning that is not an error is a warning nobody fixes." Measured at 0 warnings, 0 errors. | Before the first commit; verified 2026-11-21 |
| A null dereference from an unchecked public argument | `<Nullable>enable</Nullable>` in both projects, plus `.editorconfig` raising `CA1062` to `warning`, which `TreatWarningsAsErrors` makes fatal. | Before the first commit |
| Formatting drift | `.editorconfig` with `root = true`, enforced by `dotnet format --verify-no-changes` as the last step of `.github/workflows/ci.yml`. Exits 0 on the current tree. | Before the first commit; verified 2026-11-21 |
| A mean over an empty window silently reported as `0` | `MeanCelsius` returns `double?`, the route maps `null` to `204`, and `MeanIsNullForAnEmptyWindowRatherThanZero` fails if that changes. | Before the first commit |
| An implausible reading being logged and stored anyway | `Reading.IsPlausible` with the range as documented constants, `TryAdd` returning `false`, the route answering `400`, and `RefusesAReadingFromALooseSensor` guarding it. | Before the first commit |
| The same reading counted twice | `Reading` is a `record`, so `List.Contains` deduplicates by value; `TheSameReadingTwiceIsStoredOnce` guards it. | Before the first commit |

## Escalations

Work the factory could not finish, and why. A pattern across several of these
is a signal about the factory rather than about the work.

| Work item | Where it stopped | Cause |
| --- | --- | --- |
| — | — | — |

No factory run has been escalated, because none has run. The first entries
here will come from the roles themselves.

## Known gaps, not yet failures

Not lessons — nothing has gone wrong on account of these — but they are where
a failure would come from first, so they are worth knowing before one does.

- **`Program.cs` has no automated test.** Routing, model binding and JSON
  serialisation are checked only by running the app. A change to a route
  passes `dotnet test` whether or not it works.
- **No coverage is measured.** Neither project references a coverage
  collector, and `.factory/guardrails/coverage.json` has `high_pct` and
  `floor_pct` at `0`, so the coverage half of gate 2 reads nothing.
- **No test exercises concurrency.** `TryAdd` takes a lock and the map is a
  `ConcurrentDictionary`, but every test runs on one thread with its own
  store, so a race would pass.
