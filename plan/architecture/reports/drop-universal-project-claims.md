# 01 — Drop universal claims from project pages

The eleven project pages under [doc/current](doc/current/) now lead with that project. [Architecture](doc/current/architecture.md) still links to those pages. The home sentence about separate test projects is unchanged.

## 1. Claims removed on every project page

1. **Solution membership** — "This project is in gambol.sln." The home already states that these projects are in the solution.
2. **Home link** — See Also no longer links [architecture.md](doc/current/architecture.md).
3. **Shared target** — `net10.0` is gone on every page except [Gambol.Desktop](doc/current/gambol-desktop.md). That page keeps `net10.0-windows` with WinExe.

## 2. Glossary links removed

1. [Gambol.Client](doc/current/gambol-client.md) — See Also and the spoken-name line. The spoken name Browser remains. The page still links [browser.md](doc/current/browser.md).
2. [Gambol.Server](doc/current/gambol-server.md) — See Also and the spoken-name line. The spoken name Server remains. The page still links [server.md](doc/current/server.md).
3. [Gambol.CloudAgents](doc/current/gambol-cloud-agents.md) — See Also and the Ambit Actor citation. The sentence "An Agent is not an Ambit Actor." remains.
4. [Gambol.Desktop](doc/current/gambol-desktop.md) — See Also and the spoken-name line. The spoken name App remains.

No project page linked [CONTEXT.md](doc/current/CONTEXT.md).

## 3. Behavior copies removed

1. [Gambol.Server](doc/current/gambol-server.md) — "This project uses Npgsql." [server.md](doc/current/server.md) already states that.
2. [Gambol.Desktop](doc/current/gambol-desktop.md) — "The UI is WPF. The window hosts WebView2." and "A local HTTP proxy forwards the cloud app and serves local file routes." [desktop-local-files.md](doc/current/desktop-local-files.md) already states that host. The project page still links that page.

## 4. Facts kept

1. References, output type, spoken name, and what the code does stay on the page that they distinguish.
2. xUnit stays on the three test pages. One-thread execution stays on [Gambol.CloudAgents.Tests](doc/current/gambol-cloud-agents-tests.md).
3. Fable stays on [Gambol.Client](doc/current/gambol-client.md). Fable-must-not stays on [Gambol.Shared.DotNet](doc/current/gambol-shared-dotnet.md).
4. Class library stays on [Gambol.Shared](doc/current/gambol-shared.md), [Gambol.CloudAgents](doc/current/gambol-cloud-agents.md), [Gambol.Shared.DotNet](doc/current/gambol-shared-dotnet.md), and [Gambol.Shared.Documents](doc/current/gambol-shared-documents.md).
5. Existing "No later shape is recorded here" sentences stay. [doc/index.md](doc/index.md), [doc/README.md](doc/README.md), and [testing.md](doc/current/testing.md) were not edited.

## 5. CONTEXT rules I was unsure how to apply

1. **Shall Be on Client.** [CONTEXT.md](doc/current/CONTEXT.md) says a subject page states what Shall Be. [Gambol.Client](doc/current/gambol-client.md) had no Shall Be section. I did not add "No later shape is recorded here." The task says not to invent Shall Be items.
2. **Server publish sentence.** I kept "Publish runs Fable on Gambol.Client and writes the JavaScript into wwwroot." [server.md](doc/current/server.md) and [browser.md](doc/current/browser.md) already say the server serves Fable output from `wwwroot`. I treated the publish step as project wiring. It may still be a copy.
3. **Shared lead.** I kept "This library is the preferred home for testable logic." [testing.md](doc/current/testing.md) says to prefer `src/Shared` for code location. I treated the lead as the library role.
4. **Class library.** Four projects are class libraries. That fact is not true of every project, so I kept it. `net10.0` was the target the task called almost universal.
5. **xUnit.** [testing.md](doc/current/testing.md) already names xUnit for the three test projects. The task says to keep the test framework when it is the point of the page, so I kept the checkbox.
6. **Shared subject pointers.** I kept the three Is lines that only point at [operations.md](doc/current/operations.md), [persistence-model.md](doc/current/persistence-model.md), and [workspace-graph.md](doc/current/workspace-graph.md). They link those subjects. They do not restate the behavior. See Also also links them.
