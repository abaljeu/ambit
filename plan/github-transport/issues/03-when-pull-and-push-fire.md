# 03 — When pull and push fire

**Type:** grilling
**Status:** needs-info
Blocked by: 02

## 1. Question

When does the Server Actor pull, and when does it push, in round-trip v1?

The Destination locks pull and push on the Server Actor. It does not lock a person Command, a schedule, a post-Persist trigger, or a post-Download trigger.

Grill after [02 — Actor command surface](02-actor-command-surface.md) names the Commands. Do not implement.
