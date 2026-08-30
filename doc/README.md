# Documentation

Top level contains the front-door docs for the current system as a whole:
[[index.md]], [[arch.md]], [[spec.md]], and [[api.md]].

New docs should normally go in a subfolder:

- `current/` — current subsystem or feature docs
- `roadmap/` — leftover planned-direction files until a `.scratch` Project cites them or they move to history
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

- [[arch.md]]
- [[spec.md]]
- [[api.md]]
- [[index.md]] — Feature index of the current program
- [[roadmap/postgres-roadmap.md]] — persistence-focused roadmap index

Current feature baselines (`current/`):

- [[current/sync-mvp.md]] — multi-client sync semantics
- [[current/persistence-model.md]] — PostgreSQL schema, correlated on-disk artifacts, auto-persist from DB
- [[current/workspace-graph.md]] — workspace special nodes and graph invariants
- [[current/workspace-local-mapping.md]] — desktop workspace label → local root config
- [[current/desktop-local-files.md]] — desktop proxy and `/_desktop/*` API
- [[current/workspace-stage-plan.md]] — implemented workspace stages through Stage 8

Reference (`reference/`):

- [[reference/postgres-environments.md]] — dev/prod PostgreSQL setup
- [[reference/deploy-azure.md]] — Azure App Service deploy
- [[reference/cpanel-transparent-proxy.md]] — custom domain forwarding via cPanel and [[proxy.php]]
