# Mocking at system boundaries

Companion for [[SKILL.md]]. Mock only what you do not control: external APIs, time/randomness, and (when a real fixture is worse) databases or the file system. Prefer a real test DB or fixture file when cheap.

Pass boundary dependencies in (inject) rather than constructing them inside the unit under test. Prefer a small SDK-shaped surface (`getUser`, `createOrder`) over one generic `fetch` that forces conditional mocks.
