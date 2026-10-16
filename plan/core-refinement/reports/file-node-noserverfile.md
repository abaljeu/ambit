# File Node noServerFile

Date: 2026-10-15

Owning layer: [core-refinement architecture](../arch.md). Map, spec, and code stay unchanged. [16 — One document state](../../parse-thread/issues/16-one-document-state.md) is `cancelled`. [Parse thread architecture](../../parse-thread/arch.md) was read and not edited.

## §1 noServerFile

The File Node carries `noServerFile` (bool). It implies Unparsed. Parsed with `noServerFile` is not a state. A Directory Node and a Workspace Node do not carry it.

## Files

1. [core-refinement architecture](../arch.md) — §1 **noServerFile**, §3 step 1 **State axes on special nodes** item 3 **Contract**, §5 item 6 **Directory Parse done**, §5 item 7 **Create special**, §10 **Core loop** Interface item 4 **Parse finished** child **Parsed when clear**.
2. [16 — One document state](../../parse-thread/issues/16-one-document-state.md) — Status `cancelled`. Body and build checkboxes stay.
3. [Parse thread](../../parse-thread/project.md) — 2026-10-15 note. Stage stays `build`.

## Follow-up

[Parse thread architecture](../../parse-thread/arch.md) still forces `SetDocumentState` as a writer, and one finish sentence has no `noServerFile` guard:

1. §2 Module map, item 1 **Directory reconcile**, and Interface item 3 **Current** — the ops start with SetDocumentState from Unparsed to Current.
2. §1 story path 22 **Add InMsg**, item 3 **Core loop** — ParseFinished sets Parsed only through `GraphMutate.setParseState`, with no clear-bool guard.
3. §1 story path 1 **Unparsed becomes Graph**, item 3 **Current** — `planParseFile` marks an Unparsed File Node Current.

No sentence there forces the one-record DocumentState shape.
