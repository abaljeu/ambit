# Transport layer — overview

The top layer of this project. It gives what transport-layer is and how outside information moves through Gambol. Project card: [[project.md]]. Leg chart: [[map.md]]. Parse/Persist detail: [[details/parse-persist.md]].

## What transport-layer is

Transport-layer is the cross-cutting pattern for **moving information** between outside sources and the Graph. It is not one connector, one codec, or one Epic. It is the shared contract: inbound (outside → Graph), outbound (Graph → outside), and round-trip (export → edit elsewhere → re-import) while the **Graph stays authority**.

Prior work called this the import layer or information hub. **Transport-layer** names the same concept with clearer scope: every channel is a transport instance; Parse/Persist is the fundamental text-processing unit reused across them.

## Three flows

| Flow | Direction | Goal |
| --- | --- | --- |
| **Inbound** | Outside → Graph | Materialize external data into the Graph for examination and editing. |
| **Outbound** | Graph → outside | Push a Graph slice to an external destination; Graph remains canonical. |
| **Round-trip** | Graph ↔ outside | Export, edit elsewhere, re-import; updates land as **Changes**, not a second truth. |

Each flow uses the same building blocks: plan from a **Local Graph**, optionally stage for examination, then emit **Changes** on the ESO Actor path.

Wiki publish (Public URL in [[.scratch/roadmap/epics/build-or-explore-a-wiki.md]]) is an outbound instance (Graph / `.md` File content → HTML for visitors), not disk Parse File and not HTML File web-site publish; it still uses Parse/Persist as the text-processing unit.

Web-site publish ([[.scratch/roadmap/epics/create-and-publish-web-pages.md]]) is outbound transport: generate HTML and send attachments and CSS through transport-layer (Graph / HTML File content → visitor-facing site).

## Parse/Persist as core text-processing unit

**Parse** and **Persist** are not only Load-stage names for disk files. Together they form the transport primitive for text:

- **Parse** — text (or bytes interpreted as text) in → reconcile with the Graph → produce **Changes** (or a staged view for examination).
- **Persist** — Graph slice (typically File Node content) out → text for an outside channel.

The file channel is the first fully charted instance: **Parse File** on Load, document codecs on round-trip, workspace sync for disk mapping. Future transports (API paste, SaaS connectors, agent replies) reuse the same unit with different wire and staging; see [[details/parse-persist.md]].

## File channel — first transport instance

Disk is one transport channel, not the whole layer:

- **Upload / Download** — move bytes between App and Server.
- **Parse** — turn server files into Graph content (Parse File job as Server **Actor**).
- **Codec round-trip** — document-formats Parse/reconcile on File Node bodies for editable external copies.

The *Work with my documents from anywhere* Epic is the person job; channels include disk and future Google (Drive/Docs, example), all through transport-layer. Current Chapters are the disk beat (auto upload/download, workspace mapping). Transport-layer owns the pattern those Projects implement.

## ESO Actor boundary

Every transport that **mutates** the Graph posts **Changes** through the ESO path. A person editing in the Browser, **Parse File**, an LLM connector **Actor**, or a future shell command are the same kind of producer.

Transport-layer does not define merge, Poll, or job identity — [[.scratch/event-sourced-ops/overview.md]] does. Transport-layer requires every module to use that path. **Load** (Fetch residency) stays Graph transfer, not Change replay; inbound materialization and ongoing **Sync** both matter.

## Graph authority

Outside copies are editable views or exports. The Graph is the single truth for structure and content authority. Round-trip does not create a parallel model; re-import produces **Update** **Changes**. Conflict handling follows ESO merge rules.

## Reading order

1. This file — framing and three flows.
2. [[map.md]] — existing legs and future connector checklist.
3. [[details/parse-persist.md]] — Parse/Persist primitive, shared vs per-transport.
4. [[.scratch/roadmap/reports/hub-epic-framing.md]] — Epic fit and PKM dependency.
5. [[.scratch/event-sourced-ops/overview.md]] — mutation foundation below this layer.
