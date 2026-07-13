# Workspace Text Outline Conversion

Status: Draft

Authority: Target design for converting between document files and outline structure. Mixes settled commitments with open questions; each section marks which.

See also: [[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/workspace-file-persistence.md]], [[doc/current/workspace-stage-plan.md]], [[doc/roadmap/reference-expressions.md]], [[doc/roadmap/workspace-format-amb.md]], [[doc/roadmap/workspace-format-md.md]], [[doc/roadmap/workspace-format-plain.md]], [[doc/roadmap/workspace-format-xml.md]]

This document defines the separate conversion step used by the main import and export process. The workflow itself stays in the main process docs; this file only defines how text content becomes outline structure and how outline structure becomes text content again.

Per-format line grammar, identity encoding, and reference syntax live in separate format documents. This file defines the generic conversion contract and how those formats plug into import/export.

## Scope

This spec covers the conversion boundary between a text file and an outline tree. It does not define desktop transfer, server persistence, or user command flow.

## Settled

These are committed.

- **Bidirectional transformation.** The two directions are one paired unit, not independent functions. Neither side is a pure view of the other: a file may drop outline content (exact layout, node identity), and the outline holds content a file cannot represent. This is a symmetric transformation with a per-side **complement**: the information each side must preserve across a round trip though the other cannot represent it. Objectives:

  1. **No-op stability**: a round trip carrying no change produces no change.

  2. **Propagation**: a real edit survives the round trip.

  3. **No silent loss**: content that cannot survive *the conversion itself* is reported, never dropped or altered silently. This is about conversion fidelity, not about an external editor deleting text (see **Deletion on import**).

  4. **Determinism**: one defined result per input, except for any freshly minted identity.

- **Export/import asymmetry.** Export is delta-driven: the server file is continuously the projection of its subtree, updated per operation (`file_next = f_out(file_prev, op)`). Import is state-based: an externally edited file is reconciled against the current subtree (`out2 = f_in(file1, out1)`). Asymmetric because identity recovery is only hard where edits are untrusted.

- **Subtree mapping.** The outline spans many documents; each document (usually rooted at a `File` node) maps to one persisted artifact, and import is scoped to that document's members.

- **Ownership migration (implemented).** Every node has one owner; removing it from its owner promotes another reference to owner, transferring the subtree. A cross-document reference can thereby become contained content, and undo reverses it. Identity handling must tolerate a node's document membership changing. (When a node changes owner, the node is replaced with an updated node; all references to that node shall consider if re-persistence is needed.)

- **Per-format specs.** Format-specific rules are defined in separate documents. This doc states requirements each format must satisfy; it does not duplicate line grammar.

- **Access unit (stage 1).** Import and export operate on the whole persisted artifact for one document and its member nodes. Section windows — reconciling a matched slice while preserving text outside the window — are deferred; see **Later** below.

- **Stable identity in the artifact.** Import recovers node identity from durable ids written in the file, not from structural alignment alone. This applies to Owner and Ref lines within a file and to cross-file references. A file must not need rewriting when another file changes merely because a peer was edited or moved. Backward compatibility with the ephemeral `#n1` short-id scheme in [[src/Shared/Snapshot.fs]] is not required; workspace `.amb` **replaces** that scheme rather than extending it.

- **Format-specific reference encoding.** How a reference to a node is written readably and matched persistently through edits is defined per file format — not in this doc. Each format spec must pair export and import rules and reconcile with [[doc/roadmap/reference-expressions.md]] where the target is addressable that way. Stable `NodeId` remains authoritative; the readable anchor is `#name`, which Ambit will ensure is unique within its file, but how that is stored in a reference to that node is format-specific.

- **Deletion on import.** When a node with a known stable id exists in the current subtree but is absent from the externally edited file, that is an external-editor deletion, not conversion loss. Resolution reuses existing graph semantics: if the node has an external reference, change-owner (ownership migration) semantics apply; if it has none, the delete-to-trash mechanism applies, within the outliner. Conversion does not invent a separate deletion path.

## File Formats

| Format | Document | Notes |

|--------|----------|-------|

| `.amb` (native) | [[doc/roadmap/workspace-format-amb.md]] | [[src/Shared/Snapshot.fs]] is a pre-workspace baseline only; workspace `.amb` replaces its id scheme. |
| `.md` | [[doc/roadmap/workspace-format-md.md]] | External-editor format; heading hierarchy maps to outline depth. |
| other text | [[doc/roadmap/workspace-format-plain.md]] | Non-XML text extensions; indent-only hierarchy; infer and preserve indent style. |
| XML | [[doc/roadmap/workspace-format-xml.md]] | Well-formed XML element tree; HTML excluded; structure and content preservation per base XML spec. |

## Content Conversion

The pure content conversion pairs with identity and reference encoding defined per format.

Each format document defines:

- **Text to outline** — line-to-node mapping, parentage, blanks, invalid text, stable id matching on import, reference import grammar.
- **Outline to text** — hierarchy to line order, content and indentation, stable ids, reference export grammar, unsupported structures.

Each rule should name its counterpart in the other direction.

## Shared outline LCS reconcile

Import/warm reconcile for outline-backed text formats uses a shared mechanism in `src/Shared/dotnet/` (`OutlineLcs` + `OutlineReconcile`, DiffPlex-backed). Format codecs flatten to / project from `{ depth; text; nodeId option }` lines; sequence diff + disposition policy are format-agnostic.

- Match key is **text only** (not depth). DiffPlex equal → keep with edited depth/text; insert → mint; delete → external deletion semantics below.
- Duplicates / blank runs: after DiffPlex, pair identical keys in order between neighboring unique anchors (positional tie-break). Optional hard-match anchors (e.g. Plain ` #name-token`) are deferred.
- Minimal in-place edit pass: among unmatched adjacent slots, pair one delete+insert as keep with new text when depths match.
- Export stays operations-driven / previous-text byte preservation; LCS is the import/warm story only.

Amb/XML calling this API is deferred; Plain warm path is the first consumer.

## Generic text reconciliation

Authoritative spec: [[doc/roadmap/workspace-format-plain.md]]. That document defines line mapping (blanks both ways), identity, operations-driven export, LCS-based reconciliation outcomes, and verification targets for `Special File` artifacts whose path is neither `.amb` nor `.md`. Workspace and directory documents remain on `.amb`; markdown remains deferred.

This conversion doc supplies the shared contract: three reconciliation inputs and required outcomes in **Settled**; external deletion semantics in **Deletion on import**; shared outline LCS above.

## Generic XML reconciliation

Authoritative spec: [[doc/roadmap/workspace-format-xml.md]]. That document defines tree mapping, structural classes (`xml-*`), operations-driven export, reconciliation by change kind, move asymmetry, references, and verification targets for `File` artifacts whose persisted body is well-formed XML. Classification uses a heading scan of artifact text; HTML-shaped headings are excluded. Workspace and directory documents remain on `.amb`; markdown remains deferred.

This conversion doc supplies the shared contract only: three reconciliation inputs and required outcomes in **Settled**; external deletion semantics in **Deletion on import**.

## Later

Not stage 1; design must not foreclose these.

- **Section windows.** A request may eventually edit a matched **section** of a file: reconcile only the corresponding outline subtree and write back without altering text outside the window. Stage 1 still writes the whole file; reconciliation APIs and format grammar should remain definable on a scoped `(file, subtree)` pair rather than baking in irrecoverable whole-file-only semantics at the conversion layer.

## Open Questions

Under consideration; not committed.

- **Loss / error reporting model.** "No silent loss" is settled, but the reporting shape is TBD. Candidates: structured diagnostics with partial apply; fail-whole-conversion; or tiered by severity (hard errors block, warnings proceed).

## Non-Goals

- desktop file transfer mechanics (separate project)
- server write ordering (last write wins)
- workspace identity and persistence (just know workspace+filepath resolves to a unique dir/file on disk)
- unrelated graph operations (only consider here what an Op does to the persisted server file)
- per-format line grammar and reference syntax (lives in format documents)
- section-window editing (stage 1; see **Later**)
- backward compatibility with [[src/Shared/Snapshot.fs]] short ids for workspace `.amb`

## Verification Targets

Generic conversion (all formats):

- text converts to a deterministic outline; outline converts back to deterministic text
- export of an unchanged outline leaves the file unchanged
- import of an unchanged file produces no operations
- a real edit survives the round trip
- content that cannot round-trip is reported, not dropped or altered
- invalid text and unsupported structures are reported explicitly
- stage 1 import and export cover the whole file subtree end to end
- import matches nodes by stable id across reorder, text edit, and ownership migration
- editing file B does not require rewriting file A when A holds a cross-file reference to a node in B

Generic text slice (Stage 7 Step 5) — see [[doc/roadmap/workspace-format-plain.md]] § Verification Targets for format-specific cases. Shared tests: `PlainTextDocumentTests` (codec parse/write/reconcile), extended `DocumentAssemblyTests` (path → codec dispatch). Server tests: extended `DocumentPersistenceTests` (`readme.txt` writes plain text, reads back through plain codec; `.amb` artifacts unchanged). Loader tests (`DocumentLoaderTests`) only if dispatch changes startup behavior.

XML slice (Stage 7 Step 6) — see [[doc/roadmap/workspace-format-xml.md]] § Verification Targets. Shared tests: `XmlDocumentTests`, extended `DocumentAssemblyTests` (Xml dispatch). Server tests: extended `DocumentPersistenceTests` (XML file round-trip). Implementation plan: [[doc/reference/formats/xml-round-trip-plan.md]].

