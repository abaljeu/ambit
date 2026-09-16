Windows 10. GIT Bash shell. Use it.

Your working directory is the project root. Do not use `cd`. Do not change directories.
Use relative paths such as ./src. Do not use absolute paths.

## Git

Follow [[.agents/skills/git-protocol/SKILL.md]]. Terms: [[CONTEXT.md]].

The Desktop agent does not run remotes unless the user asks (manual approval). Remotes are [[.agents/skills/git-share/SKILL.md]], which the human invokes.

Read-only local git (`status`, `diff`, `log`, `rev-parse`, `check-ignore`, etc.) is always fine.

Do not run VSCode Tasks.

Issues are local Markdown under `plan/` — see [[doc/agents/issue-tracker.md]]. Do not file or sync issues on GitHub/GitLab.

Never override OutputPath/BaseIntermediateOutputPath (e.g. bin-verify/obj-verify); use normal Debug bin/obj. If outputs are locked, report and stop — do not redirect build output.

## Cursor Cloud Agents

Cloud agents for this repository start from the environment Build that ran `install` in [[.cursor/environment.json]]. That Build puts the .NET 10 SDK, the Fable local tool, and PostgreSQL 17 on disk. `start` brings Postgres up on 127.0.0.1:5432.

Connection strings match the compose snippet in [[doc/reference/postgres-environments.md]] (user `gambol`, password `gambol_dev`), not the desktop postgres/postgres note. `TEST_DB_CONNECTION_STRING` is `Host=localhost;Database=gambol_test;Username=gambol;Password=gambol_dev`. `DB_CONNECTION_STRING` is `Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev`.

If `dotnet` is missing during a run, the Build is missing or stale. If Postgres refuses connections on 127.0.0.1:5432, `start` failed or the Build is stale. Stop and report an environment failure. Do not install the SDK or Postgres during the run. Do not treat a missing toolchain as a skipped build. See the Build / test toolchain gate in [[.agents/rules/core-agent-behavior.md]].

The linter is never able to handle project edits until the environment reloads. Don't let this stop you.

Occasionally the first time you send a command, the tool will delete the first character of your command. If so, just send it again.
