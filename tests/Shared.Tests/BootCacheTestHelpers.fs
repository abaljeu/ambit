module BootCacheTestHelpers

open System
open Gambol.Shared

let private mkChange id =
    { id = id
      submissionId = Guid.NewGuid()
      ops = [] }

let mkEvent id = Ev.ofChange "fixture" (mkChange id)
