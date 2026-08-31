# 23 — Introduce hollow-circle presentation

**Context:** Unloaded residency and Unparsed source state are different facts. Today both show a solid circle next to the Node. Unparsed also keeps its current secondary visual indicator. A Node that is Loaded and Parsed keeps its current look. Hollow circles do not exist yet. Interactive hollow-circle Load is out of scope here; that is issue 28.

**What to build:** Introduce a hollow-circle indicator for Unloaded and Unparsed Nodes (UI presentation only). Keep Unloaded distinct from Unparsed. Keep the current Unparsed secondary indicator. Keep the current look for Nodes that are Loaded and Parsed. Do not wire hollow-circle clicks to Load. Do not change Load command behavior.

**Blocked by:** None — presentation only; click→Load is 28.

**See also:** [[plan/selective-client-loading/spec.md]] (hollow-circle indicator; Unloaded vs Unparsed); [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] (Unloaded vs Unparsed share hollow-circle indicator); [[plan/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]] (click→Load).

**Status:** done

- [x] Unloaded and Unparsed each show the hollow circle.
- [x] Unparsed also keeps its current secondary visual indicator.
- [x] A Node that is Loaded and Parsed keeps its current look.
- [x] Hollow-circle presentation does not dispatch Load or otherwise load resident content.

## Comments

- Split from the former combined hollow-circle ticket: presentation stays here; click→Load moved to [[28-make-hollow-circle-clicks-invoke-load.md]].
- Corrected after feedback: this ticket creates/introduces hollow circles; it does not improve an existing hollow-circle control.
