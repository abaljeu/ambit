# Test commands

Invocations for [[SKILL.md]].

## Foreground

Use `dotnet test` with a filter. The vscode test runner hangs.

```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~YourTestModule"
```

Replace `YourTestModule` with the test module that covers the change.

## Client compile gate

```bash
./scripts/client.sh build
```

`/ambit` serves `Program.bundle.js`. Bare `dotnet fable` is not the gate. Default `./scripts/client.sh` is watch, not this gate.

## Background

Shared:

```bash
./scripts/test.sh shared
```

Shared and Server:

```bash
./scripts/test.sh all
```
