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

Base / Editor / Scenario / Mod -> Content Source -> Provider -> Validation -> Resolver -> Runtime Definition -> Registry

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


## Current migration slice

- `SimulationEventBus` decouples cross-domain facts.
- Building and combat now publish `PracticeRecordedEvent`; Innovation consumes it.
- `Plan` no longer has parent/child plan hierarchy.
- `Plan.currentActivity` describes what the plan is doing.
- Plan executors return `PlanExecutionResult`; lifecycle decisions stay in `PlanSystem`.
- Base/Mods now enter `ContentRegistry` through an ordered `ContentSourceCatalog`.
- Existing public facades remain where needed so migration can proceed without a flag-day rewrite.

## Compatibility policy

During migration, compatibility bridges are allowed at boundaries, but new domain code must not add another direct cross-domain callback. Each bridge is temporary and should be removed when all callers use the stable contract.

## Editor and Mod relationship

The built-in editor is an authoring client, not a privileged runtime system.

A player can:
1. create or edit Race, Culture, Religion, Innovation and other definitions with the built-in authoring tools;
2. keep those definitions in an authoring workspace;
3. export the workspace as a normal content package under StreamingAssets/Mods/<packageId>;
4. distribute that package as a mod;
5. edit the same package manually or with external tools using the Extension API.

Therefore built-in editor content and mod content are not two competing data models. They are two authoring paths that produce the same package/runtime format.

World/Scenario editing remains separate: changing a settlement, terrain tile, population or other live world state is not the same operation as authoring a reusable Definition.

### Package boundary

Editor / External Tool
        |
        v
Content Authoring Workspace
        |
        v
Content Package (mod.json + canonical content files)
        |
        v
StreamingAssets/Mods/<packageId>
        |
        v
Content Source
        |
        v
Content Registry / Runtime Definitions

The runtime deliberately does not need to know whether a definition was created by the built-in editor or by hand.