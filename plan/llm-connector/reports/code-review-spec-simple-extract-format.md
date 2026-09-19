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

Spec tally: 0 missing / 1 extra / 0 wrong, worst finding Extra skip of supplied children.
