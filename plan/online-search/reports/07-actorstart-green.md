# 07 ActorStart green log

## 1. Command

`dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~SearchActorTests"`

## 2. Result

Passed. 9 tests, including `postSearch records ActorStart with root focus and the server graph` and `actorStart supplies root and focus`.

```text
Passed!  - Failed:     0, Passed:     9, Skipped:     0, Total:     9, Duration: 193 ms - Gambol.Server.Tests.dll (net10.0)
EXIT:0
```
