---
name: git-protocol
description: "How agents should use git"
---

# Git protocol

Rule 1: Do no work on staging or ready or master, unless explicitly told to use that branch.

Run `scripts/gitstatus.sh` to determine where things are.
Do not run `bash scripts/gitstatus.sh`.  Just the script.


## Cloud
Cloud agents get their files from staging.
Branch to a new branch.
Do the work.
PR back into staging.

## Desktop
Desktop agents get files from dev.
Work in dev.
Do not change branches unless asked.
Do not commit unless asked.
Do not merge unless asked.
Do no work if the active tree is not dev, unless explicitly told.

User will commit.  User will merge.  User will push/pull.

