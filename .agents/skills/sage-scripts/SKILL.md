---
name: sage-scripts
description: >-
  Run repo shell scripts as the script path so Cursor auto-approves them, and
  call SageFS MCP unattended HTTP routes through scripts/sagefs-*.sh instead of
  curl. Use when running a .sh file, when a command would be prefixed with bash
  or sh, when SageFS MCP says a confirmation could not be asked and names a
  POST route, or when a curl to 127.0.0.1 port 37749 (hard-reset, reset,
  sessions/stop, stop-app, workflow, local-data/clear) is about to be composed.
---

# Shell scripts

SageFS is MCP. Call the MCP tool. These scripts are the daemon's unattended HTTP routes: the same job as a confirm-tier tool, on the MCP port, for a caller with nobody to answer the prompt.

## How a script runs

The command is the script path from the repo root, then its arguments.

- `scripts/gitstatus.sh`
- `scripts/sagefs-hard-reset.sh 967c9074 true`

That path is what auto-approve matches. `bash`, `sh`, and `bash -lc` are a different command.

One Shell command. The working directory is the repo root.

## SageFS routes

Host is `127.0.0.1`. Port is `SAGEFS_MCP_PORT`, otherwise `37749`. A session id is 8 lowercase hex characters.

- `hard_reset_fsi_session` — `POST /hard-reset` — `scripts/sagefs-hard-reset.sh <sessionId> [true|false]` (`true` rebuilds)
- `reset_fsi_session` — `POST /reset` — `scripts/sagefs-reset.sh <sessionId>`
- `stop_session` — `POST /api/sessions/stop` — `scripts/sagefs-stop-session.sh <sessionId>`
- `stop_app` — `POST /api/sessions/<id>/stop-app` — `scripts/sagefs-stop-app.sh <sessionId>`
- `switch_workflow` — `POST /api/sessions/<id>/workflow` — `scripts/sagefs-workflow.sh <sessionId> <workflow>`
- `manage_local_data` — `POST /api/local-data/clear?store=` — `scripts/sagefs-clear-local-data.sh <friction|cohort|all>`

Workflow words the daemon accepts:

- `interactive`, `repl`, `normal`
- `hotreload`, `weblive`, `live`, `web`
- `livetesting`, `live-testing`, `testing`, `test`

`.cursor/hooks/allow-sagefs-sh.sh` allows those six commands when the arguments match. A raw `curl` to the daemon stays on the approval prompt.
