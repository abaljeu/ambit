# 08 — Amend a change conflict before the graph is stored

Ticket: [08 — Amend a change conflict before the graph is stored](08-amend-a-change-conflict-before-the-graph-is-stored-ticket.md)

Build has not run. Status is `defined`.

The scope is a write-ticket record. The planned edit is:

- `src/Server/DocumentPersistChange.fs` — list conflicting incoming roots.
- `src/Shared/ChangeAmendment.fs` — a pure `Op.SetName` list for those incoming node ids.
- `src/Server/Core/FileAgent.fs` — amend after `applyBatch` and before the graph is stored.
- `src/Server/Core/DbAgent.fs` — the same amend when `liveSaveDataDir` is `Some`. No conflict check when it is `None`.
- A conflict is an on-disk file at the incoming node's derived destination. The existing file stays. The incoming node is renamed.
- `tests/Server.Tests/DocumentPersistChangeTests.fs` — ``a change conflict is amended before the graph is stored``.
- ``persistGraphChange rejects destination artifact conflict`` stays.
