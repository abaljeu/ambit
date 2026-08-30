# Choose startup bootstrap scope

Type: grilling
Status: resolved
Blocked by: 01, 02

## Question

Given that a fresh client starts with a zoom-root whose owner chain identifies its workspace, what scope around that anchor must the startup request make authoritative before first render?

## Answer

- Before first render, startup requests `Workspace` mode for both the canonical ROOT Workspace and the nearest Workspace at-or-above the zoom root. If ROOT is also the nearest Workspace, it is one deduplicated target.
- Each Workspace closure follows the boundary from ticket 02: recursion stops at a nested Workspace unless that Workspace is independently targeted as the zoom root's nearest Workspace. ROOT therefore exposes every named Workspace header without loading sibling Workspace contents.
- SYSTEM and TRASH are included implicitly because they are transitively owned within ROOT. They require no special scope rule or extra loading behavior.
- First render waits until every required closure is authoritative. The load protocol and session-restoration failure behavior remain with tickets 03 and 05.
