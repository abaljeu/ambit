# Command-line actor

Stage: slice
Summary: Run on a node whose text starts with `>` starts a server actor that runs the program named by the first word with the rest as arguments, in the node's enclosing directory, and turns the output lines into child nodes.
Updated: 2026-11-21
Started: 2026-11-21

## Notes

- Map: [map.md](map.md)
- Spec: [spec.md](spec.md)
- Architecture: [arch.md](arch.md)
- The product owner answered every open question on 2026-11-21. The spec has no open questions, the architecture is written, and every ticket is `defined`. Stage moves from `spec` to `slice`. Nothing material stays open.
- The Project sits on Chapter [Act through CLI](../roadmap/epics/chapters/act-through-cli.md), under Epic Agent chat with managed context. The Roadmap is not edited by this Project.
- [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md) leaves `>` handling to this Project. The owner scan of that ticket runs a `>` line with the same Run code.
