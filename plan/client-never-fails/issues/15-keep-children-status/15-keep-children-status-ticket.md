# 15 — Keep ChildrenStatus in the spec and arch
**Status**: 
One job: The spec and arch say to keep `ChildrenStatus` and its cases `Loaded` and `Unloaded`, and to stop the generator side instead, so the Server always sends each Node with its child list. This is Alan's decision of 2026-10-29, which revises "remove Loaded and Unloaded".

Type: doc
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

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Rename spec story 13 to "Each Node with its child list". Keep the Server lines (`wantAnswer`, `visibleClosureWantAnswer`, `Api.nodesForRequest`, `installWantAnswer`). Delete the lines that remove `ChildrenStatus`, its helpers, the wire field and values, the `residencyText` children line, and the glossary entries. Add one line: `ChildrenStatus`, its cases, its wire values and its tests stay.
3. [ ] Rename arch path 6 to match. Delete steps 7, 8, 11 and 12. Keep the other steps and renumber them.
4. [ ] Change the "Alan step 4" note, order item 4, and the tracer-cut paragraph so none of them says the type is deleted.
5. [ ] Delete Glossary proposals 1 and 2. Keep 3 and 4.

## Outputs

[ ] `plan/client-never-fails/spec.md`
[ ] `plan/client-never-fails/arch.md`

## Stop when

[ ] Stop when the edit needs a file outside Inputs, and ask Alan.
[ ] Stop when the edit would change a ticket under `issues/`.

## Check

[ ] `rg -n "Remove Loaded|delete .ChildrenStatus|childrenStatus. and the values" plan/client-never-fails/spec.md plan/client-never-fails/arch.md` finds nothing.
[ ] A reader who reads only the One job line can say what the ticket changes.
