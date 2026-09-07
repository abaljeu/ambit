---
name: scratch-script
description: Scratch a short .sh with Write, then run that file. Use when asked for a tmp script, Write-then-run, or no-semicolon bash, or when several shell commands would go in one Shell call.
---

# Scratch script

Shell and paths: [[.cursor/rules/environment.mdc]].

A **scratch** is the same commands you would have run, written as a file, then run as that file.
The purpose is to make the commands readable and editable by a human, not to make it reusable or general.

## 1. Name the commands

Write the exact commands you were about to put in a Shell `command:`. One command per line. Same commands, same count.

Done: the body is that list. A shebang may sit on line 1.

## 2. Write the scratch

Write the body to a `.sh` under `tmp/` with the Write tool. Newlines separate commands.

Done: the file exists on disk.

## 3. Run the scratch

One Shell call: `./tmp/<name>.sh`. No need to specify bash; that's automatic.  That path is the whole `command:`.

Done: the run used the file.

## 4. Remove the scratch

Delete the file after a successful run. Keep if the exact command is likely to be reused.

Done: the scratch is gone, or you said why it stayed.
