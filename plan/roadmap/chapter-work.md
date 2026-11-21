# Chapter work

## Input

- [[../context/project-work.md]] when the Chapter names a Project
- The predecessor Chapter, when **Blocked by** names one

## Process

1. Read [[../context/project-work.md]] when the Chapter names a Project.
2. Read the predecessor Chapter when **Blocked by** names one.
3. Write the aspect of use.
4. Set **Blocked by** to that predecessor, or to None.
5. Point at each Project that serves this aspect.
6. Point at a Project issue when the Chapter needs one.
7. Add this Chapter to the Chapter list of each Epic that uses it.

## Output

The work file is `epics/chapters/<slug>.md`.

Copy [[epics/chapter-template.md]].

Every Chapter is listed on at least one Epic.

Every Project is listed on at least one Chapter.

Done: the Chapter states the aspect of use, names its predecessor or None, and points at the Projects that serve it.
