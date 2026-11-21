#!/bin/bash
# Pull staging, then merge that branch into dev.
set -euo pipefail

git switch staging
git pull
scripts/mergeto.sh dev
