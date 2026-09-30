# Tier-1 Authoring Recipe: Asteroid Prefab Creation (Task-004)

**Objective:** Rig the visual asteroid prefabs for all three size tiers, configuring their component data to be ready for spawning via the `AsteroidSpawner`.

---

## Scope 1: Main Scene Hierarchy (`Assets/Scenes/Asteroids.unity`)
*No changes required in the managed scene.*

## Scope 2: SubScene Hierarchy (`Assets/Scenes/Asteroids_entities.unity`)
*Note: We do NOT place these asteroids directly into the SubScene hierarchy. They are being prepared as independent Library Prefabs.*

| Entity Role | Visual Asset Path | Target Prefab Name | Configured Component | Properties / Values |
| :--- | :--- | :--- | :--- | :--- |
| **Large Asteroid (Tier 3)** | `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Environment/SM_Env_Asteroid_Rock_01.prefab` | `Asteroid_Large.prefab` | `AsteroidAuthoring` | `Tier=3`, `Radius=2.2f`, `ScoreValue=20`, `SplitCount=2` |
| **Medium Asteroid (Tier 2)** | `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Environment/SM_Env_Asteroid_Rock_02.prefab` | `Asteroid_Medium.prefab` | `AsteroidAuthoring` | `Tier=2`, `Radius=1.1f`, `ScoreValue=50`, `SplitCount=2` |
| **Small Asteroid (Tier 1)** | `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Environment/SM_Env_Asteroid_Rock_04.prefab` | `Asteroid_Small.prefab` | `AsteroidAuthoring` | `Tier=1`, `Radius=0.5f`, `ScoreValue=100`, `SplitCount=0` |

---

## Implementation Plan (Tier-2 Execution)

### Phase 1: Discovery & Setup
1. Select Unity Editor instance (Port 7890).
2. Open SubScene asset: `Assets/Scenes/Asteroids_entities.unity`.

### Phase 2: Prefab Assembly Loop
For each tier (**Large**, **Medium**, **Small**):
1. **Instantiate**: Spawn the visual source prefab from `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Environment/`.
2. **Attach Authoring**: Add `Asteroids.Core.AsteroidAuthoring` component.
3. **Configure Data**: Set `Tier`, `Radius`, `ScoreValue`, and `SplitCount` per the recipe table above.
4. **Library Storage**: Save this configured object as an individual Prefab in `Assets/Prefabs/[PrefabName].prefab`.
5. **Cleanup**: Delete the temporary GameObject from the SubScene.

### Phase 3: Validation
1. Verify files exist at:
    - `Assets/Prefabs/Asteroid_Large.prefab`
    - `Assets/Prefabs/Asteroid_Medium.prefab`
    - `Assets/Prefabs/Asteroid_Small.prefab`
2. Check Compilation: Confirm zero errors in the console.

---
**Status:** Awaiting User Approval to execute MCP commands.