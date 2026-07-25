namespace Gambol.Server

open System.IO
open Gambol.Shared

/// Paths and I/O for `gambol.meta` and `gambol.log`.
[<RequireQualifiedAccess>]
module Bookkeeping =

    let metaPath (dataDir: string) = Path.Combine(dataDir, "gambol.meta")

    let logPath (dataDir: string) = Path.Combine(dataDir, "gambol.log")

    let readRevision (dataDir: string) : Revision =
        let path = metaPath dataDir

        try
            if File.Exists path then
                match System.Int32.TryParse(File.ReadAllText(path).Trim()) with
                | true, rev -> Revision rev
                | _ -> Revision 0
            else
                Revision 0
        with _ ->
            Revision 0

    let writeRevision (dataDir: string) (rev: int) : Result<unit, string> =
        let path = metaPath dataDir

        try
            let tmpPath = path + ".tmp"
            File.WriteAllText(tmpPath, string rev)
            File.Move(tmpPath, path, true)
            Ok ()
        with ex ->
            Error ex.Message

    let openLogStream (dataDir: string) : FileStream =
        new FileStream(
            logPath dataDir,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite)
