# CONTEXT

## 1. Content

This folder describes architecture.  The lead document is [[architecture.md]].

The description is in  hierarchies of claims.

It describes both what IS currently, and what SHALL BE soon.  

It must never describe the work of changing the program. Describing work stays in plan/.

## 2. Goals

Reference is primary. Explanation is secondary.

One subject is one page, and this home links that page. Code and runtime are one description.

Page writing rules live here.

Explanation is why. Explanation is not the lead. Explanation stays on the page until it grows enough to deserve its own page.

Dont' link universal documents like architecture.md, glossary.md, context.md from everywhere.

Organize ontologically.

Refer by name, never by number.

Never state a thing twice within this wiki.

Write lists as checkbox sequences, not sentences or paragraphs.  Don't write sentences like "The X is Y".  Simply make a line 
```
[ ] X: Y
[o] X: Z
[x] More complicated claims are sentences or paragraphs.
```
Links stand alone.  Only detail how they fit, not their internals.

Do not link to plan/.  Instead plans link to here.  Architecture should encompass the structural elements of the linking plan.

Omit negative statements unless it's a non-obvious assertion of protocol.

Numbering is not needed everywhere.

## 3. Page categories

A subject page has one category.

Omit a topic that has no claim.

1. **Building block.** The topics are Role, Interface, Parts, and Depends on.
2. **Capability.** The topics are Job, then one topic per rule-group.
3. **Information.** The topics are Shape, Invariants, and Store.
4. **Contract.** The topics are Parties and Operations.

## 4. Subject page

The spine of a subject page is this order.

1. **Title.** The title names the subject.
2. **Category.** The category is one of the four page categories.
3. **See Also.** See Also lists neighbor subjects only.
4. **Sentence.** One sentence names the subject.
5. **Topics.** The topics for that category follow.
6. **Explanation.** Explanation is last. Explanation is on the page only when the page has a why.

The file path lives in the claim.

There is no Where section.

## 5. Kinds

These five kinds are not page categories and are not sections on the home.

1. **Context.** Context is this file.
2. **Runtime.** Runtime is a topic on a capability page.
3. **Allocation.** Allocation is [[doc/reference/]].
4. **Constraint.** Constraint is the Invariants topic on an information page.
5. **Decision.** Decision is a Committed Decision in [[doc/Decisions/]].

## 6. Status Claims

All claims about architecture get a checkbox.  The checkbox is filled according to these rules.

Is and Shall Be

Status is a mark on a claim, never a section heading.

A claim that replaces another sits on the next line under the same topic. Achievement is checking the successor and deleting the dead claim.

1. **Is** — `[x]` The program is this.
2. **Shall be** — `[ ]` The program shall be this.  But write in present tense.  The lack of x shows it's pending.
3. **Started** — `[/]`
4. **Obsolete yet implemented** — `[o]` The program still is this. This claim will be deleted when the program no longer matches the description.

## 7. Out of Scope

1. **How to use** — Being planned by [End-user wiki](plan/end-user-wiki/map.md). This wiki does not cover how to use the software.
2. **Committed Decision** — [[doc/Decisions/]]. This wiki links a Committed Decision. It does not copy the decision.  If a decision is merely a description of architecture, drop from there, and keep this the authority.

## 8. Additional architecture documents

These still hold facts that may become included here and dropped there

**Old Roadmap** - [[doc/roadmap]] These were planning documents, some of which have been implemented.
**plan/<project>/arch.md** - defines what should be a focused subset of this folder.

