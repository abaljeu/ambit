module Gambol.Client.JsInterop

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared

/// Outline model re-exports `Node` / `Selection`; keep DOM names explicit here.
type private DomNode = Browser.Types.Node
type private DomSelection = Browser.Types.Selection

/// Collapsed caret at UTF-16 offset `pos` inside `root` (contenteditable).
let setEditorCaret (root: HTMLElement) (pos: int) : unit =
    let sel = window.getSelection ()
    if isNull sel then
        ()
    else
        let range = document.createRange ()
        let mutable rem = max 0 pos
        // NodeFilter.SHOW_TEXT = 4
        let walker = document.createTreeWalker (root :> DomNode, whatToShow = 4.)
        let mutable n = walker.nextNode ()
        let mutable found = false
        while (not found) && (not (isNull n)) do
            let textLen = (n :?> CharacterData).length
            if rem <= textLen then
                range.setStart (n, float rem)
                range.collapse true
                sel.removeAllRanges ()
                sel.addRange range
                found <- true
            else
                rem <- rem - textLen
                n <- walker.nextNode ()
        if not found then
            range.selectNodeContents (root :> DomNode)
            range.collapse false
            sel.removeAllRanges ()
            sel.addRange range

/// UTF-16 offset in `root.textContent` from the start of `root` to (`container`, `offset`).
let private rangeBoundaryOffsetInRoot (root: HTMLElement) (container: DomNode) (offset: float) : int =
    let pre = document.createRange ()
    pre.selectNodeContents (root :> DomNode)
    pre.setEnd (container, offset)
    pre.toString().Length

/// Selection anchor relative to `root` (same contract as former `pre.toString().length`).
let getContentEditableSelectionStart (root: HTMLElement) : int =
    let s = window.getSelection ()
    if isNull s || s.rangeCount < 1. then 0
    else
        let r = s.getRangeAt 0.
        rangeBoundaryOffsetInRoot root r.startContainer r.startOffset

let getContentEditableSelectionEnd (root: HTMLElement) : int =
    let s = window.getSelection ()
    if isNull s || s.rangeCount < 1. then 0
    else
        let r = s.getRangeAt 0.
        rangeBoundaryOffsetInRoot root r.endContainer r.endOffset

/// Caret as UTF-16 offset: selection start boundary relative to `root` when that start
/// lies inside `root`; otherwise 0. Non-collapsed ranges use the start (anchor), not the
/// focus end — same side as typical focus restore.
let getContentEditableCaretOffset (root: HTMLElement) : int =
    let s = window.getSelection ()
    if isNull s || s.rangeCount < 1. then 0
    else
        let r = s.getRangeAt 0.
        if not (root.contains r.startContainer) then 0
        else rangeBoundaryOffsetInRoot root r.startContainer r.startOffset

/// Div has no element children: empty, or exactly one `#text` child (wrapped lines OK).
type private FlatEditable =
    | FlatEmpty
    | FlatOneText of Text
    | FlatInvalid

let private classifyFlatEditable (root: HTMLElement) : FlatEditable =
    let nch = root.childNodes.length
    if nch = 0 then FlatEmpty
    elif nch > 1 then FlatInvalid
    else
        let n = root.firstChild
        if isNull n || n.nodeType <> 3. then FlatInvalid else FlatOneText (n :?> Text)

let private visualLineEpsilon = 4.

let private rangeLineTop (container: DomNode) (offset: float) : float =
    let rr = document.createRange ()
    rr.setStart (container, offset)
    rr.collapse true
    rr.getBoundingClientRect().top

let private selectionCaretTop (root: HTMLElement) : float option =
    let s = window.getSelection ()
    if isNull s || s.rangeCount < 1. then None
    else
        let r = s.getRangeAt 0.
        if not (root.contains r.startContainer) then None
        else Some (rangeLineTop r.startContainer r.startOffset)

/// Viewport `left` of the collapsed caret at the selection **start**, if it lies in `root`.
let getContentEditableCaretClientX (root: HTMLElement) : float option =
    let s = window.getSelection ()
    if isNull s || s.rangeCount < 1. then None
    else
        let r = s.getRangeAt 0.
        if not (root.contains r.startContainer) then None
        else
            let rr = document.createRange ()
            rr.setStart (r.startContainer, r.startOffset)
            rr.collapse true
            Some (rr.getBoundingClientRect().left)

type private VisualLineKind =
    | VisualLineFirst
    | VisualLineLast

let private refTopForVisualLine (t: Text) (kind: VisualLineKind) : float =
    match kind with
    | VisualLineFirst -> rangeLineTop t 0.
    | VisualLineLast -> rangeLineTop t (float t.length)

/// Shared: flat empty = one line; `FlatOneText` compares caret `top` to line end.
let private isContentEditableCaretOnVisualLine (kind: VisualLineKind) (root: HTMLElement) : bool =
    match classifyFlatEditable root with
    | FlatInvalid -> false
    | FlatEmpty -> selectionCaretTop root |> Option.isSome
    | FlatOneText t ->
        match selectionCaretTop root with
        | None -> false
        | Some y -> abs (y - refTopForVisualLine t kind) < visualLineEpsilon

/// True when the selection start lies on the first visual line (`root` flat per
/// `classifyFlatEditable`). Non-collapsed ranges use the start boundary.
let isContentEditableCaretOnVisualFirstLine (root: HTMLElement) : bool =
    isContentEditableCaretOnVisualLine VisualLineFirst root

/// True when the selection start lies on the last visual line (`root` flat).
let isContentEditableCaretOnVisualLastLine (root: HTMLElement) : bool =
    isContentEditableCaretOnVisualLine VisualLineLast root

/// Collapsed range at (x, y) in viewport coords, or null — caretRangeFromPoint /
/// caretPositionFromPoint.
[<Emit("""(function(x,y){
if(document.caretRangeFromPoint){var r=document.caretRangeFromPoint(x,y);return r||null;}
if(document.caretPositionFromPoint){var p=document.caretPositionFromPoint(x,y);
if(!p)return null;var r=document.createRange();
r.setStart(p.offsetNode,p.offset);r.collapse(true);return r;}
return null;})($0,$1)""")>]
let private tryDocumentCaretRangeFromPoint (x: float) (y: float) : Range = jsNative

let private rootTextUtf16Length (root: HTMLElement) : int =
    let t = root.textContent
    if isNull t then 0 else t.Length

let private insetForCaretHit (left: float) (top: float) (right: float) (bottom: float) (inset: float) : float =
    let w = max 0. (right - left)
    let h = max 0. (bottom - top)
    min inset (max 1. (min w h / 4.))

let private clampClientXForCaretHit (left: float) (top: float) (right: float) (bottom: float) (clientX: float) : float =
    let ins = insetForCaretHit left top right bottom 2.
    ClientRectCaret.clamp (left + ins) (right - ins) clientX

let private selectionApplyIfInRoot (root: HTMLElement) (sel: DomSelection) (r: Range) : bool =
    if isNull r || not (root.contains r.startContainer) then
        false
    else
        sel.removeAllRanges ()
        sel.addRange r
        true

/// Focus `root`, then caret on first visual line (`caretRangeFromPoint` + `ClientRectCaret`
/// probe). Fallback: `setContentEditableCaret` at 0. Works for any contenteditable layout.
let setContentEditableCaretVisualFirstLine (root: HTMLElement) : unit =
    root.focus ()
    let sel = window.getSelection ()
    if isNull sel then
        ()
    else
        let rect = root.getBoundingClientRect ()
        let x, y =
            ClientRectCaret.probeFirstVisualLine rect.left rect.top rect.right rect.bottom 2.
        let r = tryDocumentCaretRangeFromPoint x y
        if not (selectionApplyIfInRoot root sel r) then
            setEditorCaret root 0

/// Focus `root`, then caret on last visual line; fallback end of `textContent`.
let setContentEditableCaretVisualLastLine (root: HTMLElement) : unit =
    root.focus ()
    let sel = window.getSelection ()
    if isNull sel then
        ()
    else
        let rect = root.getBoundingClientRect ()
        let x, y =
            ClientRectCaret.probeLastVisualLine rect.left rect.top rect.right rect.bottom 2.
        let r = tryDocumentCaretRangeFromPoint x y
        let endPos = rootTextUtf16Length root
        if not (selectionApplyIfInRoot root sel r) then
            setEditorCaret root endPos

/// Like `setContentEditableCaretVisualLastLine` but uses viewport `clientX` (e.g. from
/// `getContentEditableCaretClientX` on the previous field). `clientX` is clamped into the
/// element rect with the same inset rule as the line probes.
let setEditorCaretToLastLineAtX (root: HTMLElement) (clientX: float) : unit =
    root.focus ()
    let sel = window.getSelection ()
    if isNull sel then
        ()
    else
        let rect = root.getBoundingClientRect ()
        let _, y =
            ClientRectCaret.probeLastVisualLine rect.left rect.top rect.right rect.bottom 2.
        let x =
            clampClientXForCaretHit rect.left rect.top rect.right rect.bottom clientX
        let r = tryDocumentCaretRangeFromPoint x y
        let endPos = rootTextUtf16Length root
        if not (selectionApplyIfInRoot root sel r) then
            setEditorCaret root endPos

/// Like `setContentEditableCaretVisualFirstLine` but uses viewport `clientX` on the first
/// visual line (same inset/clamp as last-line-at-X).
let setEditorCarentToFirstLineAtX (root: HTMLElement) (clientX: float) :
        unit =
    root.focus ()
    let sel = window.getSelection ()
    if isNull sel then
        ()
    else
        let rect = root.getBoundingClientRect ()
        let _, y =
            ClientRectCaret.probeFirstVisualLine rect.left rect.top rect.right rect.bottom 2.
        let x =
            clampClientXForCaretHit rect.left rect.top rect.right rect.bottom clientX
        let r = tryDocumentCaretRangeFromPoint x y
        if not (selectionApplyIfInRoot root sel r) then
            setEditorCaret root 0

[<Emit("fetch($0).then(r => r.text()).then($1)")>]
let fetchText (url: string) (callback: string -> unit) : unit = jsNative

[<Emit("fetch($0, {cache: 'no-store', credentials: 'same-origin'}).then(r => r.ok ? r.text() : Promise.reject(r.status)).then($1).catch(function(){})")>]
let fetchTextNoCache (url: string) (callback: string -> unit) : unit = jsNative

[<Emit("Date.now()")>]
let nowMs () : int = jsNative

[<Emit("(typeof window.__BUILD_TS__ !== 'undefined' ? window.__BUILD_TS__ : 0)")>]
let readBuildEpochSec () : int = jsNative

[<Emit("(function(epochSec){
    var d = new Date(epochSec*1000);
    var parts = new Intl.DateTimeFormat('en-CA', {
        timeZone: 'America/Toronto',
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', second: '2-digit',
        hour12: false
    }).formatToParts(d);
    var m = {};
    parts.forEach(function(p){ if(p.type !== 'literal') m[p.type] = p.value; });
    return m.year + '-' + m.month + '-' + m.day + ' ' + m.hour + ':' + m.minute + ':' + m.second + ' ET';
})($0)")>]
let epochSecToTorontoString (epochSec: int) : string = jsNative

[<Emit("(typeof window.__PAGE_BUILD_TS__ !== 'undefined' ? window.__PAGE_BUILD_TS__ : 0)")>]
let readPageBuildEpochSec () : int = jsNative

let sessionGet (key: string) : string =
    window.sessionStorage.getItem key

let sessionSet (key: string) (value: string) : unit =
    window.sessionStorage.setItem (key, value)

let isDocumentHidden () : bool =
    document.hidden

/// Returns the timer handle (pass to browser APIs if needed).
let setInterval (f: unit -> unit) (ms: int) : float =
    window.setInterval ((fun _ -> f ()), ms)
