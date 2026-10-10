# 1. Fix 21 — pull failure ends the actor

## 1. Bug

A failed git pull still posted the Workspace Node. `runLoad` called `CoreChanges.load` after `pullTracked` returned an error, then returned that error. The actor asked the mailbox to mark that node Unparsed and push it. Alan's correction: a failed pull ends the git actor. That path posts nothing.

## 2. Files changed

1. [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) — a failed pull returns that error and ends. A successful pull posts the Workspace Node through `CoreChanges.load`, then parses a focused File. `CoreChanges.load` stays. `markUnparsedThenPush` stays the backend of mailbox Load.
2. [GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs) — `git Load ends when the pull fails and posts nothing` records an empty push list, `ActorFailed` with the pull error, and no live actor row.
3. [21 — Git Load posts the Workspace](plan/github-transport/issues/21-git-load-posts-workspace.md) — comment **Pull error** now says a failed pull ends the git actor and posts no Workspace Node. The project spec and the architecture already say inform Core after the pull lands, so those files stay as written.
4. [github-transport](plan/github-transport/project.md) — `Actual:` is 16h45m. `Stage:` stays `build`.

## 3. Commands

1. `scripts/gitstatus.sh` — workplace `dev`.
2. `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ends when the pull fails"` — fail. The push list held one Workspace id. That was the red test.
3. `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~GithubTransportActorTests"` — pass. 9 passed, 0 failed.
4. `scripts/test.sh all` — pass. Server tests: 645 passed, 0 failed. Shared tests: 1870 passed, 1 skipped, 0 failed.
5. Client compile gate — not run. This change edits Server only.

## 4. Ticket status

**Status:** `coded` on [21 — Git Load posts the Workspace](plan/github-transport/issues/21-git-load-posts-workspace.md). The failed-pull path matches Alan's correction. Status is not set to `done`.

## 5. Time

1. Ticket — added `2026-10-10 30m — a failed pull ends the git actor (from chat)`. **Actual:** `2h`.
2. Project — `Actual:` is 16h45m.

## 6. Left for the human

1. Commit — no commit. Git protocol commits on `dev` only when Alan asks.
