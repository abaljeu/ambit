# Scripts

## Context

We have commit.sh, merge.sh, push.sh.  They do the right thing, but I want better UX

## Plan

commit.sh "desc" commits the current dev branch with the message.
commit.sh <no arg> runs git status.
gitready.sh "desc" does what merge.sh ready -m "desc" did.
gitmaster.sh "desc" does what merge.sh master -m "desc" did.
gitdev.sh "dest" does whate merge.sh forward dev did.  i.e. forward-merge the squash from master to dev.
gitpush.sh does what push.sh did.  i.e. Switch to master or ready and push it.
