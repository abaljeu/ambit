# Project documentation

The canonical Gambol project documentation lives under [[doc/]]. Start with [[doc/index.md]] (Feature index of the current program). What to work on next is [[.scratch/roadmap/map.md]].

- [[doc/arch.md]], [[doc/spec.md]], and [[doc/api.md]] describe the system.
- [[doc/current/]] contains implemented feature baselines and takes precedence.
- [[doc/reference/]] contains operational and format reference material.
- [[doc/roadmap/]] is leftover planned-direction text until a `.scratch` Project cites it or it moves to history. New planned work lives in Projects, not here.
- [[doc/history/]] is historical.
- [[doc/unsorted/]] is temporary and non-authoritative.

Follow the authority and currency rules in [[doc/README.md]]. Surface contradictions instead of silently choosing between documents.

If [[CONTEXT.md]] exists, treat it as Gambol's concise domain glossary: use its preferred terms and avoid synonyms it rejects. Do not duplicate the detailed project documentation there.

Architecture Decision Records live under [[docs/Decisions/]]. Before changing an area, read any relevant records and explicitly surface proposed changes that contradict them.

When documenting what Gambol excludes or will not do, follow [[docs/agents/scope-vs-commitment.md]] — scope is effort-local; commitment requires an authorized record.
