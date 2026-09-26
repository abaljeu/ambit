# 03 — Lock ongoing want priority and when wants are attached

**Type:** grilling
**Status:** done
**Blocked by:** None
**Actual:** 10m

## 1. Question

When does the Browser attach wants to post-Event and Poll, and what happens when the Want list is empty?

Ongoing computation is locked to Fold-aware Included Nodes that miss Children. After an answer is installed, the Browser computes Included again; newly Included Children may enter the next Want. Reserved Nodes and the Focus path are bootstrap, not an ongoing tier. Auto wants need no click and no command.

Lock:

1. **Attachment moment** — Does every post-Event and every Poll carry the current Want list, or only some of those doors?
2. **Empty wants** — When no Included Node misses Children, and those Children are not yet a Want, does the Browser omit the field, send an empty list, or skip the Want door entirely?
3. **Cadence** — Is Poll the steady want door, with post-Event as the after-Change door, or do both always carry the same Want? What stops a tight Poll loop from repeating a Want the Server already answered?

## Answer

1. **Compute at send time** — Before each Poll and post-Event, the Browser runs `Want.compose` against the current Graph, SiteMap, and Zoom root. `Want.compose` returns Fold-aware Included Nodes that miss Children.
2. **Every Sync door** — Every Poll and every post-Event carries the computed `want` field. Empty is required `[]`; the Browser does not omit the field and does not skip the Sync door.
3. **Recompute after install** — Events apply first, then the Want answer installs. The next send computes again, so newly Included Children can enter the next Want without a separate second tier.
4. **Repeats allowed** — A tight Poll loop may repeat a Want. The answer and install are idempotent for the same Graph facts. There is no acknowledgement set, throttle, batching, or backpressure state.

## Time

- 2026-09-26 10m — accepted compute-at-send cadence and repeat rule (from chat)
