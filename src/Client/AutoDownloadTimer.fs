module Gambol.Client.AutoDownloadTimer

open Gambol.Client.JsInterop

/// Arm or re-arm a debounce timeout. Clears any previous id, stores the new one,
/// and clears the stored id when the callback fires.
let armOrRearm
    (getId: unit -> float option)
    (setId: float option -> unit)
    (delayMs: int)
    (onFire: unit -> unit)
    : unit =
    match getId () with
    | Some id -> clearTimeout id
    | None -> ()
    setId (
        Some (
            setTimeout
                (fun () ->
                    setId None
                    onFire ())
                delayMs))
