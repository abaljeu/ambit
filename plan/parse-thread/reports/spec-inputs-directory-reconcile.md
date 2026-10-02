# Directory reconcile inputs

Alan, 2026-10-01: an id is a fair stand-in for a Directory Node when the graph is included. The disk directory is required. The live spec now says that.

Directory reconcile inputs are the disk directory, the graph, and the directory id. The directory id plus the graph is the Directory Node. Directory reconcile reads the Directory body from that graph. The disk directory is required.

## Files

1. [Parse thread architecture](../arch.md) — §2 Module map, item 1 **Directory reconcile**, Interface **Inputs**.
2. [05 — Directory reconcile](../issues/05-directory-reconcile.md) — §1 Directory reconcile, the lead sentence and acceptance **Inputs**.
3. [Parse thread](../project.md) — Notes, 2026-10-01 Alan input decision. Stage is unchanged.

## Left as they stood

1. [Parse thread spec](../spec.md) — no sentence states those arguments.
2. [Parse thread architecture](../arch.md) — no second sentence says the body takes a Directory Node and the nodes tied to the Directory File as the arguments.
3. Code, tests, and the code-review report — not edited.
