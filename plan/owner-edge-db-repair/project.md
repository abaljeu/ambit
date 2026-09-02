# Owner-edge database repair

Stage: active
Summary: Persisted Owned Children can fail to be a tree. After Server restart, every surviving non-ROOT Node has exactly one Owned parent that reaches ROOT, unreachable Nodes are deleted, and a reachable Node with no Owned parent has a Ref promoted to Owned, durable with no History Change.
Updated: 2026-09-02
