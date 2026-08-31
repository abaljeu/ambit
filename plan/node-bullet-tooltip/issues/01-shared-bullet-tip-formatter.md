# 01 — Shared `bulletTip` formatter

**Context:** A Node occurrence's Bullet glyph hides identifying facts (Guid, residency, workspace
path, Update Time, CSS classes). Before the Client can show a hover tip, the pure formatting logic
needs to exist and be verified in isolation.

**What to build:** A pure Shared function that, given a `VM`, a `Node`, and an injected
`formatLocal` timestamp renderer, returns the `\n`-joined Bullet tip text with each fact line
self-gated (present only when applicable) and in the fixed order: Guid tail → residency →
workspace path → Update Time → CSS classes. Returns `""` when nothing applies beyond the
always-present lines.

**Blocked by:** None — can start immediately.

**See also:** [[.scratch/node-bullet-tooltip/spec.md]] (Implementation Decisions, Testing
Decisions), [[.scratch/node-bullet-tooltip/grill-notes.md]].

**Status:** ready-for-agent

- [ ] `ViewModelRowState.bulletTip (formatLocal: System.DateTime -> string) (model: VM) (node: Node) : string` added in Shared, matching the spec's signature shape.
- [ ] Guid tail line always present (`NodeId.GuidTail8 node.id.Value`).
- [ ] Residency line always present, text distinguishing `documentState` (`Current` / `Unparsed` / `NoServerFile`) and `childrenStatus` (`Loaded` / `Unloaded`), disambiguating hollow-by-Unloaded vs hollow-by-Unparsed.
- [ ] Workspace path line present only when `NodeDesktopPath.pathForNodeId` resolves to a `//label/relative` path; omitted otherwise (browser-only / unmapped / non-file Nodes). Never the absolute `%LocalAppData%` form.
- [ ] Update Time line always present via `formatLocal node.updateTime`; stub in tests verifies it is invoked with `node.updateTime` and its output appears verbatim.
- [ ] CSS classes line present only when `CssClass.toList node.cssClasses` is non-empty; the Node's own classes only (never the assembled row `className`); this line is always last.
- [ ] `tests/Shared.Tests/ViewModelRowStateTests.fs` (or a new test module following its fixture style) covers: minimal Node (Guid tail + residency + Update Time only), hollow-by-Unloaded vs hollow-by-Unparsed, Loaded+Parsed leaf, Node with resolvable workspace path, Node with CSS classes, chevron vs leaf Node line order stability.
- [ ] Focused `dotnet test` run for the new/changed test file passes.
