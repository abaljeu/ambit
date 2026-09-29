# Standards — Ticket 20 — State axes on special nodes

Range `origin/staging...HEAD` is not empty. Tip `3fe7d9f01cb6edff0602856236dd057ea36dc508`. Base `88436e6a3fc99baa594b4fc1edeb571723e83a9e`. Commits `bb40fac9` and `3fe7d9f0`.

## Hard violations

### Refer by name

[.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) requires a number and a name for every list item. Never refer by only the id. These added report lines name [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 items by number only:

- [20 — independent code review](plan/github-transport/reports/20-independent-code-review-2026-09-29.md) line 31: “§4 item 3 wants the Directory Node Unparsed.”
- Same file line 32: “§4 item 3 describes that Unparsed write.”
- Same file line 33: “§4 item 8 and item 9 say mark the Directory Node or File Node Unparsed.”
- [20 — Spec agent](plan/github-transport/reports/20-spec-agent-2026-09-29.md) line 79: “No separate axis path (spec §4 item 4).”

Other scan `BARE_ID` hits on those files put the item title on the same line, such as “Persist done — Mark that node Persisted only.” Those lines obey the rule.

Added F# bindings stay at or under the limits in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (40 lines per function, 100 characters per line, 800 lines per file). No `mutable`, tabs, one-word public names, new O(nodes) mutation scans, or extra SQL or HTML. [DatabaseProjection.fs](src/Server/DatabaseProjection.fs) and [DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) stay Adapter persist paths.

## Judgement calls

Possible Mysterious Name in [DocumentParseOps.fs](src/Shared/dotnet/DocumentParseOps.fs). `marksOwningSpecialUnpersisted` returns a bool and does not mark a node.

```
    let private marksOwningSpecialUnpersisted =
        function
        | Op.SetText _
        | Op.SetClasses _
        | Op.SetName _
        | Op.Replace _ -> true
        | _ -> false
```
