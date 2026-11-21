#!/bin/bash
# Merge dev into staging, push, then return to dev.
set -euo pipefail

git switch dev
scripts/mergeto.sh staging
git push
git switch dev
