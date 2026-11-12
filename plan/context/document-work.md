# Document work

## 1. Work

Update `doc/current` and `doc/user` for each ticket whose stories were met. Apply each Found elsewhere **Fix** the review accepted and the user confirmed. Write `<nn>-doc.md` in the ticket folder `plan/<slug>/issues/<nn>-<name>/` when the document step is about to be worked. That file links the pages updated. A pointer path starts at this file.

## 2. Document template

Copy these headings into the link file. Leave each pointer line as written. A pointer line names its target file.

```
# <NN> — <ticket name>

## 1. Current

[[../../../../doc/current/<page>.md]]

## 2. User

[[../../../../doc/user/<page>.md]]

## 3. Found elsewhere

[[<file>]]
```

## 3. Fill

1. **Pages.** Update the `doc/current` page and the `doc/user` page so they match the tickets whose stories were met.
2. **Current.** Point at each `doc/current` page this ticket updated.
3. **User.** Point at each `doc/user` page this ticket updated.
4. **Found elsewhere.** Write each accepted **Fix** into the file the **Problem** names. Point at each file changed.

Done: `doc/current` and `doc/user` match the tickets whose stories were met, each accepted Found elsewhere Fix is written, and `<nn>-doc.md` links the pages updated.
