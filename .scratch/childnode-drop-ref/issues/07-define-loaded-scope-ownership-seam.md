# Define Loaded-scope ownership seam

Type: grilling
Status: open
Blocked by: 01

## Question

Under selective load, when a child list is Loaded (those child nodes and their
`owner` fields are resident), what must the Graph/Op seam assure for membership ↔
`Node.owner` — including: agreement when the claimed owner parent is Loaded; what
stays unprovable when that parent is Unloaded; reject vs repair on violation; and
when this seam becomes mandatory relative to dropping edge `ref`?
