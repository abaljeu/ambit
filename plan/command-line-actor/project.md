# Command-line actor

Stage: spec
Summary: Run on a node whose text starts with `>` runs the rest of that text as a shell command in the Azure environment, in the node's directory, and the stdout becomes child nodes.
Updated: 2026-11-21
Started: 2026-11-21

## Notes

- Map: [map.md](map.md)
- Spec: [spec.md](spec.md)
- Architecture, provisional: [arch.md](arch.md)
- Stage stays `spec` while the Open questions in the spec are unresolved. Tickets exist and carry `needs-info` where a question gates them.
- Placement under an Epic or Chapter on the Roadmap is an open question. The Roadmap is not edited by this Project.
- [22 — Run sends = lines to Amble](../online-search/issues/22-run-sends-equals-lines-to-amble.md) leaves `>` lines out of scope. This Project is where `>` Run behavior is specified.
