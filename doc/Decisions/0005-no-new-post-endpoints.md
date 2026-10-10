# No new POST endpoints

Client requests join the client event queue and post through `POST /ambit/events`. The mailbox posts each Event to its target, so a new POST endpoint is not a door. The Load command still sends a core message on `POST /ambit/load-save-command` until [24 — Load and Save on the events list](plan/github-transport/issues/24-load-save-on-events.md) moves it onto that queue.
