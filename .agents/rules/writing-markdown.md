# Writing markdown

## Lists
Never write a list of things as prose.
Write lists of things as unnumbered lists unless otherwise specified.

###  Counterexample
```
Done: the ticket names its type, its stories, its blockers, the files, types, and functions to look at, the changes, and what done looks like. The implementer uses the ticket and the code.
```

Write that list as:
```
Done: the ticket names

- its type
- its stories
- its blockers
- the files to look at
- the types to look at
- the functions to look at
- the changes
- what done looks like

The implementer uses the ticket and the code.
```

## Positivity
Skip negative statements. Instead make sure it's implied by the positive statement.

WRONG: "This folder does not take these steps. (list of steps)"
RIGHT: "This folder takes these steps. (list of steps)"

## Precision
Avoid prose when you can express things more exactly.
WORSE:
  "The text should fade in with a bouncy spring effect"
The agent interpreted that differently every time. When I switched to code blueprints:

BETTER: 
✅  const BEATS = { TITLE_IN: 0, SUBTITLE: 30, SCENE_END: 90 };
    // AnimatedText variant="title" size={52} entrance="scale"
    //   springPreset="bouncy" startFrame={BEATS.TITLE_IN}
