namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Gambol.Shared

/// Open response for one Workspace file: media type, then inline text.
[<RequireQualifiedAccess>]
module WorkspaceFileMedia =

    let private plainText = "text/plain; charset=utf-8"

    let internal contentTypeFor (fileName: string) (sample: FileText.Sample) : string =
        match Path.GetExtension(fileName).ToLowerInvariant() with
        | ".pdf" -> "application/pdf"
        | ".png" -> "image/png"
        | ".jpg"
        | ".jpeg" -> "image/jpeg"
        | ".gif" -> "image/gif"
        | ".webp" -> "image/webp"
        | ".svg" -> "image/svg+xml"
        | ".heic" -> "image/heic"
        | ".html"
        | ".htm" -> plainText
        | _ ->
            match FileText.classifySample sample with
            | FileText.Kind.Text -> plainText
            | FileText.Kind.Binary -> "application/octet-stream"

    let private readSample (fullPath: string) : Result<FileText.Sample, string> =
        try
            use stream =
                new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read)

            let length = stream.Length

            let count =
                int (min (int64 FileText.sampleByteCount) length)

            let buffer = Array.zeroCreate count

            let rec fill (offset: int) =
                if offset >= count then
                    Ok ()
                else
                    let n = stream.Read(buffer, offset, count - offset)

                    if n = 0 then
                        Error "short read"
                    else
                        fill (offset + n)

            match fill 0 with
            | Error e -> Error e
            | Ok () ->
                Ok (FileText.sampleFromBytes buffer (length > int64 count))
        with ex ->
            Error ex.Message

    let private forceInline (httpContext: HttpContext) =
        httpContext.Response.OnStarting(
            Func<Task>(fun () ->
                httpContext.Response.Headers["Content-Disposition"] <- "inline"
                Task.CompletedTask))
        |> ignore

    let private inlineFile (fullPath: string) (contentType: string) : IResult =
        let inner = Results.File(fullPath, contentType)

        { new IResult with
            member _.ExecuteAsync(httpContext) =
                task {
                    forceInline httpContext
                    do! inner.ExecuteAsync(httpContext)
                }
                :> Task }

    let internal openFileResult (fullPath: string) : Result<IResult, string> =
        match readSample fullPath with
        | Error e -> Error e
        | Ok sample ->
            let media = contentTypeFor fullPath sample

            if media = plainText then
                Ok (inlineFile fullPath media)
            else
                Ok (Results.File(fullPath, media))
