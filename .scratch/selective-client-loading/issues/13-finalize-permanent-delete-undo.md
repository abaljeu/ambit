# Finalize permanent Delete undo semantics

Type: grilling
Status: resolved
Blocked by: 09

## Question

How can Undo and Redo reverse a permanent Delete when the fully resident server promotes a Ref occurrence hidden from the client, without loading that occurrence?

## Answer

- Keep the existing Emacs History model. Each revision applies a `Change`, `Undo`, or `Redo`; the client applies it to projected State and the server to canonical State.
- For a bare permanent Delete, the server promotes an unseen Ref when one exists and otherwise hard-deletes. That expansion changes no fact represented by the submitting client.
- Immediate Undo updates the client locally and waits behind the Delete. The server then applies Undo to its canonical History, reversing any hidden promotion. Persistence and synchronization carry the same history actions, so no hidden list is loaded.
