# Standards — 11 Simple extract format

In scope: [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), and the Compile entries in [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) and [Gambol.Shared.Tests.fsproj](tests/Shared.Tests/Gambol.Shared.Tests.fsproj). Plan cherry-picks: no [markdown-writing.md](.agents/rules/markdown-writing.md) hit to record.

## Hard violations

None. Mechanical scan listed eleven bindings in [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs) against [fsharp-source.md](.agents/rules/fsharp-source.md) (40-line functions); each is ≤40 lines (`writeNode` 20, `parseOpened` 23). No added long line, tab, `mutable`, product exception, `@` / `List.append` in a fold, or `Map.toList graph.nodes`. Public functions `writeExtract` and `parseExtract` are two words. [core-api.md](.agents/rules/core-api.md) does not apply (Shared pack, not Core). Tests use `failwith` in `requireOk`; that matches existing Shared.Tests style ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) match existing style).

## 1. Focus as bool and as Tag (judgement — Primitive Obsession)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Primitive Obsession. `NestedTagNode` and `writeNode` use `isFocus: bool`. Parse uses private `Tag`. `parseOpened` takes both, which lengthens the list with a related parameter ([fsharp-source.md](.agents/rules/fsharp-source.md) named reused param types):

```
and private parseOpened
    (source: string)
    (i: int)
    (isFocus: bool)
    (openTag: Tag)
```

Not a hard violation: four parameters, and other Shared parsers keep `(text, i)` flat.

## 2. Packed string rebuilt per Node (judgement)

[fsharp-source.md](.agents/rules/fsharp-source.md) says never rebuild a whole structure once per item; accumulate with `::` and concat once. `writeNode`:

```
openTag + node.text + String.concat "" childStrings + closeTag
```

Each ancestor copies child pack text. This is not `@` in a fold, so not a hard hit. One `writeExtract` walk, not a 10,000-op replay.

## 3. Public literals incomplete and residue (judgement — Mysterious Name)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Mysterious Name. Near [fsharp-source.md](.agents/rules/fsharp-source.md) public names more than one word (that rule names functions; these are `[<Literal>]` values):

```
let incomplete = "incomplete extract"
let residue = "residue after extract"
```

`missingRoot` is two words. Other Shared modules use two-word literals (`parentMissing`, `missingArgument`).

## 4. Repeated parent-child graphs (judgement — Duplicated Code)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code. Three facts in [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs) repeat the same Parent/Child extract (`nested node writes text then child tags`, `nested Focus wraps children in focus tags`, `round-trip parse recovers the child tree`). Local `extracted` is shared; the Node construction is not.
