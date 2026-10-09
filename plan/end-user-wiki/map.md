# End-user wiki

Labels: wayfinder:map

## Destination

A browsable wiki that describes the software for people who use it: what Ambit is, what a Graph and a Node are in use, and how to operate the App and Browser.

## Notes

- Charted from [[plan/roadmap/map.md]] after [[plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md]].
- [[doc/current/]], [[doc/roadmap/]], and [[doc/reference/]] are engineer-facing. The end-user wiki home is [[doc/user/README.md]].
-  is the agent glossary, not user documentation.
- Sister Projects: [[plan/architecture/map.md]] (how it is coded and run), [[plan/marketing-wiki/map.md]] (why to use it).

## Decisions so far

1. Audience is people who use the software, not agents and not campaign readers.
2. [02 — Choose the end-user wiki home](plan/end-user-wiki/issues/02-choose-wiki-home.md) — The home is a `doc/` subtree in the ambit repo. The folder is `doc/user/`. Alan, 2026-10-09.
3. [05 — Boundary vs Architecture and Marketing wiki](plan/end-user-wiki/issues/05-boundary-vs-architecture-and-marketing.md) — The end-user wiki is how to operate Ambit. Plan and architecture are how it is built and run. User pages may link to architecture pages. User pages must not depend on them. Alan, 2026-10-09.
4. [03 — Navigation and page set besides documents from any connected device](plan/end-user-wiki/issues/03-navigation-and-page-set.md) — The starting set is Guide (Start, Concepts) and Reference (Commands, Config, Amble Language, Document Formats). This set will grow. It is not the final list. Pages may be added later. Alan, 2026-10-09.
5. [04 — Which existing doc pages to link vs rewrite](plan/end-user-wiki/issues/04-which-doc-pages-to-link.md) — A subject is in scope when Concepts or Reference names it. Link that doc page. An out-of-scope subject has no link. Actors can be linked. Mailbox has no link. Rewrite detail into Reference when needed. Alan, 2026-10-09.
6. [06 — Boundary vs marketing wiki](plan/end-user-wiki/issues/06-boundary-vs-marketing-wiki.md) — Marketing is why to use Ambit. This work does not cover it. Alan, 2026-10-09.
7. [01 — Describe documents from any connected device](plan/end-user-wiki/issues/01-describe-documents-from-any-connected-device.md) — The Guide page is [Connecting your Documents](../../doc/user/guide/connecting-your-documents.md). The steps are not written. Alan, 2026-10-09.

## Out of scope

- Agent-instruction files under `.cursor/` and [[doc/agents/]].
- A marketing campaign.
- Implementing product features; this Project describes the software.
