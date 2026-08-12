# Why Safari harsh restart drops auth

Type: research
Status: open

## Question

Why does Safari on Apple (regular tab) often demand a new login after a harsh restart of an inactive Gambol page — despite the server setting a long-lived HttpOnly `gambol_auth` cookie (SameSite=Strict, ~10-year expiry)?

Pin down, with primary sources (WebKit/Safari docs, ITP notes, cookie lifetime rules) and confirmation against our server cookie attributes in [[src/Server/RouteRegistration.fs]] / [[src/Server/AuthToken.fs]]:

- What Safari events count as “harsh restart” vs a normal Refresh (tab discard, process kill, memory reclaim, device sleep/reboot)?
- Under which of those events does Safari drop, partition, or refuse to send first-party cookies?
- Do SameSite=Strict, HttpOnly, path, or expiry interact with Safari’s storage eviction / Intelligent Tracking Prevention in ways that explain a missing `gambol_auth` on return?
- What observable symptom distinguishes “cookie gone” from “cookie present but client routed to login anyway”?

## Comments
