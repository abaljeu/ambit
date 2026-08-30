# Normal / Directory promotion and demotion

Category: Workspace scale
Status: Planned
See also: [[doc/roadmap/workspace-file-directory-placement]], [[doc/current/workspace-graph]], [[doc/roadmap/workspace-file-model]]

## What it gives you

- **Promote:** a focused `Normal` node becomes an owned `Special Directory` when it has (or is given) a valid filename; the resulting graph passes placement and persistence-directory name checks.
- **Demote:** a focused non-canonical `Special Directory` becomes `Normal`; rejected when demotion would leave owned File/Directory specials without a valid persistence directory.

## What it avoids for now

- Server disk create/delete of `dir/` and `.amb` on promote/demote (graph slice first; persistence in a follow-on slice).
- Promoting/demoting `File`, named `Workspace`, ROOT, TRASH, Workspaces.
- Rewriting Ref edges on demote.
- Client-only guards without Shared enforcement.

## Rules (precise)

### Promote Normal → Directory

Preconditions:

- Node is `Normal`.
- Owner parent passes `GraphQuery.canOwn` (owner-chain rule; File ancestor invalid).
- Name is `Filename.Ok`, not reserved, and unused in the persistence directory.
- Node is not inside an Unparsed document.

Effect (`Op.SetKind` or equivalent):

- `kind = Special Directory`
- `name = Ok proposedName`
- `text = proposedName` (same shape as `NewSpecialNode` Directory)
- `documentState = Current`

Name source: use `Node.name` when already `Filename.Ok` and unused; else derive from trimmed `text` when valid; otherwise prompt (reuse Rename prompt).

### Demote Directory → Normal

Preconditions:

- Node is `Special Directory`, not canonical TRASH or other system nodes.
- No owned `Special File` or `Special Directory` anywhere in the owned subtree. Reject — demoting a directory that still owns document roots breaks persistence semantics without migration.
- Not `documentState = Unparsed`.

Effect:

- `kind = Normal`
- `name = Filename.Empty`
- `text` = former directory label
- Clear `documentState`

Post-change: `History.validateOwnership` on the full graph.

## Minimal state / API / ops

- New `Op.SetKind` in [[src/Shared/History.fs]] with invert for undo.
- [[src/Shared/KindTransformOps.fs]] — `planPromoteToDirectory`, `planDemoteToNormal`.
- Reuse existing validation: unparsed guard, `validateOwnershipSemantics`, `artifactNameConflict`, subtree scan for demote reject.
- Serialization encode/decode in [[src/Shared/Serialization.fs]].
- Client palette commands; rename prompt when promote needs a name.

## Implementation steps

1. Add `Op.SetKind` + `GraphMutate.setKind` + invert → verify: unit apply/undo tests.
2. `KindTransformOps` planners with preconditions above → verify: Shared.Tests promote/demote matrix.
3. Client commands + prompt wiring → verify: compile Client.
4. Persistence slice: server create/delete directory artifacts on promote/demote ([[src/Server/DocumentPersistence.fs]]; cannot use path-move file↔dir rejection path).

## Tests

New module or section in [[tests/Shared.Tests]]:

| Case | Expect |
| --- | --- |
| Promote Normal under Directory with valid unused name | Ok |
| Promote with empty/invalid name | Error or prompt |
| Promote with persistence-dir collision | Error |
| Promote under File ancestor | Error |
| Demote Directory with only Normal children | Ok |
| Demote Directory owning File or nested Directory | Error |
| Demote TRASH | Error |
| Undo promote/demote | Round-trip |

## Non-goals

- File ↔ Normal or Workspace transforms.
- Bulk subtree promotion.
- Import/parse-driven kind changes.
