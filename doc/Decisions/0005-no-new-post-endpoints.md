# No new POST endpoints

All activity on the server must be serialized; but to reduce latency the client does not wait (when it can be avoided) for a response.
Therefore, decision: There is to be one door for posting command messages to the server, i.e. one POST endpoint /events, and new event types do not result in new POST endpoints.
Notwithstanding this, there may be other endpoints that remain to be deleted.