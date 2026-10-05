# Documentation

Top level contains the front-door docs for the current system as a whole:
[[index.md]], [[current/architecture.md]], [[spec.md]], and [[current/api.md]].

New docs should normally go in a subfolder:

- `current/` — current subsystem or feature docs. [[current/architecture.md]] is the architecture home
- `roadmap/` — leftover planned-direction files until a `plan` Project cites them or they move to history
- `history/` — assessed historical project materials
- `reference/` — operational and reference material
- `unsorted/` — unassessed docs; temporary and non-authoritative

Authority rule: when a roadmap, history, or unsorted doc disagrees with a current doc, the current doc wins. `doc/` holds what is coded (achieved, current behavior); what will be coded lives under `plan/`. When plan work is achieved, update `doc/`.

Document header rule:

- Use a short lightweight header when a doc needs status metadata.
- Preferred fields: 
  - `Category` - NOT the doc's directory; some category of the program.
  - `See Also`
- Do not add YAML frontmatter by default; use plain markdown lines unless there is a specific reason to formalize metadata.

Start here:

- [[current/architecture.md]]
- [[spec.md]]
- [[current/api.md]]
- [[index.md]] — Feature index of the current program
- [[roadmap/postgres-roadmap.md]] — persistence-focused roadmap index


Reference (`reference/`):

- [[reference/postgres-environments.md]] — dev/prod PostgreSQL setup
- [[reference/deploy-azure.md]] — Azure App Service deploy
- [[reference/secrets.md]] — secret names, environments, and stores
- [[reference/cpanel-transparent-proxy.md]] — custom domain forwarding via cPanel and [[proxy.php]]
