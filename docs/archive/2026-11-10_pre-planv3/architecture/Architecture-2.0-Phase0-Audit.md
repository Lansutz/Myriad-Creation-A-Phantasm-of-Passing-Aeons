# Architecture 2.0 — Phase 0 Repository Audit

> Branch: `architecture-2-0-refactor`
> Scope: architecture migration baseline; this document records the current-state audit before the next structural migration.

## 1. Audit conclusion

The repository is already part-way through Architecture 2.0. The important foundations are present, but the migration is incomplete.

The current architecture has four distinct maturity levels:

1. **Core contracts** — Command/Query, EventBus, Scheduler and dirty propagation are already established.
2. **Content pipeline** — Provider/Store/Resolver/Authoring abstractions exist and several content types have migrated.
3. **Domain scheduling** — many GameWorld hard-coded registrations have moved into domain-owned Schedule classes.
4. **Plan runtime** — lifecycle/executor/result abstractions exist, but the Activity/Process/Condition/Waiting/Transition runtime is not yet implemented as a first-class pipeline.

The next work should therefore be migration, not another parallel architecture.

---

## 2. Target dependency direction

```
Presentation
    │ read snapshots
    ▼
Simulation Runtime
    │
    ├── Actor / AI
    ├── Plan
    ├── Activity
    ├── Process
    └── Event
          │
          ▼
      Domain State
          │
          ▼
       World State

Core
  ▲
  │ contracts
Domain ──────────► Core

Domain-to-domain calls should progressively become:
Command / Query / Event
rather than direct method calls.
```

Hard rule for migration:

- Core must not reference domain concepts.
- External/domain consumers must not depend on domain internals.
- Cross-domain effects should use Command, Query, or Event.
- GameWorld should remain the composition root/adapter, not the owner of domain behavior.

---

## 3. Current Core baseline

### Already present

- `ISimulationCommand`
- `ISimulationQuery<TResult>`
- `SimulationCommandBus`
- `SimulationQueryBus`
- `CommandResult`
- `SimulationEventBus`
- `SimulationScheduler`
- `ISimulationScheduler`
- `SimulationDirtySet`
- event → dirty bridge
- stable Content Resolver/Store/Provider contracts

### Current scheduler model

```
SimulationClock / GameWorld time
        ↓
SimulationScheduler
        ├── Immediate
        ├── Daily
        ├── Weekly
        ├── Monthly
        ├── Seasonal
        └── LongTerm
```

The scheduler already supports dirty-gated execution and preserves re-invalidations that occur while a dirty job is executing.

### Remaining Core work

- formal Simulation Frame / pipeline phases
- stable Event Envelope / subscription lifetime contract
- first-class Activity / Process / Condition / Effect / Result contracts
- immutable/read-only snapshots for domain queries
- eventual SimulationClock ownership of time instead of GameWorld mutation
- explicit save/load version + migration contracts

---

## 4. GameWorld dependency audit

GameWorld has improved substantially: schedule registration is now mostly composition.

Current registration direction is approximately:

```
GameWorld
  └── SimulationScheduler
        ├── World dirty recalculation
        ├── Disaster
        ├── Economy
        ├── Building
        ├── Population
        ├── Politics
        ├── Settlement
        ├── Culture
        ├── Diplomacy
        ├── Warfare/Religion
        ├── Characters
        ├── Succession
        ├── Thought
        ├── AI
        ├── Events
        ├── Planning
        └── Simulation time
```

This is a significant improvement over a monolithic GameWorld Tick, but several schedules still receive GameWorld delegates.

Known remaining coupling classes:

- Politics schedule → world-level politics callback
- Settlement schedule → multiple GameWorld callbacks
- Warfare/Religion schedule → religion / holy-war / war-outcome callbacks
- AI schedule → missionary callback
- Event schedule → world event callback
- world dirty recalculation → still owned by GameWorld
- time advancement → still mutates GameWorld-owned time

These are migration targets, not reasons to revert the scheduler work.

---

## 5. Domain dependency hotspots

The audit identifies a recurring legacy pattern:

```
Domain A
   ↓ direct reference
Manager / System B
   ↓
GameWorld
   ↓
Manager / System C
```

Representative examples already visible in the repository:

- AI receives several domain managers directly.
- Disease logic owns a CharacterManager dependency.
- PoliticalManager works directly over Realm/Tile state.
- warfare logic directly reaches character / diplomacy / political state.
- settlement/actor logic directly reaches GameWorld and static managers.
- older GameWorld partial files still combine initialization, simulation, events, succession and terrain concerns.

These are not all to be rewritten immediately. Each should be migrated at a domain boundary where a stable Query, Command or Event can replace the direct dependency.

---

## 6. Plan audit

### Current Plan model

The current Plan already has the intended shared semantics:

- plan id/type
- lifecycle state
- phase
- initiator
- owner
- target
- purpose
- title/description
- participants
- requirements
- current activity
- progress
- elapsed/estimated time
- result code
- result summary

The lifecycle is:

```
Proposed
  ↓
Accepted
  ↓
Preparing
  ↓
Executing
  ↓
Completed / Failed / Cancelled

Paused is a lifecycle interruption, not a task/activity.
```

### Important missing layer

The current runtime is still fundamentally:

```
Plan
  ↓
IPlanExecutor
  ↓
PlanExecutionResult
  ↓
Continue / Complete / Fail / Cancel
```

It is **not yet**:

```
Plan
  ↓
CurrentActivity
  ↓
Activity Executor
  ↓
Process
  ↓
Event / Fact
  ↓
Condition / Transition
  ↓
Next Activity
  ↺
```

There is currently no first-class runtime representation for:

- Activity definition/state
- Process
- waiting condition
- wake/resume trigger
- activity transition
- cycle/re-evaluation
- event-driven activity completion

This confirms that the previously discussed Plan loop/waiting architecture still needs to be implemented.

### Design constraint

Do not introduce:

- ParentPlan
- ChildPlan
- WaitingPlan
- WaitingForPlanId
- Waiting as PlanState

Waiting belongs to the current activity/runtime condition. The Plan remains Executing while its current activity is, for example, `WaitingForMaterial`.

---

## 7. Event architecture audit

The typed SimulationEventBus is now suitable as the common transport mechanism.

The correct direction is:

```
Domain Process
    ↓
Domain Event / Fact
    ↓
SimulationEventBus
    ├── Economy observer
    ├── Innovation observer
    ├── Character observer
    ├── Politics observer
    └── Chronicle observer
```

Legacy code still contains direct cross-domain effects. The migration rule is:

> First create the stable event contract, then migrate consumers; do not add another domain-specific callback.

Chronicle/history should consume simulation facts rather than become the transport mechanism for simulation logic.

---

## 8. Content pipeline audit

The current content architecture has already moved toward:

```
Content Source
    ↓
Content Package
    ↓
Provider
    ↓
Validation
    ↓
Content Store / Resolver
    ↓
Runtime Definition
```

Existing foundations include:

- ContentSource
- ContentPackage
- ContentAuthoringWorkspace
- ContentPackageValidation
- ContentExtensionApi
- ContentProviderCatalog
- ContentStore
- ContentResolver
- built-in Base/Mods source composition
- editor export through the same package format

This is the correct direction.

Remaining issue: the compatibility `ContentRegistry` still exposes legacy public dictionaries and therefore remains an implementation dependency for old consumers.

Migration target:

```
Old consumer → ContentRegistry dictionary
New consumer → ContentResolver / Query
```

Once consumers are migrated, ContentRegistry can become a thin compatibility facade and eventually be reduced further.

---

## 9. Definition / State separation audit

The repository is partially data-driven, but legacy domain models still mix definition and runtime state.

Target rule:

```
Definition
  = immutable rules/content

State
  = mutable world instance

Snapshot
  = read-only projection for consumers
```

Examples:

- InnovationDefinition ≠ CharacterInnovationKnowledge
- MaterialDefinition ≠ MaterialBatchState
- BuildingDefinition ≠ ActiveBuildingState
- UnitDefinition ≠ ArmyUnitState

This separation must be introduced at domain boundaries instead of attempting a repository-wide type rewrite in one pass.

---

## 10. Pipeline audit

### Current

Multiple domain schedules now register with the central scheduler. This removes much of the hidden GameWorld ordering.

However, the actual semantic pipeline is still distributed:

```
Command
Decision
Plan
Activity
Process
Event
State Commit
Derived State
```

is not yet an explicit runtime pipeline.

### Target

```
Simulation Frame
 ├── Input / Commands
 ├── Decision
 ├── Plan Update
 ├── Activity / Process
 ├── Event Commit
 ├── State / Derived State
 ├── Slow Simulation
 ├── Chronicle
 └── Presentation Snapshot
```

Scheduler cadence remains orthogonal to these semantic phases.

That distinction is important:

- **Scheduler answers when work is eligible.**
- **Pipeline answers what kind of work is happening.**

---

## 11. AI audit

The intended direction is:

```
Perception
  ↓
Knowledge / Belief
  ↓
Needs
  ↓
Goals
  ↓
Candidate Plans
  ↓
Evaluation
  ↓
Plan Selection
  ↓
Plan Creation
```

Current AI code still accepts several concrete domain managers and performs direct domain-aware evaluation.

Migration should not turn PlanSystem into an AI system. Instead:

- AI chooses the plan.
- Plan owns the persistent large behavior.
- Activity determines current concrete behavior.
- Domain Process changes the world.

---

## 12. Save / Load audit

The project still has legacy save/load coupling around GameWorld.

Target:

```
Runtime State
  ↓
Snapshot
  ↓
Versioned Save
```

and:

```
Save
  ↓
Deserialize
  ↓
Validate
  ↓
Migrate
  ↓
Rebuild indexes
  ↓
Rebuild derived state
  ↓
Simulation Ready
```

This should be addressed after stable State/Snapshot contracts exist; otherwise the save layer would freeze the current implementation details.

---

## 13. Migration order

The audit recommends this concrete order:

### Phase 0 — Audit
This document and dependency inventory.

### Phase 1 — Core runtime contracts
Add only missing reusable contracts:

- Activity
- Process
- Condition
- Transition
- Effect
- Result
- Snapshot

### Phase 2 — Plan runtime
Refactor Plan execution around the above contracts.

First migration target:

```
Research
Construction
```

because both already use the unified PlanSystem.

### Phase 3 — Event migration
Convert high-value direct cross-domain calls to facts/events.

### Phase 4 — Domain schedule ownership
Remove remaining GameWorld callback delegates.

### Phase 5 — Content consumers
Finish Resolver/Store migration and shrink ContentRegistry.

### Phase 6 — AI boundary
Separate decision-making from plan execution and remove manager-heavy AI APIs.

### Phase 7 — Definition/State separation
Migrate domain by domain.

### Phase 8 — World/GameWorld slimming
Move runtime state and composition out of GameWorld.

### Phase 9 — Performance pipeline
Apply snapshot → parallel calculation → result buffer → commit → events to expensive domains.

### Phase 10 — Physical file reorganization
Only after dependency boundaries are stable.

---

## 14. Explicit non-goals

This migration will **not**:

- rewrite the entire project in one pass
- blindly move hundreds of files
- introduce a workflow/DAG engine
- make every action a Plan
- turn Waiting into PlanState
- make Core know every domain
- make EventBus contain game rules
- optimize by prematurely converting everything to ECS
- preserve legacy public internals merely because they are convenient

The migration priority is stable contracts first, implementation replacement second, physical file movement last.

---

## 15. Immediate next architectural step

Phase 0 is now complete enough to establish the migration boundary.

The next code change should be the reusable runtime contract set for:

```
Plan
 ↓
Activity
 ↓
Process
 ↓
Condition / Wait
 ↓
Event
 ↓
Transition
 ↺
```

without introducing domain-specific research/construction logic into Core.

The first implementation should be small, testable, and backward-compatible with the existing PlanSystem so the repository can be migrated incrementally.
