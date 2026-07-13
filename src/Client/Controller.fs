module Gambol.Client.Controller

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.UpdateEdit
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateOps
open Gambol.Client.UpdateRename
open Gambol.Client.SearchDialog
open Gambol.Client.Commands

module CommandMeta = Gambol.Shared.CommandEntry
// ---------------------------------------------------------------------------
// Clipboard / paste helpers
// ---------------------------------------------------------------------------

/// Strip HTML tags to plain text via a temporary DOM element.
/// Block elements (p, div, br, tr, li, td) become newlines via innerText.
let stripHtmlToText (html: string) : string =
    let d = document.createElement "div"
    d.innerHTML <- html
    let t = d.innerText
    let s =
        if System.String.IsNullOrEmpty t then
            d.textContent
        else
            t
    if isNull s then ""
    else s.Trim()

/// Read a named format from a paste ClipboardEvent's clipboardData.
let getClipboardData (ev: Event) (format: string) : string =
    let e = ev :?> ClipboardEvent
    e.clipboardData.getData format

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
let setClipboardData (ev: Event) (format: string) (data: string) : unit =
    let e = ev :?> ClipboardEvent
    e.clipboardData.setData (format, data) |> ignore

/// Wrap an op with diagnostic state: defaults `lastCmdResult` to Ok when the op did not
/// already set a result (so a future refusal can set Error and keep it).
/// `commandName` is stamped onto the result (`None` = anonymous / no name prefix).
let withDiagnostic (commandName: string option) (f: Updater) : Updater =
    fun model ->
        let newModel, effects = f model
        let result =
            if newModel.lastCmdResult <> model.lastCmdResult then
                newModel.lastCmdResult
                |> Option.map (CmdLastResult.withCommandName commandName)
            else
                Some (CmdLastResult.Ok commandName)
        { newModel with lastCmdResult = result }, effects

/// Render `#cmd-last-result` from the last command result. Pure DOM formatter.
let setCmdLastResultDisplay (result: CmdLastResult option) : unit =
    let el = document.getElementById "cmd-last-result"
    if not (isNull el) then
        el.textContent <- CmdLastResult.formatDisplay result

/// Handle a paste event: extract plain text and optional node IDs, apply pasteNodesOp.
let onPaste (ev: Event) (dispatch: Msg -> unit) : unit =
    let plain = getClipboardData ev "text/plain"
    let html = getClipboardData ev "text/html"
    let text = if plain <> "" then plain else stripHtmlToText html
    let nodeIds = getPasteNodeIds ev
    let pastedText =
        match nodeIds with
        | Some ids -> ids
        | _ -> text
    ev.preventDefault()
    if pastedText <> "" then
        dispatch (ApplyOp (withDiagnostic None (pasteNodesOp pastedText nodeIds)))

let private onCopyOrCut (model: VM) (ev: Event) (dispatch: Msg -> unit) (updater: Updater) (includeNodeIds: bool) : unit =
    match model.selectedNodes with
    | None -> ()
    | Some sel ->
        ev.preventDefault()
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
            |> List.map (fun child -> child.id)
        let serialized = serializeSubtree model.graph model.siteMap selectedIds
        setClipboardData ev "text/plain" serialized
        if includeNodeIds then
            let idsText =
                selectedIds
                |> List.map (fun childId -> childId)
                |> List.map (fun (NodeId guid) -> guid.ToString())
                |> String.concat "\n"
            setClipboardData ev nodeIdsFormat idsText
        dispatch (ApplyOp (withDiagnostic None updater))

/// Handle a copy event: serialize the selected subtree to the clipboard.
let onCopy (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit =
    onCopyOrCut model ev dispatch copySelectionOp false

/// Handle a cut event: serialize and remove the selected subtree.
/// Puts both node IDs and full data on clipboard; paste prefers IDs when resolvable.
let onCut (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit =
    onCopyOrCut model ev dispatch cutSelectionOp true

/// True when `#edit-input` has a non-collapsed text range (browser should own copy/cut).
let editFieldHasTextRangeSelection () : bool =
    readEditInputCursor () <> readEditInputSelectionEnd ()

/// Structured copy while editing; skipped when user selected text inside the field.
let onCopyWhileEditing (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit =
    if editFieldHasTextRangeSelection () then ()
    else onCopy model ev dispatch

/// Structured cut while editing; skipped when user selected text inside the field.
let onCutWhileEditing (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit =
    if editFieldHasTextRangeSelection () then ()
    else onCut model ev dispatch

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let app = document.getElementById "app"
let ambDocument = document.getElementById "amb-document"

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

/// US keyboard: (base, shifted) pairs for punctuation keys.
let private punctuationShiftPairs: (string * string) list =
    [ ",", "<"; ".", ">"; "/", "?"; ";", ":"; "'", "\""
      "[", "{"; "]", "}"; "\\", "|"; "=", "+"; "-", "_"; "`", "~" ]

let private shiftedToBase = punctuationShiftPairs |> List.map (fun (b, s) -> s, b) |> Map.ofList
let private baseToShifted = punctuationShiftPairs |> Map.ofList

let private tryUnshiftPunctuationKey (key: string) : string option = Map.tryFind key shiftedToBase
let private tryShiftPunctuationKey (key: string) : string option = Map.tryFind key baseToShifted

/// Normalize a registry key for matching: drop Shift, use resolved character.
/// "Shift+M" -> "M", "Ctrl+Shift+C" -> "Ctrl+C"; "Shift+Tab" unchanged (Tab not single-char).
let private normalizeRegistryKey (keyStr: string) : string =
    if keyStr.Contains "+" then
        let parts = keyStr.Split '+' |> Array.toList
        let keyPart = List.last parts
        if keyPart.Length = 1 && System.Char.IsLetter keyPart[0] && List.contains "Shift" parts then
            let mods = parts |> List.filter ((<>) "Shift")
            match mods with
            | [ k ] -> k  // Just the key (Shift+M -> M)
            | _ -> String.concat "+" mods  // Ctrl+Shift+C -> Ctrl+C
        else
            keyStr
    else
        keyStr

let private normalizeKeyToken (key: string) : string =
    if isSingleLetterKey key then string (System.Char.ToUpperInvariant key[0]) else key

/// True when this keydown is only a modifier key (no "real" key yet).
let private isModifierOnlyKeyPress (key: string) : bool =
    match key with
    | "Control" | "Shift" | "Alt" | "Meta" -> true
    | _ -> false

/// Format a KeyboardEvent as a normalized modifier+key string (e.g. "Ctrl+Shift+P").
/// For single-char keys with only Shift, outputs just the final character (no "Shift+") so
/// "]" and "Shift+[" both normalize to "]" across browsers.
let formatKeyCombo (ke: KeyboardEvent) : string =
    if isModifierOnlyKeyPress ke.key then
        ""
    else
        let hasNonShiftModifier = ke.ctrlKey || ke.altKey || ke.metaKey
        let shiftOnlySource = ke.shiftKey || (not hasNonShiftModifier && isUppercaseLetterKey ke.key)

        if hasNonShiftModifier then
            let parts = ResizeArray<string>()
            let keyToken =
                if shiftOnlySource then
                    match tryShiftPunctuationKey ke.key with
                    | Some k -> k
                    | None ->
                        if isSingleLetterKey ke.key then string (System.Char.ToUpperInvariant ke.key[0]) else ke.key
                else
                    match tryUnshiftPunctuationKey ke.key with
                    | Some k -> k
                    | None -> if isSingleLetterKey ke.key then string (System.Char.ToLowerInvariant ke.key[0]) else ke.key
            if ke.ctrlKey then parts.Add "Ctrl"
            if ke.metaKey then parts.Add "Cmd"
            if ke.altKey  then parts.Add "Alt"
            if shiftOnlySource then parts.Add "Shift"
            parts.Add (normalizeKeyToken keyToken)
            String.concat "+" parts
        else
            // Single-char key with only Shift (or none): output just the key character.
            // Browsers inconsistently report e.g. "]" vs "Shift+[", so we drop Shift and use the key.
            // Multi-char keys (Tab, ArrowUp, etc.): browser never includes Shift in key; preserve it.
            if ke.key.Length > 1 && ke.shiftKey then "Shift+" + ke.key else ke.key

/// Key table entry: key, handler, display name, and whether this is command-bar chrome.
/// Command-bar-only bindings keep a name but do not update `#cmd-last-result`.
type KeyBinding = {
    key: string
    handler: CommandOp
    commandName: string
    commandBarOnly: bool
}

/// A key binding that matched a key event, ready to dispatch.
type ResolvedKeyBinding = {
    handler: CommandOp
    commandName: string
    commandBarOnly: bool
}

/// Key was not bound in the table.
type KeyResolveError = 
    | KeyNotBound of string
    | IncompleteKey of string

// ---------------------------------------------------------------------------
// Palette ops
// ---------------------------------------------------------------------------

let private keyAlways (updater: Updater) : CommandOp = fun () -> Some updater

let private onPalette (f: string -> int -> Mode -> VM -> VM * Effect list) (model: VM) : VM * Effect list =
    match model.mode with
    | CommandPalette (q, selectedCommand, ret) -> f q selectedCommand ret model
    | _ -> model, []

let paletteRunOp =
    onPalette (fun q selectedCommand ret model ->
        match List.tryItem selectedCommand (filteredCommands model ret q) with
        | None -> { model with mode = ret }, []
        | Some cmd ->
            match cmd.run () with
            | None ->
                { model with mode = ret }, []
            | Some op ->
                withDiagnostic
                    (Some (CommandMeta.displayName cmd.id))
                    op
                    { model with mode = ret })

let paletteSetQueryOp (q: string) =
    onPalette (fun _ _ ret model -> { model with mode = Mode.CommandPalette (q, 0, ret) }, [])

/// Editing uses a text field; skip bare one-character registry keys so the browser inserts that character.
let private isSingleCharKeyBinding (k: string) : bool = k.Length = 1

/// Rebuild selection key bindings from commandRegistry (first binding per key wins).
let private selectionKeyBindings : KeyBinding list =
    let rec collect seen acc (entries: CommandEntry2 list) =
        match entries with
        | [] -> acc
        | entry :: rest ->
            match CommandMeta.commandFor entry.id with
            | None -> collect seen acc rest
            | Some meta ->
                if not (CommandMeta.scopeInSelection meta.keyScope) then collect seen acc rest
                else
                    let rowHandler = entry.run
                    let bindings =
                        meta.keys
                        |> List.choose (fun k ->
                            let nk = normalizeRegistryKey k
                            if Set.contains nk seen then None
                            else
                                Some {
                                    key = nk
                                    handler = rowHandler
                                    commandName = meta.name
                                    commandBarOnly = false
                                })
                    let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
                    collect seen' (acc @ bindings) rest
    collect Set.empty [] commandRegistry

/// Rebuild editing key bindings from commandRegistry (first binding per key wins).
let private editingKeyBindings : KeyBinding list =
    let rec collect seen acc (entries: CommandEntry2 list) =
        match entries with
        | [] -> acc
        | entry :: rest ->
            match CommandMeta.commandFor entry.id with
            | None -> collect seen acc rest
            | Some meta ->
                if not (CommandMeta.scopeInEditing meta.keyScope) then collect seen acc rest
                else
                    let rowHandler = entry.run
                    let bindings =
                        meta.keys
                        |> List.choose (fun k ->
                            let nk = normalizeRegistryKey k
                            if isSingleCharKeyBinding nk || Set.contains nk seen then None
                            else
                                Some {
                                    key = nk
                                    handler = rowHandler
                                    commandName = meta.name
                                    commandBarOnly = false
                                })
                    let seen' = bindings |> List.fold (fun s e -> Set.add e.key s) seen
                    collect seen' (acc @ bindings) rest
    collect Set.empty [] commandRegistry

/// Literal palette key bindings (Escape, ArrowUp, ArrowDown, Enter). Not derived from registry.
/// `commandBarOnly = true`: names are kept, but `#cmd-last-result` is not updated.
let private paletteKeyBindings : KeyBinding list =
    [ { key = "Escape"
        handler = keyAlways closeCommandPaletteOp
        commandName = "Close palette"
        commandBarOnly = true }
      { key = "ArrowUp"
        handler = keyAlways moveSelectionUp
        commandName = "Select previous"
        commandBarOnly = true }
      { key = "ArrowDown"
        handler = keyAlways moveSelectionDown
        commandName = "Select next"
        commandBarOnly = true }
      { key = "Enter"
        handler = keyAlways paletteRunOp
        commandName = "Run command"
        commandBarOnly = true } ]

/// Key bindings for the CSS class prompt overlay (Escape to cancel, Enter to submit).
let private cssClassPromptKeyBindings : KeyBinding list =
    [ { key = "Escape"
        handler = keyAlways closeCssClassPromptOp
        commandName = "Cancel"
        commandBarOnly = true }
      { key = "Enter"
        handler = keyAlways submitCssClassPromptOp
        commandName = "Apply class"
        commandBarOnly = true } ]

/// Key bindings for the rename prompt overlay (Escape to cancel, Enter to submit).
let private renamePromptKeyBindings : KeyBinding list =
    [ { key = "Escape"
        handler = keyAlways closeRenamePromptOp
        commandName = "Cancel"
        commandBarOnly = true }
      { key = "Enter"
        handler = keyAlways submitRenamePromptOp
        commandName = "Rename"
        commandBarOnly = true } ]


let private tryResolveFromNamed
    (table: KeyBinding list)
    (ke: KeyboardEvent)
    : Result<ResolvedKeyBinding, KeyResolveError> =
    if isModifierOnlyKeyPress ke.key then
        Error (IncompleteKey ke.key)
    else
        let keyStr = formatKeyCombo ke
        match table |> List.tryFind (fun e -> e.key = keyStr) with
        | None -> Error (KeyNotBound keyStr)
        | Some e ->
            Ok {
                handler = e.handler
                commandName = e.commandName
                commandBarOnly = e.commandBarOnly
            }

let private dispatchResolvedKey
    (resolved: ResolvedKeyBinding)
    (keyEvent: KeyboardEvent)
    (dispatch: Msg -> unit)
    : unit =
    match resolved.handler () with
    | Some op ->
        keyEvent.preventDefault()
        if resolved.commandBarOnly then
            // Command-bar chrome: run the op, leave `#cmd-last-result` alone.
            dispatch (ApplyOp op)
        else
            dispatch (ApplyOp (withDiagnostic (Some resolved.commandName) op))
    | None ->
        ()

/// Route keyboard handling by mode: palette overlay, CSS class prompt, editing field, or selection (hidden input).
let handleKey (mode: Mode) (ke: KeyboardEvent) (dispatch: Msg -> unit) : unit =
    let hasNonShiftModifier = ke.ctrlKey || ke.altKey || ke.metaKey
    match mode with
    | Editing _ when 
            not hasNonShiftModifier && ke.key.Length = 1 ->
            () // Let the visible edit input receive the character; skip hidden-input bindings.
    | _ ->
        let table =
            match mode with
            | CommandPalette _ -> paletteKeyBindings
            | SearchDialog _ -> [] // keys handled by search input's own listener
            | FileSearchDialog _ -> []
            | CssClassPrompt _ -> cssClassPromptKeyBindings
            | RenamePrompt _ -> renamePromptKeyBindings
            | Editing _ -> editingKeyBindings
            | Selecting -> selectionKeyBindings
        match tryResolveFromNamed table ke with
        | Error _ -> ()
        | Ok resolved ->
            dispatchResolvedKey resolved ke dispatch

/// Command palette input: fixed binding list (listener wired once; no Mode value in closure).
let handlePaletteKey (keyEvent: KeyboardEvent) (dispatch: Msg -> unit) : unit =
    match tryResolveFromNamed paletteKeyBindings keyEvent with
    | Error _ -> ()
    | Ok resolved ->
        dispatchResolvedKey resolved keyEvent dispatch

/// CSS class prompt input: Escape to cancel, Enter to submit.
let handleCssClassPromptKey (keyEvent: KeyboardEvent) (dispatch: Msg -> unit) : unit =
    match tryResolveFromNamed cssClassPromptKeyBindings keyEvent with
    | Error _ -> ()
    | Ok resolved -> dispatchResolvedKey resolved keyEvent dispatch

/// Rename prompt input: Escape to cancel, Enter to submit.
let handleRenamePromptKey (keyEvent: KeyboardEvent) (dispatch: Msg -> unit) : unit =
    match tryResolveFromNamed renamePromptKeyBindings keyEvent with
    | Error _ -> ()
    | Ok resolved -> dispatchResolvedKey resolved keyEvent dispatch

