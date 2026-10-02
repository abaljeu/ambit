# Directory reconcile owner

2026-10-01. Alan named the operation and the owner. This note records that edit.

## 1. Result

The operation is Directory reconcile. The definition is [Parse thread architecture](../arch.md) §2 Module map, item 1 **Directory reconcile**. [core-refinement architecture](../../core-refinement/arch.md) §5 item 10 **Directory reconcile** is the deferred pointer. [01 — Directory body home](../issues/01-directory-parse-body-home.md) was only commented. Status stays done.

## 2. Files changed

1. [GLOSSARY](../../../GLOSSARY.md) — **Directory reconcile** is the scan of a directory that updates that Directory Node's Body. _Avoid_: Directory Parse body. **Body** is unchanged.
2. [Parse thread map](../map.md) — Decisions so far item 1 names Directory reconcile and the definition section.
3. [Parse thread spec](../spec.md) — the operation name is Directory reconcile. Directory body means the nodes.
4. [Parse thread architecture](../arch.md) — §2 Module map, item 1 **Directory reconcile** holds the definition. One sentence says a scan of the `.amb` file would be Directory parse, and that scan is not this operation.
5. [Parse thread project](../project.md) — dated note, 2026-10-01. Stage stays arch.
6. [core-refinement architecture](../../core-refinement/arch.md) — §5 Axis-write mechanics, item 10 **Directory reconcile** only. The walk, missing File Nodes, and disk-newer Unparsed stay. Core stacks, axes, locks, mailbox, and the core loop stay here.
7. [01 — Directory body home](../issues/01-directory-parse-body-home.md) — one comment. The Answer text is the earlier lock. Status stays done.
8. [Parse and persist](../../../doc/current/parse-persist.md) — the open claim says Directory reconcile. Parse thread owns it. The claim does not link into plan/. [doc/current CONTEXT](../../../doc/current/CONTEXT.md) says plans link to the claim home.

## 3. Left alone

[arch-match-core-and-ops](arch-match-core-and-ops.md) still says Directory Parse body. [02 — Structure-match Directory Load](../issues/02-structure-match-directory-load.md) and [03 — Workspace Load after incoming files](../issues/03-workspace-load-after-incoming-files.md) were not edited. No commit. No coding tickets. Stage stays arch.
