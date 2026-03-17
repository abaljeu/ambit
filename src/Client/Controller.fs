module Gambol.Client.Controller

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel
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

/// Prefix for "copy as links" in text/plain (Clipboard API only supports text/plain).
let private nodeIdsPrefix = "x-gambol-nodeids:"

/// Read node IDs from a paste event. Checks text/plain for prefix first (async copy), else application/x-gambol-nodeids (sync cut).
let getPasteNodeIds (ev: Event) : string option =
    let plain = getClipboardData ev "text/plain"
    if plain.StartsWith nodeIdsPrefix then
        plain.Substring nodeIdsPrefix.Length
        |> fun s -> s.TrimStart([| '\r'; '\n' |])
        |> fun s -> if s = "" then None else Some s
    else
        let s = getClipboardData ev nodeIdsFormat
        if s = "" || isNull s then None else Some s

/// Write a named format to a copy/cut ClipboardEvent's clipboardData.
[<Emit("$0.clipboardData.setData($1,$2)")>]
let setClipboardData (ev: Event) (format: string) (data: string) : unit = jsNative

/// Last successful operation for diagnostic display (preserved when key has no match).
let private lastSuccessfulOp = ref None

/// Single function to set the last-key diagnostic. Never appends; always replaces. Preserves last successful operation when operation is None.
let setLastKeyDisplay (key: string option) (operation: string option) : unit =
    let el = document.getElementById "key-last-key"
    if isNull el then () else
    match operation with
    | Some op -> lastSuccessfulOp := Some op
    | None -> ()
    let o = match operation with
            | Some _ -> operation
            | None -> lastSuccessfulOp.contents
    let txt =
        match key, o with
        | None, None -> " | Last key: (none)"
        | Some keyStr, None -> " | Last key: " + keyStr
        | Some keyStr, Some op -> " | Last key: " + keyStr + " → " + op
        | None, Some op -> " | Last key: Palette → " + op
    el.textContent <- txt

[<Emit("navigator.clipboard.writeText($0).then(function(){ $1(); }).catch(function(e){ console.error('Clipboard write failed:', e); })")>]
let private writeClipboardText (text: string) (continuation: unit -> unit) : unit = jsNative

/// Copy selection as raw NodeId GUIDs (for paste-as-link). Writes text/plain with x-gambol-nodeids prefix.
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
        writeClipboardText (nodeIdsPrefix + "\n" + idsText) ignore
        copySelectionOp model dispatch

/// Get the character offset at a given client (x, y) position using the browser's caret APIs.
[<Emit("(function(x,y){if(document.caretRangeFromPoint){var r=document.caretRangeFromPoint(x,y);return r?r.startOffset:0;}if(document.caretPositionFromPoint){var p=document.caretPositionFromPoint(x,y);return p?p.offset:0;}return 0;})($0,$1)")>]
let getCaretOffset (x: float) (y: float) : int = jsNative

/// Handle a paste event: extract plain text and optional node IDs, apply pasteNodesOp.
let onPaste (ev: Event) (applyOp: VmMsgUnitVm -> unit) : unit =
    ev.preventDefault()
    let plain = getClipboardData ev "text/plain"
    let text  = if plain <> "" then plain else stripHtmlToText (getClipboardData ev "text/html")
    let nodeIds = getPasteNodeIds ev
    let pastedText =
        match nodeIds with
        | Some ids -> ids
        | _ -> text
    if pastedText <> "" then
        setLastKeyDisplay (Some "Ctrl+V") (Some "Paste")
        applyOp (pasteNodesOp pastedText nodeIds)

let private onCopyOrCut (model: VM) (ev: Event) (applyOp: VmMsgUnitVm -> unit) (op: VmMsgUnitVm) (includeNodeIds: bool) : unit =
    match model.selectedNodes with
    | None -> ()
    | Some sel ->
        ev.preventDefault()
        setLastKeyDisplay (Some (if includeNodeIds then "Ctrl+X" else "Ctrl+C")) (Some (if includeNodeIds then "Cut" else "Copy"))
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
let onCopy (model: VM) (ev: Event) (applyOp: VmMsgUnitVm -> unit) : unit =
    onCopyOrCut model ev applyOp copySelectionOp false

/// Handle a cut event: serialize and remove the selected subtree.
/// Puts both node IDs and full data on clipboard; paste prefers IDs when resolvable.
let onCut (model: VM) (ev: Event) (applyOp: VmMsgUnitVm -> unit) : unit =
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

type SelectionKeyContext =
    { keyEvent: KeyboardEvent
      selectedNodeText: string }

type EditingKeyContext = { keyEvent: KeyboardEvent; editInput: HTMLInputElement }

type KeyContext =
    | SelectionKey of SelectionKeyContext
    | EditingKey of EditingKeyContext

type PaletteKeyContext = { keyEvent: KeyboardEvent }

/// A key handler returns an Op to apply, or None to let the browser handle the event.
type KeyHandler<'Context> = 'Context -> VmMsgUnitVm option

let printableKeyToken = "__PRINTABLE__"

// ---------------------------------------------------------------------------
// Key op helpers and context-dependent ops
// ---------------------------------------------------------------------------

/// Helpers for the generic (sel/edit) patterns. Used when defining named key ops.
let private selOnly (f: SelectionKeyContext -> VmMsgUnitVm option) = function SelectionKey s -> f s | _ -> None
let private editOnly (f: EditingKeyContext -> VmMsgUnitVm option) = function EditingKey e -> f e | _ -> None
let private both op = fun _ -> Some op

[<Emit("new KeyboardEvent('keydown',{key:'',bubbles:true})")>]
let private dummyKeyEvent () : KeyboardEvent = jsNative

/// Enter edit mode: use key as prefill if printable, else keep existing text.
let private startEditFromKey (key: string) (existingText: string) : VmMsgUnitVm =
    startEdit (if isPrintableKey key then key else existingText)

// ---------------------------------------------------------------------------
// EditingKey key handlers (used in bindings; defined before commandRegistry)
// ---------------------------------------------------------------------------

let private splitAtCursor (ctx: EditingKeyContext) : VmMsgUnitVm option =
    let text = ctx.editInput.value
    let pos  = int ctx.editInput.selectionStart
    Some (splitNodeOp text pos)

let private editMoveUp (ctx: EditingKeyContext) : VmMsgUnitVm option =
    Some (moveEditUp ctx.editInput.selectionStart)

let private editMoveDown (ctx: EditingKeyContext) : VmMsgUnitVm option =
    Some (moveEditDown ctx.editInput.selectionStart)

let handleBackspace (ctx: EditingKeyContext) : VmMsgUnitVm option =
    if int ctx.editInput.selectionStart = 0 then
        Some (joinWithPrevious ctx.editInput.value)
    else None

let handleDelete (ctx: EditingKeyContext) : VmMsgUnitVm option =
    if int ctx.editInput.selectionStart = ctx.editInput.value.Length then
        Some (joinWithNext ctx.editInput.value)
    else None

let handleArrowLeft (ctx: EditingKeyContext) : VmMsgUnitVm option =
    if int ctx.editInput.selectionStart = 0 && int ctx.editInput.selectionEnd = 0 then
        Some (moveEditUp System.Int32.MaxValue)
    else None

let handleArrowRight (ctx: EditingKeyContext) : VmMsgUnitVm option =
    let len = ctx.editInput.value.Length
    if int ctx.editInput.selectionStart = len && int ctx.editInput.selectionEnd = len then
        Some (moveEditDown 0)
    else None

// ---------------------------------------------------------------------------
// Named key ops (KeyContext -> VmMsgUnitVm option) — one per command
// ---------------------------------------------------------------------------

/// Context-dependent key op
let private editNodeKeyOp = function
    | SelectionKey s -> Some (startEditFromKey s.keyEvent.key s.selectedNodeText)
    | _ -> None

// ---------------------------------------------------------------------------
// Command registry and palette ops
// ---------------------------------------------------------------------------

type CommandEntry = {
    name: string
    op: KeyContext -> VmMsgUnitVm option
    sel: bool
    edit: bool
    keys: string list
}

let commandRegistry : CommandEntry list =
    [ { name = "Edit node"
        op = editNodeKeyOp
        sel = true; edit = false
        keys = ["F2"; "Enter"] }
      { name = "Split at cursor"
        op = editOnly splitAtCursor
        sel = false; edit = true
        keys = ["Enter"] }
      { name = "Delete"
        op = selOnly (fun _ -> Some deleteSelectionOp)
        sel = true; edit = false
        keys = ["Delete"; "Backspace"] }
      { name = "Join with previous"
        op = editOnly handleBackspace
        sel = false; edit = true
        keys = ["Backspace"] }
      { name = "Join with next"
        op = editOnly handleDelete
        sel = false; edit = true
        keys = ["Delete"] }
      { name = "Move selection up"
        op = selOnly (fun _ -> Some moveSelectionUp)
        sel = true; edit = false
        keys = ["ArrowUp"] }
      { name = "Move selection down"
        op = selOnly (fun _ -> Some moveSelectionDown)
        sel = true; edit = false
        keys = ["ArrowDown"] }
      { name = "Move focus left"
        op = selOnly (fun _ -> Some arrowLeftSelectionOp)
        sel = true; edit = false
        keys = ["ArrowLeft"] }
      { name = "Move to previous node"
        op = editOnly handleArrowLeft
        sel = false; edit = true
        keys = ["ArrowLeft"; "Ctrl+ArrowLeft"] }
      { name = "Move focus right"
        op = selOnly (fun _ -> Some arrowRightSelectionOp)
        sel = true; edit = false
        keys = ["ArrowRight"] }
      { name = "Move to next node"
        op = editOnly handleArrowRight
        sel = false; edit = true
        keys = ["ArrowRight"; "Ctrl+ArrowRight"] }
      { name = "Extend selection up"
        op = selOnly (fun _ -> Some (shiftArrowOp -1))
        sel = true; edit = false
        keys = ["Shift+ArrowUp"] }
      { name = "Extend selection down"
        op = selOnly (fun _ -> Some (shiftArrowOp 1))
        sel = true; edit = false
        keys = ["Shift+ArrowDown"] }
      { name = "Move edit up"
        op = editOnly editMoveUp
        sel = false; edit = true
        keys = ["ArrowUp"] }
      { name = "Move edit down"
        op = editOnly editMoveDown
        sel = false; edit = true
        keys = ["ArrowDown"] }
      { name = "Move up"
        op = both moveNodeUpOp
        sel = true; edit = true
        keys = ["Alt+ArrowUp"; "Ctrl+ArrowUp"] }
      { name = "Move down"
        op = both moveNodeDownOp
        sel = true; edit = true
        keys = ["Alt+ArrowDown"; "Ctrl+ArrowDown"] }
      { name = "Indent"
        op = both indentOp
        sel = true; edit = true
        keys = ["Tab"] }
      { name = "Outdent"
        op = both outdentOp
        sel = true; edit = true
        keys = ["Shift+Tab"] }
      { name = "Cancel"
        op = both cancelEdit
        sel = true; edit = true
        keys = ["Escape"] }
      { name = "Fold / unfold"
        op = both toggleFoldSelectionOp
        sel = true; edit = true
        keys = ["Ctrl+."] }
      { name = "Zoom in"
        op = both zoomInOp
        sel = true; edit = true
        keys = ["Ctrl+]"] }
      { name = "Zoom out"
        op = both zoomOutOp
        sel = true; edit = true
        keys = ["Ctrl+["] }
      { name = "Undo"
        op = both undoOp
        sel = true; edit = true
        keys = ["Ctrl+z"] }
      { name = "Redo"
        op = both redoOp
        sel = true; edit = true
        keys = ["Ctrl+y"] }
      { name = "Copy as links"
        op = selOnly (fun _ -> Some copySelectionAsLinks)
        sel = true; edit = false
        keys = ["Ctrl+Shift+c"] }
      { name = "Command palette"
        op = both openCommandPaletteOp
        sel = true; edit = true
        keys = ["Ctrl+Shift+P"] }
      { name = "Toggle class"
        op = both toggleClassOp
        sel = true; edit = true
        keys = ["Alt+c"] } ]

/// Commands invokable from the palette (sel or edit mode, palette flag set).
let paletteCommands : CommandEntry list =
    commandRegistry |> List.filter (fun c -> c.sel || c.edit) 

let filteredCommands (query: string) : CommandEntry list =
    if query = "" then paletteCommands
    else
        let q = query.ToLowerInvariant()
        paletteCommands |> List.filter (fun c -> c.name.ToLowerInvariant().Contains(q))

/// Build KeyContext from the mode we're returning to when running a command from the palette.
let contextFromReturnMode (ret: Mode) (model: VM) : KeyContext option =
    match ret with
    | Selecting ->
        let viewRootId = model.zoomRoot |> Option.defaultValue model.graph.root
        let rootNode = model.graph.nodes.[viewRootId]
        let textToEdit =
            match model.selectedNodes with
            | None -> rootNode.text
            | Some sel ->
                let nodeId = focusedNodeId model.graph sel
                model.graph.nodes.[nodeId].text
        Some (SelectionKey { keyEvent = dummyKeyEvent (); selectedNodeText = textToEdit })
    | Editing _ ->
        match document.getElementById "edit-input" with
        | null -> None
        | el -> Some (EditingKey { keyEvent = dummyKeyEvent (); editInput = el :?> HTMLInputElement })
    | CommandPalette _ -> None

let private onPalette (f: string -> int -> Mode -> VM -> (Msg -> unit) -> VM)
                      (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode with
    | CommandPalette (q, selectedCommand, ret) -> f q selectedCommand ret model dispatch
    | _ -> model

let paletteRunOp = onPalette (fun q selectedCommand ret model dispatch ->
    match List.tryItem selectedCommand (filteredCommands q) with
    | None     -> { model with mode = ret }
    | Some cmd ->
        match contextFromReturnMode ret model |> Option.bind (fun ctx -> cmd.op ctx) with
        | None     -> { model with mode = ret }
        | Some runOp ->
            setLastKeyDisplay None (Some cmd.name)
            runOp { model with mode = ret } dispatch)

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
                        let h s = entry.op (SelectionKey s)
                        Some { key = k; handler = h; commandName = entry.name })
            let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
            let acc' = acc @ bindings
            collect seen' acc' rest
        | _ :: rest -> collect seen acc rest
    let fromRegistry = collect Set.empty [] commandRegistry
    let printEdit (s: SelectionKeyContext) = editNodeKeyOp (SelectionKey s)
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
                        let h e = entry.op (EditingKey e)
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
    (applyOp: VmMsgUnitVm -> unit)
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
