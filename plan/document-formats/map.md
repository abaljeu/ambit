* # Document formats
* Labels: wayfinder:map
*#`Destination
  * The remaining document formats for Workspace files after the current file-model baseline, including XML read/write. 
    * Recognize pasted markdown as markdown.

## Notes

- Charted from [[plan/roadmap/issues/03-retire-index-development-sequence.md]]. The index item Workspace file model and persistence is current for snapshots, persist, and file-status; format remainder (XML and other still-draft codecs) lives here.
- The workspace format specs now live in this project: [Amb](plan/document-formats/workspace-format-amb.md), [plain](plan/document-formats/workspace-format-plain.md), [markdown](plan/document-formats/workspace-format-md.md), [XML](plan/document-formats/workspace-format-xml.md), [code](plan/document-formats/workspace-format-code.md), [C-style braces](plan/document-formats/workspace-format-cstyle-braces.md), and [dispatch](plan/document-formats/workspace-format-dispatch.md). [[doc/roadmap/workspace-file-model.md]] stays in the roadmap.
- Implemented placement and persistence stay in [[doc/current/workspace-graph.md]] and [[doc/current/workspace-stage-plan.md]].
- 2026-09-29 — Alan locked markdown lists, sentences, and tables. The implement ticket is [03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md). The slice design is [Markdown parse and write design](plan/document-formats/arch-md-parse-write.md). Chapter [Markdown codec](plan/roadmap/epics/chapters/markdown-codec.md) already points at this map for the `.md` File codec.
- 2026-09-30 — Alan placed the goal here: recognize pasted markdown as markdown. [Paste Document Codec Import](doc/roadmap/paste-document-codec-import.md) deferred that choice; this map owns it.
- 2026-09-30 — Alan locked the [Paste markdown line test](plan/document-formats/map.md) under Decisions so far.

## Decisions so far

- This is a feature-set Project, not an Epic.
- Recognize pasted markdown as markdown. This Project owns that goal.
- **Paste markdown line test.** Paste is markdown when the text has one ATX heading (`# ` with a space, or two or more `#`), or one line that starts with `|`, or two list lines (`- `, `* `, or digits, a period, and a space). One list line stays plain. A lone `#tag` and `#include` stay plain. A markdown hit uses `__paste__.md`. Other paste stays `__paste__.txt`.

## Not yet specified

- Which formats are in the first destination besides XML.
- Expand-to-parse vs [[plan/selective-client-loading/]] (residency/Load, not codec).

## Out of scope

- Desktop local files and workspace mapping (current; not this Project).
- Rewriting every [[doc/roadmap/]] format file in this charting pass.
