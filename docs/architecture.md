# Architecture

This repository uses a layered ASP.NET backend structure that stays maintainable as the codebase grows.

## Layers

- `src/KH2.ManagementSystem.Api`
  - HTTP entry point.
  - Controllers, request/response contracts, app configuration, API-only concerns.
- `src/KH2.ManagementSystem.Application`
  - Use cases and orchestration.
  - Interfaces that infrastructure implements.
  - Feature folders so business workflows stay grouped.
- `src/KH2.ManagementSystem.Domain`
  - Core entities and domain rules.
  - No dependencies on infrastructure or API.
- `src/KH2.ManagementSystem.Infrastructure`
  - Technical implementations such as time, persistence, messaging, storage, and external services.
- `src/KH2.ManagementSystem.Shared`
  - Small cross-cutting primitives that are safe to reuse across layers.
- `tests`
  - Reserved for unit, integration, and architecture tests.

## Dependency Direction

Only inward dependencies are allowed:

- `Api -> Application`
- `Api -> Infrastructure`
- `Application -> Domain`
- `Application -> Shared`
- `Infrastructure -> Application`
- `Infrastructure -> Domain`
- `Infrastructure -> Shared`

`Domain` should remain the most stable project.

## Feature Organization

Application features are grouped by business capability instead of technical type:

- `Features/System/GetSystemOverview`
- future example: `Features/Students/CreateStudent`
- future example: `Features/Attendance/GetAttendanceSummary`

Inside a feature folder, keep request, handler, validator, DTO, and mapping close together.
