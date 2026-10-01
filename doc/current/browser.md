# Browser

Category: Architecture
See Also: [[doc/current/arch.md]], [[doc/current/view.md]], [[doc/current/operations.md]], [[doc/current/sync-mvp.md]], [[doc/current/desktop-local-files.md]], [[GLOSSARY.md]]

The Browser is the spoken name for Gambol.Client ([[GLOSSARY.md]]). It is F# compiled to JavaScript with Fable, and a client-side MVU-style loop. It does local-first editing, renders outline, maintains selection, and syncs via poll + change POST.

## Is

The Browser needs to:

- Render “lines” for visible occurrences (respect folding/opened state)
- Capture keys and drive edits via operations
- Maintain selection state (nodeview + span)
- Support undo/redo (client-local history; inverse changes submitted as normal edits)

Fable with a tiny MVU loop (no React):

- Model/update in F# compiled to JS (`src/Client/Update*.fs`, `View.fs`)
- Direct DOM via `Fable.Browser.Dom` (see `other/fable.browser.dom.fs` when needed)
- `update : VM -> Msg -> VM * Cmd list` (or `VM` only when no cmds)
- Minimal dependencies; no React stack
- Served under `/ambit` from server `wwwroot` (Fable `--outDir src/Server/wwwroot`)

When running in the desktop shell, the Browser talks to `localhost` (local proxy). Graph authority remains the cloud server. The App adds `/_desktop/*` for capabilities and local file access ([[doc/current/desktop-local-files.md]]).

The line view is [[doc/current/view.md]]. Sync semantics are [[doc/current/sync-mvp.md]].

## Should Become

No later Browser shape is recorded here.

## Where

- Project: [[src/Client]]
- `src/Client/Update*.fs`, [[src/Client/View.fs]]
- Fable output: `src/Server/wwwroot`, served at `/ambit`

## Explanation

Because learning F# is a core project goal, the Browser is authored in F#.

Principle: keep the architecture benefits of MVU while avoiding a heavy UI framework.
