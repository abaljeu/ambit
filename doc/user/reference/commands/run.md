# Run

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

These commands start a node, stop that work, or check the outline.

## Run

Keys: Ctrl+Enter and Alt+Enter. Any Mode.

What it does: A line that starts with `?` starts that [Actor](doc/user/guide/actor.md). A line that contains `=` (including `= expr` and `name = expr`) starts a query [Actor](doc/user/guide/query-language.md). The [Amble](doc/user/reference/amble-language.md) evaluator runs on a line that starts with `>`, or on a line under an `=` owner when no `?` owner sits above it.

Related: [Cancel](#cancel).

## Cancel

Keys: Ctrl+Shift+Enter. Any Mode.

What it does: Stops the live run on the focused node. The command palette hides Cancel when there is no run to stop.

## Check graph

Keys: none. Run it from the [Command palette](search.md#command-palette). Any Mode.

What it does: Checks ownership in the outline. A failing node becomes the selection, and Ambit shows the error.
