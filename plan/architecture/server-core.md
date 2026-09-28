# Server Core

Updated: 2026-09-28

This page records the Server description Alan gave on 2026-09-28. The description is decided. It states what Core deals with and what stays outside Core.

## 1. Dated note

1. **2026-09-28 inbound** — Alan locked this Server description. This page is the home. This lock does not chart coding tickets.

## 2. Core deals with

1. **Database backend** — The Graph modifies the database backend. The database stores Events.
2. **Graph** — The Graph stores on the database. The mailbox modifies the Graph.
3. **Events** — Events store on the database. The mailbox creates Events. The Event sequence is the event source. An Event notes Graph ops, Actor events, file events, and git actions.
4. **File system** — File-system objects correspond to Graph objects. Before a file update and after a file update, the Graph must signal which elements that work uses. File work may take time, but the time is not unlimited. An extended process delays file work until the work is certain. Then it sends a message to the mailbox.
5. **Actors** — Actors are functions defined outside Core. An Actor receives a Graph. An Actor runs on a thread. An Actor sends messages to the mailbox.
6. **Mailbox** — The mailbox controls access to the Graph, Events, files, and Actors. It receives messages. It converts each message to Events by actioning that message. Message processing is fast. Graph modifications are simple and synchronous. Other work runs in the background and may have an end Event. The mailbox sends an immediate response.

## 3. Outside Core

1. **Define Actors** — Actor function bodies stay outside Core.
2. **Outside I/O** — Send and receive HTTP and other outside traffic outside Core.

## 4. Related

1. **Map** — [[map.md]]
2. **Actor as client** — [[plan/actor-as-client/project.md]] — concept-only; this description confirms privilege-less Actors that only post to the mailbox.
3. **Core creation** — [[plan/core-creation/project.md]] — existing Core baseline.
4. **Mailbox clear-fast** — [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]
