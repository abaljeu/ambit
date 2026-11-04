# Arch work

## 1. Work

The work file is `plan/<slug>/arch.md`. Write that file when the arch step is about to be worked.

Arch is pseudocode. It defines the types and the function interfaces.

A signature has the form `name : arg types -> result`.

Arch is not a plain list, not prose, and not a function body.

The arch always has bounds, types, interfaces, flow paths, a module map, a slicing strategy, and open questions.

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

## 6. Slicing strategy

## 7. Open questions
```

## 3. Fill

1. **Bounds.** Write one line for what is in scope. Write one line for what is out of scope.
2. **Types.** Put each record or union this change adds or changes in one fenced `fsharp` block. Mark each block `[x]`, `[ ]`, or `[o]`.
3. **Interfaces.** Write signatures only, grouped by module. Use the same marks. Write no function body.
4. **Flow paths.** Write one numbered path for each path. The path is short pseudocode that names those signatures in call order. Write no body.
5. **Module map.** Name each module and the file that holds it.
6. **Slicing strategy.** Name the direction of the cuts. [[slicing-work.md]] cuts the slices and orders them. Use one of these:
   - `tracer-cut`
   - `module-build`
   - `expand-contract`
7. **Open questions.** Name each undecided question. Give it no mark that decides it.

Done: `plan/<slug>/arch.md` holds the bounds, the types, the interfaces, the flow paths, the module map, the slicing strategy, and the open questions. The Spec pointer names `spec.md` for a user-facing Project. An internal Project may have no `spec.md`.
