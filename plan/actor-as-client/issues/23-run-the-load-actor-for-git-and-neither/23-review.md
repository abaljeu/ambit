# 23 — Run the Load actor for Git and Neither

Ticket: [[23-run-the-load-actor-for-git-and-neither-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Judgement** — Duplicated Code. The same `Order.StartActor` scan for `actorName = "github-load"` is copied in `tests/Server.Tests/LoadSaveCommandTests.fs` at lines 838, 934, 982, and 1069, and in `tests/Server.Tests/ParseThreadLoadTests.fs` at line 263. Quote: `order.actorNodeId = dirId && order.actorName = "github-load"`.
- **Judgement** — Duplicated Code. `runSaveBody` (`src/Server/GithubTransportActor.fs` lines 366–388) and `runLoadBody` (lines 390–409) repeat the `graphAtEvent` then `workspaceFromFocus` match. Quote: `match workspaceFromFocus dataDir current.graph focusId with`.

## 2. Spec

The project folder has no `spec.md`. These items score the diff against [23 — Run the Load actor for Git and Neither](23-run-the-load-actor-for-git-and-neither-ticket.md) and the owner instruction.

- **Partial** — `runLoadChoice` in `src/Server/GithubTransportActor.fs` lines 223–224 calls `runLoadOn` for every `LoadSubject.Workspace`. The Git arm is a work tree with a Git remote. A Workspace work tree with no remote stays on that pull. The Neither arm is `runNeither`.
- **Partial** — `runGitOrNeither` in `src/Server/GithubTransportActor.fs` lines 216–217 maps every `Error` from `WorkspaceGit.remoteExists` to `runNeither`. A directory that is not a repository fails `git remote`, and that case is Neither. Any other git failure takes the same arm. `finishLoadOn` can then return `Ok`, and `finishActor` posts `ActorSucceeded`. On `runLoad`, a failed pull is the actor result (lines 186–187).

## 3. Arch

- No findings.

## 4. Tests

- `tests/Server.Tests/GithubTransportActorTests.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/ParseThreadLoadTests.fs` exist. This diff leaves `GithubTransportActorTests.fs` unchanged. The new Load cases are in `LoadSaveCommandTests.fs` and `ParseThreadLoadTests.fs`.
- `countingGit` in `tests/Server.Tests/LoadSaveCommandTests.fs` line 854 builds a `GithubTransportGit` value with no `fetchTracked` field. `scripts/agent-test.sh --full` stops there. Server.Tests does not compile, so the named tests do not run. Shared.Tests does not start.
- `scripts/check-test.sh 2924` prints `DONE` and `/tmp/ambit-full-suite-20261111-133809.log`. The log ends with `error FS0764: No assignment given for field 'fetchTracked' of type 'Gambol.Server.GithubTransportGit'` at `tests/Server.Tests/LoadSaveCommandTests.fs` line 854.

## 5. Miss

- **Partial** — `runLoadChoice` sends every Workspace to `runLoadOn` (`src/Server/GithubTransportActor.fs` lines 223–224). A Workspace work tree with no Git remote misses the Neither arm.
- **Partial** — `runGitOrNeither` treats every `remoteExists` error as Neither (`src/Server/GithubTransportActor.fs` lines 216–217). A git failure other than "not a repository" skips the pull and can stop as `ActorSucceeded`.
- The full suite fails to compile. `countingGit` omits `fetchTracked` at `tests/Server.Tests/LoadSaveCommandTests.fs` line 854 (`FS0764`).

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
