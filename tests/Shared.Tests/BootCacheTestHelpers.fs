module BootCacheTestHelpers

open System
open Gambol.Shared

let mkEvent id =
    SpecialNodeTestHelpers.changeEvent
        "fixture"
        (EventId.fromJson id)
        (Guid.NewGuid())
        []
