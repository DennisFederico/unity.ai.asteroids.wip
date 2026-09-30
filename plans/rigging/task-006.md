# Task-006: Authoring Recipe (Tier-1)

## Overview
This recipe defines the scene configuration required to support collision detection scoring. It follows the Two-Scope Model to separate managed UI components from unmanaged ECS data.

---

## Scope 1: Main Scene Hierarchy (`Assets/Scenes/Asteroids.unity`)
**Role**: Contains runtime managed objects, including the user interface and hybrid bridges.

| GameObject Name | Type | Components Attached | Serialized Field Values | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **UI Canvas** | `Canvas` | `Canvas`, `CanvasScaler`, `GraphicRaycaster` | Render Mode: Screen Space - Overlay | Root of the UI system. |
| ├── **ScoreText** | `Empty GameObject` | `TextMeshProUGUI`, `ScoreDisplayView` | `_scoreText`: Reference to `ScoreText` component | Hybrid Bridge: Synchronizes `GameScore` entity data to this text object via `SystemBase`. |
| **Entities** | `GameObject` | `SubScene` | `_SceneAsset`: `Assets/Scenes/Asteroids_entities.unity` | Anchor for the entities subscene. |

---

## Scope 2: SubScene Hierarchy (`Assets/Scenes/Asteroids_entities.unity`)
**Role**: Contains authoring scripts that are baked into pure ECS entities.

| GameObject Name | Type | Components Attached | Serialized Field Values | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **ScoreManager** | `Empty GameObject` | `GameScoreAuthoring` | `StartingScore`: 0 | **Pure Data Entity**. This will be converted into a singleton `GameScore` IComponentData entity. No visual mesh is attached. |

---

## Implementation Summary Checklist
- [x] Separate Scopes defined? Yes.
- [x] Visual vs. Pure Data identified? Yes (`ScoreManager` is Pure Data).
- [x] Managed Companion Bridges included? Yes (`ScoreDisplayView`).
- [x] Asset paths verified? Checked against `IMPLEMENTATION_PLAN.md`.

**Proceed to Tier-2 Gated Execution upon approval.**
