# 12 — Pointer: Launch Actor (Focus registration)

**Type:** pointer
**Status:** done
**Blocked by:** None

## Context

Solid core v1 ([core-creation](../../core-creation/project.md)) is implemented (Alan 2026-10-07).

## Pointer

- **Source (history):** [15 — Launch an Actor and hold the span](../../core-creation/issues/15-launch-actor-and-hold-span.md)
- **Owned / continued here:** no open work.
- **Why v2:** Focus registration. The span model is superseded.

## What to build

Focus registration is landed. A second live Focus does not register: a start error stores ActorStart and ActorStop with reason ActorFailed ([22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md)).

## See also

[core-refinement](../project.md), [core-refinement architecture](../arch.md), [15 — Launch an Actor and hold the span](../../core-creation/issues/15-launch-actor-and-hold-span.md)

Launch registration stays on this pointer. The ordered stream calls that launch from the events list, after the edits in the same list. [22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md).
