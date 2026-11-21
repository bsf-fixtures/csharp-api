<!--
  How to build, run, test and verify this app. Written by setup's phase 2,
  with every command tested rather than copied from a README.

  This is the most load-bearing context file: a verifier agent that cannot
  start the app cannot verify anything, and then the factory produces
  confident unverified PRs.
-->

# Runbook — csharp-api

## Build

```bash
dotnet restore
dotnet build --no-restore
```

`dotnet restore` is the sandbox's install step and `dotnet build
--no-restore` its build step, kept separate so a failure lands on one of
them. Run from the repo root: both resolve `Readings.sln`, which covers both
projects.

The build is a real gate, not a formality. `src/Readings/Readings.csproj`
sets `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild` and `AnalysisLevel
latest-recommended`, so an analyzer warning or a code-style violation fails
the build outright. Measured here: 0 warnings, 0 errors.

No SDK is installed as part of the build. The runner's preinstalled .NET SDK
is used as-is; the repo targets `net9.0` and has no `global.json`, so the
newest installed SDK is selected. Verified against SDK 11.0.100 building
`net9.0` with 0 warnings, so a newer SDK's analyzer set does not trip the
strict settings. `.github/workflows/ci.yml` pins `9.0.x` through
`actions/setup-dotnet`; that step is CI provisioning and is deliberately not
part of the sandbox.

## Run

```bash
dotnet run --project src/Readings/Readings.csproj --no-build
```

The app is then at http://127.0.0.1:5080, and `app_url` —
http://127.0.0.1:5080/sensors/boiler/readings — is what a person lands on.
There is no root route: `GET /` returns 404.

The port comes from `ASPNETCORE_URLS`, not from the command. There is no
`Properties/launchSettings.json`, so without that variable the host binds its
built-in default and nothing answers on 5080.

The sandbox's `run` command wraps the line above: a readiness loop and three
seeding POSTs run in a background subshell, then the server is `exec`ed so it
becomes the process the sandbox supervises and a SIGTERM to the process group
stops both. If the seeding fails, the app still answers 200 with `[]`.

## Test

```bash
dotnet test --no-build
```

Six `[Fact]`s in `tests/Readings.Tests/ReadingStoreTests.cs`, all passing.
They cover every public member of `ReadingStore` and state the two decisions
the README says a change could quietly reverse: a mean over an empty window
is `null` and not `0`, and an implausible reading is refused rather than
stored.

What `dotnet test` does **not** cover: `Program.cs`. There is no
`WebApplicationFactory` test and no `Microsoft.AspNetCore.Mvc.Testing`
reference, so routing, model binding and JSON serialisation pass no test. A
change to a route has to be checked by running the app.

No coverage collector is configured in either project, so `dotnet test`
emits no coverage data.

Separately, and not part of `test`:

```bash
dotnet format --verify-no-changes
```

This is the lint gate `.github/workflows/ci.yml` runs last. It checks the
code against `.editorconfig`. It exits 0 on the current tree.

## Verify

How to see for yourself that the app works, as a developer would:

1. Build and start it, then wait for `Now listening on: http://127.0.0.1:5080`
   in the output.
2. Post a plausible reading and expect `202`:
   ```bash
   curl -i -X POST http://127.0.0.1:5080/readings \
     -H 'Content-Type: application/json' \
     -d "{\"sensor\":\"boiler\",\"at\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"celsius\":21.5}"
   ```
3. Post what a disconnected sensor reports and expect `400` with
   `{"error":"-127C is outside the plausible range"}` — not a `202`, and not
   a logged warning:
   ```bash
   curl -i -X POST http://127.0.0.1:5080/readings \
     -H 'Content-Type: application/json' \
     -d "{\"sensor\":\"boiler\",\"at\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"celsius\":-127}"
   ```
4. List the readings and check they come back newest first:
   ```bash
   curl http://127.0.0.1:5080/sensors/boiler/readings
   ```
   The JSON objects carry `sensor`, `at`, `celsius` **and `isPlausible`** —
   that last one is a public property of the `Reading` record, so it
   serialises whether or not anyone wanted it on the wire.
5. Ask for the mean over a window that holds readings and expect `200` with
   the mean:
   ```bash
   curl -i 'http://127.0.0.1:5080/sensors/boiler/mean?minutes=60'
   ```
6. Ask for the mean over a window that holds nothing and expect `204` with an
   empty body — **not** `200` with `0`:
   ```bash
   curl -i 'http://127.0.0.1:5080/sensors/unknown/mean?minutes=60'
   ```
7. Open http://127.0.0.1:5080/sensors/boiler/readings in a browser. Chromium
   renders the raw JSON; that page is what the sandbox screenshots.

Posting the same reading twice stores it once — `Reading` is a record, so
equality is by value and `ReadingStore.TryAdd` deduplicates on it. The second
POST still returns `202`.

## Seed data

```bash
for s in 25:18.5 15:20.0 5:21.5; do curl -sS -X POST "$ASPNETCORE_URLS/readings" -H 'Content-Type: application/json' -d "{\"sensor\":\"boiler\",\"at\":\"$(date -u -d "-${s%%:*} minutes" +%Y-%m-%dT%H:%M:%SZ)\",\"celsius\":${s##*:}}"; echo; done
```

Three readings for the `boiler` sensor at 25, 15 and 5 minutes ago, mean
20.0. The timestamps are relative on purpose: `GET /sensors/{sensor}/mean`
measures its window back from `DateTimeOffset.UtcNow`, so fixed timestamps
would fall outside any reasonable window and the mean would be `204`.

The app must already be running — this seeds over HTTP, because that is the
only way in. Nothing is written to disk, so seeding has to be redone after
every restart. The sandbox's `run` command does it automatically.

## Commands that change persistent state

These are kept out of the build path on purpose. Each one writes to something
that outlives the process, so running it against the wrong target is the kind
of mistake that cannot be undone by reverting a commit.

| Command | Writes to | Only when |
| --- | --- | --- |
| — | — | — |

There are none. The app has no database, no migration, no file store and no
outbound call to anything; every byte of state lives in a
`ConcurrentDictionary` that dies with the process. The only thing any command
here writes to disk is `bin/` and `obj/`, both gitignored.

## Services this app needs

| Service | How it starts | Reached at |
| --- | --- | --- |
| — | — | — |

None. No database, cache, queue, object store or mail catcher, and no third
party to stub. The sandbox starts no containers.

## The sandbox's environment

Built from this list, and from nothing else. It does not inherit a
developer's environment and it does not read any `.env` file, because a
documented build command that runs a migration would otherwise migrate
whatever a developer's credentials happen to point at.

| Variable | Value in the sandbox |
| --- | --- |
| `ASPNETCORE_URLS` | `http://127.0.0.1:5080` — the app's real origin here. Kestrel binds this; without it nothing answers where `app_url` points. |
| `ASPNETCORE_ENVIRONMENT` | `Development` — keeps the developer exception page and per-request logging on, so a failing request shows which endpoint and why. |
| `DOTNET_CLI_TELEMETRY_OPTOUT` | `1` — the SDK's first run otherwise posts usage telemetry the repo never asked for. |
| `DOTNET_NOLOGO` | `1` — keeps the first-run banner out of build output, so the tail of a failing build is the failure. |

All four are the app's own configuration (`kind: self`). There is no secret,
no credential and no third-party key anywhere in this repo, so the sandbox
holds none. Alongside these, the sandbox passes only `PATH`, `HOME`,
`TMPDIR`, `LANG`, `CI` and `RUNNER_TEMP`.

One trap, recorded because it cost a round here: this repo's `dotnet`
commands crash with `DirectoryNotFoundException: /run/user/<uid>` if
`XDG_RUNTIME_DIR` is set to a directory that does not exist — NuGet's
first-run migration opens a named mutex there. The sandbox never sets it, so
this does not happen on the runner; it bites in agent containers that export
it without creating it. Unset it rather than trying to create it.
