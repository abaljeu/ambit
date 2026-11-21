# Cursor Cloud

Linux cloud agent VM for this repository.

## Toolchain

Agents start from the environment Build that ran `install` in [[.cursor/environment.json]]. That Build puts the .NET 10 SDK, the Fable local tool, and PostgreSQL 17 on disk. `start` brings Postgres up on 127.0.0.1:5432.

Connection strings match the compose snippet in [[doc/reference/postgres-environments.md]] (user `gambol`, password `gambol_dev`). `TEST_DB_CONNECTION_STRING` is `Host=localhost;Database=gambol_test;Username=gambol;Password=gambol_dev`. `DB_CONNECTION_STRING` is `Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev`.

If `dotnet` is missing during a run, the Build is missing or stale. If Postgres refuses connections on 127.0.0.1:5432, `start` failed or the Build is stale. Those are environment failures to report; the run does not install the SDK or Postgres, and a missing toolchain is not a skipped build. Build / test gate: [[.agents/rules/core-agent-behavior.md]].

## Runtime

Cloud git procedure (disposable branch, staging drop): [[.agents/skills/cloud-agent-git/SKILL.md]]. Desktop places stay [[.agents/skills/git-protocol/SKILL.md]].

## Chat Window

When you name a repo file or issue in chat, write one Markdown link whose target is a clickable `https://github.com/abaljeu/life/blob/<ref>/…` URL (current branch tip, or `ready` when the path exists there). Put the path in that same link. Relative paths and `file://` URLs are not clickable in this harness — do not use them as the link target.
