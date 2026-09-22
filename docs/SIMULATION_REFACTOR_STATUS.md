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

Main and refactor are intentionally synchronized by functional delta: the latest main-line architecture changes are carried into the authoritative `MyriadCreation/...` source root without introducing a second competing Unity source root.

Completed:
1. Ported the latest main-line command/query boundary into the refactor core contracts.
2. Added `PoliticsSimulationSystem` under the refactor branch.
2. Moved the full politics daily runtime out of `GameWorld.PoliticsTick()` into that system.
3. Removed the GameWorld-owned politics/social-pulse implementation and its private state.
4. Changed `PoliticsSchedule` from a GameWorld callback adapter to a domain-system registration.
5. Constructed the politics runtime from explicit domain dependencies during subsystem initialization.
6. Kept scheduler ownership in `RegisterSimulationSchedules()`.

The important boundary is now:

`SimulationScheduler -> PoliticsSchedule -> PoliticsSimulationSystem -> domain state changes`

rather than:

`SimulationScheduler -> GameWorld.PoliticsTick()`

## Settlement migration checkpoint

Settlement has now moved beyond callback-shaped execution for its six scheduled phases:
- SettlementControlRuntime owns the control-state dependencies and invokes the existing control rules.
- SettlementEvolutionRuntime owns the GameWorld dependency for evolution.
- SettlementDestructionRuntime owns the GameWorld dependency for ruin recovery and destruction entry points.
- LandAbandonmentRuntime owns the GameWorld dependency for abandonment/resettlement/bandit-spawn entry points.
- MapActorManager and CampManager remain explicit injected dependencies for their daily phases.

The execution boundary is now:

SimulationScheduler -> SettlementSchedule -> SettlementSimulationSystem -> domain runtimes -> state/rules

No Action callback fields remain in SettlementSimulationSystem.

## Remaining migration

The settlement domain still has legacy static rule APIs underneath the runtime boundaries. The next step is to move state-changing operations toward command/process/event boundaries rather than treating runtime wrappers as the final architecture.

Warfare/religion, AI, events, and time already have schedule boundaries on this branch, but their remaining callback dependencies still need to be reduced where they point back into GameWorld.

Warfare/religion, AI, events, and time already have schedule boundaries on this branch, but their remaining callback dependencies still need to be reduced where they point back into GameWorld.

## Merge policy

Do not merge this branch into `main` yet.

Merge only after:
1. all simulation phases have domain-owned execution boundaries;
2. callback-shaped GameWorld dependencies have been removed or reduced to legitimate composition boundaries;
3. source-root and namespace consistency is verified;
4. a real Unity build/test is run successfully.


## Main synchronization checkpoint

At 2026-09-22, main was at cf548666d0e868d7ceae6c6f1ed39bf44b93f730 and architecture-2-0-refactor was at c127ad15e5911d331afaee285c216e5a88be3b3a.

A direct main -> architecture-2-0-refactor PR was created for verification, but GitHub reported it as non-mergeable because the branches have different source-root histories. PR #7 is therefore not used as the synchronization mechanism.

The synchronization rule is:
- preserve the refactor branch's authoritative MyriadCreation/... implementation;
- port functional changes from the latest main into that source root;
- do not copy the less-refactored CivilizationEvolution/... tree back into the refactor project;
- verify each port against the latest main implementation before continuing.

This means the refactor branch must contain the latest applicable main-line behavior, while retaining its deeper refactor work.
