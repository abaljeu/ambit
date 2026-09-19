# Standards — 11 Simple extract format

In scope: [DocumentNestedTag.fs](../../../src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), [Model.fs](../../../src/Shared/Model.fs) Graph.focus, [GraphBuild.fs](../../../src/Shared/GraphBuild.fs) fromNodes / fromExtracted / withFocus / appendChildren, [GraphOps.fs](../../../src/Shared/GraphOps.fs) withFocus, [Serialization.fs](../../../src/Shared/Serialization.fs) encodeGraph, [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) Graph JSON omits focus, and the Compile entries in [Gambol.Shared.fsproj](../../../src/Shared/Gambol.Shared.fsproj) and [Gambol.Shared.Tests.fsproj](tests/Shared.Tests/Gambol.Shared.Tests.fsproj). Prior review drafts and plan cherry-picks are out of product scope.

## Hard violations

This change has no hard violation. Scan BARE_ID hits sit in prior review drafts this file overwrites ([refer-by-name.md](.agents/rules/refer-by-name.md)). Scan FILE 591→601 on [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) cites the [fsharp-source.md](.agents/rules/fsharp-source.md) 800/400 split; that split does not apply to tests. All measured bindings are ≤40 lines (writeNode 19, parseOpened 22). No added long line, tab, mutable, or product exception. Public functions tagName, writeExtract, parseExtract, and withFocus are two words. [core-api.md](.agents/rules/core-api.md) does not apply. Tests use `failwith` in requireOk; that matches Shared.Tests style ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Graph.focus extends the Graph record per named reused types.

## 1. Packed string rebuilt per Node (judgement)

[fsharp-source.md](.agents/rules/fsharp-source.md) says never rebuild a whole structure once per item; accumulate with `::` and concat once. writeNode:

```
openTag + node.text + String.concat "" childStrings + closeTag
```

Each ancestor copies child pack text. This is not `@` in a fold, so not a hard hit. One writeExtract walk, not a 10,000-op replay.

## 2. Public literals incomplete and residue (judgement — Mysterious Name)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Mysterious Name. Near [fsharp-source.md](.agents/rules/fsharp-source.md) public names more than one word (that rule names functions; these are `[<Literal>]` values):

```
let incomplete = "incomplete extract"
let residue = "residue after extract"
```

missingRoot is two words. Other Shared modules use two-word literals (parentMissing).

## 3. Open and Close name read (judgement — Duplicated Code)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code. tagAt repeats the same readName / `'>'` / length shape for Open and Close:

```
match readName source (i + 2) with
| Some(name, after) when after < source.Length
    && source.[after] = '>' ->
    Some(Close name, after + 1 - i)
```

The Open arm uses `i + 1` and Open name.

## 4. Repeated parent-child graphs (judgement — Duplicated Code)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code. Three facts in [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs) repeat the same Parent/Child extract (nested node writes text then child tags, nested Focus wraps children in focus tags, round-trip parse recovers the child tree). Local extracted is shared; the Node construction is not.

Standards tally: 0 hard / 4 judgement, worst Packed string rebuilt per Node.
