# Simulation Synchronization Plan

> Updated: 2026-09-22
>
> Purpose: record the current synchronization state between the simulation scheduler, domain systems, Plan runtime, and world state, including the tooling constraint discovered during repository inspection.

## 1. Target execution boundary

The intended direction is:

Schedule
-> DomainSimulationSystem
-> Process / Command
-> State Change
-> Event
-> Dirty / Derived State

Avoid:

Schedule
-> GameWorld.SomePrivateMethod()

The scheduler should know cadence and ordering, but not domain rules or GameWorld internals.

## 2. Plan runtime direction

The unified Plan line is:

Plan
-> Activity
-> Activity Executor
-> ISimulationProcess
-> ProcessResult
   -> Continue
   -> Complete
   -> Wait
   -> Fail
   -> Cancel
-> Event / State Change

PlanSystem owns lifecycle and scheduling. Domain systems own domain rules.

Current repository state already contains a partial Plan runtime:
- PlanType / PlanState / PlanPhase
- PlanParticipant / PlanRequirement
- Plan
- IPlanExecutor
- PlanSystem
- ConstructionPlanSystem
- ResearchPlanSystem

The remaining synchronization work is to introduce the process/activity layer without duplicating the lifecycle per domain.

## 3. Scheduler migration targets

Known legacy/direct world-loop coupling that still needs migration:
- Politics: scheduler/domain wrapper still accepts a GameWorld callback through Action.
- Settlement: legacy schedule/action coupling must be migrated to a domain simulation system.
- Warfare/Religion: war and religion resolution callbacks still cross directly into GameWorld.
- AI: MissionaryTick is still invoked from GameWorld rather than being owned by an independent domain process.
- Events: ProcessEvents is still called directly by GameWorld.
- Time: AdvanceTime is still called directly by GameWorld.

Migration rule: first create the domain runtime boundary, then move the GameWorld call behind that boundary; do not replace one direct callback with another callback-shaped dependency.

## 4. Tooling / repository inspection issue — MUST NOT BE LOST

Repository inspection exposed a tooling constraint that affected synchronization work:

1. GitHub code search is scoped to the repository's default branch.
2. fetch_file also reads the default branch when ref is omitted.
3. The repository contains two source roots with different responsibilities:
   - MyriadCreation/...
   - CivilizationEvolution/...
4. Therefore a search result from one root cannot be treated as evidence that the corresponding file/class exists in the other root.
5. A path that appears missing can be a real path/branch/root mismatch rather than evidence that the implementation does not exist.
6. Before creating or moving synchronization files, verify:
   - repository
   - default branch / explicit ref when known
   - exact source root
   - exact namespace
   - existing file path
7. Do not force-write a guessed path merely because a previous tool lookup failed.

Concrete verification from this pass:
- MyriadCreation/Assets/Scripts/Core/Simulation/SimulationScheduler.cs exists on main and defines the cadence scheduler.
- MyriadCreation/Assets/Scripts/Simulation/Politics/PoliticsSimulationSystem.cs exists on main and currently wraps PoliticalManager plus an Action world callback.
- CivilizationEvolution/Assets/Scripts/Simulation/Planning/PlanSystem.cs exists on main and already provides the unified Plan lifecycle.
- CivilizationEvolution/Assets/Scripts/Simulation/Planning/PlanTypes.cs defines IPlanExecutor, but no ISimulationProcess / ProcessResult layer was found by repository search.

This tooling constraint is part of the synchronization plan because ignoring it can produce false architecture conclusions and incorrect file writes.

## 5. Next implementation order

1. Establish one authoritative simulation source root/boundary for the new scheduler-facing systems.
2. Add the process/activity abstraction required by Plan execution.
3. Connect PlanSystem to that abstraction while preserving existing Construction/Research executors.
4. Migrate one domain schedule at a time from GameWorld callbacks to domain-owned simulation systems.
5. Remove the corresponding GameWorld direct call only after the replacement is wired.
6. Move event processing behind the simulation/event boundary.
7. Move time advancement to the simulation clock boundary last, so existing systems can migrate against a stable day/tick context.
8. Verify exact repository path and branch before every write.

## 6. Non-goals

- Do not create parallel Plan systems for Construction, Research, Military, Diplomacy, etc.
- Do not make GameWorld the universal service locator for domain execution.
- Do not treat a failed repository search as proof that a class/file is absent.
- Do not claim Unity compilation success unless an actual build/test has been run.
