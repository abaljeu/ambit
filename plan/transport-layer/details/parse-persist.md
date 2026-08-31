# Parse/Persist — transport primitive

Short reference for Parse/Persist as the shared text-processing unit across transport channels. Parent: [[../overview.md]].

## Definition

| Unit | Input | Output | Typical trigger |
| --- | --- | --- | --- |
| **Parse** | Text (or bytes read as text) | Graph content as **Changes** (or a staged Local Graph for examination) | Load stage, import job, connector inbound |
| **Persist** | Graph slice (often one File Node body or export selection) | Text for an outside channel | Save, export, publish hook, connector outbound |

Parse reconciles outside text with existing Graph content; it is more than lexical parsing. Persist is the inverse direction for editable or archival copies.

## Flow through the Graph

```mermaid
%%{init: {'themeVariables': {'fontSize': '20px'}}}%%
flowchart LR
  OUT[Outside text]
  PARSE[Parse]
  G[Graph]
  PERSIST[Persist]
  BACK[Outside text]

  OUT --> PARSE --> G
  G --> PERSIST --> BACK
```

Inbound: outside text → Parse → **Changes** on the Actor path. Outbound: Graph slice → Persist → outside text. Round-trip: Persist → edit outside → Parse → **Update** **Changes**.

## File channel mapping

| Transport concern | File channel instance |
| --- | --- |
| Bytes on disk | Upload / Download (not Parse/Persist themselves) |
| Text → Graph | Load **Parse** stage; **Parse File** Server **Actor** |
| Graph → text | Document codec **Persist** / reconcile on File Node |
| Round-trip | Codec Parse/reconcile + workspace sync ([[plan/document-formats/map.md]]) |
| Residency | **Fetch** after Parse ([[plan/selective-client-loading/project.md]]) |

Parse File on disk is the reference **Actor** implementation; generalized produce path and job identity live in event-sourced-ops.

## Shared vs per-transport

| Shared (transport-layer contract) | Per-transport (each connector Project) |
| --- | --- |
| Plan from Local Graph; emit **Changes** through ESO | Wire protocol, auth, rate limits |
| Graph authority; merge on re-import | External model mapping (Graph ↔ API shape) |
| Parse/Persist as the text leg naming | Staging UX, examine-before-commit |
| Optional long-running **Actor** + soft-lock | Schedule, trigger, connection config Nodes |

Codecs (document-formats) own format-specific Parse/Persist grammar. Transport-layer owns the pattern; sibling Projects own one channel or codec.

## Related vocabulary

Locked terms: [[plan/event-sourced-ops/details/vocabulary.md]]. Gambol glossary: [[CONTEXT.md]] — **Parse**, **Load**, **Change**, **Actor**, **File Node**, **Sync**.
