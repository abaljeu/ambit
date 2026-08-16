# External multiline paste — accomplished

Verdict: passed

Board advice: remove the [[src/Client/UpdatePaste.fs]] item. Its requested HITL verification is complete.

## Accomplished claim

External multiline Ctrl+V passed in the Browser on 2026-08-15 in both select and edit modes: surrounding siblings remained and the pasted lines became the expected Nodes.

## Evidence

- In response to the exact two-phase manual-check question, the user reported: “Tests pass.” This is authoritative confirmation that both requested Browser interactions passed on 2026-08-15.
- Selecting a Node and pasting two external lines kept the surrounding siblings and replaced the selected Node with the expected pasted Nodes.
- Pasting two external lines while editing a Node kept the surrounding siblings, spliced the first line into the edited Node, and created the expected following Node from the second line.
- Commit `ea76342c334d8993c9c2c3a64940fb6404454ce5` added [[src/Shared/documents/DocumentColdParse.fs]] `planPasteOps`, routed select and multiline edit paste through it, and added automated regression coverage in [[tests/Shared.Tests/DocumentColdParseTests.fs]].

## Result

The implementation and its required manual Browser verification are accomplished. No evidence gap or follow-up check remains for this board item.
