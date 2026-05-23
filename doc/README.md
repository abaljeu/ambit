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
- Preferred fields: `Category`, `Status`, `Authority`, `See also`.
- Do not add YAML frontmatter by default; use plain markdown lines unless there is a specific reason to formalize metadata.

Start here:

- [[doc/arch.md]]
- [[doc/spec.md]]
- [[doc/api.md]]
- [[doc/roadmap/overview.md]]
