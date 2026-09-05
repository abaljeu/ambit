# Delete runtime mirror and remove production Persistence:Mode

Type: task
Status: open
Blocked by:

## What to build

Delete the unused runtime mirror and remove `Persistence:Mode` from production Server decisions. When the database is available, the supported writable path uses DbAgent and correlated files remain secondary. When the database is unavailable, retain file-backed Graph-data and file queries and Reject Changes. Legacy `Persistence:Mode` configuration is ignored.

This issue is independent and does not block [[01-generalized-server-actor-produce-path.md]]. Issue 01 must not migrate or expand the mirror path.

## Out of scope

- Existing initialization, repair, database/file reconciliation, and Graph-to-file/file-to-Graph protocols and files remain unchanged until ACID apply is redesigned.
- Test-only writable FileAgent construction and its focused tests remain unchanged.
- Do not add or redesign Graph/file algorithms or functions.

## Acceptance criteria

- [ ] With a database available, production startup selects DbAgent for writable Changes regardless of `Persistence:Mode`.
- [ ] With no database available, the Server Rejects Changes while existing Graph-data and file queries still work.
- [ ] The runtime mirror path is deleted and production behavior does not read `Persistence:Mode`.
- [ ] Existing initialization, repair, reconciliation, secondary-file, and test-only FileAgent behavior remains unchanged.
- [ ] Reuse or adjust focused startup and fallback tests; do not add duplicate behavior matrices.
