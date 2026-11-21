---
name: setup-pre-commit
description: Set up Husky pre-commit hooks with lint-staged (Prettier), type checking, and tests in the current repo. Use when user wants to add pre-commit hooks, set up Husky, configure lint-staged, or add commit-time formatting/typechecking/testing.
---

# Setup Pre-Commit Hooks

## What This Sets Up

- **Husky** pre-commit hook
- **lint-staged** running Prettier on all staged files
- **Prettier** config (if missing)
- **typecheck** and **test** scripts in the pre-commit hook

## Steps

### 1. Detect package manager

Check for `package-lock.json` (npm), `pnpm-lock.yaml` (pnpm), `yarn.lock` (yarn), `bun.lockb` (bun). Use whichever is present. Default to npm if unclear.
Done: the package manager for this repo is named.

### 2. Install dependencies

Install as devDependencies: `husky lint-staged prettier`.
Done: those three packages are listed under devDependencies.

### 3. Initialize Husky

```bash
npx husky init
```

This creates `.husky/` and adds `prepare: "husky"` to package.json.
Done: `.husky/` exists and `prepare` is `"husky"`.

### 4. Create `.husky/pre-commit`

Husky v9+ hook files need no shebang. Write:

```
npx lint-staged
npm run typecheck
npm run test
```

Replace `npm` with the detected package manager. If package.json has no `typecheck` or `test` script, omit those lines and tell the user. Run lint-staged first (staged-only), then typecheck and tests.
Done: `.husky/pre-commit` exists with lint-staged and only the scripts the repo actually defines.

### 5. Create `.lintstagedrc`

```json
{
  "*": "prettier --ignore-unknown --write"
}
```

`--ignore-unknown` skips files Prettier cannot parse.
Done: `.lintstagedrc` matches the block above.

### 6. Create `.prettierrc` (if missing)

Only create if no Prettier config exists. Defaults:

```json
{
  "useTabs": false,
  "tabWidth": 2,
  "printWidth": 80,
  "singleQuote": false,
  "trailingComma": "es5",
  "semi": true,
  "arrowParens": "always"
}
```

Done: a Prettier config exists (new or pre-existing).

### 7. Verify

- [ ] `.husky/pre-commit` exists and is executable
- [ ] `.lintstagedrc` exists
- [ ] `prepare` script in package.json is `"husky"`
- [ ] `prettier` config exists
- [ ] Run `npx lint-staged` to verify it works

Done: every checkbox above is satisfied.

### 8. Commit

Stage all changed/created files and commit with message: `Add pre-commit hooks (husky + lint-staged + prettier)`.
Done: the commit landed (and exercised the new hooks).
