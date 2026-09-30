# Task-005: Density-Regulated Asteroid Ring Spawner (Rigging Recipe)

## Section 1: Objective & Scope
* **Objective**: Rig the asteroid spawner as a Pure Data entity in the SubScene, ensuring it holds correctly configured authoring components for local density regulation and adaptive spawn cadence.
* **Target Main Scene**: `Assets/Scenes/Asteroids.unity`
* **Target SubScene**: `Assets/Scenes/Asteroids_entities.unity`

---

## Section 2: Two-Scope Hierarchy Plan

### Scope 1: Main Scene Hierarchy (`Assets/Scenes/Asteroids.unity`)
| GameObject Path | Component Type | Property / Field | Value |
| :--- | :--- | :--- | :--- |
| `Entities` | `Unity.Scenes.SubScene` | `_SceneAsset` | `Assets/Scenes/Asteroids_entities.unity` |

### Scope 2: SubScene Hierarchy (`Assets/Scenes/Asteroids_entities.unity`)
*Note: This entity is designated as **Pure Data / Non-Visual**.*

| GameObject Path | Visual Type | Component to Add | Property / Field | Value |
| :--- | :--- | :--- | :--- |
| `AsteroidSpawner` | Pure Data (Empty) | `AsteroidSpawnerAuthoring` | `LargeAsteroidPrefab` | `Assets/Prefabs/Asteroid_Large.prefab` |
| | | | `MediumAsteroidPrefab` | `Assets/Prefabs/Asteroid_Medium.prefab` |
| | | | `SmallAsteroidPrefab` | `Assets/Prefabs/Asteroid_Small.prefab` |
| | | | `DensityRadius` | `70.0` |
| | | | `TargetLocalDensityMin` | `8` |
| | | | `TargetLocalDensityMax` | `16` |
| | | | `BaseSpawnInterval` | `2.5` |
| | | | `FastSpawnInterval` | `0.8` |
| | | | `Seed` | `1337` |

---

## Section 3: Implementation Checklist (Tier-2 Execution Steps)

- [ ] Open SubScene: `Assets/Scenes/Asteroids_entities.unity`.
- [ ] Create Empty GameObject named `AsteroidSpawner`.
- [ ] Attach `AsteroidSpawnerAuthoring` component.
- [ ] Assign Prefab references from `/Assets/Prefabs/`:
    - `LargeAsteroidPrefab` -> `Asteroid_Large.prefab`
    - `MediumAsteroidPrefab` -> `Asteroid_Medium.prefab`
    - `SmallAsteroidPrefab` -> `Asteroid_Small.prefab`
- [ ] Set Numerical Properties:
    - `DensityRadius = 70.0`
    - `TargetLocalDensityMin = 8`
    - `TargetLocalDensityMax = 16`
    - `BaseSpawnInterval = 2.5`
    - `FastSpawnInterval = 0.8`
    - `Seed = 1337`
- [ ] Save SubScene.
- [ ] Return to Main Scene, enter PlayMode, verify no baking errors, and confirm visual spawning via screenshots.