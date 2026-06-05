# Documentation

Top level contains the front-door docs for the current system as a whole:
[[doc/arch.md]], [[doc/spec.md]], and [[doc/api.md]].

New docs should normally go in a subfolder:

- `current/` — current subsystem or feature docs
- `roadmap/` — committed direction and rollout tracking
- `history/` — assessed historical project materials
- `reference/` — operational and reference material
- `unsorted/` — unassessed docs; temporary and non-authoritative

Authority rule: when a roadmap, history, or unsorted doc disagrees with a current doc, the current doc wins.

Document header rule:

- Use a short lightweight header when a doc needs status metadata.
- Preferred fields: 
  - `Category` - NOT the doc's directory; some category of the program.
  - `See Also`
- Do not add YAML frontmatter by default; use plain markdown lines unless there is a specific reason to formalize metadata.

Start here:

- [[doc/arch.md]]
- [[doc/spec.md]]
- [[doc/api.md]]
- [[doc/roadmap/postgres-roadmap.md]] — roadmap index

Current feature baselines (`current/`):

- [[doc/current/sync-mvp.md]] — multi-client sync semantics
- [[doc/current/persistence-model.md]] — PostgreSQL schema and `file` / `db` modes
- [[doc/current/workspace-graph.md]] — workspace special nodes and graph invariants
- [[doc/current/workspace-local-mapping.md]] — desktop workspace label → local root config
- [[doc/current/desktop-local-files.md]] — desktop proxy and `/_desktop/*` API

Reference (`reference/`):

- [[doc/reference/postgres-environments.md]] — dev/prod PostgreSQL setup
- [[doc/reference/deploy-azure.md]] — Azure App Service deploy
