# Review context

## 1. Work

Write one report per ticket to `plan/<slug>/review/NN-<ticket-name>.md`. Write that report when the review step is about to be worked.

## 2. Review template

Copy these headings into the report. Leave each pointer line as written. A pointer line names its target file.

```
# <NN> — <ticket name>

Ticket: [[../issues/NN-<ticket-name>.md]]

## 1. Score

## 2. Tests

## 3. Miss
```

## 3. Fill

1. **Score.** Score the report against each story the ticket named.
2. **Tests.** Check that each test the ticket named exists and checks what the ticket says. Record the filtered result and the full-suite result from its log.
3. **Miss.** Name what failed. These are misses:

   - a named test that is missing
   - a failing test
   - a test that checks something other than the ticket's done

   A miss returns that ticket to build.

Done: the report scores the stories that ticket named and records the test results. A miss names what failed, and that ticket returns to build.
