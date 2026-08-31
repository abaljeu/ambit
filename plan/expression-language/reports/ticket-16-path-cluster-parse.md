# Ticket 16 — path-cluster parse realignment

Branch: `w/expr`

## Summary

Implemented spec-aligned layer-two path-cluster parsing in Shared, plus a minimal layer-one term parser for spaced quoted name arguments and missing-argument cases. Legacy `RefExprParse` is unchanged; eval remains stubbed for ticket 17.

## Test results

```
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ExprPathClusterParseTests"
Passed: 15, Failed: 0
```

## Files touched

| File | Role |
| --- | --- |
| [[src/Shared/ExprPathClusterTypes.fs]] | `ClusterStep`, `PathCluster`, `ExprTerm`, `ExprSeq` AST |
| [[src/Shared/ExprPathClusterParse.fs]] | Layer-two cluster tokenize + parse |
| [[src/Shared/ExprParse.fs]] | Layer-one segment split; term juxtaposition for ticket-16 spaced cases |
| [[src/Shared/Gambol.Shared.fsproj]] | Register new modules |
| [[tests/Shared.Tests/ExprPathClusterParseTests.fs]] | 15 parse-outcome tests |
| [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]] | Register test module |

## Parse AST shape (for ticket 17)

### `ClusterStep` (left-fold cluster steps)

| Step | Spelling / source | Argument |
| --- | --- | --- |
| `Root` | `//` desugar prefix | — |
| `Structural name` | `/ name`, implicit `/` on leading or post-`^`/`.`/`**` name | glob string |
| `Content name` | `# name` | glob string |
| `StructuralUp` | `^` | — |
| `DirectoryUp` | `.` (lone or before `/`) | — |
| `Tree` | `**` | — |
| `ChildAt (Some n)` | `:n` | zero-based index |
| `ChildAt None` | `:*` | all children |
| `SiblingAt (Some n)` | `!n` | signed offset |
| `SiblingAt None` | `!*` | all siblings |

`PathCluster` is `ClusterStep list` in evaluation order.

### `//` desugar at parse time

`//ws` → `[ Root; Structural "ws" ]` (not a separate `//` row).

`root /ws` and `root / "ws"` → expression terms:

```text
Word("root", None)  Cluster([ Structural "ws" ], None)
```

Ticket 17 eval should map `Root` → catalog `root` row, `Structural` → `/` row with `NameGlob` argument, etc.

### `ExprTerm` (minimal layer-one, ticket 19 extends)

- `Word(spelling, optional trailing literal)` — catalog lookup deferred
- `Cluster(steps, optional trailing literal)` — trailing literal fills final step name slot when cluster ends with bare `/` or `#`

### Parse errors (uniform `missing argument`)

Bare `//`, `/`, `#`, `:`, `!`; `containing` without quoted string; `// ws` (cluster `//` then spaced word).

Standalone number: `a number is only valid as the right operand of : or !`.

## Ticket 16 acceptance criteria

- [x] `//ws` and `root /ws` parse to the same cluster steps as `root / "ws"`; bare `//` and bare `/` are parse errors.
- [x] `a/b/c` parses as implicit `/ "a"`, then `/ "b"`, then `/ "c"` from the omitted left input.
- [x] `:*` and `!*` parse as child-all and sibling-all steps; `3` alone is a parse error.
- [x] `x / "filename with spaces"` and `x # "a b"` accept spaced quoted name arguments.
