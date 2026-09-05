# Core creation Project reorganization

Date: 2026-09-05

## Moved

- [[plan/core-creation/reports/kernel-fsproj.md]] moved from the Roadmap reports.
- [[plan/core-creation/reports/solid-core-module-fit.md]] moved from the Roadmap reports.
- [[plan/core-creation/reports/current-edit-core-reconciliation.md]] moved from the Roadmap reports.
- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] moved from ESO issues and was renumbered from 07.

The old files were removed after the new files were written.

## Created

- [[plan/core-creation/project.md]] at Stage `charting`.
- [[plan/core-creation/issues/02-core-actor-pool.md]] for Core-owned pool machinery.
- [[plan/core-creation/reports/create-project-reorganization.md]] as this transactional report.

## Issue ownership

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] owns the sole Server Changes path.
- [[plan/core-creation/issues/02-core-actor-pool.md]] owns launch off the apply queue, Core job identity, cancellation of further output, and finish through Core Changes and inner apply.
- [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]] stays in ESO and owns the Parse definition.
- [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]] stays in ESO and owns advisory soft-lock semantics and Browser-facing job access. It does not own Core pool implementation.

Roadmap Chapters and Epics now sequence these Projects and issues without copying the Core specification. Actor-spine consumers now point to Core. [[plan/index.md]] contains 35 Project rows, including Core creation at Stage `charting`.

## Unresolved questions

- The exact Core pool API, packaging, and cancel-after-enqueue behavior are not specified.
- Advisory soft-lock issuance, expiry, and exact Browser chrome are not specified.
- The scope of file-mode view-only behavior for Files send is not specified.
- Durable progress or safe replay for the asynchronous individual-document file queue is not specified.

The planned db-authority order is fixed in [[plan/core-creation/reports/current-edit-core-reconciliation.md]]: tentative apply; PostgreSQL ChangeLog and projection commit; publish Graph; enqueue affected individual document files; acknowledge. **DbAgent.startSnapshot** is leftover naming and code, not this planned queue.
