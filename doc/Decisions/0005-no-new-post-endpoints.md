# No new POST endpoints

All activity on the server must be serialized through a single mailbox.  However, to reduce latency the client does not wait (when it can be avoided) for a response to individual messages, but batches messages.
Therefore, decision: There is to be one door for posting core messages to the server, i.e. one POST endpoint /events, and new event types do not result in new POST endpoints.
Notwithstanding this, there may yet be other endpoints that remain to be deleted.