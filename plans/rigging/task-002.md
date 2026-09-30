# Task-002: Authoring Recipe — Infinite Map Culling & Camera Tracking

**Task File:** [`plans/tasks/task-002.md`](plans/tasks/task-002.md)  
**Implementation Plan:** [`plans/IMPLEMENTATION_PLAN.md`](plans/IMPLEMENTATION_PLAN.md)  
**Date:** 2026-09-30

---

## 1. Scope Summary

| Scope | GameObject | Entity Type | Visual Prefab | Authoring Component |
| :--- | :--- | :--- | :--- | :--- |
| **SubScene** | `InfiniteMapManager` | Pure Data (Non-Visual) | `None (Primitive Empty)` | `InfiniteMapAuthoring` |
| **Main Scene** | `Main Camera` | Managed | N/A | `CameraFollowBridgeSystem` (runtime, no scene component) |

### Entity Purpose
**`InfiniteMapManager`** — Singleton configuration provider for the infinite map spawn buffer and out-of-bounds culling bubble ($50\times$ area multiplier). The baked `InfiniteMapConfig` component contains:
- `ViewportBufferRadius`: 35.0m (minimum safe spawn distance outside camera frustum)
- `SpawnOuterRadius`: 50.0m (outer limit of spawn ring)
- `DespawnRadius`: ~353.55m ($\sqrt{50} \times 50$)
- `DespawnRadiusSq`: ~125,000 (precomputed squared distance)

---

## 2. Scope 1: Main Scene Hierarchy (`Assets/Scenes/Asteroids.unity`)

```
[Main Scene: Assets/Scenes/Asteroids.unity]
├── Main Camera (Camera, AudioListener, UniversalAdditionalCameraData)
│     └── CameraFollowBridgeSystem (runtime SystemBase, PresentationSystemGroup)
├── Directional Light (Light, UniversalAdditionalLightData)
└── Entities (GameObject with Unity.Scenes.SubScene component)
      └── Linked _SceneAsset -> Assets/Scenes/Asteroids_entities.unity
```

**Changes Required:** None. Main Scene is already correctly configured from task-001.

---

## 3. Scope 2: SubScene Hierarchy (`Assets/Scenes/Asteroids_entities.unity`)

```
[SubScene: Assets/Scenes/Asteroids_entities.unity]
├── PlayerShip (Prefab Instance: SM_Ship_Fighter_02.prefab) [from task-001]
│     ├── Transform: Position(0, 0, 0), Rotation(0, 0, 0), Scale(1, 1, 1)
│     └── PlayerAuthoring (ThrustAcceleration: 35, MaxSpeed: 12, Drag: 2, RotationDamping: 18)
└── InfiniteMapManager (Primitive Empty GameObject) [NEW — task-002]
      ├── Transform: Position(0, 0, 0), Rotation(0, 0, 0), Scale(1, 1, 1)
      └── InfiniteMapAuthoring
            ├── ViewportBufferRadius: 35.0f
            ├── SpawnOuterRadius: 50.0f
            └── OutOfBoundsAreaMultiplier: 50.0f
```

### Component Wiring Details (SubScene Scope)

| Target Scope | GameObject Path | Component to Add | Property | Value |
| :--- | :--- | :--- | :--- | :--- |
| **SubScene** | `InfiniteMapManager` | `InfiniteMapAuthoring` | `ViewportBufferRadius` | `35.0f` |
| **SubScene** | `InfiniteMapManager` | `InfiniteMapAuthoring` | `SpawnOuterRadius` | `50.0f` |
| **SubScene** | `InfiniteMapManager` | `InfiniteMapAuthoring` | `OutOfBoundsAreaMultiplier` | `50.0f` |

### ECS Component Output (Baked)

| Baked Component | Namespace | Memory | Usage Flags |
| :--- | :--- | :--- | :--- |
| `InfiniteMapConfig` | `Asteroids.Core` | 16 bytes | `TransformUsageFlags.None` (singleton) |

---

## 4. Runtime Systems (No Scene Assembly Required)

| System | Type | Group | File |
| :--- | :--- | :--- | :--- |
| `AsteroidCullingSystem` | `ISystem` (Burst) | `SimulationSystemGroup` | [`Systems/AsteroidCullingSystem.cs`](Assets/Scripts/Systems/AsteroidCullingSystem.cs) |
| `CameraFollowBridgeSystem` | `SystemBase` | `PresentationSystemGroup` | [`Systems/CameraFollowBridgeSystem.cs`](Assets/Scripts/Systems/CameraFollowBridgeSystem.cs) |

These systems are already implemented and will operate at runtime without additional scene assembly.

---

## 5. Execution Checklist

- [ ] Create `InfiniteMapManager` GameObject (Primitive Empty) in SubScene
- [ ] Attach `InfiniteMapAuthoring` component to `InfiniteMapManager`
- [ ] Set `ViewportBufferRadius = 35.0f`
- [ ] Set `SpawnOuterRadius = 50.0f`
- [ ] Set `OutOfBoundsAreaMultiplier = 50.0f`
- [ ] Save SubScene
- [ ] Verify compilation (0 errors, 0 Burst warnings)
- [ ] PlayMode verification: check baking and camera tracking
