# 02 — Client wiring: rename, local-time formatter, attach tip

**Context:** With `bulletTip` available in Shared, the Client needs to rename its Bullet binding
and CSS classes to honest "Bullet" vocabulary, supply a real local-timezone formatter, and attach
the resulting tip text as a native `title` attribute — with no change to click/fold/zoom behavior.

**What to build:** Hovering any Node's Bullet (chevron, solid circle, or hollow circle) shows the
native-tooltip Bullet tip with the Node's facts, rendered in the viewer's local timezone. The
Bullet binding and its CSS hooks read as `nodeBullet` / `amb-bullet-*` throughout, with no
behavioral change to any existing listener.

**Blocked by:** 01 — Shared `bulletTip` formatter.

**See also:** [[.scratch/node-bullet-tooltip/spec.md]] (Client attachment, Local-time formatter,
CSS class rename), [[.scratch/node-bullet-tooltip/grill-notes.md]].

**Status:** ready-for-agent

- [ ] In `RowView.Layout.buildRowElement`, rename the local binding `leafBullet` to `nodeBullet` across all three glyph branches, the downstream returns, and `Behavior.wireRow` / `wireSelectingActivate` parameters. No change to listener wiring.
- [ ] Rename CSS classes `amb-leaf-dot` → `amb-bullet-dot` and `amb-leaf-hollow` → `amb-bullet-hollow` at both the class-adding call sites in `RowView.Layout.buildRowElement` and the rule definitions in `src/Server/wwwroot/style.css` (~lines 229–242), in the same change so no rule is orphaned. Cosmetic only — no styling behavior change.
- [ ] Add a net-new Client helper (Fable `Intl.DateTimeFormat`, browser default zone) that renders a UTC `DateTime` to date + time to the minute, for use as the `formatLocal` argument to `bulletTip`.
- [ ] Call `bulletTip` with the real formatter and set `nodeBullet.setAttribute("title", tip)` for every glyph variant when `tip` is non-empty; do not set the attribute when it is empty.
- [x] Manual/HITL check: `PASS — 2026-08-16; chevron, solid-circle, and hollow-circle Bullets each showed the expected hover tip; clicking, folding, and zooming remained normal.`
