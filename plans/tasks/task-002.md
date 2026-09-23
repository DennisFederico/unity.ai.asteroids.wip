# Task-002: Infinite Map Culling & Camera Tracking

**Status:** `READY_FOR_IMPLEMENTATION`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---\n
## Section 1: Objective & Scope
* **Objective:** Implement the infinite map culling and isometric camera tracking systems, establishing the configurable player-centric despawn bubble ($50\times$ the area of the spawn zone) and destroying out-of-bounds asteroids via `BeginSimulationEntityCommandBufferSystem.Singleton`.
* **In Scope:**
  - Unmanaged component: `InfiniteMapConfig` (singleton holding configurable spawn radii, despawn radius, and precomputed squared despawn distance).
  - Burst-compiled unmanaged `AsteroidCullingSystem` (`ISystem`) in `SimulationSystemGroup` destroying any asteroid whose distance from the player exceeds `DespawnRadius`.
  - Managed `CameraFollowBridgeSystem` (`SystemBase`) in `PresentationSystemGroup` smoothly tracking the player ship across unbounded space while preserving the isometric perspective.
  - `InfiniteMapAuthoring` `MonoBehaviour` and Baker with `TransformUsageFlags.None` computing the $50\times$ area boundary.
* **Out of Scope:**
  - Toroidal boundary wrapping (discarded in favor of infinite map).
  - Spawning asteroids (handled in `task-005`).

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/InfiniteMapConfig.cs`
* `Assets/Scripts/Systems/AsteroidCullingSystem.cs`
* `Assets/Scripts/Systems/CameraFollowBridgeSystem.cs`
* `Assets/Scripts/Authoring/InfiniteMapAuthoring.cs`

### Data Contracts (Components)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;

    // Singleton holding infinite map bubble radii
    public struct InfiniteMapConfig : IComponentData
    {
        public float ViewportBufferRadius; // Inner ring edge strictly outside camera frustum (e.g. 35m)
        public float SpawnOuterRadius;     // Outer ring edge (e.g. 50m)
        public float DespawnRadius;        // Out-of-bounds distance (e.g. sqrt(50) * 50m ≈ 353.55m)
        public float DespawnRadiusSq;      // Precomputed for fast Burst comparison (353.55^2 ≈ 125,000)
    }
}
```

### System Signatures

#### 1. Asteroid Culling System (`AsteroidCullingSystem.cs`)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AsteroidCullingSystem : ISystem
    {
        private EntityQuery _playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InfiniteMapConfig>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _playerQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playerQuery.IsEmpty) return;

            var playerEntity = _playerQuery.GetSingletonEntity();
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
            var config = SystemAPI.GetSingleton<InfiniteMapConfig>();

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, entity) in 
                SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<AsteroidTag>()
                         .WithEntityAccess())
            {
                float distSq = math.distancesq(transform.ValueRO.Position, playerPos);
                if (distSq > config.DespawnRadiusSq)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
```

#### 2. Camera Follow Bridge (`CameraFollowBridgeSystem.cs`)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class CameraFollowBridgeSystem : SystemBase
    {
        private Vector3 _cameraOffset;
        private bool _offsetInitialized;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnUpdate()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            foreach (var transform in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<PlayerTag>())
            {
                Vector3 playerPos = transform.ValueRO.Position;

                if (!_offsetInitialized)
                {
                    // Compute initial isometric offset relative to ship starting point
                    _cameraOffset = mainCam.transform.position - playerPos;
                    if (_cameraOffset.sqrMagnitude < 0.01f)
                    {
                        // Default high isometric angle if camera started at origin
                        _cameraOffset = new Vector3(-20f, 30f, -20f);
                    }
                    _offsetInitialized = true;
                }

                // Smoothly update camera position tracking the player
                Vector3 targetCamPos = playerPos + _cameraOffset;
                mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, targetCamPos, SystemAPI.Time.DeltaTime * 15f);
            }
        }
    }
}
```

### Authoring & Baker (`InfiniteMapAuthoring.cs`)
```csharp
namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    public class InfiniteMapAuthoring : MonoBehaviour
    {
        [Header("Spawn Ring Dimensions")]
        [Tooltip("Minimum safe spawn distance outside the camera frustum")]
        public float ViewportBufferRadius = 35.0f;

        [Tooltip("Outer limit of the spawn ring")]
        public float SpawnOuterRadius = 50.0f;

        [Header("Out-of-Bounds Culling")]
        [Tooltip("Area multiplier for out-of-bounds destruction (default: 50x area)")]
        public float OutOfBoundsAreaMultiplier = 50.0f;

        class Baker : Baker<InfiniteMapAuthoring>
        {
            public override void Bake(InfiniteMapAuthoring authoring)
            {
                // Pure data singleton entity
                Entity entity = GetEntity(TransformUsageFlags.None);

                // Area = pi * r^2. If Area_oob = multiplier * Area_spawn,
                // then r_oob = sqrt(multiplier) * r_spawn.
                float despawnRadius = Mathf.Sqrt(authoring.OutOfBoundsAreaMultiplier) * authoring.SpawnOuterRadius;

                AddComponent(entity, new InfiniteMapConfig
                {
                    ViewportBufferRadius = authoring.ViewportBufferRadius,
                    SpawnOuterRadius = authoring.SpawnOuterRadius,
                    DespawnRadius = despawnRadius,
                    DespawnRadiusSq = despawnRadius * despawnRadius
                });
            }
        }
    }
}
```

### Visual & Scene Rigging Contracts
*(Defines how the entity is assembled in the SubScene)*
* **Target Scope**: `SubScene` (`Assets/Scenes/Asteroids_entities.unity`)
* **Entity Visual Type**:
  - [ ] **Visual Entity (Prefab-Backed)**:
  - [x] **Pure Data / Non-Visual Entity (Explicitly Empty)**:
    - **Entity Name**: `InfiniteMapManager`
    - **Entity Purpose**: Singleton configuration provider for infinite map spawn buffer and out-of-bounds culling bubble ($50\times$ area).
    - **Mesh Requirement**: `None (Primitive Empty GameObject)`
    - **Rationale**: `InfiniteMapConfig` is a pure unmanaged configuration struct baked with `TransformUsageFlags.None`. It contains no runtime geometry, renderers, or physics colliders.
* **Main Scene Bridge**:
  - `CameraFollowBridgeSystem` operates in `Scope 1: Main Scene` (`Assets/Scenes/Asteroids.unity`), smoothly adjusting `Camera.main` to follow the player entity's `LocalToWorld` coordinate.

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `singletons`, `ecb-best-practices`, & `hybrid-ecs`
*Source: `concepts/singletons.md`, `concepts/ecb-best-practices.md`, `concepts/hybrid-ecs.md`*
* **Dynamic Bubble**: The culling boundary travels with the player, eliminating map edge limits.
* **Precomputed Squared Distance**: Always compare `distSq > DespawnRadiusSq` in Burst systems to avoid expensive `math.sqrt()` calculations.
* **Record-and-Forget**: Defer entity destruction to `ecb.DestroyEntity(entity)`. Never call `ecb.Playback()`.

### Anti-Pattern Warnings
* ⛔ **DO NOT USE `Camera.main` IN `ISystem`**: Unmanaged Burst code cannot touch `UnityEngine.Camera`. Camera tracking belongs in `SystemBase` under `PresentationSystemGroup`.
* ⛔ **DO NOT COMPUTE SQRT PER ENTITY**: Use `math.distancesq` and compare against `DespawnRadiusSq`.
* ⛔ **DO NOT CALL `.Playback()` OR `.Dispose()` ON ECB**: Triggers runtime `InvalidOperationException`.

---

## Section 4: Developer Definition of Done (DoD)
- [ ] Code implemented strictly within Section 2 paths.
- [ ] `InfiniteMapConfig` singleton bakes `DespawnRadius` equal to $\sqrt{50} \times \text{SpawnOuterRadius}$ by default ($50\times$ area).
- [ ] Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = **0 errors**).
- [ ] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [ ] **Data Footprint**: `InfiniteMapAuthoring` uses `TransformUsageFlags.None`.
- [ ] **Burst Compliance**: `AsteroidCullingSystem` is fully Burst-compiled.
- [ ] **Camera Tracking**: Isometric camera follows player smoothly in infinite space without jitter.
- [ ] **Visual & Rigging Audit**: Verified `InfiniteMapManager` is an explicit Primitive Empty GameObject in `SubScene` with `TransformUsageFlags.None`.

---

## Actionable Implementation Checklist
- [x] Step 1: Create `InfiniteMapConfig.cs` in `Assets/Scripts/Components/`.
- [x] Step 2: Implement `AsteroidCullingSystem.cs` in `Assets/Scripts/Systems/`.
- [x] Step 3: Implement `CameraFollowBridgeSystem.cs` in `Assets/Scripts/Systems/`.
- [x] Step 4: Implement `InfiniteMapAuthoring.cs` in `Assets/Scripts/Authoring/`.
- [x] Step 5: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.

---

## Reviewer Sign-off & Verdict
* **Review Date:** `YYYY-MM-DD`
* **Verdict:** `PENDING`
* **Findings:**
  * [Notes or verification logs here]
