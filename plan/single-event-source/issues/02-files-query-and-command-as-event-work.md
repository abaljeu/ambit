# 02 — Files, Query, and Command as Event work

**Type:** grilling
**Status:** done
Blocked by: none

## 1. Question

Does “all work goes through Event” include Files (send, get, and git of file bytes) and Query, or only Graph-mutating work and Actor lifecycle? Command launch already records ActorStart. What stays a Core API call that is not an EventLog append?

## Answer

- Files stay Files for this effort (deferred). File upload handling should become an Actor later; that is not this Project.
- Query stays Query. A read is not an Event. The snapshot cursor is event id.
- Run either starts an Actor (ActorStart) or institutes a client-sourced Change Event. That Change Event's `commandName` is the Run command. There is no third EventBody kind.
- Core API calls that are not EventLog appends: Files (bytes) and Query. Graph-mutating work and Actor lifecycle are Events.
