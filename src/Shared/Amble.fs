namespace Gambol.Shared

[<RequireQualifiedAccess>]
module Amble =
    let parse = AmbleParse.parse
    let run = AmbleRun.run
