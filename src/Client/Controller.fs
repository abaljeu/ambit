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

/// Check if a key string represents a single printable character
let isPrintableKey (key: string) : bool =
    key.Length = 1 && key >= " "

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

/// Record the last key combo and refresh the diagnostic display. Call from keydown handlers.
let recordKeyAndRenderDiagnostic (ke: KeyboardEvent) : unit =
    let key = formatKeyCombo ke
    let el = document.getElementById "key-last-key"
    if not (isNull el) then
        el.textContent <- " | Last key: " + key

let private appendCommandName (name: string) : unit =
    let el = document.getElementById "key-last-key"
    if not (isNull el) then
        el.textContent <- el.textContent + " → " + name

let private recordCommandRun (name: string) : unit =
    let el = document.getElementById "key-last-key"
    if not (isNull el) then
        el.textContent <- " | Last key: Palette → " + name

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

let printableKeyToken = "__PRINTABLE__"

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

type CommandEntry = {
    name: string
    op: Op
    sel: bool
    edit: bool
    palette: bool
    keys: string list
    handler: KeyContext -> Op option
}

let commandRegistry : CommandEntry list =
    [ { name = "Edit node"
        op = startEditOp
        sel = true; edit = false; palette = true
        keys = ["F2"; "Enter"]
        handler = function SelectionKey s -> Some (startEdit s.keyEvent.key) | _ -> None }
      { name = "Split at cursor"
        op = noOp; sel = false; edit = true; palette = true
        keys = ["Enter"]
        handler = function EditingKey e -> splitAtCursor e | _ -> None }
      { name = "Delete"
        op = deleteSelectionOp
        sel = true; edit = false; palette = true
        keys = ["Delete"; "Backspace"]
        handler = function SelectionKey _ -> Some deleteSelectionOp | _ -> None }
      { name = "Join with previous"
        op = noOp; sel = false; edit = true; palette = true
        keys = ["Backspace"]
        handler = function EditingKey e -> handleBackspace e | _ -> None }
      { name = "Join with next"
        op = noOp; sel = false; edit = true; palette = true
        keys = ["Delete"]
        handler = function EditingKey e -> handleDelete e | _ -> None }
      { name = "Move selection up"
        op = moveSelectionUp
        sel = true; edit = false; palette = true
        keys = ["ArrowUp"]
        handler = function SelectionKey _ -> Some moveSelectionUp | _ -> None }
      { name = "Move selection down"
        op = moveSelectionDown
        sel = true; edit = false; palette = true
        keys = ["ArrowDown"]
        handler = function SelectionKey _ -> Some moveSelectionDown | _ -> None }
      { name = "Move focus left"
        op = arrowLeftSelectionOp
        sel = true; edit = false; palette = true
        keys = ["ArrowLeft"]
        handler = function SelectionKey _ -> Some arrowLeftSelectionOp | _ -> None }
      { name = "Move to previous node"
        op = noOp; sel = false; edit = true; palette = true
        keys = ["ArrowLeft"; "Ctrl+ArrowLeft"]
        handler = function EditingKey e -> handleArrowLeft e | _ -> None }
      { name = "Move focus right"
        op = arrowRightSelectionOp
        sel = true; edit = false; palette = true
        keys = ["ArrowRight"]
        handler = function SelectionKey _ -> Some arrowRightSelectionOp | _ -> None }
      { name = "Move to next node"
        op = noOp; sel = false; edit = true; palette = true
        keys = ["ArrowRight"; "Ctrl+ArrowRight"]
        handler = function EditingKey e -> handleArrowRight e | _ -> None }
      { name = "Extend selection up"
        op = shiftArrowOp -1
        sel = true; edit = false; palette = true
        keys = ["Shift+ArrowUp"]
        handler = function SelectionKey _ -> Some (shiftArrowOp -1) | _ -> None }
      { name = "Extend selection down"
        op = shiftArrowOp 1
        sel = true; edit = false; palette = true
        keys = ["Shift+ArrowDown"]
        handler = function SelectionKey _ -> Some (shiftArrowOp 1) | _ -> None }
      { name = "Move edit up"
        op = noOp; sel = false; edit = true; palette = true
        keys = ["ArrowUp"]
        handler = function EditingKey e -> editMoveUp e | _ -> None }
      { name = "Move edit down"
        op = noOp; sel = false; edit = true; palette = true
        keys = ["ArrowDown"]
        handler = function EditingKey e -> editMoveDown e | _ -> None }
      { name = "Move up"
        op = moveNodeUpOp
        sel = true; edit = true; palette = true
        keys = ["Alt+ArrowUp"; "Ctrl+ArrowUp"]
        handler = fun _ -> Some moveNodeUpOp }
      { name = "Move down"
        op = moveNodeDownOp
        sel = true; edit = true; palette = true
        keys = ["Alt+ArrowDown"; "Ctrl+ArrowDown"]
        handler = fun _ -> Some moveNodeDownOp }
      { name = "Indent"
        op = indentOp
        sel = true; edit = true; palette = true
        keys = ["Tab"]
        handler = fun _ -> Some indentOp }
      { name = "Outdent"
        op = outdentOp
        sel = true; edit = true; palette = true
        keys = ["Shift+Tab"]
        handler = fun _ -> Some outdentOp }
      { name = "Cancel"
        op = cancelEdit
        sel = true; edit = true; palette = true
        keys = ["Escape"]
        handler = function
          | SelectionKey _ -> Some cancelEdit
          | EditingKey _ -> Some cancelEdit }
      { name = "Fold / unfold"
        op = toggleFoldSelectionOp
        sel = true; edit = true; palette = true
        keys = ["Ctrl+."]
        handler = fun _ -> Some toggleFoldSelectionOp }
      { name = "Zoom in"
        op = zoomInOp
        sel = true; edit = true; palette = true
        keys = ["Ctrl+]"]
        handler = fun _ -> Some zoomInOp }
      { name = "Zoom out"
        op = zoomOutOp
        sel = true; edit = true; palette = true
        keys = ["Ctrl+["]
        handler = fun _ -> Some zoomOutOp }
      { name = "Undo"
        op = undoOp
        sel = true; edit = true; palette = true
        keys = ["Ctrl+z"]
        handler = fun _ -> Some undoOp }
      { name = "Redo"
        op = redoOp
        sel = true; edit = true; palette = true
        keys = ["Ctrl+y"]
        handler = fun _ -> Some redoOp }
      { name = "Copy as links"
        op = copySelectionAsLinks
        sel = true; edit = false; palette = true
        keys = ["Ctrl+Shift+c"]
        handler = function SelectionKey _ -> Some copySelectionAsLinks | _ -> None }
      { name = "Command palette"
        op = openCommandPaletteOp
        sel = true; edit = true; palette = true
        keys = ["Ctrl+Shift+P"]
        handler = fun _ -> Some openCommandPaletteOp }
      { name = "Toggle class"
        op = toggleClassOp
        sel = true; edit = true; palette = true
        keys = ["Alt+c"]
        handler = fun _ -> Some toggleClassOp } ]

/// Commands invokable from the palette (sel or edit mode, palette flag set).
let paletteCommands : CommandEntry list =
    commandRegistry |> List.filter (fun c -> (c.sel || c.edit) && c.palette) 

let filteredCommands (query: string) : CommandEntry list =
    if query = "" then paletteCommands
    else
        let q = query.ToLowerInvariant()
        paletteCommands |> List.filter (fun c -> c.name.ToLowerInvariant().Contains(q))

let private onPalette (f: string -> int -> Mode -> VM -> (Msg -> unit) -> VM)
                      (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode with
    | CommandPalette (q, selectedCommand, ret) -> f q selectedCommand ret model dispatch
    | _ -> model

let paletteRunOp = onPalette (fun q selectedCommand ret model dispatch ->
    match List.tryItem selectedCommand (filteredCommands q) with
    | None     -> { model with mode = ret }
    | Some cmd ->
        recordCommandRun cmd.name
        cmd.op { model with mode = ret } dispatch)

let paletteSetQueryOp (q: string) = onPalette (fun _ _ ret model _ ->
    { model with mode = CommandPalette (q, 0, ret) })

/// Key table entry: key string, handler, and command name for diagnostic.
type KeyTableEntry<'Context> = {
    key: string
    handler: KeyHandler<'Context>
    commandName: string
}

/// Rebuild selection key table from commandRegistry (first binding per key wins).
/// Printable key binding is injected separately; keys do not include printableKeyToken.
let selectionKeyTable : KeyTableEntry<SelectionKeyContext> list =
    let rec collect seen acc entries =
        match entries with
        | [] -> acc
        | entry :: rest when entry.sel ->
            let bindings =
                entry.keys
                |> List.choose (fun k ->
                    if Set.contains k seen then None
                    else
                        let h s = entry.handler (SelectionKey s)
                        Some { key = k; handler = h; commandName = entry.name })
            let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
            let acc' = acc @ bindings
            collect seen' acc' rest
        | _ :: rest -> collect seen acc rest
    let fromRegistry = collect Set.empty [] commandRegistry
    let printEdit (s: SelectionKeyContext) = Some (startEdit s.keyEvent.key)
    fromRegistry
    @ [ { key = printableKeyToken; handler = printEdit; commandName = "Edit node" } ]

/// Rebuild editing key table from commandRegistry (first binding per key wins).
let editingKeyTable : KeyTableEntry<EditingKeyContext> list =
    let rec collect seen acc entries =
        match entries with
        | [] -> acc
        | entry :: rest when entry.edit ->
            let bindings =
                entry.keys
                |> List.choose (fun k ->
                    if Set.contains k seen then None
                    else
                        let h e = entry.handler (EditingKey e)
                        Some { key = k; handler = h; commandName = entry.name })
            let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
            let acc' = acc @ bindings
            collect seen' acc' rest
        | _ :: rest -> collect seen acc rest
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
    | None ->
        match tryKey ke.key with
        | Some r -> Some r
        | None ->
            if isPrintableKey ke.key && not ke.ctrlKey && not ke.metaKey && not ke.altKey then
                tryKey printableKeyToken
            else
                None

let handleKey
    (table: KeyTableEntry<'Context> list)
    (ctx: 'Context)
    (keyEvent: KeyboardEvent)
    (applyOp: Op -> unit)
    : unit =
    match tryResolveFromNamed table keyEvent with
    | None -> ()
    | Some (handler, name) ->
        match handler ctx with
        | Some op ->
            keyEvent.preventDefault()
            appendCommandName name
            applyOp op
        | None -> ()
