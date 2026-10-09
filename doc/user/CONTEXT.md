# CONTEXT

## 1. Content

This folder explains how to operate Ambit. The lead document is [End-user wiki](README.md). No details, only overview and links.

[Concepts](guide/concepts.md) is the complete big picture. The Guide guides. [Reference](reference/README.md) expands those concepts with detail. Feature has its ordinary software meaning. Why to use Ambit is marketing. This work does not cover that.

The wiki uses concepts as the user sees them, and tasks in the App and in the Browser.

It describes how to operate the software now. It does not describe work. Work stays in plan/. It does not describe how the program is built and run. That stays in [doc/current/](../current/).

These documents should not be complicated, and they should not lie about complexity. When complexity shows up, look for a way to simplify the product.

## 2. Goals

Explanation is secondary.

One subject is one page, and the home links that page. When the App and the Browser do the same task, that task is one description.

Page writing rules live here.

Explanation is why. Explanation is not the lead. Explanation stays on the page until it grows enough to deserve its own page.

Do not link universal documents from everywhere. Universal documents are the home, this file, and [Architecture](../current/architecture.md).

A user page may link to an architecture page. A user page must stand alone. The reader finishes the task without the architecture page.

Organize ontologically. Use the names the user sees.

Refer by name, never by number.

Never state a thing twice within this wiki. Do not multiply links.

Write a list as lines. A line is `Name: fact`. A task uses numbered steps. Do not write a sentence where a line will do. No blank lines between those lines.

Links stand alone. Say only how the neighbor fits. Do not restate the neighbor.

Do not link to plan/. Plans link to here.

Omit a negative statement unless it is a non-obvious rule of use.

Numbering is not needed everywhere.

## 3. Page categories

A subject page has one category.

Omit a topic that has nothing to say.

1. **Concept.** The page abstracts the product.
2. **Guide.** The page guides a task.
3. **Reference.** The page defines a feature.

## 4. Subject page

The spine of a subject page is this order.

1. **Title.** The title names the subject.
2. **Category.** The category is one of the four page categories.
3. **See Also.** See Also lists neighbor subjects only.
4. **Sentence.** One sentence names the subject.
5. **Topics.** Use the topics for that category.
6. **Explanation.** Explanation is last. Explanation is on the page only when the page has a why.

There is no Sources section. There is no Where section. The page does not name source files.

## 5. Kinds

These kinds are not page categories and are not sections on the home.

1. **Context.** Context is this file.
2. **Concepts.** Concepts is [Concepts](guide/concepts.md).
3. **Guide.** The Guide is [Guide](guide/README.md).
4. **Reference.** Reference is [Reference](reference/README.md).
5. **Architecture page.** An architecture page is a page under [doc/current/](../current/). A link to one is optional. Dependence on one is forbidden.

## 6. Status claims

This wiki has no status marks.

A page says how to operate the software now. A page does not say what the program shall be. A page does not say what work remains. Do not use `[x]`, `[ ]`, `[/]`, or `[o]` on these pages.

## 7. Out of scope

1. **How it is built and run.** [Architecture](../current/architecture.md). This subject has no link. Dependence on an architecture page is forbidden.
2. **Work.** plan/. This wiki does not describe the work of changing the program.
3. **Agent glossary.** [GLOSSARY.md](../../GLOSSARY.md) is architecture-level. This wiki uses the user's words.
4. **Committed Decision.** [Decisions](../Decisions/). Do not copy a decision. When the user needs the rule, write the user-visible rule on the page.

## 8. Pages outside this folder

A subject is in scope when [Concepts](guide/concepts.md) or [Reference](reference/README.md) names it. Link the doc page for that subject. An out-of-scope subject has no link. Rewrite that page into Reference when the detail is needed. The user page still stands alone.
