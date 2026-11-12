# 06 — Keep a nested node whose derived path differs

Ticket: [06 — Keep a nested node whose derived path differs](06-keep-a-nested-node-whose-derived-path-differs-ticket.md)

Build has not run. Status is `defined`.

The scope is a write-ticket record. The planned edit is:

- `src/Shared/DocumentPathMove.fs` — `coalescePathMoves` keeps a nested File or Directory whose new derived path differs from the path the enclosing Directory move produces.
- `coalescePathMoves` reads the pre graph and the move list.
- `tests/Shared.Tests/DocumentPathMoveTests.fs` — add ``coalesce keeps a nested node whose derived path differs``. ``coalescePathMoves drops nested roots covered by directory move`` and ``coalescePathMoves keeps file-owned directory separate from file move`` stay.
- 05 — Relocate in the same workspace adds `sameWorkspaceMoves`. This ticket does not re-scope that work.
