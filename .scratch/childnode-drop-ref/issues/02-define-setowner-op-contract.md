# Define SetOwner op contract

Type: grilling
Status: resolved
Blocked by: 01, 07

## Question

What is the exact `Op.SetOwner` shape (args, undo/redo, validation), when must it appear relative to `Replace` inside a Change, and what happens for NewNode defaults, reparent/Owner transfer, and Ref-only attaches that must not change `Node.owner`?

## Answer

- **Shape:** `Op.SetOwner of nodeId * oldOwner * newOwner` — mutates `Node.owner` only; undo swaps old/new.
- **Transfer:** same Change also `Replace`s old/new parent Children (remove Owned appearance / insert under new parent). Ref-only attach = `Replace` without `SetOwner`.
- **Order:** any Op order inside the Change.
- **Integrity:** Change-complete — after apply, Loaded-scope validation must pass; else reject. **Apply → Check → Undo (no history recorded)** is valid enforcement.
- **NewNode / NewSpecialNode:** take an **owner argument** at construction; `SetOwner` only for later transfers.
- **Lands:** spine step 3.
