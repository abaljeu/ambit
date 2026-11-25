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
let ``appendDbConnectionFallback writes the fallback message`` () =
    let dataDir = newTempDir ()
    ErrorLog.appendDbConnectionFallback (Some dataDir) "falling back to file store"
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("DB-CONNECTION-FALLBACK", text)
    Assert.Contains("falling back to file store", text)

[<Fact>]
let ``a None data directory is a no-op, not an exception`` () =
    ErrorLog.appendProjectionRepair None "unused"
    ErrorLog.appendDbConnectionFallback None "unused"

[<Fact>]
let ``logPath sits at SYSTEM errors.log under the data directory`` () =
    let dataDir = newTempDir ()
    let expected = Path.Combine(dataDir, "SYSTEM", "errors.log")
    Assert.Equal(expected, ErrorLog.logPath dataDir)
