module Gambol.Server.Tests.ErrorLogTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend

[<Fact>]
let ``append writes one timestamped line`` () =
    let dataDir = newTempDir ()
    ErrorLog.append dataDir "SOME-KIND" "some detail"
    let line = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("SOME-KIND some detail", line)
    let timestamp = line.Split(' ').[0]
    Assert.True(DateTime.TryParse(timestamp) |> fst, $"not a timestamp: {timestamp}")

[<Fact>]
let ``append is append-only: a later call keeps the earlier line`` () =
    let dataDir = newTempDir ()
    ErrorLog.append dataDir "FIRST" "one"
    ErrorLog.append dataDir "SECOND" "two"
    let lines = File.ReadAllLines(ErrorLog.logPath dataDir)
    Assert.Equal(2, lines.Length)
    Assert.Contains("FIRST one", lines.[0])
    Assert.Contains("SECOND two", lines.[1])

[<Fact>]
let ``append never truncates content written before this process started`` () =
    // Simulates a restart: content from a prior run already sits in the file
    // before this process's first `ErrorLog.append` call.
    let dataDir = newTempDir ()
    let logPath = ErrorLog.logPath dataDir
    Directory.CreateDirectory(Path.GetDirectoryName logPath) |> ignore
    File.WriteAllText(logPath, "2026-01-01T00:00:00.0000000Z PRIOR-RUN before restart\n")

    ErrorLog.append dataDir "AFTER-RESTART" "after restart"

    let lines = File.ReadAllLines(logPath)
    Assert.Equal(2, lines.Length)
    Assert.Contains("PRIOR-RUN before restart", lines.[0])
    Assert.Contains("AFTER-RESTART after restart", lines.[1])

[<Fact>]
let ``appendProjectionRepair writes the projection repair message`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendProjectionRepair (Some dataDir) "DbAgent: projection repair deleted=1"
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("PROJECTION-REPAIR", text)
    Assert.Contains("deleted=1", text)

[<Fact>]
let ``appendLoadFallback writes the fallback reason`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendLoadFallback (Some dataDir) "non-dense ordinals"
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("LOAD-FALLBACK", text)
    Assert.Contains("reason=non-dense ordinals", text)

[<Fact>]
let ``appendDecodeDrop writes the row's event id and decode error`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendDecodeDrop (Some dataDir) 42 "bad json"
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("DECODE-DROP", text)
    Assert.Contains("eventId=42", text)
    Assert.Contains("reason=bad json", text)

[<Fact>]
let ``appendRecoverInvalid writes the skipped event id and invalid reason`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendRecoverInvalid (Some dataDir) 7 "old span out of bounds"
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("RECOVER-INVALID", text)
    Assert.Contains("eventId=7", text)
    Assert.Contains("reason=old span out of bounds", text)

[<Fact>]
let ``appendDbConnectionFallback writes the fallback message`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendDbConnectionFallback (Some dataDir) "falling back to file store"
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("DB-CONNECTION-FALLBACK", text)
    Assert.Contains("falling back to file store", text)

[<Fact>]
let ``appendStartupFallback writes the source and detail`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendStartupFallback (Some dataDir) "SomeModule.someFunction" "detail text"
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("STARTUP-FALLBACK", text)
    Assert.Contains("source=SomeModule.someFunction", text)
    Assert.Contains("detail=detail text", text)

[<Fact>]
let ``appendStartupException writes the source, exception type and message`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendStartupException
        (Some dataDir) "SomeModule.someFunction" (InvalidOperationException("boom"))
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("STARTUP-EXCEPTION", text)
    Assert.Contains("source=SomeModule.someFunction", text)
    Assert.Contains("type=System.InvalidOperationException", text)
    Assert.Contains("message=boom", text)

[<Fact>]
let ``a None data directory is a no-op, not an exception`` () =
    ErrorLog.appendLoadFallback None "unused"
    ErrorLog.appendDecodeDrop None 1 "unused"
    ErrorLog.appendRecoverInvalid None 1 "unused"
    ErrorLog.appendProjectionRepair None "unused"
    ErrorLog.appendDbConnectionFallback None "unused"
    ErrorLog.appendStartupFallback None "unused" "unused"
    ErrorLog.appendStartupException None "unused" (Exception("unused"))

[<Fact>]
let ``logPath sits at SYSTEM errors.log under the data directory`` () =
    let dataDir = newTempDir ()
    let expected = Path.Combine(dataDir, "SYSTEM", "errors.log")
    Assert.Equal(expected, ErrorLog.logPath dataDir)
