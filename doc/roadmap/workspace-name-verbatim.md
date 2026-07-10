# Workspace name verbatim (drop `@` marker)

Category: Workspace scale
Status: Slice A + B done
See also: [[doc/roadmap/workspaces]], [[doc/current/workspace-graph]], [[doc/current/workspace-local-mapping]], [[doc/history/workspaces/plans/drop_@_marker_from_workspace_disk_paths_492fd207.plan]]

## What it gives you

- Code stops treating `@` as a workspace disk marker: no prepend, no strip, no `@`-prefix heuristics.
- Workspace node name equals DataDir folder name, verbatim.
- Reference / path syntax stays `//` (ROOT) and `//workspacename/...`. No `@name:` address form; no `:` as workspace address punctuation.
- Docs and fixtures match that syntax.

## What it avoids for now

- Immutable workspace names.
- Allowing `@` in Filename / treating leading `@` as ordinary name content.
- Git sync, desktop mapping UI, import/export, format work.
- Any production DataDir migration, rename, or delete of local `data/@…` folders (ignore them).
- UI glyph for workspace rows (`specialKindSymbol`).

## Findings (restart baseline)

| Site | Behavior to remove |
|------|--------------------|
| [[src/Shared/NodeDesktopPath.fs]] `diskWorkspacePrefix` / `workspaceLabel` | Prepend / strip `@` |
| [[src/Shared/DocumentPartition.fs]] | Builds `"@" + name + "/"` paths |
| [[src/Shared/DocumentAssembly.fs]] | `@…` artifact heuristic; `TrimStart '@'` on stubs |
| [[src/Server/DocumentPersistence.fs]] `hasArtifactSet` | `rel.StartsWith("@")` |

Already fine: `pathForNode` emits `//name`; LocalProxy resolves `//label/rel`; Filename create already rejects `@`; WorkspaceLocalMapping stores bare labels.

Tests still assume `@`-prefixed names / `@bobby:` queries in RefExpr, Search, ViewModelFileSearch, and disk/persistence suites — addressed in slices below.

## Approach

Rewrite surgically from this plan. Do not cherry-pick commits from discarded `db` (`5a24a88..22e28ca`); Model.fs / DocumentAssembly there mixed unrelated work. History plan is reference only.

## Implementation slices

### Slice A — Remove `@` thinking from Shared + Server persistence — done

1. [[src/Shared/NodeDesktopPath.fs]]: delete add/strip helpers; disk and parse paths use the segment verbatim.
2. [[src/Shared/DocumentPartition.fs]]: prefixes become `name + "/"` and `name + "/.amb"`.
3. [[src/Shared/DocumentAssembly.fs]]: structural workspace-artifact check (no `@`); stub name without TrimStart.
4. [[src/Server/DocumentPersistence.fs]]: drop `@`-prefix disjunct in `hasArtifactSet`.
5. Update disk/persistence test expectations `@home` → `home` (no WorkspaceGit/Connect tests — absent at restart).
6. Verify: Shared.Tests + Server.Tests DocumentPersistence / DocumentPathMoveExecution green.

### Slice B — Refs and docs without `@name:` / `:` — done

1. RefExprTestTree: bare `bobby` / `other` via `Filename.create`.
2. RefExpr / Search / ViewModelFileSearch: queries use `//bobby/...`; drop or replace `@bobby:` coverage so it is not documented as workspace address syntax.
3. Strike `@label:` / auto-prepend `@` from current docs that claim implemented behavior ([[doc/current/workspace-graph]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]], [[doc/current/workspace-stage-plan]], [[doc/current/persistence-model]]); fix [[doc/roadmap/reference-expression-interpretation]] if it says workspace names start with `@`.
4. Verify: RefExpr + search suites green.

## Tests

- Disk-path / persistence expectations without marker.
- RefExpr + search fixtures on `//name`.
- No Filename-`@`, rename-immutability, git, or desktop-mapping tests in this change.

## Non-goals

- Git gateway / remote naming.
- Desktop mapping editor.
- Replaying DocumentAssembly assemble-queue redesign or Model `fileState` from `db`.
- New reference grammar / workspace anchor.
- Filename charset changes.
- Immutable names.
- Local `data/@…` cleanup.
