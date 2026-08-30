# Define JSON owner-then-drop-ref wire windows

Type: grilling
Status: resolved
Blocked by: 01

## Question

For [[src/Shared/Serialization.fs]] node/child JSON (graphs, Replace children, change log / API payloads): when does encode start writing `Node.owner`; what decode default applies for old payloads that omit it; when do readers stop depending on edge `ref=Owner`; when does encode stop writing child `"ref"`; and how long (if at all) must decode still accept legacy child `"ref"` afterward?

## Answer

Hard **before/after** wire shape per matched Browser+Server deploy — no mixed omit-compat messages.

| Slice | Wire |
| --- | --- |
| Step 2 | Encode **both** node `owner` and edge `ref`. After this slice, node JSON **requires** `owner` (no omit path). |
| Steps 3–6 | Readers move to `Node.owner` / Loaded-scope; edge `ref` still on wire for children. |
| Step 7 | Stop encoding child `"ref"`; ownership on wire/DB classification from `Node.owner`. Hard cutover — no legacy child `"ref"` decode window. |
| Step 8 | Delete `ChildNode.ref` from the type. |

DB `node_children.ownership` is separate (ticket 03) and stays for this effort.
