# GRAPHIFY_WORKFLOW.md

## Purpose

This document defines the mandatory Graphify workflow for architectural, cross-module, domain, EF Core, API, configuration, and migration-related changes in the KH2 Management System backend.

Graphify is not an optional final verification step. It must be used before, during, and after significant changes to understand dependencies and reduce the risk of breaking hidden consumers.

---

## Mandatory Rule

Graphify MUST be used when modifying, renaming, removing, or replacing any of the following:

- Domain entities
- Entity properties
- Domain methods
- Application services
- Infrastructure services
- Controllers/endpoints
- DTOs
- EF Core relationships/configurations
- Configuration classes/options
- Database-related abstractions
- Shared interfaces
- Legacy compatibility paths
- Cross-module workflows

Do not rely only on text search when Graphify dependency information is available.

If Graphify is unavailable, stale, inconsistent, or cannot analyze a critical dependency, STOP broad architectural changes and report the problem.

---

## Standard Graphify-First Loop

For every significant logical change, use this sequence:

```text
Graphify refresh/check
        ↓
inspect target node
        ↓
inspect inbound dependencies
        ↓
inspect outbound dependencies
        ↓
classify affected consumers
        ↓
manually inspect relevant source
        ↓
plan smallest safe change
        ↓
implement
        ↓
run targeted tests/build
        ↓
refresh Graphify
        ↓
verify dependency changes
        ↓
continue
```

Do not batch unrelated architectural changes into one implementation step.

---

## Before Making Changes

Before editing a domain entity, property, service, endpoint, EF relationship, or configuration:

1. Refresh or verify the Graphify project graph.
2. Confirm the graph represents the current branch and latest source.
3. Locate the target node.
4. Inspect inbound dependencies.
5. Inspect outbound dependencies.
6. Locate all consumers of the property/class/method being changed.
7. Classify each consumer as one of:
   - DOMAIN
   - APPLICATION
   - INFRASTRUCTURE
   - API
   - LEGACY
   - COMPATIBILITY
   - TEST
   - MIGRATION_HISTORY
8. Inspect the important source files manually.
9. Define the smallest safe change before editing code.

Do not infer that something is unused only because a text search finds few references.

---

## During Implementation

After each logical group of changes:

1. Run relevant targeted tests.
2. Run `dotnet build`.
3. Refresh Graphify.
4. Re-inspect the modified nodes.
5. Verify expected dependency removals/additions.
6. Record stale consumers that belong to a later phase.
7. Do not continue if unexpected unrelated dependencies appear.

A logical group should normally be limited to one entity, one service boundary, or one tightly related architectural concern.

---

## After Implementation

Before declaring a phase complete:

1. Refresh Graphify one final time.
2. Confirm the graph is current.
3. Re-run dependency analysis for renamed/removed properties.
4. Confirm no unexpected active consumer remains.
5. Distinguish historical references from operational references.
6. Record unresolved Application, Infrastructure, API, Legacy, or Test consumers.
7. Include the Graphify findings in the phase report.

A property is not considered fully removed just because it disappeared from the Domain model if active downstream consumers still depend on the same concept.

---

## Required Graphify Usage for KH2 Face Recognition Work

Graphify must be used before and after changes involving:

- `FaceProfile`
- `FaceEnrollment`
- `FaceEmbedding`
- `AttendanceDevice`
- `FaceRecognitionEvent`
- `FaceAttendanceSession`
- `ProviderFaceProfile`
- `LegacyFaceEnrollment`
- `LegacyFaceEnrollmentCapture`
- `LegacyFaceRecognitionEvent`
- `FaceEnrollmentCapture`
- `Presensi`
- `Sesi`

Graphify must also be used to trace all consumers of:

- `CurrentEnrollmentId`
- `ReferenceImagePath`
- `AcceptedSampleCount`
- `CaptureCount`
- `IsActive`
- `FaceProfileId` compatibility aliases
- `Recognized`
- `FailureReason`
- `FaceAttendanceSessionId`
- `FaceRecognitionOptions`
- `FaceRecognitionServiceOptions`

---

## Legacy Dependency Mapping

Before removing or replacing a legacy entity, build a dependency map containing:

```text
Legacy Entity
    ↓
Writers
    ↓
Readers
    ↓
Controllers / Endpoints
    ↓
Application Services
    ↓
Infrastructure Services
    ↓
EF Configuration
    ↓
Tests
    ↓
Migration Origin
    ↓
Canonical Replacement
```

Do not remove a legacy table/entity until its write paths and read paths are understood.

If compatibility can be preserved through an adapter over canonical services, prefer the adapter instead of preserving duplicate database state.

---

## EF Core and Migration Changes

Graphify does not replace EF Core migration tooling, but must be used to understand impact before changing schema-related code.

Before changing:

- Entity relationships
- Foreign keys
- Navigation properties
- DbSets
- EF configurations
- Database options
- Migration-related application logic

use Graphify to identify dependent code first.

Do not delete or regenerate migrations merely because the Domain model changed.

Migration cleanup must happen only in an explicitly authorized migration phase.

---

## Configuration Changes

Before modifying configuration options such as:

- `MigrateOnStartup`
- `SeedOnStartup`
- `ConnectionStrings`
- `FaceRecognition`
- `LegacyFaceProvider`
- JWT settings
- device authentication settings

use Graphify to locate:

- configuration binding
- startup consumers
- service registration
- `IOptions<T>` consumers
- HttpClient setup
- background services
- deployment-specific behavior

Never commit secrets merely to make configuration dependencies easier to inspect.

---

## Database Safety

Graphify analysis does not authorize database writes.

Unless a task explicitly authorizes database mutation, do NOT:

- run `dotnet ef database update`
- apply migration SQL
- create/drop/alter tables
- install extensions
- seed staging/production
- change RLS
- change roles
- modify Supabase schema through MCP

For Supabase inspection phases, MCP must remain read-only.

---

## Stop Conditions

STOP and report instead of continuing if:

- Graphify cannot run.
- Graphify cannot refresh.
- The graph appears stale.
- A critical target node cannot be resolved.
- Dependency results conflict materially with the source code.
- A supposedly isolated change affects unrelated modules unexpectedly.
- A legacy entity appears to be required by an undocumented workflow.
- Removing a property requires broad architectural changes outside the active phase.
- Migration history would need to be rewritten outside a migration phase.
- A tracked secret is discovered.
- Database mutation would be required but has not been authorized.

Do not improvise around these conditions.

---

## Reporting Requirements

Every architecture-related phase report should include:

### Graphify Status

- Graphify baseline status
- Graph refreshes performed
- Target nodes inspected
- Dependency inventory before changes
- Dependency inventory after changes
- Remaining stale consumers

### Change Impact

- Domain consumers affected
- Application consumers affected
- Infrastructure consumers affected
- API consumers affected
- Legacy consumers affected
- Tests affected
- Migration-history-only references

### Validation

- Targeted test result
- `dotnet build` result
- Full test result when required
- Pending model change status when relevant

### Safety

- Database writes performed
- Migration operations performed
- Supabase writes performed
- Secrets discovered
- Blockers

---

## Recommended Agent Behavior

For multi-phase work, the agent should follow this pattern:

```text
Phase start
    ↓
Graphify baseline
    ↓
dependency inventory
    ↓
one logical change
    ↓
build/test
    ↓
Graphify refresh
    ↓
dependency verification
    ↓
next logical change
    ↓
final Graphify audit
    ↓
phase report
```

Do not perform a large cross-cutting refactor first and inspect dependencies afterward.

---

## Principle

The purpose of Graphify is not only to visualize the repository.

It is a safety gate for architecture changes.

The expected behavior is:

> understand dependencies first, change the smallest necessary surface, verify dependencies again, then continue.

When Graphify and source inspection disagree, investigate the disagreement before proceeding.
