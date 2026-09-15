# 34b-corrections.md

Actors are not supposed to be in Core.  They must be passed in.
TestActor and CoreRuntime are at fault.

Credentials should not be passed around everywhere.  They should be strictly in mailbox?
Credentials should not have its own mailbox.
Why the indirection of credential functions into a type?

IncludedDescendantIds is still in terms of Loaded rather than Unfolded
It needs to be a function operating on ViewModel, not Model Graph.
