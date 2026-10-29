# 18 — Retarget closure and bootstrap tests scope

Slice: Retarget the named tests off ChildrenStatus. The type stays until 25 — Delete ChildrenStatus.

## Signatures

[ ] `ChildrenStatus`, `Graph.childrenStatus`, `Graph.isLoaded`, `GraphChildren.isLoaded`, and `GraphChildren.status` are the calls these tests drop.
[ ] `GraphChildren.tryGet` and `Graph.children` stay. A present child-list key is the old Loaded list. An absent key is the old Unloaded list.

## Match sites

[ ] Each named test asserts child-list Loaded or Unloaded today.

## Existing tests

[ ] ``visibleClosureGraph bad Zoom stays inside reserved set``, ``visibleClosureWantAnswer installs through installWantAnswer`` in [[tests/Shared.Tests/WantTests.fs]].
[ ] ``rootBootstrapGraph includes nested workspace header Unloaded``, ``rootBootstrapGraph keeps ROOT owned content Loaded`` in [[tests/Shared.Tests/BootstrapScopeTests.fs]].

## Copy-from

[ ] `GraphChildren.tryGet` in `src/Shared/Model.fs` is the read these assertions use after the change.

## write-ticket

### Source files

[ ] Production files stay. This ticket changes the named tests only.

### Tests

[ ] Change the 4 named tests. Assert the child list or the Header. Leave the five status calls uncalled.

## Size

[ ] Source files: 0. Under the limit of 8.
[ ] Tests added or changed: 4. Within the limit of 4.
