# Test commands

Invocations for [[SKILL.md]].

## Foreground

Run `scripts/agent-test.sh` and wait. That command tests the files edited against `origin/staging`.

To run one module:

```bash
scripts/agent-test.sh --filter "FullyQualifiedName~YourTestModule" shared
```

Replace `YourTestModule` with the test module that covers the change.

## Client compile gate

```bash
./scripts/client.sh build
```

`/ambit` serves `Program.bundle.js`. Bare `dotnet fable` is not the gate. Default `./scripts/client.sh` is watch, not this gate.

## Background

```bash
scripts/agent-test.sh --full
```

Do not wait. The command prints the log path and the PID.
