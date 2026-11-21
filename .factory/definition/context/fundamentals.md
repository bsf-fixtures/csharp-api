<!--
  The conventions no check enforces. Everything here is something a script
  could not decide, which is why it is prose.

  The bar for a line in this file: it has to be a judgement. If a static rule
  could catch it, it belongs in .factory/guardrails/ instead, because a rule
  is reproducible and prose is something agents argue with.

  Each line carries where it came from, so a line nobody can justify can be
  removed later.

  This repository has no application code, so it has no codebase conventions
  to record. What is below comes from the only two sources that exist: the
  README, and the factory's own definition. Nothing here is inferred from
  C# the repo does not contain.
-->

# Fundamentals — csharp-api

## Dominant patterns

For each recurring concern, the pattern most of this codebase already uses.
Follow it rather than introducing a second way of doing the same thing.

| Concern | The pattern here | Source |
| --- | --- | --- |

None. There is no application code, so no concern recurs and no pattern is
dominant. A pattern claimed here today would be a preference invented by
setup, not something the codebase already does.

The first change that adds code therefore gets to choose — and the choice is
load-bearing, because every later change will be told to follow it. The
scheduled review fills this table from what that code actually did. Until
then, a spec that needs a convention should state it in the plan and say it
is new, rather than citing this file.

What is settled, because the factory's own definition settles it:

| Concern | The pattern here | Source |
| --- | --- | --- |
| Changing a role's behaviour | The prompt lives in `.github/workflows/factory-role-<id>.md` and the compiled `.lock.yml` beside it; the `.factory/definition/roles/*.yaml` file records only the role's shape. Edit the prompt and recompile; never edit the lock file alone. | `.factory/definition/roles/*.yaml`: "the prompt lives with the workflow so the two cannot drift apart" |
| Enforcing a stated practice | A practice a static rule can enforce becomes a rule in `.factory/guardrails/` as well as a line in `practices.md`; one that takes judgement stays prose. | `.factory/definition/context/practices.md` header |
| Where a judgement goes | A judgement goes in prose (this file, `practices.md`); anything a script could decide goes in `.factory/guardrails/` instead, because a rule is reproducible. | This template's own header |

## Product intent

What this product is trying to be, where that would change a decision an
agent has to make.

The README states it in full: "Build Software Factory: Fixture of a C# API to
install factory on." That is the whole of the recorded intent — there is no
other prose, no design document, no issue and no commit message that says
more. The repository has one commit on `main`, "Upgrade the factory to
e29d8e0 (#4)".

Two things follow for a decision an agent has to make:

- **It is a fixture.** It exists to have the factory installed on it and to
  demonstrate that the factory works. That is a reason to prefer the
  ordinary, idiomatic shape of an ASP.NET Core API over anything clever: the
  point is that the machinery around it is exercised, not that the API is
  novel.
- **"C# API" is the only constraint on the shape, and nothing narrows it
  further.** Which .NET version, minimal APIs or controllers, whether there
  is a database, what the API is *of* — none of it is recorded anywhere.
  Setup did not choose, because choosing the architecture of a repository is
  not setup's decision to make. A work item that assumes one of these
  answers should say so explicitly and expect to be the thing that
  establishes it.

## Taste

Things a reviewer would ask for that no rule states.

- **Do not claim a change was verified by running it.** The sandbox tier is
  `static`: nothing compiles, nothing is tested and nothing starts. A verdict
  of verified, on anything, is false here until the sandbox is amended.
  Source: the sandbox manifest's `tier` and `capabilities.not_exercised`.
- **The first project sets the conventions for every one after it.** Because
  the tables above are empty, the first change that adds C# is making
  decisions out of all proportion to its size — layout, target framework,
  minimal APIs versus controllers, test framework, nullable and warning
  settings. Decide them deliberately and write them down, rather than
  letting them fall out of `dotnet new`. Source: there is nothing in the
  repository to follow.
- **Pin the SDK before relying on it.** Eleven SDKs are installed on the
  runner and `dotnet --version` resolves to 11.0.100, the newest. A project
  without a `global.json` silently moves when the runner image changes.
  Source: `dotnet --list-sdks` on the setup runner.
- **A prompt change that does not recompile its lock file does nothing.**
  It will pass review and change no behaviour, which is worse than failing.
  Source: the paired `.md`/`.lock.yml` files in `.github/workflows/`.
- **Prefer a check to a paragraph.** When a convention is settled, add it to
  `.factory/guardrails/` — an `.editorconfig` rule, an analyzer — rather than
  only writing it here. `baselines.json` currently has an empty rule set, so
  `bsf gates static` passes vacuously; every rule added is a gate that starts
  doing work. Source: `.factory/guardrails/baselines.json`, this template's
  header.

## Deliberate exceptions

Places that look like they break a rule and should stay as they are, with the
reason. Without this section, cleanup keeps rediscovering them.

| Place | Looks like | Why it stays |
| --- | --- | --- |
| `.factory/bin/runner.tgz` | A binary blob checked into source control, and the one file no linter reads. | Deliberate. It is packed "so that no linter, formatter or type checker in this repo reads it" (`.factory/bin/bsf`), and it is the program every gate runs. `policy.yaml` protects `.factory/bin/**` so a pull request cannot grade itself with a modified copy. Do not unpack, lint or reformat it. |
| `.github/workflows/*.lock.yml` | Enormous generated YAML with inlined JSON config and pinned action SHAs, duplicated across seven role files. | Compiled output from the `.md` prompt beside it. Editing it by hand, deduplicating it, or "tidying" the pinned digests breaks the guarantee that prompt and lock agree. Regenerate instead. |
| `.factory/guardrails/baselines.json`, `coverage.json` | Guardrails configured to enforce nothing: an empty rule set, a 0% coverage floor. | Correct for a repository with no code. They are not weakened defaults to be raised on sight — they are the true starting point, and `policy.yaml` makes them one-way, so any pipeline may lower a count or raise the floor as real code arrives. |
| The repository having no source at all | An empty project that someone forgot to push. | It is a fixture at its starting state, and setup confirmed against the GitHub API that the default branch really is `.factory/`, `.github/` and `README.md` — not a partial checkout. |
