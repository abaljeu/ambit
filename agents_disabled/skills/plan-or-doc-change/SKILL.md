---
name: plan-or-doc-change
description: Layered change across map, spec, architecture, ticket, and code. Use when one of those artifacts should change, or when a requested fix might belong at a different layer than where it was named.
---

# Plan or Doc Change

The stack is **map → spec → architecture → ticket → code**. Each layer states only what must be true across all valid realizations below it. A layer that admits only one realization is **overspecified** — **strip upward**: remove the clause there and place the specific choice down. Tickets consume architecture; cross-ticket coupling belongs in architecture. Signals that reach above the map are logged; the map stays a map.

## Recipe

1. **Find the owning layer.** The user names where they saw the problem, not where it lives. Name the highest layer whose invariant or clause must change for the request to hold. Done: one **owning layer** is named, it is the highest that must change, and the user's named location is recorded when it differs.

2. **Check upward.** Read the next layer up, until you've reached a layer that doesn't need change. When a clause above forces the ugliness and is not load-bearing to intent, **strip upward**. Done: every layer above the owner has been read, and each forcing clause is either kept as load-bearing to intent or stripped with the choice moved down.

3. **Declare scope.** Before editing, state which layers will change and which will not. Done: the reply names every layer on the stack as change or no-change.

4. **Apply top-down.** Update affected layers from highest to lowest. Regenerate or flag every lower artifact that no longer matches. Done: every layer marked change is updated in that order, and every stale lower artifact is regenerated or flagged.

5. **Check the ratchet.** If an upper layer grew longer or more conditional, it is now **overspecified** — **strip upward** and restore it. Done: no layer above the owner is longer or more conditional than before this change.
