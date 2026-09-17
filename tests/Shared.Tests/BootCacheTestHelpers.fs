module BootCacheTestHelpers

open System
open Gambol.Shared

let private mkChange n =
    { id = EventId.fromJson n
      submissionId = Guid.NewGuid()
      ops = [] }

let mkEvent id = Ev.ofChange "fixture" (mkChange id)
