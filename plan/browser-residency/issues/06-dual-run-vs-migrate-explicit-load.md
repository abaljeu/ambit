# 06 — Dual-run vs migrate explicit Load Fetch

**Type:** grilling
**Status:** needs-info
**Blocked by:** 01

## 1. Question

When does the user-facing Load command stop using the old Fetch path, and may it dual-run both paths for a while?

Auto growth and bootstrap use the edges-plus-Nodes package. They are not the Load command. Explicit Load may dual-run the old Fetch path. Hollow-click Load may remain wired to that command. This ticket decides when the old path dies.

Lock:

1. **Dual-run window** — Which Load invocations keep the old Fetch packages, and which use the new edges-plus-Nodes answer?
2. **Death of the old path** — What must be true before Load Fetch uses only the new package? Is there a single cut, or a per-door migrate?
3. **App vs Browser** — If the App still needs the old Fetch path after the Browser migrates, is that in scope here or a [transport-layer](plan/transport-layer/project.md) note?
