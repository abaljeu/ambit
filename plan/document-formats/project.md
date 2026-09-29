# Document formats

Stage: build
Summary: Remaining document formats (including XML read/write) after the workspace file model baseline.
Updated: 2026-09-29
Started: 2026-09-29
Actual: 4h

## Notes

- 2026-09-29 — [03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md) is in build. Parse and write for lists, sentences, and tables is coded. The rest of this chart stays open.
- 2026-09-29 — Cold read, warm write, and Persist bytes for that ticket are [Markdown parse and write design](plan/document-formats/arch-md-parse-write.md). Persist file-format processing in that design keeps `DocumentWarm.writeArtifact` as the caller and names the MdDocument write amendments. That file is not a project-wide architecture.
