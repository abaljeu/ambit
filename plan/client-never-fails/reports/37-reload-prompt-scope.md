# 37 — Reload prompt scope

Slice: Delete the status line DataOutdated arm, including the sentence Data changed on server — click to reload. Delete the blocking alert DataOutdated arm, including the title View is out of date. ServerRejected stays. CodeOutdated keeps the title New version available and the status sentence New version available — click to reload.

## Signatures

[ ] `StatusView` matches `DataOutdated`, `CodeOutdated`, and `ServerRejected` in `src/Client/StatusView.fs`.
[ ] `Overlays` matches the same three states in `src/Client/Overlays.fs`.
[ ] The status sentence for `DataOutdated` is `Data changed on server — click to reload`. The alert title is `View is out of date`.
[ ] The `CodeOutdated` status sentence is `New version available — click to reload`. The alert title is `New version available`.

## Match sites

[ ] `ServerRejected` has its own arm in both modules.
[ ] No test file contains those sentences. They live in the two view modules.

## Existing tests

[ ] No existing fact asserts the reload sentences.

## Copy-from

[ ] The `CodeOutdated` arms are the copy for the sentences this ticket keeps.

## write-ticket

### Source files

[ ] [[src/Client/StatusView.fs]] — status line.
[ ] [[src/Client/Overlays.fs]] — blocking alert.

### Tests

[ ] The two view modules are the outputs. No test name is added.

## Size

[ ] Source files: 2. Under the limit of 8.
[ ] Tests added or changed: 0. Under the limit of 4.
