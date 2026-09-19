# Code review — 11 Simple extract format

Range: `35df23193b92b4bbc2c0db1f3a525cf093955d19...HEAD` (three-dot vs merge-base with `origin/staging`). Subject: [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), and the Compile entries in [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) and [Gambol.Shared.Tests.fsproj](tests/Shared.Tests/Gambol.Shared.Tests.fsproj). Spec: [11 — Simple extract format](plan/llm-connector/issues/11-simple-extract-format.md) and [arch.md](plan/llm-connector/arch.md) module **Document (pack + Reference Paste)** Interface item 1. Plan-file cherry-pick noise is out of scope; it does not contradict the ticket. Ticket Status stays `coded`. Axis drafts: [Standards](code-review-standards-simple-extract-format.md), [Spec](code-review-spec-simple-extract-format.md).

## Standards

In scope: [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), and the Compile entries in [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) and [Gambol.Shared.Tests.fsproj](tests/Shared.Tests/Gambol.Shared.Tests.fsproj). Plan cherry-picks: no [markdown-writing.md](.agents/rules/markdown-writing.md) hit to record.

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff 35df23193b92b4bbc2c0db1f3a525cf093955d19`) printed measure-fs-size only. Eleven bindings in [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs); each is ≤40 lines (`writeNode` 20, `parseOpened` 23). No other scanner findings.

### Hard violations

None. Mechanical scan listed eleven bindings in [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs) against [fsharp-source.md](.agents/rules/fsharp-source.md) (40-line functions); each is ≤40 lines (`writeNode` 20, `parseOpened` 23). No added long line, tab, `mutable`, product exception, `@` / `List.append` in a fold, or `Map.toList graph.nodes`. Public functions `writeExtract` and `parseExtract` are two words. [core-api.md](.agents/rules/core-api.md) does not apply (Shared pack, not Core). Tests use `failwith` in `requireOk`; that matches existing Shared.Tests style ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) match existing style).

### 1. Focus as bool and as Tag (judgement — Primitive Obsession)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Primitive Obsession. `NestedTagNode` and `writeNode` use `isFocus: bool`. Parse uses private `Tag`. `parseOpened` takes both, which lengthens the list with a related parameter ([fsharp-source.md](.agents/rules/fsharp-source.md) named reused param types):

```
and private parseOpened
    (source: string)
    (i: int)
    (isFocus: bool)
    (openTag: Tag)
```

Not a hard violation: four parameters, and other Shared parsers keep `(text, i)` flat.

### 2. Packed string rebuilt per Node (judgement)

[fsharp-source.md](.agents/rules/fsharp-source.md) says never rebuild a whole structure once per item; accumulate with `::` and concat once. `writeNode`:

```
openTag + node.text + String.concat "" childStrings + closeTag
```

Each ancestor copies child pack text. This is not `@` in a fold, so not a hard hit. One `writeExtract` walk, not a 10,000-op replay.

### 3. Public literals incomplete and residue (judgement — Mysterious Name)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Mysterious Name. Near [fsharp-source.md](.agents/rules/fsharp-source.md) public names more than one word (that rule names functions; these are `[<Literal>]` values):

```
let incomplete = "incomplete extract"
let residue = "residue after extract"
```

`missingRoot` is two words. Other Shared modules use two-word literals (`parentMissing`, `missingArgument`).

### 4. Repeated parent-child graphs (judgement — Duplicated Code)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code. Three facts in [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs) repeat the same Parent/Child extract (`nested node writes text then child tags`, `nested Focus wraps children in focus tags`, `round-trip parse recovers the child tree`). Local `extracted` is shared; the Node construction is not.

## Spec

Range `35df23193b92b4bbc2c0db1f3a525cf093955d19...HEAD`. Subject: [DocumentNestedTag.fs](src/Shared/DocumentNestedTag.fs), [DocumentNestedTagTests.fs](tests/Shared.Tests/DocumentNestedTagTests.fs), and the two fsproj entries. Spec: [11 — Simple extract format](plan/llm-connector/issues/11-simple-extract-format.md) and [arch.md](plan/llm-connector/arch.md) module **Document (pack + Reference Paste)** Interface item 1. Focused tests: 9 passed.

### Checks

Write starts at `graph.root` and follows each `node.children` id. It does not read `ChildNode.ref` and does not call [DocumentPartition](src/Shared/DocumentPartition.fs). Focus uses `<focus>` when `node.id = focusId`. `parseExtract` yields `Error residue` when bytes remain after one tree, and `Error incomplete` when a tag is unclosed or mismatched. The module does not call [MdDocument](src/Shared/documents/MdDocument.fs) and does not write a file. Md artifact files are not in the diff.

### (a) Missing or partial

None of the five ticket items is missing. Leaf, nested, Ref walk, round-trip, residue, and incomplete each have a test.

### (b) Behaviour not asked for

#### 1. Extra skip of supplied children

Spec: "walk the given extract child lists as supplied; do not bound by Owner edges or document-root ownership."

`writeNode` also drops a child when the id is already in `ancestors`, or when `Map.tryFind` misses in `graph.nodes`. The miss case is locked by test `write omits a child id missing from the extract`. [11 — Simple extract format](plan/llm-connector/issues/11-simple-extract-format.md) does not ask for omit or cycle-break. A Ref child that points at an ancestor is in the supplied list but does not appear in the string.

#### 2. Write error path

Spec: "A Document call that writes the supplied subgraph to a string."

`writeExtract` returns `Result` and `Error missingRoot` when `graph.root` is absent. The ticket does not ask for a write error string.

### (c) Implemented but wrong

None. Walk is not Owner-bounded. Focus spelling matches locked item "Focus mark spelling — First pack marks Focus with `<focus>`". Parse reject matches "reject a partial parse (no residue)". Escaping stays out of scope. Existing Md artifact write does not change.

## Summary

Standards: 0 hard / 4 judgement (worst: 1 Focus as bool and as Tag). Spec: 0 missing / 2 extras / 0 wrong (worst: 1 Extra skip of supplied children).

**Verdict:** Approve with nits
