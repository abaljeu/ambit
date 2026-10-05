# Windows Cursor IDE

Windows 10 with GIT Bash. The Shell tool is already bash.

## Shell

For a repo script, the command string that works (and matches auto-approve) is the script path alone — e.g. `scripts/gitstatus.sh`. Same for other scripts under `scripts/` and `./tmp/`. Prefixing `bash`, `sh`, `bash -lc`, or another shell binary is a different command and breaks auto-approve.

DO not invoke `bash scripts/<name>` because that will trip up command approval.  Use `scripts/name` instead.

PowerShell that works is `pwsh.exe` (not `powershell.exe`). When pwsh or cmd needs escaping-heavy work, a short `.ps1` or `.cmd` file run as that path works; bash invokes it. One Shell `command:` at a time works more reliably than `&&` or `;` chains. `cmd /c` does not work well here.

## Paths

Working directory is the project root. Relative paths such as `./src` work. Absolute paths and `cd` are not how this environment is set up.

## Tooling

Git procedure: [[.agents/skills/git-protocol/SKILL.md]]. Terms: [[GLOSSARY.md]]. Remotes that need approval stay with [[.agents/skills/git-share/SKILL.md]] (human-invoked); this Desktop agent does not run remotes unless the user asks.

VSCode Tasks are not the agent command path.

Issues are local Markdown under `plan/` — [[doc/agents/issue-tracker.md]]. GitHub/GitLab issue filing is not used here.

Do not websearch.

Normal Debug `bin`/`obj` output paths work. Overriding OutputPath/BaseIntermediateOutputPath (e.g. bin-verify/obj-verify) is not the path; if outputs are locked, report and stop.

The linter cannot handle project edits until the environment reloads; work continues without waiting on the linter.

Occasionally the first Shell command loses its first character; sending the same command again works.

## Chat Window

Present file links as relative paths using []() syntax.