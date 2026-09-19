# Code review — 11 Simple extract format

Range: `35df23193b92b4bbc2c0db1f3a525cf093955d19...HEAD` (three-dot vs merge-base with `origin/staging`; HEAD `cbea13b29851a5c32191f8e9bd74202c38423a09`). Re-pass after reshape `Graph.focus` / `tagName` (commit `cbea13b2`). Subject: [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), [Model.fs](src/Shared/Model.fs) `Graph.focus`, [GraphBuild.fs](src/Shared/GraphBuild.fs), [GraphOps.fs](src/Shared/GraphOps.fs), [Serialization.fs](src/Shared/Serialization.fs), [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) fact Graph JSON omits focus, and the Compile entries in [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) and [Gambol.Shared.Tests.fsproj](tests/Shared.Tests/Gambol.Shared.Tests.fsproj). Spec: [11 — Simple extract format](plan/llm-connector/issues/11-simple-extract-format.md) and [arch.md](plan/llm-connector/arch.md) module **Document (pack + Reference Paste)** State Extract-pack Focus and Interface Write and parse the supplied extract. Plan-file cherry-pick noise is out of scope. Ticket Status stays `coded`. Axis drafts: [Standards](code-review-standards-simple-extract-format.md), [Spec](code-review-spec-simple-extract-format.md).

First-pass Focus-as-bool / Tag and write-error extras are gone: Focus is `Graph.focus` on the extract copy; `writeExtract` takes only that Graph; `NestedTagNode` carries `tag` not `isFocus`. Extra skip of supplied children remains.

## Standards

In scope: [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), [Model.fs](src/Shared/Model.fs) Graph.focus, [GraphBuild.fs](src/Shared/GraphBuild.fs) fromNodes / fromExtracted / withFocus / appendChildren, [GraphOps.fs](src/Shared/GraphOps.fs) withFocus, [Serialization.fs](src/Shared/Serialization.fs) encodeGraph, [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) Graph JSON omits focus, and the Compile entries in [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) and [Gambol.Shared.Tests.fsproj](tests/Shared.Tests/Gambol.Shared.Tests.fsproj). Prior review drafts and plan cherry-picks are out of product scope.

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff 35df23193b92b4bbc2c0db1f3a525cf093955d19`) printed BARE_ID on prior review drafts (overwritten here), FILE 591→601 on [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) (test-file split does not apply), and measure-fs-size. All measured bindings are ≤40 lines (`writeNode` 19, `parseOpened` 22, `withFocus` 3).

### Hard violations

This change has no hard violation. Scan BARE_ID hits sit in prior review drafts this file overwrites ([refer-by-name.md](.agents/rules/refer-by-name.md)). Scan FILE 591→601 on [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) cites the [fsharp-source.md](.agents/rules/fsharp-source.md) 800/400 split; that split does not apply to tests. All measured bindings are ≤40 lines (writeNode 19, parseOpened 22). No added long line, tab, mutable, or product exception. Public functions tagName, writeExtract, parseExtract, and withFocus are two words. [core-api.md](.agents/rules/core-api.md) does not apply. Tests use `failwith` in requireOk; that matches Shared.Tests style ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Graph.focus extends the Graph record per named reused types.

### 1. Packed string rebuilt per Node (judgement)

[fsharp-source.md](.agents/rules/fsharp-source.md) says never rebuild a whole structure once per item; accumulate with `::` and concat once. writeNode:

```
openTag + node.text + String.concat "" childStrings + closeTag
```

Each ancestor copies child pack text. This is not `@` in a fold, so not a hard hit. One writeExtract walk, not a 10,000-op replay.

### 2. Public literals incomplete and residue (judgement — Mysterious Name)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Mysterious Name. Near [fsharp-source.md](.agents/rules/fsharp-source.md) public names more than one word (that rule names functions; these are `[<Literal>]` values):

```
let incomplete = "incomplete extract"
let residue = "residue after extract"
```

missingRoot is two words. Other Shared modules use two-word literals (parentMissing).

### 3. Open and Close name read (judgement — Duplicated Code)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code. tagAt repeats the same readName / `'>'` / length shape for Open and Close:

```
match readName source (i + 2) with
| Some(name, after) when after < source.Length
    && source.[after] = '>' ->
    Some(Close name, after + 1 - i)
```

The Open arm uses `i + 1` and Open name.

### 4. Repeated parent-child graphs (judgement — Duplicated Code)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code. Three facts in [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs) repeat the same Parent/Child extract (nested node writes text then child tags, nested Focus wraps children in focus tags, round-trip parse recovers the child tree). Local extracted is shared; the Node construction is not.

## Spec

Range `35df23193b92b4bbc2c0db1f3a525cf093955d19...HEAD` (HEAD `cbea13b29851a5c32191f8e9bd74202c38423a09`). Subject: [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), `Graph.focus` on [Model.fs](src/Shared/Model.fs), `Graph.withFocus` / `fromExtracted` `focus=None` in [GraphBuild.fs](src/Shared/GraphBuild.fs) and [GraphOps.fs](src/Shared/GraphOps.fs), [Serialization.encodeGraph](src/Shared/Serialization.fs) omits focus, and [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) fact `Graph JSON omits focus`. Spec: [11 — Simple extract format](plan/llm-connector/issues/11-simple-extract-format.md) (Status stays coded) and [arch.md](plan/llm-connector/arch.md) module **Document (pack + Reference Paste)** State item Extract-pack Focus and Interface item Write and parse the supplied extract. Comments reshape plus that Interface are the contract for Focus and `tagName`. Checklist items Leaf form, Nested form, Supplied walk, Round-trip parse, and No codec / file write stay in force. Locked item Focus mark spelling stays `<focus>`.

### Checks

Focused tests: 10 passed (`DocumentNestedTagTests` and `Graph JSON omits focus`). `writeExtract` takes only `Graph` and starts at `graph.root`. `tagName` returns `"div"`. Write uses `<focus>` when `graph.focus` matches `node.id`. `NestedTagNode` is `{ tag; text; children }`. `parseExtract` keeps the tag string and returns `Error residue` or `Error incomplete`. `fromExtracted` sets `focus = None`. `encodeGraph` emits `root` and `nodes` only. The module does not call [MdDocument](src/Shared/documents/MdDocument.fs) and does not write a file.

### (a) Missing or partial

None. Leaf form, Nested form, Ref supplied walk, round-trip child tree, residue reject, `Graph.focus` / `Graph.withFocus`, JSON omit, and `writeExtract : Graph -> Result` match the ticket and the reshape Comments. Result now belongs on the contract, so a `missingRoot` write error is not a miss.

### (b) Behaviour not asked for

#### 1. Extra skip of supplied children

Spec: "walk the given extract child lists as supplied; do not bound by Owner edges or document-root ownership."

`writeNode` drops a child when the id is in `ancestors`, or when `Map.tryFind` misses in `graph.nodes`. Test `write omits a child id missing from the extract` locks the miss case. A Ref child that points at an ancestor is in the supplied list but does not appear in the string. [11 — Simple extract format](plan/llm-connector/issues/11-simple-extract-format.md) does not ask for omit or cycle-break.

### (c) Implemented but wrong

None. Walk is not Owner-bounded. Focus spelling matches locked item Focus mark spelling: "First pack marks Focus with `<focus>` (simple nested-tag format)." Parse reject matches "reject a partial parse (no residue)." Tag name today is `div` per Interface Write and parse the supplied extract. Existing Md artifact write does not change.

## Summary

Standards: 0 hard / 4 judgement (worst: 1 Packed string rebuilt per Node). Spec: 0 missing / 1 extra / 0 wrong (worst: 1 Extra skip of supplied children).

**Verdict:** Approve with nits

## First pass (before Graph.focus reshape)

Range then was the same merge-base; product scope was pack files only. Verdict was Approve with nits. Standards worst was Focus as bool and as Tag (now resolved). Spec extras were Extra skip of supplied children (still open) and Write error path (dropped: `writeExtract : Graph -> Result` is on the reshape contract).
