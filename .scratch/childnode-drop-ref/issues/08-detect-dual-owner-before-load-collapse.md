# Detect dual-Owner before load collapse

Type: grilling
Status: open
Blocked by: 01

## Question

Once edge `ref` cannot express a second Owner in the live model, how does load
detect dual-Owner signals in DB `node_children.ownership` (and legacy JSON `ref`)
**before** collapsing into a single `Node.owner` — and what is the repair or fail
policy when that signal appears?
