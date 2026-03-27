module Gambol.Client.JsInterop

open Browser.Dom
open Browser.Types
open Fable.Core

/// Collapsed caret at UTF-16 offset `pos` inside `root` (contenteditable).
let setContentEditableCaret (root: HTMLElement) (pos: int) : unit =
    let sel = window.getSelection ()
    if isNull sel then
        ()
    else
        let range = document.createRange ()
        let mutable rem = max 0 pos
        // NodeFilter.SHOW_TEXT = 4
        let walker = document.createTreeWalker (root :> Node, whatToShow = 4.)
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
            range.selectNodeContents (root :> Node)
            range.collapse false
            sel.removeAllRanges ()
            sel.addRange range

/// UTF-16 offset in `root.textContent` from the start of `root` to (`container`, `offset`).
let private rangeBoundaryOffsetInRoot (root: HTMLElement) (container: Node) (offset: float) : int =
    let pre = document.createRange ()
    pre.selectNodeContents (root :> Node)
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
