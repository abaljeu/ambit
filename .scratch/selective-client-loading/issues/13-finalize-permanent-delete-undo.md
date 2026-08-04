# Finalize permanent Delete undo semantics

Type: grilling
Status: open
Blocked by: 09

## Question

How should undo and redo represent and reverse a permanent Delete when the fully resident server promotes a Ref occurrence hidden from the client, including canonical expansion delivery, history-stack state, and an immediate Undo before the client learns the remote promotion, without loading hidden parent lists?
