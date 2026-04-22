---
name: roll-dice
description: Simulates dice rolls for any random decision.
---

# Roll dice

## When to use

Apply this skill whenever randomness is requested with dice, coins, or similar
fair picks—not only literal "roll a d20" but also "randomly choose" when dice
notation or game-style rolls fit the context.

## Notation (interpret before rolling)

| Form | Meaning |
|------|---------|
| `XdY` | Roll X dice, each Y-sided (e.g. `3d6`). |
| `XdY+Z` / `XdY-Z` | Sum dice, then add or subtract Z. |
| `dY` | Same as `1dY`. |
| `d%` or `d100` | Percentile: one number 1–100 (or two d10 as tens/ones if user asks). |
| Advantage / disadvantage | Two d20; take higher (advantage) or lower (disadvantage). |
| Pool | Roll many dice, often count successes (user defines threshold, e.g. 5+ on d6). |

If notation is ambiguous, ask one short clarifying question before rolling.

## How to roll

1. Parse the request into count, sides, modifiers, and any special rules.
2. Produce random outcomes:
   - In bash, roll **one** die with this pattern (replace `<sides>` with the
     integer side count before running—e.g. `6` for d6, `20` for d20):

     ```bash
     echo $((RANDOM % <sides> + 1))
     ```

     For `XdY`, run one roll per die `X` times (or a short loop) and sum the
     results.
   - Otherwise prefer a small script or RNG when the environment allows.
   - If you cannot execute code, state that rolls are simulated and show the
     rolled values explicitly (still list individual results when multiple dice).
3. Sum and apply modifiers. Show **each die** and the **final total** (or
   success count for pools).

## Output format

Use a compact block the user can scan:

```text
Roll: 2d6+3
Dice: 4, 6  →  subtotal 10  →  total 13
```

For advantage:

```text
Roll: d20 with advantage
Dice: 17, 8  →  kept 17
```

## Edge cases

- **Single die**: still show the one value and total if a modifier exists.
- **Large counts** (e.g. 40d6): summarize if needed (subtotals per batch) but
  keep the final sum correct.
- **Exploding dice** or **reroll 1s**: only if the user asks; confirm rules first.

## Do not

- Do not substitute a fixed "example" number when the user wanted a random roll.
- Do not use vague prose ("you rolled high") without numbers when numbers were
  requested.
