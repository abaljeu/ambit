# Separate the HTTP Adapter from Core Changes

Type: grilling
Status: resolved
Blocked by: 03
Actual: 10m

## Question

Where does authentication and JSON decoding end before the typed Core Changes call, and how does its internal accepted, Reject, deduplication, produced-sequence, and Revision result become the current HTTP acknowledgement while Poll remains the Browser's authoritative Change-delivery path?

## Answer

The `/ambit/changes` route remains pointed at `Api.postChange`. `RouteRegistration` keeps authentication, the client hint, and HTTP body reading. `Api.postChange` is the HTTP Adapter source function. It decodes the body to a typed `Change list`, calls the typed normal Post Change function, adds the current build and protocol response fields to the typed accepted facts, and encodes the existing HTTP acknowledgement. It maps decode failures and typed Rejects to the current HTTP responses.

The typed boundary starts at the call below `Api.postChange`. `AgentHandle`, the FileAgent and DbAgent mailbox messages, and their replies carry typed Change input and accepted output instead of JSON strings. Deduplication needs no Adapter branch because it remains a normal accepted result. Poll remains separate and keeps its current Browser behavior.

Parse remains outside Core. After Parse reads a file and produces Changes, it calls the typed Graph-only Post Change function directly. It does not route through `Api.postChange` and does not encode an internal HTTP request. Its existing HTTP response remains unchanged.

The extraction preserves current acknowledgement, Poll, Reject, mirror, timeout, persistence-mode, and Parse behavior. Exact module placement and mailbox ownership remain for [[05-place-core-changes-in-existing-projects]].

## Time

- 2026-09-05 10m — grilled and resolved the HTTP Adapter boundary
