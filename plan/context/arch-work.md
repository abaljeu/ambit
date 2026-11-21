# Architecture work

## 1. Work

The work file is `plan/<slug>/arch.md`. Write that file when the arch step is about to be worked.

The arch records who owns each behavior, where its types and its interface live, and which owners it uses.

A signature has the form `name : arg types -> result`.

An interface holds that signature and the prose rules a caller must know.

A flow path is pseudocode that names module-map entries in call order.  It is NOT full function definition.

Each fact has one home.

- Caller rules live in Interfaces.
- Type bodies live in Types.
- The owner, the files, the function, and Uses live on the module map.
- A seam names the two owners and the tests that cross that boundary.

## 2. Arch template

Copy these headings into the work file. Leave each pointer line as written. A pointer line names its target file.

```
# <name> architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

## 2. Types

## 3. Interfaces

## 4. Flow paths

## 5. Module map

## 6. Seams

## 7. Slicing strategy

## 8. Open questions
```

## 3. Fill

1. **Bounds.** Write one line for what is in scope. Write one line for what is out of scope.
2. **Types.** Put each record or union this change adds or changes in one fenced `fsharp` block. Mark each block.
3. **Interfaces.** Group the rules by the module-map entry name. Mark each rule. A holding the caller must know is a prose rule. Name a callee outside the module map in a prose rule when the caller must know that callee. In each group write
   - the signature
   - the prose rules a caller must know
4. **Flow paths.** Write one numbered path for each path. The path names module-map entries in call order.
5. **Module map.** One entry owns one behavior. The entry name is that behavior and the name of its interface group. Mark each Uses line. Record
   - the interface file and function, when a home exists
   - each type that owner defines, with that type's file
   - Uses, naming other owners
6. **Seams.** Write a seam for a boundary between two owners where tests cross. Name the two owners and the tests. Mark each seam line.
7. **Slicing strategy.** Name the direction of the cuts. [[slicing-work.md]] cuts the slices and orders them. Use one of these:
   - `tracer-cut`
   - `module-build`
   - `expand-contract`
8. **Open questions.** Name each undecided question in plain text.

Done: `plan/<slug>/arch.md` holds

- the bounds
- the types
- the interfaces
- the flow paths
- the module map
- the seams
- the slicing strategy
- the open questions
