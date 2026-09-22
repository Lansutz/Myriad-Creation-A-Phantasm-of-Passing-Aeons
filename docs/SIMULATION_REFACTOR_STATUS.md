# Simulation Refactor Branch Status

> Branch: `architecture-2-0-refactor`
> Updated: 2026-09-22

## Branch policy

This branch is the active integration line for the simulation refactor.

`main` is not merged wholesale into this branch because the two lines diverged structurally:
- `main` keeps the current active Unity source under `CivilizationEvolution/...`.
- `architecture-2-0-refactor` has the larger refactor history and the authoritative source under `MyriadCreation/...`.

A direct merge of current `main` into the refactor branch produces conflicts around the source-root migration. Therefore synchronization is performed as **functional delta porting**, not by replacing the refactor tree with the less-refactored `main` tree.

## Current verified architecture

The refactor branch already contains:
- central `SimulationScheduler` with dirty-state support;
- world schedule composition through `RegisterSimulationSchedules()`;
- domain schedules for map, disaster, economy, building, population, politics, settlement, culture, diplomacy, warfare/religion, characters, succession, thought, AI, events, research plans, plans, and simulation time;
- structured Activity/Plan contracts and wait conditions;
- domain-owned schedule boundaries for most simulation phases.

## This synchronization pass

The latest active-line work was ported into the refactor architecture instead of copying the newer `CivilizationEvolution/...` files verbatim.

Completed:
1. Added `PoliticsSimulationSystem` under the refactor branch.
2. Moved the full politics daily runtime out of `GameWorld.PoliticsTick()` into that system.
3. Removed the GameWorld-owned politics/social-pulse implementation and its private state.
4. Changed `PoliticsSchedule` from a GameWorld callback adapter to a domain-system registration.
5. Constructed the politics runtime from explicit domain dependencies during subsystem initialization.
6. Kept scheduler ownership in `RegisterSimulationSchedules()`.

The important boundary is now:

`SimulationScheduler -> PoliticsSchedule -> PoliticsSimulationSystem -> domain state changes`

rather than:

`SimulationScheduler -> GameWorld.PoliticsTick()`

## Remaining migration

The settlement schedule still contains callback-shaped dependencies, including settlement control, map actors, camps, evolution, recovery, and abandonment. These should be extracted one domain boundary at a time.

Warfare/religion, AI, events, and time already have schedule boundaries on this branch, but their remaining callback dependencies still need to be reduced where they point back into GameWorld.

## Merge policy

Do not merge this branch into `main` yet.

Merge only after:
1. all simulation phases have domain-owned execution boundaries;
2. callback-shaped GameWorld dependencies have been removed or reduced to legitimate composition boundaries;
3. source-root and namespace consistency is verified;
4. a real Unity build/test is run successfully.
