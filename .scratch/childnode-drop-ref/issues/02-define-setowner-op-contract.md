# Define SetOwner op contract

Type: grilling
Status: open
Blocked by: 01, 07

## Question

What is the exact `Op.SetOwner` shape (args, undo/redo, validation), when must it
appear relative to `Replace` inside a Change, and what happens for NewNode defaults,
reparent/Owner transfer, and Ref-only attaches that must not change `Node.owner`?
