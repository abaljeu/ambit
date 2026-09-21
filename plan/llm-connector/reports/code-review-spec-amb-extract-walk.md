# Spec review: Amb extract-walk

Spec: [11 — Pack extract with Amb (supplied-fragment walk)](plan/llm-connector/issues/11-simple-extract-format.md); architecture module **Document (Amb pack + Reference Paste)** and Locked items **Focus on extract Graph** and **First pack is Amb extract-walk** in [llm-connector architecture](plan/llm-connector/arch.md). Range: `origin/staging...HEAD`. Subject: [AmbDocument.fs](src/Shared/documents/AmbDocument.fs) extract-walk, [Graph.focus](src/Shared/Model.fs) / [withFocus](src/Shared/GraphOps.fs), JSON/History omit, tests [AmbExtractWalkTests.fs](tests/Shared.Tests/AmbExtractWalkTests.fs) and [GraphFocusTests.fs](tests/Shared.Tests/GraphFocusTests.fs).

## (a) Missing or partial

None. Ticket 11 write, walk, omit-missing, no partition, no persist, default parse, `Graph.focus` / `withFocus`, JSON/History omit, and no Amb Focus sentinel are present. Actor consume stays on [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md).

## (b) Behaviour the spec did not ask

`AmbDocument.serializeLinesWith` is extra public surface. Ticket Comments: "Coded: `AmbWriteWalk.SuppliedExtract` on `AmbDocument.writeWith`." The ticket names `writeWith`, not a second serialize entry.

Product code does not add mixed-format owning-codec or Md extract serialize (tabled). [map.md](plan/llm-connector/map.md) Destination still says "mixed-format Graph extract, mark Focus in the outbound document," which contradicts ticket 11; that is plan text, not product behaviour.

## (c) Implemented but wrong

Nested File Node / Directory Node line form. Spec: "Reuse the Amb (Ambit `.amb` / `AmbDocument`) codec." Spec: "Do not stop at nested document or File Node boundaries." Default Amb write emits an owner-line (`^` stable id plus Filename) at a nested document boundary, then stops. Extract walk recurses (correct; `inside-file` appears) but `extractNodeLine` writes plain `node.text` unless the node is shared, a Ref target, or has ambiguous text. Nested File Node `note.txt` therefore loses stable id and Filename. The walk change is continue after the boundary, not drop Amb owner-line identity. Locked **First pack is Amb extract-walk**: "Reuse `AmbDocument`." Recurse-and-plain is a different line grammar for artifact Nodes, not only a different walk.
