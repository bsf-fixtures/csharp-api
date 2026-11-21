<!--
  What has gone wrong before, and what was learned. Seeded by setup from the
  mined corrections, then added to by the scheduled review.

  This file is for failures whose lesson could not be turned into a check. A
  correction that became a rule does not belong here: the rule is the record,
  and repeating it in prose means two things to keep in step.

  There was nothing to mine. `main` has one commit and the repository has no
  application code, so there are no reverts, no fix-ups, no bug-fix commits
  and no review corrections to learn from. The tables are empty for that
  reason, not because setup skipped the step.
-->

# History — csharp-api

## Lessons

| What happened | What to do instead | Where it came from |
| --- | --- | --- |

None mined. `git log` on `main` returns a single commit — "Upgrade the
factory to e29d8e0 (#4)" — which installs the factory and touches no
application code. There are no revert commits, no commits whose subject
matches a fix pattern, and no closed pull requests against application code
to read corrections out of.

One observation from setup is worth carrying, though it is not yet a failure:
**this repository's sandbox cannot exercise anything.** The tier is `static`
because no project exists. The predictable way for this to go wrong is a role
treating "the gates passed" as "the change works" — every gate here passes
vacuously. `bsf gates static` compares against an empty rule set, `bsf gates
coverage` against a 0% floor, and `bsf gates tests` has no tests to run. Read
`capabilities.not_exercised` in `.factory/sandbox/manifest.json` before
believing a green result means anything.

## Problem classes already eliminated

Kept so nobody proposes a rule that exists, and so a regression is
recognisable as one.

| Problem class | How it was eliminated | When |
| --- | --- | --- |

None. No problem class has been eliminated in this repository, because none
has yet occurred. The empty `rules` object in
`.factory/guardrails/baselines.json` is the honest record of that: no rule
has been adopted, so no class of problem is being held down by one.

Do not read the factory's own protections as entries here. `policy.yaml`
protecting `.factory/bin/**` or `.github/workflows/**` is a boundary the
factory shipped with, not a problem this repository hit and fixed.

## Escalations

Work the factory could not finish, and why. A pattern across several of these
is a signal about the factory rather than about the work.

| Work item | Where it stopped | Cause |
| --- | --- | --- |
| Setup phase 2, round 1 (this run) | Produced a valid manifest, but at tier `static` rather than `renders` or `build_and_test`. | There is no application. The repository holds `README.md`, `.factory/` and `.github/workflows/` and nothing else; with no project file, `dotnet restore`, `dotnet build` and `dotnet test` all exit 1 with MSB1003. Setup cannot resolve this by trying harder — the fix is a change that adds a project, followed by an amendment episode. Not a factory failure, and not a reason to spend further rounds. |

The first inadequacy report this repository should expect is the one filed
the moment a pull request adds a `.csproj`: that change touches paths listed
under `capabilities.not_exercised`, so it cannot be verified by running it,
and the sandbox must be amended before any role claims otherwise.
