namespace Gambol.Shared

/// Shared UTF-16 storage policy for materializing in-memory text as graph nodes.
[<RequireQualifiedAccess>]
module DocumentParseLimits =

    [<Literal>]
    let maxInputUtf16Bytes = 100_000L

    [<Literal>]
    let maxInputCodeUnits = 50_000

    let errorForCodeUnits (actualCodeUnits: int) =
        let actualBytes = int64 actualCodeUnits * 2L

        $"parse input is too large: {actualCodeUnits} UTF-16 code units "
        + $"({actualBytes} UTF-16 bytes); limit is {maxInputCodeUnits} code units "
        + $"({maxInputUtf16Bytes} UTF-16 bytes)"

    let refuseCodeUnits (actualCodeUnits: int) : Result<unit, string> =
        if actualCodeUnits > maxInputCodeUnits then
            Error(errorForCodeUnits actualCodeUnits)
        else
            Ok ()

    let refuseText (text: string) : Result<unit, string> =
        refuseCodeUnits text.Length

    let refuseEmptyText (text: string) : Result<unit, string> =
        if System.String.IsNullOrWhiteSpace text then
            Error "import text is empty"
        else
            Ok ()
