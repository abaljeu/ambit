# Define JSON owner-then-drop-ref wire windows

Type: grilling
Status: open
Blocked by: 01

## Question

For [[src/Shared/Serialization.fs]] node/child JSON (graphs, Replace children, change
log / API payloads): when does encode start writing `Node.owner`; what decode default
applies for old payloads that omit it; when do readers stop depending on edge
`ref=Owner`; when does encode stop writing child `"ref"`; and how long (if at all)
must decode still accept legacy child `"ref"` afterward?
