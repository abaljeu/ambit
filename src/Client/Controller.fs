module Gambol.Client.Controller

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Client.Update

// ---------------------------------------------------------------------------
// Clipboard / paste helpers
// ---------------------------------------------------------------------------

/// Strip HTML tags to plain text via a temporary DOM element.
/// Block elements (p, div, br, tr, li, td) become newlines via innerText.
[<Emit("(function(h){var d=document.createElement('div');d.innerHTML=h;return(d.innerText||d.textContent||'').trim();})(" + "$0" + ")")>]
let stripHtmlToText (html: string) : string = jsNative

/// Read a named format from a paste ClipboardEvent's clipboardData.
[<Emit("$0.clipboardData.getData($1)")>]
let getClipboardData (ev: Event) (format: string) : string = jsNative

let private nodeIdsFormat = "application/x-gambol-nodeids"

/// Read node IDs format from a paste event, if present.
let getPasteNodeIds (ev: Event) : string option =
    let s = getClipboardData ev nodeIdsFormat
    if s = "" || isNull s then None else Some s

/// Write a named format to a copy/cut ClipboardEvent's clipboardData.
[<Emit("$0.clipboardData.setData($1,$2)")>]
let setClipboardData (ev: Event) (format: string) (data: string) : unit = jsNative

[<Emit("navigator.clipboard.write([new ClipboardItem({'text/plain'
    :new Blob([$0],{type:'text/plain'}),'" 
    + "application/x-gambol-nodeids" + "':new Blob([$0],{type:'text/plain'})})]).then(()=>{$1();})")>]
let private writeClipboardTextAndNodeIds (text: string) (continuation: unit -> unit) : unit = jsNative

/// Copy selection as raw NodeId GUIDs (for paste-as-link). Bypasses the copy event.
/// Sets both text/plain and application/x-gambol-nodeids for consistency with cut.
let copySelectionAsLinks (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        let idsText =
            selectedIds
            |> List.map (fun (NodeId guid) -> guid.ToString())
            |> String.concat "\n"
        writeClipboardTextAndNodeIds idsText ignore
        copySelectionOp model dispatch

/// Get the character offset at a given client (x, y) position using the browser's caret APIs.
[<Emit("(function(x,y){if(document.caretRangeFromPoint){var r=document.caretRangeFromPoint(x,y);return r?r.startOffset:0;}if(document.caretPositionFromPoint){var p=document.caretPositionFromPoint(x,y);return p?p.offset:0;}return 0;})($0,$1)")>]
let getCaretOffset (x: float) (y: float) : int = jsNative

/// Handle a paste event: extract plain text and optional node IDs, apply pasteNodesOp.
let onPaste (ev: Event) (applyOp: Op -> unit) : unit =
    ev.preventDefault()
    let plain = getClipboardData ev "text/plain"
    let text  = if plain <> "" then plain else stripHtmlToText (getClipboardData ev "text/html")
    let nodeIds = getPasteNodeIds ev
    if text <> "" then applyOp (pasteNodesOp text nodeIds)

let private onCopyOrCut (model: VM) (ev: Event) (applyOp: Op -> unit) (op: Op) (includeNodeIds: bool) : unit =
    match model.selectedNodes with
    | None -> ()
    | Some sel ->
        ev.preventDefault()
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        let serialized = serializeSubtree model.graph model.siteMap selectedIds
        setClipboardData ev "text/plain" serialized
        if includeNodeIds then
            let idsText =
                selectedIds
                |> List.map (fun (NodeId guid) -> guid.ToString())
                |> String.concat "\n"
            setClipboardData ev nodeIdsFormat idsText
        applyOp op

/// Handle a copy event: serialize the selected subtree to the clipboard.
let onCopy (model: VM) (ev: Event) (applyOp: Op -> unit) : unit =
    onCopyOrCut model ev applyOp copySelectionOp false

/// Handle a cut event: serialize and remove the selected subtree.
/// Puts both node IDs and full data on clipboard; paste prefers IDs when resolvable.
let onCut (model: VM) (ev: Event) (applyOp: Op -> unit) : unit =
    onCopyOrCut model ev applyOp cutSelectionOp true

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let app = document.getElementById "app"

/// True when running on iOS (iPad, iPhone, iPod). Cmd+key is then treated as Ctrl+key.
[<Emit("typeof navigator !== 'undefined' && (/iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1))")>]
let isIOS () : bool = jsNative

/// Platform string for diagnostics: platform, touchPoints, isIOS, userAgent snippet.
[<Emit("(typeof navigator !== 'undefined' ? navigator.platform + ' | maxTouchPoints=' + navigator.maxTouchPoints + ' | isIOS=' + $0 + ' | ' + navigator.userAgent.substring(0, 100) : 'n/a')")>]
let getPlatformDiagnostic (isIOSResult: bool) : string = jsNative

/// Format a KeyboardEvent as a modifier+key string (e.g. "Ctrl+Shift+z").
let private formatKeyCombo (ke: KeyboardEvent) : string =
    let parts = ResizeArray<string>()
    if ke.ctrlKey  then parts.Add "Ctrl"
    if ke.metaKey  then parts.Add "Cmd"
    if ke.altKey   then parts.Add "Alt"
    if ke.shiftKey then parts.Add "Shift"
    parts.Add ke.key
    String.concat "+" parts

/// Single function to set the last-key diagnostic. Never appends; always replaces.
/// key: the key combo (if any); operation: the command/operation name (if any).
let setLastKeyDisplay (key: string option) (operation: string option) : unit =
    let el = document.getElementById "key-last-key"
    if isNull el then () else
    let txt =
        match key, operation with
        | None, None -> " | Last key: (none)"
        | Some k, None -> " | Last key: " + k
        | Some k, Some o -> " | Last key: " + k + " → " + o
        | None, Some o -> " | Last key: Palette → " + o
    el.textContent <- txt

type SelectionKeyContext =
    { keyEvent: KeyboardEvent
      selectedNodeText: string }

type EditingKeyContext = { keyEvent: KeyboardEvent; editInput: HTMLInputElement }

type KeyContext =
    | SelectionKey of SelectionKeyContext
    | EditingKey of EditingKeyContext

type PaletteKeyContext = { keyEvent: KeyboardEvent }

/// A key handler returns an Op to apply, or None to let the browser handle the event.
type KeyHandler<'Context> = 'Context -> Op option

// ---------------------------------------------------------------------------
// Selection key handlers
// ---------------------------------------------------------------------------

/// Lift an Op into any key-handler context (for keys with fixed, context-free behaviour).
let private always (op: Op) (_: 'ctx) : Op option = Some op

/// Like always but for ops that need context (model, applyOp).
let private alwaysFromCtx (opFrom: SelectionKeyContext -> Op) (ctx: SelectionKeyContext) : Op option =
    Some (opFrom ctx)

// ---------------------------------------------------------------------------
// EditingKey key handlers (used in bindings; defined before commandRegistry)
// ---------------------------------------------------------------------------

let private splitAtCursor (ctx: EditingKeyContext) : Op option =
    let text = ctx.editInput.value
    let pos  = int ctx.editInput.selectionStart
    Some (splitNodeOp text pos)

let private editMoveUp (ctx: EditingKeyContext) : Op option =
    Some (moveEditUp ctx.editInput.selectionStart)

let private editMoveDown (ctx: EditingKeyContext) : Op option =
    Some (moveEditDown ctx.editInput.selectionStart)

let handleBackspace (ctx: EditingKeyContext) : Op option =
    if int ctx.editInput.selectionStart = 0 then
        Some (joinWithPrevious ctx.editInput.value)
    else None

let handleDelete (ctx: EditingKeyContext) : Op option =
    if int ctx.editInput.selectionStart = ctx.editInput.value.Length then
        Some (joinWithNext ctx.editInput.value)
    else None

let handleArrowLeft (ctx: EditingKeyContext) : Op option =
    if int ctx.editInput.selectionStart = 0 && int ctx.editInput.selectionEnd = 0 then
        Some (moveEditUp System.Int32.MaxValue)
    else None

let handleArrowRight (ctx: EditingKeyContext) : Op option =
    let len = ctx.editInput.value.Length
    if int ctx.editInput.selectionStart = len && int ctx.editInput.selectionEnd = len then
        Some (moveEditDown 0)
    else None

// ---------------------------------------------------------------------------
// Command registry and palette ops
// ---------------------------------------------------------------------------

/// No-op for key-only commands that are not invokable from the palette.
let private noOp : Op = fun m _ -> m

type CommandKeyHandler =
    | SelectionHandler of (SelectionKeyContext -> Op option)
    | EditingHandler of (EditingKeyContext -> Op option)
    | UniversalHandler of (KeyContext -> Op option)

type CommandEntry = {
    name: string
    bbbbbbb: Op
    keys: string list
    handler: CommandKeyHandler
}

let commandRegistry : CommandEntry list =
    [ 
      { name = "Edit node"
        bbbbbbb = startEditOp
        keys = ["F2"; "Enter"]
        handler =
            SelectionHandler (fun _ -> Some startEditOp) }
      
      { name = "Split at cursor"
        bbbbbbb = noOp
        keys = ["Enter"]
        handler = EditingHandler splitAtCursor }
      
      { name = "Delete"
        bbbbbbb = deleteSelectionOp
        keys = ["Delete"; "Backspace"]
        handler = SelectionHandler (fun _ -> Some deleteSelectionOp) }
      
      { name = "Join with previous"
        bbbbbbb = noOp
        keys = ["Backspace"]
        handler = EditingHandler handleBackspace }
      
      { name = "Join with next"
        bbbbbbb = noOp
        keys = ["Delete"]
        handler = EditingHandler handleDelete }
      
      { name = "Move selection up"
        bbbbbbb = moveSelectionUp
        keys = ["ArrowUp"]
        handler = SelectionHandler (fun _ -> Some moveSelectionUp) }
      
      { name = "Move selection down"
        bbbbbbb = moveSelectionDown
        keys = ["ArrowDown"]
        handler = SelectionHandler (fun _ -> Some moveSelectionDown) }
      
      { name = "Move focus left"
        bbbbbbb = arrowLeftSelectionOp
        keys = ["ArrowLeft"]
        handler = SelectionHandler (fun _ -> Some arrowLeftSelectionOp) }
      
      { name = "Move to previous node"
        bbbbbbb = noOp
        keys = ["ArrowLeft"; "Ctrl+ArrowLeft"]
        handler = EditingHandler handleArrowLeft }
      
      { name = "Move focus right"
        bbbbbbb = arrowRightSelectionOp
        keys = ["ArrowRight"]
        handler = SelectionHandler (fun _ -> Some arrowRightSelectionOp) }
      
      { name = "Move to next node"
        bbbbbbb = noOp
        keys = ["ArrowRight"; "Ctrl+ArrowRight"]
        handler = EditingHandler handleArrowRight }
      
      { name = "Extend selection up"
        bbbbbbb = shiftArrowOp -1
        keys = ["Shift+ArrowUp"]
        handler = SelectionHandler (fun _ -> Some (shiftArrowOp -1)) }
      
      { name = "Extend selection down"
        bbbbbbb = shiftArrowOp 1
        keys = ["Shift+ArrowDown"]
        handler = SelectionHandler (fun _ -> Some (shiftArrowOp 1)) }
      
      { name = "Move edit up"
        bbbbbbb = noOp
        keys = ["ArrowUp"]
        handler = EditingHandler editMoveUp }
      
      { name = "Move edit down"
        bbbbbbb = noOp
        keys = ["ArrowDown"]
        handler = EditingHandler editMoveDown }
      
      { name = "Move up"
        bbbbbbb = moveNodeUpOp
        keys = ["Alt+ArrowUp"; "Ctrl+ArrowUp"]
        handler = UniversalHandler (fun _ -> Some moveNodeUpOp) }
      
      { name = "Move down"
        bbbbbbb = moveNodeDownOp
        keys = ["Alt+ArrowDown"; "Ctrl+ArrowDown"]
        handler = UniversalHandler (fun _ -> Some moveNodeDownOp) }
      
      { name = "Indent"
        bbbbbbb = indentOp
        keys = ["Tab"]
        handler = UniversalHandler (fun _ -> Some indentOp) }
      
      { name = "Outdent"
        bbbbbbb = outdentOp
        keys = ["Shift+Tab"]
        handler = UniversalHandler (fun _ -> Some outdentOp) }
      
      { name = "Cancel"
        bbbbbbb = cancelEdit
        keys = ["Escape"]
        handler =
            UniversalHandler (function
                | SelectionKey _ -> Some cancelEdit
                | EditingKey _ -> Some cancelEdit) }
      
      { name = "Fold / unfold"
        bbbbbbb = toggleFoldSelectionOp
        keys = ["Ctrl+."]
        handler = UniversalHandler (fun _ -> Some toggleFoldSelectionOp) }
      
      { name = "Zoom in"
        bbbbbbb = zoomInOp
        keys = ["Ctrl+]"]
        handler = UniversalHandler (fun _ -> Some zoomInOp) }
      
      { name = "Zoom out"
        bbbbbbb = zoomOutOp
        keys = ["Ctrl+["]
        handler = UniversalHandler (fun _ -> Some zoomOutOp) }
      
      { name = "Undo"
        bbbbbbb = undoOp
        keys = ["Ctrl+z"]
        handler = UniversalHandler (fun _ -> Some undoOp) }
      
      { name = "Redo"
        bbbbbbb = redoOp
        keys = ["Ctrl+y"]
        handler = UniversalHandler (fun _ -> Some redoOp) }
      
      { name = "Copy as links"
        bbbbbbb = copySelectionAsLinks
        keys = ["Ctrl+Shift+c"]
        handler = SelectionHandler (fun _ -> Some copySelectionAsLinks) }
      
      { name = "Command palette"
        bbbbbbb = openCommandPaletteOp
        keys = ["Ctrl+Shift+P"]
        handler = UniversalHandler (fun _ -> Some openCommandPaletteOp) }
      
      { name = "Toggle class"
        bbbbbbb = toggleClassOp
        keys = ["Alt+c"]
        handler = UniversalHandler (fun _ -> Some toggleClassOp) } ]

/// True if palette was opened from selection (unwrap nested CommandPalette to the real return target).
let rec paletteWasSelecting (returnTo: Mode) : bool =
    match returnTo with
    | Selecting -> true
    | Editing _ -> false
    | CommandPalette (_, _, inner) -> paletteWasSelecting inner

let commandsForPalette (returnTo: Mode) : CommandEntry list =
    let sel = paletteWasSelecting returnTo
    commandRegistry
    |> List.filter (fun c ->
        match c.handler with
        | SelectionHandler _ -> sel
        | EditingHandler _ -> not sel
        | UniversalHandler _ -> true)

let filteredCommands (returnTo: Mode) (query: string) : CommandEntry list =
    let baseList = commandsForPalette returnTo
    if query = "" then baseList
    else
        let q = query.ToLowerInvariant()
        baseList |> List.filter (fun c -> c.name.ToLowerInvariant().Contains(q))

let private onPalette (f: string -> int -> Mode -> VM -> (Msg -> unit) -> VM)
                      (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode with
    | CommandPalette (q, selectedCommand, ret) -> f q selectedCommand ret model dispatch
    | _ -> model

let paletteRunOp = onPalette (fun q selectedCommand ret model dispatch ->
    match List.tryItem selectedCommand (filteredCommands ret q) with
    | None     -> { model with mode = ret }
    | Some cmd ->
        setLastKeyDisplay None (Some cmd.name)
        cmd.bbbbbbb { model with mode = ret } dispatch)

let paletteSetQueryOp (q: string) = onPalette (fun _ _ ret model _ ->
    { model with mode = CommandPalette (q, 0, ret) })

/// Key table entry: key string, handler, and command name for diagnostic.
type KeyTableEntry<'Context> = {
    key: string
    handler: KeyHandler<'Context>
    commandName: string
}

/// Map registry entry to selection-mode key handler, if any.
let private selectionKeyHandler (entry: CommandEntry) : (SelectionKeyContext -> Op option) option =
    match entry.handler with
    | SelectionHandler h -> Some h
    | UniversalHandler h -> Some (fun s -> h (SelectionKey s))
    | EditingHandler _ -> None

/// Rebuild selection key table from commandRegistry (first binding per key wins).
let selectionKeyTable : KeyTableEntry<SelectionKeyContext> list =
    let rec collect seen acc entries =
        match entries with
        | [] -> acc
        | entry :: rest ->
            match selectionKeyHandler entry with
            | None -> collect seen acc rest
            | Some rowHandler ->
                let bindings =
                    entry.keys
                    |> List.choose (fun k ->
                        if Set.contains k seen then None
                        else Some { key = k; handler = rowHandler; commandName = entry.name })
                let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
                let acc' = acc @ bindings
                collect seen' acc' rest
    collect Set.empty [] commandRegistry

/// Map registry entry to editing-mode key handler, if any.
let private editingKeyHandler (entry: CommandEntry) : (EditingKeyContext -> Op option) option =
    match entry.handler with
    | EditingHandler h -> Some h
    | UniversalHandler h -> Some (fun e -> h (EditingKey e))
    | SelectionHandler _ -> None

/// Rebuild editing key table from commandRegistry (first binding per key wins).
let editingKeyTable : KeyTableEntry<EditingKeyContext> list =
    let rec collect seen acc entries =
        match entries with
        | [] -> acc
        | entry :: rest ->
            match editingKeyHandler entry with
            | None -> collect seen acc rest
            | Some rowHandler ->
                let bindings =
                    entry.keys
                    |> List.choose (fun k ->
                        if Set.contains k seen then None
                        else Some { key = k; handler = rowHandler; commandName = entry.name })
                let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
                let acc' = acc @ bindings
                collect seen' acc' rest
    collect Set.empty [] commandRegistry

/// Literal palette key table (Escape, ArrowUp, ArrowDown, Enter). Not derived from registry.
let paletteKeyTable : KeyTableEntry<PaletteKeyContext> list =
    [ { key = "Escape"
        handler = fun _ -> Some closeCommandPaletteOp
        commandName = "Close palette" }
      { key = "ArrowUp"
        handler = fun _ -> Some moveSelectionUp
        commandName = "Select previous" }
      { key = "ArrowDown"
        handler = fun _ -> Some moveSelectionDown
        commandName = "Select next" }
      { key = "Enter"
        handler = fun _ -> Some paletteRunOp
        commandName = "Run command" } ]

let private tryResolveFromNamed
    (table: KeyTableEntry<'Context> list)
    (ke: KeyboardEvent)
    : (KeyHandler<'Context> * string) option =
    let tryKey k =
        table
        |> List.tryPick (fun e ->
            if e.key = k then Some (e.handler, e.commandName) else None)
    let ctrlOrCmd = ke.ctrlKey || (isIOS () && ke.metaKey)
    let qualified =
        if ctrlOrCmd && ke.shiftKey then tryKey ("Ctrl+Shift+" + ke.key) else
        if ke.altKey   then tryKey ("Alt+"   + ke.key) else
        if ke.shiftKey then tryKey ("Shift+" + ke.key) else
        if ctrlOrCmd   then tryKey ("Ctrl+"  + ke.key) else
        None
    match qualified with
    | Some _ -> qualified
    | None -> tryKey ke.key

let handleKey
    (table: KeyTableEntry<'Context> list)
    (ctx: 'Context)
    (keyEvent: KeyboardEvent)
    (applyOp: Op -> unit)
    : unit =
    let keyStr = formatKeyCombo keyEvent
    match tryResolveFromNamed table keyEvent with
    | None ->
        setLastKeyDisplay (Some keyStr) None
    | Some (handler, name) ->
        match handler ctx with
        | Some op ->
            keyEvent.preventDefault()
            setLastKeyDisplay (Some keyStr) (Some name)
            applyOp op
        | None ->
            setLastKeyDisplay (Some keyStr) None
