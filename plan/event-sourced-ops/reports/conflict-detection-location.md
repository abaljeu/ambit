# Conflict detection location (issue 03)

**Primary (recoverable collision + amend):** [[../../../src/Shared/ChangeAmendment.fs]] — module `ChangeAmendment`.

Ticket [[../issues/03-server-amends-recoverable-field-collisions.md]] lives here: `isRecoverableCas` classifies CAS fail messages; `tryAmendSetText` / `tryAmendSetName` / `tryAmendSetClasses` rewrite losers (`amb-conflict` child or class set delta via `mergeClassesFromPrior`); `applyChange` is the public entry.

**CAS mismatch (stale prior value):** [[../../../src/Shared/GraphMutate.fs]] — `setText` / `setName` / `setClasses` return `"old … does not match"`. That is the raw compare-and-swap refuse; amendment only kicks in when `ChangeAmendment` sees those messages.

**Server wire (not detection):** [[../../../src/Server/FileAgent.fs]] and [[../../../src/Server/DbAgent.fs]] call `ChangeAmendment.applyChange` and set `externalChanges` when `amended` is true.

**Tests:** [[../../../tests/Shared.Tests/ChangeAmendmentTests.fs]] (unit amend path); [[../../../tests/Server.Tests/StateEndpointTests.fs]] (POST concurrent stale text/name/classes end-to-end).
