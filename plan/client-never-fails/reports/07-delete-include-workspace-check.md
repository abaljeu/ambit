pass

Mapped `scripts/agent-test.sh` passed: Server `ApiPostLoadTests` 6, Shared `LoadCaptureTests` `SerializationTests` `SyncPlannerTests` 89.

Named filters passed: Server 6, Shared 8.

`bash scripts/client.sh build` finished.

Full `scripts/agent-test.sh --full` passed: Server 742 passed, 1 skipped; Shared 1933 passed, 3 skipped.

Renamed tests: ``postLoad Ev-only when includeWorkspace false`` is ``postLoad returns Events and the target Header``. ``postLoad Want answer when includeWorkspace true`` is ``postLoad Want answer for the target``. ``wantAnswerForTargets without residency intent returns empty`` is ``wantAnswerForTargets returns a leaf target Header``.

The ticket names ``wantAnswerForTargets returns the target Header and its child list``.

`LoadRefuse.MultiWorkspace` stays. The Output `.fs` files have no `includeWorkspace`.
