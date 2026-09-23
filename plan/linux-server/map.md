# Linux server

## 1. Destination

A spec to hand off and iterate on. The spec has two halves that name one Linux: compile the Server for Linux, and the Linux setup.

## 2. Notes

Server is the server project and the server process. Linux setup is the machine and the setup the Server needs. The compile half is compile the Server for Linux.

One Linux. The two halves stay separate in the spec, and both halves name that same Linux.

The main target is a fast-loading Azure server. A docker container is a second target and is not locked. Whether the fast path is Docker within Azure is open.

The frontier question is on [01 — Docker deploy usefulness](issues/01-docker-deploy-usefulness.md). The research component's first question is whether a Docker deploy is useful to the recorded goals, before any other ticket. Usefulness is not known. The docker container stays unlocked until that result is in.

The map stops at the spec. A later pass in this project compiles the Server and does the Linux setup.

A research component identifies what needs to be done. Its first question is a ticket, and that ticket comes before any other ticket.

Skills: [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]], [[.agents/skills/research/SKILL.md]].

## 3. Decisions so far

<!-- the index — one numbered line per resolved ticket: enough to judge relevance, then zoom the link for the detail the ticket holds -->

## 4. Not yet specified

1. **Research component remainder** — What else needs to be done, after [01 — Docker deploy usefulness](issues/01-docker-deploy-usefulness.md). The other items in this section wait on that ticket.
2. **Linux setup contents** — What the Linux setup holds so the Server can run. This waits on [01 — Docker deploy usefulness](issues/01-docker-deploy-usefulness.md), on which Linux the spec names, and on which Server run needs are in play.
3. **Compile steps** — How the compile half is done for that Linux. This waits on [01 — Docker deploy usefulness](issues/01-docker-deploy-usefulness.md), on which Linux the spec names, and on the Server build as it is.

## 5. Out of scope

1. **Perform the compile and the Linux setup** — The map ends at the spec. A later pass in this project compiles the Server and does the Linux setup.
