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
    | Some op -> lastSuccessfulOp.Value <- Some op
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
let onPaste (ev: Event) (applyOp: Op -> unit) : unit =
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

let private onCopyOrCut (model: VM) (ev: Event) (applyOp: Op -> unit) (op: Op) (includeNodeIds: bool) : unit =
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

let private isSingleLetterKey (key: string) : bool =
    key.Length = 1 && System.Char.IsLetter key[0]

let private isUppercaseLetterKey (key: string) : bool =
    isSingleLetterKey key && System.Char.IsUpper key[0]

let private tryUnshiftPunctuationKey (key: string) : string option =
    match key with
    | "<" -> Some ","
    | ">" -> Some "."
    | "?" -> Some "/"
    | ":" -> Some ";"
    | "\"" -> Some "'"
    | "{" -> Some "["
    | "}" -> Some "]"
    | "|" -> Some "\\"
    | "+" -> Some "="
    | "_" -> Some "-"
    | "~" -> Some "`"
    | _ -> None

let private normalizeKeyToken (key: string) : string =
    if isSingleLetterKey key then string (System.Char.ToUpperInvariant key[0]) else key

/// Format a KeyboardEvent as a normalized modifier+key string (e.g. "Ctrl+Shift+P").
let private formatKeyCombo (ke: KeyboardEvent) : string =
    let parts = ResizeArray<string>()
    let hasNonShiftModifier = ke.ctrlKey || ke.altKey || ke.metaKey
    let shiftOnlySource = ke.shiftKey || (not hasNonShiftModifier && isUppercaseLetterKey ke.key)
    let keyToken =
        if shiftOnlySource then
            match tryUnshiftPunctuationKey ke.key with
            | Some key -> key
            | None -> ke.key
        else
            ke.key

    if ke.ctrlKey then parts.Add "Ctrl"
    if ke.metaKey then parts.Add "Cmd"
    if ke.altKey  then parts.Add "Alt"
    if shiftOnlySource then parts.Add "Shift"

    parts.Add (normalizeKeyToken keyToken)
    String.concat "+" parts

/// Single function to set the last-key diagnostic. Never appends; always replaces.
/// key: the key combo (if any); operation: the command/operation name (if any).

/// Which keyboard maps include this command's bindings.
type CommandKeyScope =
    | SelectionOnly
    | EditingOnly
    | SelectionOrEditing

/// Resolves an Op to apply, or None to let the browser handle the key event.
type CommandOp = unit -> Op option

/// Key table entry: key string, resolver, and command name for diagnostic.
type KeyBinding = {
    key: string
    handler: CommandOp
    commandName: string
}

// ---------------------------------------------------------------------------
// Editing command ops (read live caret from DOM; defined before commandRegistry)
// ---------------------------------------------------------------------------

let private keyAlways (op: Op) : CommandOp = fun () -> Some op

let private splitAtCursor () : Op option =
    let text = readEditInputValue ()
    let pos = readEditInputCursor ()
    Some (splitNodeOp text pos)

let private editMoveUp () : Op option =
    Some (moveEditUp (readEditInputCursor ()))

let private editMoveDown () : Op option =
    Some (moveEditDown (readEditInputCursor ()))

let private handleBackspace () : Op option =
    if readEditInputCursor () = 0 then
        Some (joinWithPrevious (readEditInputValue ()))
    else None

let private handleDelete () : Op option =
    let v = readEditInputValue ()
    if readEditInputCursor () = v.Length then
        Some (joinWithNext v)
    else None

let private handleArrowLeft () : Op option =
    if readEditInputCursor () = 0 && readEditInputSelectionEnd () = 0 then
        Some (moveEditUp System.Int32.MaxValue)
    else None

let private handleArrowRight () : Op option =
    let v = readEditInputValue ()
    let len = v.Length
    if readEditInputCursor () = len && readEditInputSelectionEnd () = len then
        Some (moveEditDown 0)
    else None

// ---------------------------------------------------------------------------
// Command registry and palette ops
// ---------------------------------------------------------------------------

type CommandEntry = {
    name: string
    run: CommandOp
    keys: string list
    keyScope: CommandKeyScope
}

let commandRegistry : CommandEntry list =
    [
      { name = "Edit node"
        run = keyAlways startEditOp
        keys = [ "F2"; "Enter" ]
        keyScope = SelectionOnly }

      { name = "Split at cursor"
        run = splitAtCursor
        keys = [ "Enter" ]
        keyScope = EditingOnly }

      { name = "Delete"
        run = keyAlways deleteSelectionOp
        keys = [ "Delete"; "Backspace" ]
        keyScope = SelectionOnly }

      { name = "Join with previous"
        run = handleBackspace
        keys = [ "Backspace" ]
        keyScope = EditingOnly }

      { name = "Join with next"
        run = handleDelete
        keys = [ "Delete" ]
        keyScope = EditingOnly }

      { name = "Cursor up"
        run = keyAlways moveSelectionUp
        keys = [ "ArrowUp"; "," ]
        keyScope = SelectionOnly }

      { name = "Cursor down"
        run = keyAlways moveSelectionDown
        keys = [ "ArrowDown"; "O" ]
        keyScope = SelectionOnly }

      { name = "Cursor left to parent"
        run = keyAlways arrowLeftSelectionNoFoldOp
        keys = [ "ArrowLeft"; "A" ]
        keyScope = SelectionOnly }

      { name = "Cursor fold left"
        run = keyAlways arrowLeftSelectionOp
        keys = [ "Shift+ArrowLeft"; "Shift+A" ]
        keyScope = SelectionOnly }

      { name = "Cursor unfold right"
        run = keyAlways arrowRightSelectionOp
        keys = [ "Shift+ArrowRight"; "ArrowRight" ;"Shift+E" ;"E" ]
        keyScope = SelectionOnly }

      { name = "Move to previous node"
        run = handleArrowLeft
        keys = [ "ArrowLeft"; "Ctrl+ArrowLeft" ]
        keyScope = EditingOnly }

      { name = "Move to next node"
        run = handleArrowRight
        keys = [ "ArrowRight"; "Ctrl+ArrowRight" ]
        keyScope = EditingOnly }

      { name = "Selection up"
        run = keyAlways (shiftArrowOp -1)
        keys = [ "Shift+ArrowUp" ;"Shift+,"]
        keyScope = SelectionOnly }

      { name = "Selection down"
        run = keyAlways (shiftArrowOp 1)
        keys = [ "Shift+ArrowDown" ;"Shift+O"]
        keyScope = SelectionOnly }

      { name = "Move cursor up"
        run = editMoveUp
        keys = [ "ArrowUp" ]
        keyScope = EditingOnly }

      { name = "Move cursor down"
        run = editMoveDown
        keys = [ "ArrowDown" ]
        keyScope = EditingOnly }

      { name = "Move selection up"
        run = keyAlways moveNodeUpOp
        keys = [ "Alt+ArrowUp"; "Ctrl+ArrowUp" ]
        keyScope = SelectionOrEditing }

      { name = "Move selection down"
        run = keyAlways moveNodeDownOp
        keys = [ "Alt+ArrowDown"; "Ctrl+ArrowDown" ]
        keyScope = SelectionOrEditing }

      { name = "Indent"
        run = keyAlways indentOp
        keys = [ "Tab" ]
        keyScope = SelectionOrEditing }

      { name = "Outdent"
        run = keyAlways outdentOp
        keys = [ "Shift+Tab" ]
        keyScope = SelectionOrEditing }

      { name = "Cancel"
        run = keyAlways handleEsc
        keys = [ "Escape" ]
        keyScope = SelectionOrEditing }

      { name = "Fold / unfold"
        run = keyAlways toggleFoldSelectionOp
        keys = [ "Ctrl+." ]
        keyScope = SelectionOrEditing }

      { name = "Zoom in"
        run = keyAlways zoomInOp
        keys = [ "Ctrl+]"; "]" ]
        keyScope = SelectionOrEditing }

      { name = "Zoom out"
        run = keyAlways zoomOutOp
        keys = [ "Ctrl+["; "[" ]
        keyScope = SelectionOrEditing }

      { name = "Undo"
        run = keyAlways undoOp
        keys = [ "Ctrl+Z" ; "Z"]
        keyScope = SelectionOrEditing }

      { name = "Redo"
        run = keyAlways redoOp
        keys = [ "Ctrl+Y" ; "Y"]
        keyScope = SelectionOrEditing }

      { name = "Copy as links"
        run = keyAlways copySelectionAsLinks
        keys = [ "Ctrl+Shift+C";"Shift+C" ]
        keyScope = SelectionOnly }


      { name = "Duplicate (link)"
        run = keyAlways duplicateSelectionOp
        keys = [ "Shift+D" ]
        keyScope = SelectionOnly }


      { name = "Command palette"
        run = keyAlways openCommandPaletteOp
        keys = [ "Ctrl+P";"P" ]
        keyScope = SelectionOrEditing }

      { name = "Toggle class"
        run = keyAlways toggleClassOp
        keys = [ "Alt+C"; "." ]
        keyScope = SelectionOrEditing } ]

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
        match c.keyScope with
        | SelectionOnly -> sel
        | EditingOnly -> not sel
        | SelectionOrEditing -> true)

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
    | None -> { model with mode = ret }
    | Some cmd ->
        match cmd.run () with
        | None ->
            setLastKeyDisplay None None
            { model with mode = ret }
        | Some op ->
            setLastKeyDisplay None (Some cmd.name)
            op { model with mode = ret } dispatch)

let paletteSetQueryOp (q: string) = onPalette (fun _ _ ret model _ ->
    { model with mode = CommandPalette (q, 0, ret) })

let private scopeInSelectionMap =
    function
    | SelectionOnly | SelectionOrEditing -> true
    | EditingOnly -> false

let private scopeInEditingMap =
    function
    | EditingOnly | SelectionOrEditing -> true
    | SelectionOnly -> false

/// Editing uses a text field; skip bare one-character registry keys so the browser inserts that character.
let private isSingleCharKeyBinding (k: string) : bool = k.Length = 1

/// Rebuild selection key bindings from commandRegistry (first binding per key wins).
let private selectionKeyBindings : KeyBinding list =
    let rec collect seen acc entries =
        match entries with
        | [] -> acc
        | entry :: rest ->
            if not (scopeInSelectionMap entry.keyScope) then collect seen acc rest
            else
                let rowHandler = entry.run
                let bindings =
                    entry.keys
                    |> List.choose (fun k ->
                        if Set.contains k seen then None
                        else Some { key = k; handler = rowHandler; commandName = entry.name })
                let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
                collect seen' (acc @ bindings) rest
    collect Set.empty [] commandRegistry

/// Rebuild editing key bindings from commandRegistry (first binding per key wins).
let private editingKeyBindings : KeyBinding list =
    let rec collect seen acc entries =
        match entries with
        | [] -> acc
        | entry :: rest ->
            if not (scopeInEditingMap entry.keyScope) then collect seen acc rest
            else
                let rowHandler = entry.run
                let bindings =
                    entry.keys
                    |> List.choose (fun k ->
                        if isSingleCharKeyBinding k || Set.contains k seen then None
                        else Some { key = k; handler = rowHandler; commandName = entry.name })
                let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
                collect seen' (acc @ bindings) rest
    collect Set.empty [] commandRegistry

/// Literal palette key bindings (Escape, ArrowUp, ArrowDown, Enter). Not derived from registry.
let private paletteKeyBindings : KeyBinding list =
    [ { key = "Escape"
        handler = keyAlways closeCommandPaletteOp
        commandName = "Close palette" }
      { key = "ArrowUp"
        handler = keyAlways moveSelectionUp
        commandName = "Select previous" }
      { key = "ArrowDown"
        handler = keyAlways moveSelectionDown
        commandName = "Select next" }
      { key = "Enter"
        handler = keyAlways paletteRunOp
        commandName = "Run command" } ]

let private tryResolveFromNamed
    (table: KeyBinding list)
    (ke: KeyboardEvent)
    : (CommandOp * string) option =
    let keyStr = formatKeyCombo ke
    table
    |> List.tryPick (fun e ->
        if e.key = keyStr then Some (e.handler, e.commandName) else None)

let private dispatchResolvedKey
    (keyStr: string)
    (handler: CommandOp, name: string)
    (keyEvent: KeyboardEvent)
    (applyOp: Op -> unit)
    : unit =
    match handler () with
    | Some op ->
        keyEvent.preventDefault()
        setLastKeyDisplay (Some keyStr) (Some name)
        applyOp op
    | None ->
        setLastKeyDisplay (Some keyStr) None

/// Route keyboard handling by mode: palette overlay, editing field, or selection (hidden input).
let handleKey (mode: Mode) (keyEvent: KeyboardEvent) (applyOp: Op -> unit) : unit =
    let keyStr = formatKeyCombo keyEvent
    let table =
        match mode with
        | CommandPalette _ -> paletteKeyBindings
        | Editing _ -> editingKeyBindings
        | Selecting -> selectionKeyBindings
    match tryResolveFromNamed table keyEvent with
    | None ->
        setLastKeyDisplay (Some keyStr) None
    | Some pair ->
        dispatchResolvedKey keyStr pair keyEvent applyOp

/// Command palette input: fixed binding list (listener wired once; no Mode value in closure).
let handlePaletteKey (keyEvent: KeyboardEvent) (applyOp: Op -> unit) : unit =
    let keyStr = formatKeyCombo keyEvent
    match tryResolveFromNamed paletteKeyBindings keyEvent with
    | None ->
        setLastKeyDisplay (Some keyStr) None
    | Some pair ->
        dispatchResolvedKey keyStr pair keyEvent applyOp

