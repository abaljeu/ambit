# 01 — Shared success envelope expand (behavior-identical)

**Context:** Today Post and Poll use different success shapes, and Post success is a confirmation echo. The accepted design keeps two channels (Post signals external Changes and notes a baseline; Poll carries the Change list for rewind and replay) while preferring one shared success response type so the contract is smaller and easier to verify.

**What to build:** One shared success response type used by both Post and Poll encode/decode. It carries last Server-received revision, readiness/stamps as required today, an `externalChanges` (or equivalent) signal, and a Change list that may be empty. In this ticket behavior stays identical: Post still confirmation-echo succeeds with `externalChanges = false` and the Client ignores new fields for apply; Poll still returns its list via the same type. Channels remain separate. Sharing a type must not make Post apply a Change list.

**Blocked by:** None — can start immediately.

**See also:** [[../details/messaging.md]], [[../post-poll-envelope-unify.md]]

**Status:** done

- [x] Post and Poll encode/decode share one success response type including revision, readiness/stamps, external-changes signal, and a possibly empty Change list.
- [x] With `externalChanges = false`, Post confirmation-echo success and Client reconcile behavior match today (no apply from Post body).
- [x] Poll still delivers its Change list through the shared type without collapsing the Post and Poll channels.
