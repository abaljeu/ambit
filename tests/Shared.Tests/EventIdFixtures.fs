module EventIdFixtures

open Gambol.Shared

/// Stored EventId after `count` EventLog serials. count is 1-based.
let storedId count =
    let rec loop n id =
        if n <= 1 then id
        else loop (n - 1) (EventId.next id)
    loop count EventLog.empty.nextId
