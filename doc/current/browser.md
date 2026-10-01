# Browser

Category: Capability

See Also:

[View](view.md)
[Operations](operations.md)
[Multi-client sync](sync-mvp.md)
[Desktop local files](desktop-local-files.md)
[Gambol.Client](gambol-client.md)

The Browser is the spoken name for Gambol.Client.

## Job

[x] F# compiled to JavaScript with Fable.
[x] Client-side MVU loop for local-first outline editing, selection, and poll-and-change sync.
[x] Renders lines for visible occurrences and respects folding and opened state.
[x] Captures keys and drives edits through operations.
[x] Maintains selection state as a nodeview plus a span.
[x] Supports undo and redo as client-local history. The client submits inverse changes as normal edits.
[x] Model and update: F# compiled to JavaScript. Files: `src/Client/Update*.fs` and `src/Client/View.fs`, in `src/Client`.
[x] Writes the DOM through `Fable.Browser.Dom`. See `other/fable.browser.dom.fs` when needed.
[x] The update function has the shape `update : VM -> Msg -> VM * Cmd list`, or returns `VM` only when there are no commands.
[x] Keeps dependencies minimal. No React stack.
[x] Server serves the Browser under `/ambit` from `wwwroot`. Fable `--outDir`: `src/Server/wwwroot`.
[x] In the desktop shell, the Browser talks to `localhost` through the local proxy. Graph authority remains the cloud server.
[x] The App adds `/_desktop/*` for capabilities and local file access. Detail: Desktop local files.

## Explanation

Learning F# is a core project goal. For this reason, the Browser is authored in F#.

The Browser keeps the architecture benefits of MVU and avoids a heavy UI framework.
