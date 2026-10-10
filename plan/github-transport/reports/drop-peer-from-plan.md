# Drop peer from the github-transport plan

## 1. Result

1. **Prose** — These files changed: [github-transport architecture](plan/github-transport/arch.md), [github-transport map](plan/github-transport/map.md), [github-transport project](plan/github-transport/project.md), [github-transport spec](plan/github-transport/spec.md), and [21 — Server Actor](plan/github-transport/issues/21-server-actor.md).
2. **Rename** — `plan/github-transport/issues/21-peer-actor.md` is now `plan/github-transport/issues/21-server-actor.md`.
3. **Links** — [github-transport map](plan/github-transport/map.md) and [github-transport project](plan/github-transport/project.md) were the only markdown files whose target was `21-peer-actor.md`. Those links now target `21-server-actor.md`. The label is `21 — Server Actor`. A repo search for `21-peer-actor` returns no matches.
4. **Search count** — `rg -i --count-matches peer` on the five files (the renamed ticket included) printed no counts and exited 1. That is 0 matches for peer/Peer. No match remains to quote.

## 2. Wording kept

Server Peer Actor, GitHub Peer Actor, Peer Actor, and peer actor became Server Actor where they name this git Load/Save actor. The 2026-09-26 note in the map says the lock names the Server Actor, Actor is the glossary word, and that Actor is the Server Actor and is not a new Kind. Module 4 title and spoken name are Server Actor. The seam title is Server Actor git Load/Save. The ticket heading is 21 — Server Actor. Subsection 1 is Server Actor.

Stage was not changed. [GLOSSARY.md](GLOSSARY.md) was not changed.
