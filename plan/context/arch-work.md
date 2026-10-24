# Arch work

## 1. Work

The work file is `plan/<slug>/arch.md`. Write that file when the arch step is about to be worked.

## 2. Arch template

Copy these headings into the work file. Leave each pointer line as written. A pointer line names its target file.

```
# <name> architecture

Spec: [[spec.md]]

## 1. Story paths

## 2. Module map

## 3. Slicing strategy
```

## 3. Fill

1. **Story paths.** Write one numbered path for each named user story.
2. **Module map.** Name each module and the file that holds it.
3. **Slicing strategy.** Name the direction of the cuts. The ticket step cuts the slices and orders them. Use one of these:
   - `tracer-cut`
   - `module-build`
   - `expand-contract`

Done: `plan/<slug>/arch.md` holds the story paths, the module map, and the slicing strategy, and the Spec pointer names `spec.md`.
