# 15 — Keep ChildrenStatus in the spec and arch
**Status**: 
One job: The spec and arch say to keep `ChildrenStatus` and its cases `Loaded` and `Unloaded`, and to stop the generator side instead, so the Server always sends each Node with its child list. This is Alan's decision of 2026-10-29, which revises "remove Loaded and Unloaded".

Type: manual (Alan)
Blockers: none

## Inputs

### Working

[ ] [[plan/client-never-fails/spec.md]] story 13, Remove Loaded and Unloaded, lists the removal of type `ChildrenStatus` and its cases, `GraphChildren.isLoaded`, `GraphChildren.status`, `GraphBuild.isLoaded`, `GraphBuild.childrenStatus`, `Graph.isLoaded`, `Graph.childrenStatus`, the wire field `childrenStatus` with values `"loaded"` and `"unloaded"`, the children line in `ViewModelRowState.residencyText`, and the glossary entries Loaded and Unloaded.
[ ] [[plan/client-never-fails/arch.md]] path 6, Remove Loaded and Unloaded, has steps 7 Row state (children line deleted), 8 Node wire (field and values deleted), 11 and 12 Graph model (type and helpers deleted, tests in batches).
[ ] [[plan/client-never-fails/arch.md]] lines that depend on that path: the "Alan step 4" note above path 6, order item 4, the tracer-cut paragraph, and Glossary proposals 1 and 2 (delete Loaded, delete Unloaded).

### Reference

[ ] [[plan/client-never-fails/issues/06-server-sends-child-list/06-server-sends-child-list-ticket.md]] is the generator-side ticket.
[ ] [[plan/client-never-fails/issues/07-delete-include-workspace/07-delete-include-workspace-ticket.md]]

## Do not load

[ ] Closed: all of `src/` and `tests/`. This ticket changes only the spec and arch.
[ ] Closed: [[GLOSSARY.md]]. It stays unchanged.

## Process

1. [ ] Ask Alan to edit spec story 13 and arch path 6 so they keep `ChildrenStatus`, its cases `Loaded` and `Unloaded`, its wire values, and the children line in `residencyText`, and to drop Glossary proposals 1 and 2. No agent does this edit.

## Outputs

[ ] `plan/client-never-fails/spec.md`
[ ] `plan/client-never-fails/arch.md`

## Stop when

[ ] Stop after asking Alan, and wait for him.

## Check

[ ] `rg -n "Remove Loaded|delete .ChildrenStatus|childrenStatus. and the values" plan/client-never-fails/spec.md plan/client-never-fails/arch.md` finds nothing.
[ ] A reader who reads only the One job line can say what the ticket changes.
