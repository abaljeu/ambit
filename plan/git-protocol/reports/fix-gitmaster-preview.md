# Fix gitmaster preview

## Red

Command:

```sh
./tmp/check-gitmaster-preview.sh
```

Result: Exit 1. The expected seven commits ended at d753796, but the preview printed 54 older commits after that boundary. The temporary focused check compared the full expected commit sequence and was removed after the green run.

## Implementation

[[scripts/gitmaster.sh]] now examines first-parent merge commits on ready and selects the latest merge whose non-first parent is the current master commit. It prints the normal ready log through that boundary, inclusive, so it keeps side-history commits that appear before the boundary in normal log order. If no matching boundary exists, it keeps the former master..ready preview. The trees_match path and argument-taking squash path are unchanged.

## Green

Commands:

```sh
./tmp/check-gitmaster-preview.sh
bash -n scripts/gitmaster.sh
```

Result: The focused check exited 0 and reported that the preview stops at the current-master forward boundary. The syntax check exited 0 with no output.

## Changed files

- [[scripts/gitmaster.sh]]
- [[plan/git-protocol/reports/fix-gitmaster-preview.md]]
