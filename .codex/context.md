# Environment Notes

- Always terminate lines with CRLF.
- In this workspace, `functions.shell_command` sometimes fails inside the default sandbox with:
  `windows sandbox: setup refresh failed with status exit code: 1`.
- When that happens, the same read/search command often works if rerun with
  `sandbox_permissions: "require_escalated"`.
- `rg` works once the command is allowed.
- PowerShell quoting is easy to get wrong for `rg` patterns with embedded quotes; prefer simpler patterns
  or single-quoted regex arguments.
- Do not edit compiler-generated Javascript files. Make source changes in the F# files instead.
