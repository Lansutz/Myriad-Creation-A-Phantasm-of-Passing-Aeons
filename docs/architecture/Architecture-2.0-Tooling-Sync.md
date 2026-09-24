# Architecture 2.0 Tooling / Sync Notes

## Canonical Unity project root

The active Unity project root on `architecture-2-0-refactor` is:

`MyriadCreation/`

All architecture refactors, runtime code changes, tests, and package configuration for the current project must target this tree.

The repository currently also contains a legacy `CivilizationEvolution/` tree. This is a migration artifact and must not be treated as the canonical source when making new changes.

## Why this matters for repository tools

Repository tooling can surface both trees when searching recursively. In particular:

- Searching by filename or symbol can return a result from the legacy `CivilizationEvolution/` tree even when the current implementation is under `MyriadCreation/`.
- Fetching a path from the legacy tree may return an older implementation and can make a refactor appear to have been lost.
- The safe workflow is therefore: resolve the repository tree first, select `MyriadCreation/`, then fetch/update that exact path on branch `architecture-2-0-refactor`.
- Do not copy an implementation from the legacy tree back into `MyriadCreation/` merely because search returned it first.

This is a tooling/source-selection issue, not evidence that the current refactor was reverted.

## Unity MCP dependency

The canonical `MyriadCreation/Packages/manifest.json` includes:

`com.coplaydev.unity-mcp` from the Unity MCP Git repository.

Therefore Unity-side MCP integration is an intentional project dependency. When diagnosing editor/runtime tool behavior, distinguish:

1. repository connector path/source-selection problems;
2. Unity MCP package/editor connectivity problems;
3. actual C# compilation or runtime problems.

Do not attribute a C# or architecture failure to MCP merely because the MCP tool is involved in the workflow.

## Synchronization rule

For future autonomous architecture work:

1. Inspect the recursive repository tree.
2. Confirm the canonical `MyriadCreation/` path.
3. Fetch the exact current file before editing.
4. Apply changes against the current file SHA.
5. Commit to `architecture-2-0-refactor`.
6. Re-fetch the changed file from the same branch and verify the commit SHA/content.
7. Only then treat the change as synchronized.

When a search result points at `CivilizationEvolution/`, treat it as legacy unless the task explicitly concerns migration compatibility.

## Current known state

The architecture refactor is intentionally moving toward:

`Schedule -> Domain Simulation System -> Command / Query / Event -> State`

and:

`Plan -> Activity -> Process -> Result / Event`

Schedules should not become another layer of `GameWorld` callbacks. Compatibility methods may remain temporarily, but new runtime coupling should not be added there.
