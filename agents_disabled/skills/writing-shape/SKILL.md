---
name: writing-shape
description: Writing, exploit — shape raw material into an article, paragraph by paragraph.
disable-model-invocation: true
---

<what-to-do>

The user has passed (or will pass) a markdown file of raw material. Treat it as the input pile — anything from a tidy list of fragments to a wall of unstructured prose to a transcript. The format does not matter. Read it end-to-end before doing anything else.

Then run a shaping session that produces a separate article document. This is **exploit**: the exploring is done, the pile is fixed — commit to a structure and mine the pile to fill it. The pile is read-only; handle gaps in [Pulling from the pile](#pulling-from-the-pile).

If the user did not say where to save the article, ask once and remember the path.

1. **Read the pile.** Read the input file in full. Form a sense of what's in it.
   Done: the input file has been read end-to-end.
2. **Establish the prerequisites.** Settle with the user what the reader knows walking in — the concepts that are **grounded** from the start. Everything else must be grounded by a block before a later block can lean on it. Grounding: [[.agents/skills/writing-grounding.md]].
   Done: the user has agreed the prerequisite list; the running grounded set starts as that list.
3. **Draft 2–3 candidate openings.** Each opening should imply a different thesis or angle for the article. Show all of them. Force the user to pick or compose a hybrid. The chosen opening defines what the rest of the article must do.
   Done: 2–3 openings are shown; the user has picked one or composed a hybrid.
4. **Grow paragraph by paragraph.** After the opening lands, ask "given this opening, what does the reader need to hear next?" Pull material from the pile to answer. The next block may only lean on grounded concepts, and grounds new ones as it lands. An ungrounded concept the next move needs is itself the answer: ground it first. Argue about the form the next block takes — a paragraph, a list, a table, a callout, a quote, a code block. Choose deliberately how to format according to: [[FORMAT-ARGUMENTS.md]]. Prune the argument for clarity: [[CONVERSATIONAL-FEEL.md]].
   Done: the next block is agreed, reachable from the grounded set, and its form is chosen.
5. **Append to the article file as you go.** Don't batch. Write each agreed paragraph or block immediately so the user can see the article taking shape.
   Done: the agreed block is on disk in the article file.
6. **Loop steps 4–5** until the user confirms the article is complete.
   Done: the user has confirmed the article complete, and the article file matches the agreed blocks in order.

</what-to-do>

<supporting-info>

## Pulling from the pile

Treat the raw material as a quarry, not a script. Pull a fragment, rework it to fit the surrounding paragraph, and place it. A fragment may be split across multiple paragraphs, merged with another, or paraphrased. The pile's job is to be mined; the article's job is to read as one voice.

If the pile lacks something the article needs, name the gap explicitly: "We need an example here and the pile doesn't have one — give me one now or we cut this section."

## Writing rhythm

Append to the article file as each block is agreed. Re-read the file from disk before every write — the user may have edited between turns. Never overwrite blindly. If the user wants a paragraph rewritten, edit that specific paragraph in place; leave the rest alone.

</supporting-info>
