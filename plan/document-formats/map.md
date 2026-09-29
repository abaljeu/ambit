# Document formats

Labels: wayfinder:map

## Destination

The remaining document formats for Workspace files after the current file-model baseline, including XML read/write.

## Notes

- Charted from [[plan/roadmap/issues/03-retire-index-development-sequence.md]]. The index item Workspace file model and persistence is current for snapshots, persist, and file-status; format remainder (XML and other still-draft codecs) lives here.
- The workspace format specs now live in this project: [Amb](plan/document-formats/workspace-format-amb.md), [plain](plan/document-formats/workspace-format-plain.md), [markdown](plan/document-formats/workspace-format-md.md), [XML](plan/document-formats/workspace-format-xml.md), [code](plan/document-formats/workspace-format-code.md), [C-style braces](plan/document-formats/workspace-format-cstyle-braces.md), and [dispatch](plan/document-formats/workspace-format-dispatch.md). [[doc/roadmap/workspace-file-model.md]] stays in the roadmap.
- Implemented placement and persistence stay in [[doc/current/workspace-graph.md]] and [[doc/current/workspace-stage-plan.md]].
- 2026-09-29 — Alan locked markdown lists, sentences, and tables. The implement ticket is [03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md). Chapter [Markdown codec](plan/roadmap/epics/chapters/markdown-codec.md) already points at this map for the `.md` File codec.

## Decisions so far

- This is a feature-set Project, not an Epic.

## Not yet specified

- Which formats are in the first destination besides XML.
- Expand-to-parse vs [[plan/selective-client-loading/]] (residency/Load, not codec).

## Out of scope

- Desktop local files and workspace mapping (current; not this Project).
- Rewriting every [[doc/roadmap/]] format file in this charting pass.
