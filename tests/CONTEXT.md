# CONTEXT

xUnit tests live here.

## Folder map

- Shared.Tests/
- Server.Tests/
- CloudAgents.Tests/

## Routing

- You want shared tests: Shared.Tests/
- You want server tests: Server.Tests/
- You want cloud agent tests: CloudAgents.Tests/
- You want the source under test: [[../src/CONTEXT.md]]
- You want the test script: [[../scripts/agent-test.sh]]

## Running tests

- Before you commit: run `scripts/agent-test.sh --filter <names>` for the tests your change touches. Commit and push only when they pass.
- At review: run `scripts/agent-test.sh --full` first. It prints a pid and runs in the background. Do the rest of the review while it runs. Poll with `scripts/check-test.sh <pid>`.

## Handoff

No folder under tests/ has its own CONTEXT.md.

This file is the router.
