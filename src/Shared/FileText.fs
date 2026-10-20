namespace Gambol.Shared

/// Text or binary from a short byte sample. No NUL, and valid UTF-8.
[<RequireQualifiedAccess>]
module FileText =

    [<RequireQualifiedAccess>]
    type Kind =
        | Text
        | Binary

    [<RequireQualifiedAccess>]
    type Sample = {
        bytes: byte[]
        truncated: bool
    }

    /// Leading bytes read before the text or binary decision.
    let sampleByteCount = 8192

    let sampleFromBytes (bytes: byte[]) (truncated: bool) : Sample =
        { bytes = bytes; truncated = truncated }

    let private anyCont (_: int) = true

    let private atLeast (minByte: int) (b: int) = b >= minByte

    let private atMost (maxByte: int) (b: int) = b <= maxByte

    let private isCont (b: int) = b >= 0x80 && b <= 0xBF

    let private takeConts
        (bytes: byte[])
        (allowTail: bool)
        (start: int)
        (count: int)
        (secondOk: int -> bool)
        : bool =
        let len = bytes.Length

        let rec take (n: int) =
            let j = start + n

            if n > count then
                true
            elif j >= len then
                allowTail
            else
                let b = int bytes.[j]

                if not (isCont b) then
                    false
                elif n = 1 && not (secondOk b) then
                    false
                else
                    take (n + 1)

        take 1

    let private utf8Step
        (bytes: byte[])
        (allowTail: bool)
        (i: int)
        (b: int)
        : int option =
        let go count secondOk =
            if takeConts bytes allowTail i count secondOk then
                Some (i + count + 1)
            else
                None

        if b < 0x80 then Some (i + 1)
        elif b < 0xC2 then None
        elif b <= 0xDF then go 1 anyCont
        elif b = 0xE0 then go 2 (atLeast 0xA0)
        elif b <= 0xEC then go 2 anyCont
        elif b = 0xED then go 2 (atMost 0x9F)
        elif b <= 0xEF then go 2 anyCont
        elif b = 0xF0 then go 3 (atLeast 0x90)
        elif b <= 0xF3 then go 3 anyCont
        elif b = 0xF4 then go 3 (atMost 0x8F)
        else None

    let private utf8Ok (bytes: byte[]) (allowTail: bool) : bool =
        let len = bytes.Length

        let rec loop (i: int) =
            if i >= len then
                true
            else
                match utf8Step bytes allowTail i (int bytes.[i]) with
                | None -> false
                | Some next -> loop next

        loop 0

    let classifySample (sample: Sample) : Kind =
        if Array.exists ((=) 0uy) sample.bytes then
            Kind.Binary
        elif utf8Ok sample.bytes sample.truncated then
            Kind.Text
        else
            Kind.Binary
