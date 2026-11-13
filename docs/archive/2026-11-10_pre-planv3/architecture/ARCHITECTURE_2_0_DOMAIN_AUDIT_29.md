# Architecture 2.0 Domain Audit 29 — Innovation Practice Progress Call Graph

> Audit scope: verify the runtime call graph and cadence of the upgraded practice-driven Innovation progress model before deciding its 2.0 authority and save scope.
>
> Evidence baseline: repository source at commit `cf548666d0e868d7ceae6c6f1ed39bf44b93f730`, plus the Architecture 2.0 refactor audit trail. This audit records source evidence only; it does not assume that a designed API is actually wired into runtime execution.

## 1. Executive conclusion

The upgraded practice-driven model is **implemented as a method/data model but is not proven to be wired into the live simulation loop**.

The critical finding is:

- `InnovationTree.MonthlyTickProgress(...)` exists.
- Repository search found **no external call site** for `MonthlyTickProgress(`.
- `CharacterResearchManager.GetResearcherCount(innovationId)` exists specifically to supply the method's `researcherCount`, but no caller chain was found connecting it to `MonthlyTickProgress`.
- `GetProgress(innovationId)` is only used internally by `InnovationTree`; no UI/AI/simulation caller was found that establishes `InnovationProgress` as an externally consumed authoritative state.
- The live proven research path remains the legacy realm path:
  `AIController → InnovationTree.DailyTick → CompleteResearch → _realmInnovations`.
- `ResearchPlanSystem` separately implements the newer personal practice/discovery path through `InnovationKnowledgeSystem`, and its final formalization still uses a temporary compatibility bridge back into the legacy realm research API.

Therefore the previous Audit 28 conclusion must be strengthened:

> `_innovationProgress` is not merely a scope-mismatched active aggregate; at the audited revision, its monthly advancement path is not proven to be connected to the live scheduler. It should remain **UNRESOLVED / compatibility-design evidence**, not be migrated as an active 2.0 runtime authority yet.

## 2. Exact call-graph evidence

### 2.1 `MonthlyTickProgress`

Definition:

`InnovationTree.MonthlyTickProgress(GameWorld world, int realmId, int innovationId, float monthlyOutput, float averageQuality, int researcherCount, bool hasFacility)`

Its implementation:

1. checks the InnovationDef;
2. skips already-completed realm innovation;
3. obtains `InnovationProgress` by **innovationId only**;
4. checks realm prerequisites;
5. accumulates monthly output;
6. writes average quality and old-method practice count;
7. accumulates realm × goods output in `_resourceCumulativeOutput`;
8. computes production-practice experience;
9. adds character-research contribution from `researcherCount`;
10. adds facility contribution;
11. updates `progress`;
12. calls `CompleteResearch(realmId, innovationId)` at 100%.

This makes the intended semantic inputs clearly realm-contextual even though the stored `InnovationProgress` object is keyed only by innovation.

### 2.2 External caller search

Repository search for:

- `MonthlyTickProgress(`
- `Innovations.MonthlyTickProgress`

returned no external caller.

The only source match is the method definition in `InnovationTree.cs`.

This is stronger evidence than merely failing to find a scheduler: the exact public method call pattern itself has no repository match outside its declaration.

### 2.3 `GetProgress`

`InnovationTree.GetProgress(int innovationId)` creates/returns the same innovation-only keyed `InnovationProgress`.

The audited repository search found no external consumer establishing a UI, AI, scheduler, save, or other domain system dependency on this object.

Within `InnovationTree`, it is used by `MonthlyTickProgress` to mutate the object and by the local prerequisite/progress logic.

Consequently, there is currently no evidence that `InnovationProgress` is an independently observed runtime authority.

## 3. Researcher-count chain

`CharacterResearchManager.GetResearcherCount(innovationId)` exists with an explicit comment saying it is for `InnovationTree`'s `researcherCount` parameter.

Its implementation counts character research records whose `currentResearchInnovationId` matches the innovation.

However:

- no caller to `MonthlyTickProgress` was found;
- therefore the intended chain

`CharacterResearchManager.GetResearcherCount`
→ `InnovationTree.MonthlyTickProgress`
→ `InnovationProgress`

is **designed but not proven wired**.

This matters because the README's upgraded design emphasizes personal researchers/practice, but the currently visible runtime path does not establish that the monthly social-progress aggregate actually consumes those researcher records.

## 4. Proven live legacy path

The audited source contains a real caller:

`AIController` computes a research rate and calls:

`innovations.DailyTick(realmId, researchRate)`

That path is:

`AIController`
→ `InnovationTree.DailyTick`
→ `_realmResearchPoints[realmId]`
→ `CompleteResearch(realmId, innovationId)`
→ `_realmInnovations[realmId]`
→ `OnInnovationCompleted`

This is a real realm-scoped runtime lifecycle.

It means the legacy state cannot be removed merely because the practice-driven API exists.

## 5. ResearchPlan path is separate

`ResearchPlanSystem` contains a newer personal discovery/practice architecture:

`Character`
→ `InnovationKnowledgeSystem`
→ character × innovation practice/mastery
→ discovery candidate
→ `ResearchPlan`
→ PlanSystem lifecycle
→ verification
→ temporary compatibility formalization
→ `InnovationTree.StartResearch(realmId, innovationId)`
→ `InnovationTree.DailyTick(...)`
→ `_realmInnovations`

Important distinction:

- `InnovationKnowledgeSystem` is character-scoped knowledge/practice.
- `ResearchPlanSystem` is a personal planning/execution bridge.
- `_realmInnovations` is the currently proven social/realm completion state.
- `InnovationProgress` is a separate innovation-only keyed aggregate whose live caller is not proven.

Therefore the newer architecture does **not** make `InnovationProgress` the authority by implication.

## 6. Consequences for 2.0 domain ownership

### 6.1 Keep as established

- `InnovationDef` → static content definition.
- `_realmInnovations` → current proven Realm/Social Innovation ownership state.
- `CharacterResearchData` → character research state.
- `InnovationKnowledgeSystem` mastery/practice → character × innovation knowledge/practice state.
- `ResearchPlanData` → research-plan-specific state.
- `_realmResearchPoints` / `_realmCurrentResearch` → legacy compatibility state while live callers remain.

### 6.2 Do not promote yet

`_innovationProgress : Dictionary<int, InnovationProgress>`

Do not currently classify it as:

- Realm Innovation Progress;
- Society-wide Innovation Progress;
- Culture Innovation Progress;
- Character Innovation Progress;
- global Innovation Progress;
- final save authority.

The current evidence supports only:

> a prototype/practice-driven progress aggregate keyed by innovation, designed to be advanced with realm-specific evidence.

That is insufficient for a 2.0 authority decision.

### 6.3 `_resourceCumulativeOutput`

The method that would advance practice progress also maintains:

`_resourceCumulativeOutput[realmId_goodsId]`

This remains clearly Realm × Goods economic/production evidence.

The absence of a live `MonthlyTickProgress` caller means the current InnovationTree implementation is also carrying a dormant/embedded economic accumulation mechanism.

Target boundary remains:

`Economy / Production Evidence`
→ Innovation Query/Input
rather than InnovationTree owning cumulative production.

## 7. What this changes from Audit 28

Audit 28 established a **scope mismatch**:

`MonthlyTickProgress` receives `realmId`, but `_innovationProgress` is keyed only by `innovationId`.

Audit 29 adds a second, independent finding:

**The intended monthly advancement path is not proven connected to runtime execution.**

So there are two separate problems:

1. **Storage grain mismatch**
   - intended calculation context: Realm × Innovation × practice evidence;
   - stored progress grain: Innovation.

2. **Runtime wiring gap**
   - monthly progression API exists;
   - no external caller was found.

These must not be conflated. Fixing the key alone would not prove the feature is active; wiring the method alone would not solve its state-grain problem.

## 8. Required next audit

Before implementing Innovation runtime/save migration, trace these remaining boundaries:

1. Where monthly Economy/Production output is actually calculated.
2. Whether there is an existing monthly scheduler/domain system that should emit an Innovation practice input rather than directly calling InnovationTree.
3. Where character practice is recorded from production, construction, mining, crafting, or other activities.
4. Whether `InnovationKnowledgeSystem` practice can be aggregated into social innovation progress without duplicating the same evidence.
5. Whether `InnovationProgress` has any UI/debug/save serialization path outside the searched source.
6. Whether `AIController.DailyTick` is still the authoritative live caller after the Architecture 2.0 schedule migration, or merely legacy code awaiting migration.

Only after those points are closed should the project choose a final replacement for `_innovationProgress`.

## 9. Migration rule

Do **not**:

- delete `MonthlyTickProgress`;
- delete `InnovationProgress`;
- change its dictionary key to `realmId + innovationId`;
- move it into SaveData;
- merge it into `InnovationKnowledgeSystem`;
- make `ResearchPlanSystem` its owner.

The correct current status is:

**UNRESOLVED — prototype/legacy upgraded practice-progress path; runtime caller and aggregation grain both require closure.**

No runtime code was changed by this audit.
