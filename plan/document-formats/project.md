# Document formats

Stage: chart
Summary: Remaining document formats (including XML read/write) after the workspace file model baseline.
Updated: 2026-09-29
Actual: 2h

## Notes

- 2026-09-29 — The markdown parse and write slice is [03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md). That ticket is the implement frontier for this slice. The rest of this chart stays open.
- 2026-09-29 — Cold read, warm write, and Persist bytes for that ticket are [Markdown parse and write design](plan/document-formats/arch-md-parse-write.md). Persist file-format processing in that design keeps `DocumentWarm.writeArtifact` as the caller and names the MdDocument write amendments. Stage stays chart. That file is not a project-wide architecture.
