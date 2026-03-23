module Gambol.Client.JsInterop

open Fable.Core

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

[<Emit("sessionStorage.getItem($0)")>]
let sessionGet (key: string) : string = jsNative

[<Emit("sessionStorage.setItem($0, $1)")>]
let sessionSet (key: string) (value: string) : unit = jsNative

[<Emit("document.hidden")>]
let isDocumentHidden () : bool = jsNative

[<Emit("setInterval($0, $1)")>]
let setInterval (f: unit -> unit) (ms: int) : int = jsNative
