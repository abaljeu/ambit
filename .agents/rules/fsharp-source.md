Use pure functional F#. Don't use mutable.
Don't use Exceptions. Use Error types.

Don't embed anything more than tiny amounts of html, CSS, JS or SQL in F# code.
If you are looking for browser access functions, look at ./other/fable.browser.dom.fs.

100 characters or less per line on source code.

40 lines or less per function.

Group related function parameters into a named, reused type (record or DU). When adding a parameter that belongs with existing ones, extend that type instead of lengthening the argument list. Reuse a type that already exists. Do not invent a one-off tuple or a mega-record of unrelated values; split by cohesion.

800 lines or less per file. To minimize churn, when this occurs, split the file into three pieces each under 400 lines.  If a file is already longer, only restructure to split up the code if your changes would increase it.

This rule does not apply to tests.  Tests should split to match the source files.

Splitting should be a standalone operation, executed after the project edits are posted.  This operation will only have commits whose role is to restructure code.  Avoid creating a forwarding module in favor of updating the users to point to the right new file.

Public function names must be either more than one word, or explicitly require context to be called.

TABs are not allowed in F#. Always indent 4 spaces.
Indentation is equally important as it is in python or Haskell.

Follow language norms. Match existing style.

Graphs may be millions of nodes. In hot paths, avoid O(nodes) full-graph scans (e.g. `Map.toList graph.nodes`). Prefer owner-subtree or other local walks (see `GraphQuery.ownedArtifactsInDirectory`).

Never rebuild a whole structure once per item. Ops are applied in long batches (a parse tail or history replay is thousands in a row), so a per-item full rebuild or full scan is quadratic overall even when each call looks cheap. Watch for: calling `Graph.fromNodes` or any re-index/re-sort inside a per-op path; growing a list with `@` or `List.append` in a fold; `List.length`/`List.last`/`List.item` inside a loop over the same list. Instead update the index incrementally (see `GraphBuild.addDetachedNode` and `GraphBuild.appendChildren`), accumulate with `::` and reverse once, or rebuild once after the batch. When adding or changing a mutation op, cost it for a batch of 10,000, not for one call.

Before adding a custom graph walk in Shared, check `GraphQuery` helpers first: `enclosing` walks **up** the owner chain; `ownedArtifactsInDirectory` walks **down** through Owner children (recurse Normal|Workspaces; stop at artifacts). A new `walk` is rarely needed — e.g. `ownsFileOrDirectoryThroughSkippables` is just a File|Directory filter over that downward walk, not a separate traversal.
