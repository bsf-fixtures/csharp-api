<!--
  How to build, run, test and verify this app. Written by setup's phase 2,
  with every command tested rather than copied from a README.

  This is the most load-bearing context file: a verifier agent that cannot
  start the app cannot verify anything, and then the factory produces
  confident unverified PRs.

  There is no app. Every command below is empty for that reason, and each one
  says what was run to establish it. Nothing here is copied from a README —
  the README is two lines and describes no commands.
-->

# Runbook — csharp-api

**There is nothing to build, run or test yet.** The repository contains no
project file and no source. Until a change adds one, the sandbox tier is
`static` and no role can claim a change was exercised by running it.

## Build

```bash
# none — there is no project to build
```

`dotnet build` was run in a clean directory with no project file and exited 1
with `MSBUILD : error MSB1003: Specify a project or solution file.` The same
is true at the repository root: `git ls-files` returns no `.csproj`, `.sln`,
`.slnx`, `Directory.Build.props` or `global.json`.

The toolchain itself is ready, so a build command is a matter of adding a
project and nothing else. `dotnet` is at `/usr/bin/dotnet`; `dotnet
--list-sdks` reports 8.0.134, 8.0.206, 8.0.319, 8.0.428, 9.0.124, 9.0.205,
9.0.321, 10.0.115, 10.0.204, 10.0.303, 10.0.404 and 11.0.100, with
`Microsoft.AspNetCore.App` runtimes from 8.0 through 11.0. `dotnet --version`
resolves to 11.0.100, so the first change that adds a project should pin the
target with a `global.json` rather than inherit whichever SDK the runner
happens to ship.

Once a project exists the build command will be `dotnet build` (or
`dotnet build <solution>.sln`), but **it is not that yet** — writing it into
the sandbox before a project exists would make the manifest declare a command
that fails. Adding a project makes this sandbox inadequate on purpose; file
an inadequacy report so `factory-setup` runs as an amendment and this runbook
is rewritten with commands that were actually run.

## Run

```bash
# none — there is no host, entry point or listening port
```

The app is then at nowhere. `app_url` in the sandbox manifest is `null`:
nothing serves HTTP, so the trial has no URL to request and no page to
screenshot. This is precisely what holds the tier at `static` rather than
`renders`.

## Test

```bash
# none — there is no test project
```

`dotnet test` in a directory with no project exits 1 with the same MSB1003
error. No test project, test runner package or test framework is referenced
anywhere in the repository. `.factory/guardrails/coverage.json` is
correspondingly zeroed — `high_pct: 0`, `floor_pct: 0` — so there is no
coverage history for a change to be measured against, and `bsf gates
coverage` has nothing to compare.

## Verify

How to see for yourself that the app works, as a developer would:

You cannot, yet. There is no app to see. For a change against this
repository as it stands today, the only honest verification is reading it:

1. `git ls-files` and `git diff` — read every changed file in full. This is
   the whole of what the sandbox can exercise.
2. For JSON the factory owns, validate rather than eyeball it:
   `.factory/bin/bsf sandbox check --file .factory/sandbox/manifest.json`
   validates the sandbox manifest against the runner's schema.
3. For a change to a role, check that the `.md` prompt and its
   `.lock.yml` were regenerated together; a prompt edited without
   recompiling its lock file is a silent no-op.
4. **If the change adds C# code, stop.** You cannot compile it, run its
   tests, or start it. Do not report a verdict of verified. File an
   inadequacy report naming the paths — they are listed under
   `capabilities.not_exercised` in the sandbox manifest — so the sandbox is
   amended before anything claims to have exercised them.

## Seed data

```bash
# none — there is no store to seed
```

No database, cache, object store or fixture loader exists. `commands.seed` in
the sandbox manifest is `null`.

## Commands that change persistent state

These are kept out of the build path on purpose. Each one writes to something
that outlives the process, so running it against the wrong target is the kind
of mistake that cannot be undone by reverting a commit.

| Command | Writes to | Only when |
| --- | --- | --- |

None. Nothing in this repository writes to anything that outlives the
process. The sandbox has no services and no credentials, so there is no
target to write to even by accident — which is the point: a variable that is
absent makes the app fail loudly rather than quietly reach something real.

## Services this app needs

| Service | How it starts | Reached at |
| --- | --- | --- |

None. `services` in the sandbox manifest is an empty array. No code reads a
connection string, because no code exists.

## The sandbox's environment

Built from this list, and from nothing else. It does not inherit a
developer's environment and it does not read any `.env` file, because a
documented build command that runs a migration would otherwise migrate
whatever a developer's credentials happen to point at.

| Variable | Value in the sandbox |
| --- | --- |

None. `env` in the sandbox manifest is an empty array, and the environment is
closed, so the sandbox supplies no variables at all. Setup searched for every
place the code reads configuration — environment variables, `appsettings*.json`,
`launchSettings.json`, an options class, a `requireEnv`-style helper — and
found none, because there is no code. The first change that reads
configuration must add the variable here and to the manifest, or it will be
absent at run time.
