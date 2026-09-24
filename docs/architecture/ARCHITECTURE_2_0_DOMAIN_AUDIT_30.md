# Architecture 2.0 Domain Audit 30 — Planning / AI / Innovation Runtime Boundary

> Purpose: connect the Innovation audit back to the actual Architecture 2.0 migration goal: move simulation authority out of the monolithic GameWorld loop into DomainSimulationSystem + Schedule + Process/Command/Event boundaries, while preserving the upgraded innovation design.

## 1. Why this audit matters

The previous audit established that the upgraded `MonthlyTickProgress` method is not externally wired.

The broader runtime inspection now shows a more important architectural fact:

**Innovation is not the only problem. The current GameWorld loop still directly owns or invokes Planning and AI execution, while the new SimulationScheduler is only partially adopted.**

The migration target therefore remains:

`Schedule → DomainSimulationSystem → Process → Command/State Change → Domain Event → Reaction → Dirty/Derived`

rather than merely replacing one Innovation method.

## 2. Current scheduler adoption

The repository contains a real central `SimulationScheduler` with:

- explicit cadence;
- explicit order;
- stable order + id sorting;
- duplicate registration protection;
- Daily/Weekly/Monthly/Seasonal/LongTerm cadence.

At the audited revision, confirmed registered schedules include:

- `PoliticsSimulationSchedule`
- `SettlementSimulationSchedule`

The scheduler is therefore real infrastructure, not a design-only document.

However, repository search did not find a corresponding scheduler registration for:

- AI;
- Planning;
- ResearchPlan;
- Innovation;
- Economy;
- Population;
- Diplomacy;
- Warfare;
- Character;
- Thought;
- Event processing.

This means scheduler adoption is **partial**, not yet the complete simulation execution spine.

## 3. GameWorld remains a second scheduler

The current `GameWorld.GameTick()` directly executes many domain operations before/after the central scheduler.

Confirmed sequence includes:

1. dirty recalculation;
2. disaster;
3. disease;
4. economy;
5. building;
6. population;
7. `SimulationScheduler.Tick(...)`;
8. map actors;
9. camps;
10. settlement evolution;
11. settlement destruction recovery;
12. abandonment/bandit checks;
13. culture stage evolution;
14. diplomacy;
15. combat/war outcomes;
16. character;
17. succession/admin maintenance;
18. thought;
19. missionary;
20. AI;
21. event processing;
22. `TickPlans(1f)`;
23. time advance.

Therefore the Architecture 2.0 scheduler is currently an **embedded subsystem inside GameWorld**, not yet the sole domain execution boundary.

## 4. Planning path

The current planning integration is:

`GameWorld.GameTick`
→ `GameWorld.TickPlans(1f)`
→ `PlanSystem.DailyTick`
→ ResearchPlan executor
→ personal breakthrough processing.

Inside `TickPlans`:

1. generic PlanSystem advances plans;
2. `ResearchPlanSystem.DrainPracticeDirtyCharacters` drains characters with recorded practice;
3. each dirty character receives `TryDailyBreakthrough`.

This is a useful architecture improvement because:

- generic plan lifecycle remains in PlanSystem;
- ResearchPlan-specific behavior remains in ResearchPlanSystem;
- character practice is submitted through `GameWorld.RecordInnovationPractice`.

But the entry point is still a GameWorld callback.

### Required target

The eventual target should be:

`PlanningSchedule`
→ `PlanningSimulationSystem`
→ `PlanSystem / ResearchPlanSystem`
→ `Process`
→ events/effects

GameWorld should provide state/context, not directly run `TickPlans`.

## 5. AI path

The current AI path is:

`GameWorld.GameTick`
→ `_aiManager.SyncRulers(...)`
→ `_aiManager.DailyTick(...)`
→ `AIController.DailyTick(...)`
→ `DailyActions`
→ `InnovationTree.GetCurrentResearch`
→ `InnovationTree.GetAvailableInnovations`
→ `InnovationTree.StartResearch`
→ `InnovationTree.DailyTick`

Thus the AI currently performs two different responsibilities in one daily callback:

### A. AI decision

Choosing which innovation to research.

### B. Domain simulation

Advancing the legacy research points.

These must eventually be separated.

The target architecture should be conceptually:

`AISchedule`
→ `AISimulationSystem`
→ AI evaluates available Actions/Intents
→ research intent/action
→ Innovation domain accepts the intent
→ Innovation process/effect advances research.

AI should not own Innovation state mutation merely because it is the caller.

## 6. Important Innovation consequence

The legacy path is therefore not simply:

`InnovationSchedule → InnovationTree.DailyTick`

It is currently:

`GameWorld`
→ `AI`
→ `InnovationTree.StartResearch`
→ `InnovationTree.DailyTick`

That is an architectural dependency violation relative to the intended 2.0 model.

It also explains why deleting `InnovationTree.DailyTick` immediately would break a real runtime path.

## 7. New ResearchPlan path vs old AI path

Two research concepts coexist:

### Legacy AI realm research

`AI → StartResearch → DailyTick → CompleteResearch → _realmInnovations`

### Upgraded personal research

`Character practice → InnovationKnowledge → breakthrough → ResearchPlan → verification → compatibility formalization → legacy realm research`

This is not yet a clean replacement.

The upgraded path still terminates in the legacy social completion mechanism, while AI can bypass the personal practice path entirely.

This is strong evidence that the Innovation upgrade is **partially implemented and architecturally bridged**, not yet a completed 2.0 replacement.

## 8. What we should migrate first

The safest migration order is now clear:

### Phase A — scheduler boundary

Create a dedicated Planning/AI schedule boundary without changing research semantics.

Goal:

- stop GameWorld from directly calling `TickPlans`;
- stop GameWorld from directly invoking AI behavior;
- retain the existing underlying behavior.

### Phase B — AI / research responsibility split

Separate:

- AI decision/intent;
- Innovation research state;
- research advancement.

AI chooses; Innovation executes.

### Phase C — Innovation practice aggregation

Only after the caller graph is stable should we redesign `InnovationProgress`.

At that point the desired input should be something like:

`Realm × Innovation × PracticeEvidence`

and the aggregate must have an explicit owner.

### Phase D — remove compatibility bridge

Only after the upgraded social completion path is authoritative should:

- `InnovationTree.StartResearch`
- `InnovationTree.DailyTick`
- `_realmResearchPoints`
- `_realmCurrentResearch`

become removable compatibility state.

## 9. Migration prohibition

Do not currently:

- move `InnovationTree.DailyTick` into a new InnovationSchedule as a direct copy;
- make AI the Innovation authority;
- make ResearchPlanSystem the social Innovation authority;
- change `_innovationProgress` key shape without proving its intended aggregation grain;
- delete legacy realm research;
- add a fake `InnovationSimulationSystem` merely to satisfy naming.

The next implementation should move an actual execution boundary, not create wrapper-only classes.

## 10. Next audit / implementation gate

Before writing the first Planning/AI schedule migration, verify:

1. `AIManager` ownership and daily call path;
2. `GameWorldPlanning.TickPlans` is only called from GameWorld;
3. current `PlanSystem` process integration remains intact;
4. existing Architecture 2.0 schedule registrations and order numbers;
5. whether AI needs a command/intent boundary before any state mutation is moved.

After that, the first real migration should be **Planning**, because its generic lifecycle is already separated and it has a clean `TickPlans` boundary.

No runtime code was changed in this audit.
