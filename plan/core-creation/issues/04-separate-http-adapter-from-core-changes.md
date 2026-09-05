# Separate the HTTP Adapter from Core Changes

Type: grilling
Status: open
Blocked by: 03

## Question

Where does authentication and JSON decoding end before the typed Core Changes call, and how does its internal accepted, Reject, deduplication, produced-sequence, and Revision result become the current HTTP acknowledgement while Poll remains the Browser's authoritative Change-delivery path?
