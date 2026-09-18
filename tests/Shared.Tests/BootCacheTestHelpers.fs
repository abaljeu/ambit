module BootCacheTestHelpers

open System
open Gambol.Shared

let mkEvent id =
    SpecialNodeTestHelpers.changeEvent
        "fixture"
        (EventIdFixtures.storedId id)
        (Guid.NewGuid())
        []
