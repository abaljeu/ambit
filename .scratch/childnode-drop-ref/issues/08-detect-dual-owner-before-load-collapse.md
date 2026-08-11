# Detect dual-Owner before load collapse

Type: grilling
Status: resolved
Blocked by: 01

## Question

Once edge `ref` cannot express a second Owner in the live model, how does load detect dual-Owner signals in DB `node_children.ownership` (and legacy JSON `ref`) **before** collapsing into a single `Node.owner` — and what is the repair or fail policy when that signal appears?

## Comments

- 2026-08-11 grill Round 1: policy = **deterministic repair** — choose exactly one Owned parent; continue load (not fail-hard).
- 2026-08-11: losing extras → **downgrade to Ref** (keep appearances). Encounter-order preferred only if simple; SELECT has no ORDER BY → unstable → rejected.
- 2026-08-11: winner = **lowest parent NodeId**.

## Answer

Detect dual Owned claims (same child, multiple parents with Owned in DB `node_children.ownership` and/or legacy JSON edge `ref`) **before** writing a single `Node.owner`.

Repair (do not fail load): winner = lowest parent `NodeId`; set `Node.owner` to that parent; downgrade every other Owned appearance of that child to Ref. Continue load.
