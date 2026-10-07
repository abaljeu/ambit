# 07 ActorStart red log

## 1. Command

`dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~records ActorStart"`

## 2. Result

Failed. `System.Exception : ActorStart missing on the event source`

```text
[xUnit.net 00:00:00.46]     Gambol.Server.Tests.SearchActorTests.postSearch records ActorStart with root focus and the server graph [FAIL]
  Failed Gambol.Server.Tests.SearchActorTests.postSearch records ActorStart with root focus and the server graph [114 ms]
  Error Message:
   System.Exception : ActorStart missing on the event source
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 239 ms - Gambol.Server.Tests.dll (net10.0)
EXIT:1
```

The filter also matched `mailbox records ActorStarted before actor body runs`, which passed. The Search Actor test failed because the event source had no `ActorStart`.
