---
name: tdd
description: Red-green loop for building a ticket. Use when writing or changing tests, or when the user mentions TDD or red-green.
---

# Test-Driven Development

Read [[../../../GLOSSARY.md]] so test names use the project's terms.

## A good test

A good test

- checks behavior through a public interface (a seam)
- reads like a specification: ``checkout confirms a valid cart``
- survives a refactor that keeps the behavior
- takes its expected value from an independent source: a known literal, a worked example, or the spec
- checks behavior with logic in it, beyond a constant assignment

Mock only what you don't control:

- external APIs
- time and randomness
- the database or file system, when a real fixture costs more

Pass those dependencies in.

```fsharp
[<Fact>]
let ``total sums line item prices`` () =
    let items = [ { Price = 10 }; { Price = 5 } ]
    Assert.Equal(15, Totals.sum items)
```

## Seams

Test at the seams the ticket's Tests section names. A test at another seam needs the user's agreement first.

## The loop

Work in vertical slices. Each cycle:

1. Write one failing test at one seam.
2. Run `scripts/agent-test.sh --filter <names>` and see it fail.
3. Write the least code that passes it.
4. Run the filter again and see it pass.

Repeat until the ticket's Tests section is covered. Refactoring belongs to review.

## Finish

Commit and push only when the focused tests pass. The full run belongs to review.
