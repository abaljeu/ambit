# Transport layer — overview

The top layer of this project. It gives what transport-layer is and how outside information moves through Gambol. Project card: [[project.md]]. Leg chart: [[map.md]]. Parse/Persist detail: [[details/parse-persist.md]].

## What transport-layer is

Transport-layer is the cross-cutting pattern for **moving information** between outside sources and the Graph. It is not one connector, one codec, or one Epic. It is the shared contract: inbound (outside → Graph), outbound (Graph → outside), and round-trip (export → edit elsewhere → re-import) while the **Graph stays authority**.

Prior work called this the import layer or information hub. **Transport-layer** names the same concept with clearer scope: every outside source is a transport instance; Parse/Persist is the fundamental text-processing unit reused across them.

## Three flows

| Flow | Direction | Goal |
| --- | --- | --- |
| **Inbound** | Outside → Graph | Materialize external data into the Graph for examination and editing. |
| **Outbound** | Graph → outside | Push a Graph slice to an external destination; Graph remains canonical. |
| **Round-trip** | Graph ↔ outside | Export, edit elsewhere, re-import; updates land as **Changes**, not a second truth. |

Each flow uses the same building blocks: plan from a **Local Graph**, optionally stage for examination, then emit **Changes** on the ESO Actor path.

Wiki publish (Public URL in [[plan/roadmap/epics/build-or-explore-a-wiki.md]]) is an outbound instance (Graph / `.md` File content → HTML for visitors), not disk Parse File and not HTML File web-site publish; it still uses Parse/Persist as the text-processing unit.

Web-site publish ([[plan/roadmap/epics/create-and-publish-web-pages.md]]) is outbound transport: generate HTML and send attachments and CSS through transport-layer (Graph / HTML File content → visitor-facing site).

## Parse/Persist as core text-processing unit

**Parse** and **Persist** are not only Load-stage names for disk files. Together they form the transport primitive for text:

- **Parse** — text (or bytes interpreted as text) in → reconcile with the Graph → produce **Changes** (or a staged view for examination).
- **Persist** — Graph slice (typically File Node content) out → text for an outside destination.

Desktop App sync is the first fully charted instance: **Parse File** on Load, document codecs on round-trip, and Upload and Download between the App folder and Server DataDir. Future transports (API paste, SaaS connectors, agent replies) reuse the same unit with different wire and staging; see [[details/parse-persist.md]].

## Desktop App sync — first transport instance

Sync with the Desktop App is one transport instance of this layer. Upload and Download move file bytes between the App folder and Server DataDir over WebDAV.

- **Upload** — move bytes from the App folder to Server DataDir.
- **Download** — move bytes from Server DataDir to the App folder.
- **Parse** — turn server files into Graph content.
- **Codec round-trip** — document-formats Parse and reconcile on File Node bodies for editable external copies.

This redesign is Desktop App sync. GitHub stays the separate external remote on [[plan/github-transport/project.md]].

The [Work with my documents from anywhere](plan/roadmap/epics/work-with-text-files-from-anywhere.md) Epic is the User Epic. The current disk beat is this Desktop App sync (Upload, Download, and workspace mapping). A later Google (Drive/Docs) source is another instance of the same layer. Transport-layer owns the pattern those Projects implement.

## ESO Actor boundary

Every transport that **mutates** the Graph posts **Changes** through the ESO path. A person editing in the Browser, an LLM connector **Actor**, or a future shell command are the same kind of producer.

Transport-layer does not define merge, Poll, or job identity — [[plan/event-sourced-ops/overview.md]] does. Transport-layer requires every module to use that path. **Load** (Fetch residency) stays Graph transfer, not Change replay; inbound materialization and ongoing **Sync** both matter.

## Graph authority

Outside copies are editable views or exports. The Graph is the single truth for structure and content authority. Round-trip does not create a parallel model; re-import produces **Update** **Changes**. Conflict handling follows ESO merge rules.

## Reading order

1. This file — framing and three flows.
2. [[map.md]] — existing legs and future connector checklist.
3. [[details/parse-persist.md]] — Parse/Persist primitive, shared vs per-transport.
4. [[plan/roadmap/reports/hub-epic-framing.md]] — Epic fit and PKM dependency.
5. [[plan/event-sourced-ops/overview.md]] — mutation foundation below this layer.
