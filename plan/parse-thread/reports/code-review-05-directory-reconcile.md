# Code review 05 — Directory reconcile

Range: uncommitted changes vs HEAD. Scan command: `python .agents/skills/code-review/scripts/standards-scan.py`.

## 1. Standards

Scan printed two `BARE_ID` lines on [05 — Directory reconcile](../issues/05-directory-reconcile.md). Each line already names **Directory reconcile** beside the item number, so they do not break [refer by name](../../../.agents/rules/refer-by-name.md). `replaceBlockedByInaccessible` is 40 lines, which is within [F# source](../../../.agents/rules/fsharp-source.md). [History.fs](../../../src/Shared/History.fs) is 764 lines.

No standards findings.

## 2. Spec

1. **Apply guard** — [DocumentPartition.fs](../../../src/Shared/DocumentPartition.fs) `replaceBlockedByInaccessible` allows a Replace that adds or removes a File or Directory node under an Unparsed Directory or Workspace when the outline sibling list is unchanged. [05 — Directory reconcile](../issues/05-directory-reconcile.md) needs that so the create ops apply. The ticket does not name this guard.
2. **Parsed skip** — [ParseThread.fs](../../../src/Server/ParseThread.fs) skips a popped node whose parse state is Parsed, including a File Node. The previous thread always ran `planParseFile`. [Parse thread architecture](../arch.md) Uses item **Pop skip** states this rule.

## 3. Totals

Standards: 0 findings. Spec: 2 findings. Worst spec finding: the apply guard in [DocumentPartition.fs](../../../src/Shared/DocumentPartition.fs).
