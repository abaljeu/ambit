namespace Gambol.Shared

type FileState =
    | Unparsed
    | Parsed of sourceMtimeUtc: int64

[<RequireQualifiedAccess>]
module FileState =
    let defaultValue = Unparsed

    let isParsed (state: FileState) : bool =
        match state with
        | Unparsed -> false
        | Parsed _ -> true

    let mtime (state: FileState) : int64 option =
        match state with
        | Unparsed -> None
        | Parsed m -> Some m

    let isStale (diskMtimeUtc: int64) (state: FileState) : bool =
        match state with
        | Unparsed -> false
        | Parsed m -> diskMtimeUtc > m

    let toPersistString (state: FileState) : string option =
        match state with
        | Unparsed -> None
        | Parsed m -> Some(string m)

    let fromPersistString (value: string option) : FileState =
        match value with
        | None -> Unparsed
        | Some s ->
            match System.Int64.TryParse s with
            | true, m -> Parsed m
            | _ -> Unparsed
