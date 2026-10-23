# Review context

## 1. Work

Write one report per ticket to `plan/<slug>/review/NN-<ticket-name>.md`. Write that report when the review step is about to be worked.

## 2. Review template

Copy these headings into the report. Leave each pointer line as written. A pointer line names its target file.

```
# <NN> — <ticket name>

Ticket: [[../issues/NN-<ticket-name>.md]]

## 1. Score

## 2. Miss
```

## 3. Fill

1. **Score.** Score the report against each story the ticket named.
2. **Miss.** Name what failed. A miss returns that ticket to build.

Done: the report scores the stories that ticket named. A miss names what failed, and that ticket returns to build.
