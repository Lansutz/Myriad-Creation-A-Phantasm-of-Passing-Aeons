# Simulation Architecture 2.0

This migration establishes one project-wide contract for simulation, built-in content, editors and external mods.

## Invariants

1. Domain systems do not call other domain systems for cross-domain side effects.
2. Cross-domain communication uses only **Event**, **Query** or **Command**.
3. Definitions are separate from runtime state.
4. Plan is a high-level intent/lifecycle object; activities are its current behavior, not child plans.
5. Runtime systems do not care whether content came from Base, Editor, Scenario or Mod.
6. Unity is the host/presentation/runtime adapter, not the simulation architecture.
7. Internal data structures are private implementation details behind stable contracts.
8. Expensive simulation follows: read snapshot -> calculate -> result buffer -> commit -> events.
9. Dirty propagation and simulation frequency are first-class performance mechanisms.
10. Content authoring and world-state editing are distinct operations.

## Runtime flow

Definition -> State -> Perception -> Decision -> Plan -> Activity -> Process -> Command -> State Change -> Event -> Derived State

## Extension flow

Base / Editor / Scenario / Mod -> Content Provider -> Validation -> Resolver -> Runtime Definition -> Registry

## Domain contract

A domain exposes stable:
- Queries
- Commands
- Events
- Definitions

It does not expose dictionaries, caches, jobs, or internal algorithms.

## Migration order

1. Core contracts and event bus.
2. Plan contract and execution result.
3. Replace direct cross-domain callbacks with events.
4. Content pipeline/registry boundary.
5. Simulation scheduler and dirty dependency graph.
6. Domain-by-domain migration.
7. Editor and Mod authoring on the same content contracts.
8. Save/snapshot migration and compatibility tooling.
9. Remove legacy bridges only after all consumers migrate.

## Performance rules

- Do not optimize only the hottest method; first reduce unnecessary work.
- Prefer dirty sets and dependency-driven recalculation over global scans.
- Calculate derived data once and expose it through queries.
- Keep parallel work side-effect free; commit state changes centrally.
- Keep Unity API usage outside core simulation calculations where practical.
