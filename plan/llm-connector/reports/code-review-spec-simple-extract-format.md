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
