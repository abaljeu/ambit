# 04 — Client consumes merge success without reload

**Context:** Recoverable concurrent kick-back today forces `ServerRejected` / reload and drops unsaved Changes. That Reject/reload path is indirectly a critical information loss. Accepted consume is: note baseline from Post signal, then when the posting queue is empty Poll, rewind to baseline, and replay the Change list. Neither channel clears History.

**What to build:** When `externalChanges` is true (or the ack is not a confirmation echo), the Browser does not enter `ServerRejected` / forced reload. It notes the baseline, and when the posting queue is empty Polls, rewinds to baseline and replays the Change list from the shared envelope. Neither post nor poll clears History. Leftover pending stays planned and unamended for the next post. History must retain Server-originated Changes (not freeze as own-posts-only). Post remains signal-only even though the envelope type is shared.

**Blocked by:** 01 — Shared success envelope expand (behavior-identical), 03 — Server amends recoverable field collisions (text, name, classes)

**See also:** [[../details/client-consume.md]], [[../details/messaging.md]]

**Status:** ready-for-agent

- [ ] Recoverable merge success no longer lands the Browser in forced-reload Reject with lost pending work.
- [ ] Empty posting queue + Poll rewinds to baseline and replays the Server Change list (no in-place optimistic patch).
- [ ] Neither Post nor Poll clears History; leftover pending remains for the next post; History can retain Server-originated Changes.
