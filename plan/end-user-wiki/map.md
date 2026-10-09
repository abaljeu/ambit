# End-user wiki

Labels: wayfinder:map

## Destination

A browsable wiki that describes the software for people who use it: what Ambit is, what a Graph and a Node are in use, and how to operate the App and Browser.

## Notes

- Charted from [[plan/roadmap/map.md]] after [[plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md]].
- [[doc/current/]], [[doc/roadmap/]], and [[doc/reference/]] are engineer-facing. The end-user wiki home is [[doc/user/README.md]].
-  is the agent glossary, not user documentation.
- Sister Projects: [[plan/architecture/map.md]] (how it is coded and run), [[plan/marketing-wiki/map.md]] (uses, not how-to).

## Decisions so far

1. Audience is people who use the software, not agents and not campaign readers.
2. [02 — Choose the end-user wiki home](plan/end-user-wiki/issues/02-choose-wiki-home.md) — The home is a `doc/` subtree in the ambit repo. The folder is `doc/user/`. Alan, 2026-10-09.
3. [05 — Boundary vs Architecture and Marketing wiki](plan/end-user-wiki/issues/05-boundary-vs-architecture-and-marketing.md) — The end-user wiki is how to operate Ambit. Plan and architecture are how it is built and run. User pages may link to architecture pages. User pages must not depend on them. Alan, 2026-10-09.
4. [03 — Navigation and page set besides documents from any connected device](plan/end-user-wiki/issues/03-navigation-and-page-set.md) — The starting set is Guide (Start, Concepts) and Reference (Commands, Config, Amble Language, Document Formats). This set will grow. It is not the final list. Pages may be added later. Alan, 2026-10-09.

## Not yet specified

1. How to work with documents from any connected device. That how-to lands under the Guide. The page is not written. [01 — Describe documents from any connected device](plan/end-user-wiki/issues/01-describe-documents-from-any-connected-device.md).
2. Which existing [[doc/]] pages are linked vs rewritten in user language. [04 — Which existing doc pages to link vs rewrite](plan/end-user-wiki/issues/04-which-doc-pages-to-link.md).
3. Boundary vs the marketing wiki (uses). [06 — Boundary vs marketing wiki](plan/end-user-wiki/issues/06-boundary-vs-marketing-wiki.md).

## Out of scope

- Agent-instruction files under `.cursor/` and [[doc/agents/]].
- A marketing campaign.
- Implementing product features; this Project describes the software.
