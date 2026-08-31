# Define Loaded-scope ownership seam

Type: grilling
Status: resolved
Blocked by: 01

## Question

Under selective load, when a child list is Loaded (those child nodes and their `owner` fields are resident — but `Node.owner` may name a node **absent from the graph**, not only an Unloaded parent), what must the Graph/Op seam assure for membership ↔ `Node.owner` — including: agreement when the claimed owner parent is resident and Loaded; what stays unprovable when that parent is absent or Unloaded; reject vs repair on violation; and when this seam becomes mandatory relative to dropping edge `ref`?
Distinguish client Loaded-scope checks (partial graph; owner may be absent) from **server** edit apply, which already validates ownership and rejects invalid Owners (`History.validateOwnershipSemantics` on shape-changing ops).

## Answer

- **Provable** (claimed owner parent Resident + Loaded child list): membership ↔ `Node.owner` must agree. On disagreement → **reject** (no auto-repair of field or Children).
- **Unprovable** (owner parent Absent or Unloaded): accept; do not invent a violation.
- **Authority:** Server apply remains the reject authority; Browser partial Graph uses the same Loaded-scope rules locally where applicable.
- **When mandatory:** with `SetOwner` / Change-complete check (spine step 3); on all shape/ownership Changes by step 6 — **before** stopping edge `ref` encode (step 7) and deleting the field (step 8).
