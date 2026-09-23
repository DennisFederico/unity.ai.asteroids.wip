# Task-005: Density-Regulated Asteroid Ring Spawner

**Status:** `READY_FOR_IMPLEMENTATION`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---

## Section 1: Objective & Scope
* **Objective:** Implement the player-centric dynamic ring spawner with local density regulation, ensuring asteroids spawn strictly out of view (beyond the camera frustum at $[35\text{m}, 50\text{m}]$), maintain an ideal gameplay density band ($[8, 16]$ asteroids within a $70\text{m}$ engagement radius), adaptively pace spawn cadence, and never crowd the player.
* **In Scope:**
  - Unmanaged components: `GlobalRandom` (singleton), `AsteroidPrefabsConfig`, and `AsteroidSpawnerData` (holding density radii and target intervals).
  - Burst-compiled unmanaged `AsteroidSpawnSystem` (`ISystem`) in `SimulationSystemGroup`:
    - Counting active asteroids within $70\text{m}$ of the player using squared distance (`math.distancesq`).
    - Enforcing strict density gating: suppresses spawning if local density $\ge 16$; accelerates spawning ($0.8\text{s}$) if density $< 8$; operates at standard pace ($2.5\text{s}$) within $[8, 15]$.
    - Instantiating Large asteroids strictly within $[35\text{m}, 50\text{m}]$ outside the player's view frustum, aiming drift vectors toward the player's vicinity via ECB.
  - `AsteroidSpawnerAuthoring` `MonoBehaviour` and Baker with `TransformUsageFlags.None`.
* **Out of Scope:**
  - Laser collisions and asteroid splitting (handled in `task-006`).
  - Out-of-bounds culling (handled in `task-002`).

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/GlobalRandom.cs`
* `Assets/Scripts/Components/AsteroidPrefabsConfig.cs`
* `Assets/Scripts/Components/AsteroidSpawnerData.cs`
* `Assets/Scripts/Systems/AsteroidSpawnSystem.cs`
* `Assets/Scripts/Authoring/AsteroidSpawnerAuthoring.cs`

### Data Contracts (Components)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    // Deterministic random generator singleton
    public struct GlobalRandom : IComponentData
    {
        public Unity.Mathematics.Random Value;
    }

    // References to baked asteroid prefabs for spawning and splitting
    public struct AsteroidPrefabsConfig : IComponentData
    {
        public Entity LargePrefab;
        public Entity MediumPrefab;
        public Entity SmallPrefab;
    }

    // Local density regulation and cadence configuration
    public struct AsteroidSpawnerData : IComponentData
    {
        public float BaseSpawnInterval;   // Standard cadence within target density band (e.g. 2.5s)
        public float FastSpawnInterval;   // Accelerated cadence when under-populated (e.g. 0.8s)
        public float CooldownTimer;       // Current countdown timer
        public float DensityRadius;       // Local engagement sampling radius (e.g. 70m)
        public float DensityRadiusSq;     // Precomputed 70^2 = 4900 for fast Burst evaluation
        public int TargetLocalDensityMin; // Lower threshold (e.g. 8 asteroids)
        public int TargetLocalDensityMax; // Maximum cap (e.g. 16 asteroids)
    }
}
```

### System Signature
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial struct AsteroidSpawnSystem : ISystem
    {
        private EntityQuery _playerQuery;
        private EntityQuery _asteroidQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalRandom>();
            state.RequireForUpdate<InfiniteMapConfig>();
            state.RequireForUpdate<AsteroidPrefabsConfig>();
            state.RequireForUpdate<AsteroidSpawnerData>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

            _playerQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            _asteroidQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AsteroidTag>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playerQuery.IsEmpty) return;

            float dt = SystemAPI.Time.DeltaTime;
            ref var spawner = ref SystemAPI.GetSingletonRW<AsteroidSpawnerData>().ValueRW;
            spawner.CooldownTimer -= dt;

            if (spawner.CooldownTimer > 0f) return;

            var playerEntity = _playerQuery.GetSingletonEntity();
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

            // 1. Measure local asteroid density within the player's engagement radius
            int localCount = 0;
            foreach (var asteroidTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<AsteroidTag>())
            {
                if (math.distancesq(asteroidTransform.ValueRO.Position, playerPos) <= spawner.DensityRadiusSq)
                {
                    localCount++;
                }
            }

            // 2. Strict density cap check: prevent overcrowding
            if (localCount >= spawner.TargetLocalDensityMax)
            {
                // Defer next evaluation by the standard interval
                spawner.CooldownTimer = spawner.BaseSpawnInterval;
                return;
            }

            // 3. Adaptive pacing: replenish fast if below min, steady if in optimal band
            if (localCount < spawner.TargetLocalDensityMin)
            {
                spawner.CooldownTimer = spawner.FastSpawnInterval;
            }
            else
            {
                spawner.CooldownTimer = spawner.BaseSpawnInterval;
            }

            ref var rand = ref SystemAPI.GetSingletonRW<GlobalRandom>().ValueRW.Value;
            var mapConfig = SystemAPI.GetSingleton<InfiniteMapConfig>();
            var prefabs = SystemAPI.GetSingleton<AsteroidPrefabsConfig>();

            if (prefabs.LargePrefab == Entity.Null) return;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 4. Generate random spawn coordinate strictly outside view frustum [ViewportBufferRadius, SpawnOuterRadius]
            float angle = rand.NextFloat(0f, math.PI * 2f);
            float radius = rand.NextFloat(mapConfig.ViewportBufferRadius, mapConfig.SpawnOuterRadius);

            float3 spawnPos = playerPos + new float3(
                math.cos(angle) * radius,
                0f,
                math.sin(angle) * radius
            );
            spawnPos.y = 0f;

            // 5. Direct velocity inward toward or across the player's vicinity
            float3 targetJitter = new float3(rand.NextFloat(-14f, 14f), 0f, rand.NextFloat(-14f, 14f));
            float3 targetPos = playerPos + targetJitter;
            float3 driftDir = math.normalize(targetPos - spawnPos);
            float speed = rand.NextFloat(2.5f, 5.0f);

            // 6. Random 3-axis angular tumble velocity (radians per sec)
            float3 angularVel = rand.NextFloat3(-2.0f, 2.0f);

            // 7. Deferred ECB instantiation
            Entity newAsteroid = ecb.Instantiate(prefabs.LargePrefab);
            ecb.SetComponent(newAsteroid, LocalTransform.FromPositionRotationScale(spawnPos, rand.NextQuaternionRotation(), 1f));
            ecb.SetComponent(newAsteroid, new DriftVelocity
            {
                Linear = driftDir * speed,
                Angular = angularVel
            });
        }
    }
}
```

### Authoring & Baker
```csharp
namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    public class AsteroidSpawnerAuthoring : MonoBehaviour
    {
        [Header("Prefab References")]
        public GameObject LargeAsteroidPrefab;
        public GameObject MediumAsteroidPrefab;
        public GameObject SmallAsteroidPrefab;

        [Header("Density Regulation")]
        [Tooltip("Sampling radius around player to regulate hazard density (meters)")]
        public float DensityRadius = 70.0f;

        [Tooltip("Minimum desired asteroid count in density radius; spawns accelerate if below this")]
        public int TargetLocalDensityMin = 8;

        [Tooltip("Maximum allowed asteroid count in density radius; spawns halt if reached")]
        public int TargetLocalDensityMax = 16;

        [Header("Spawn Cadence (Seconds)")]
        public float BaseSpawnInterval = 2.5f;
        public float FastSpawnInterval = 0.8f;
        public uint Seed = 1337u;

        class Baker : Baker<AsteroidSpawnerAuthoring>
        {
            public override void Bake(AsteroidSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new GlobalRandom
                {
                    Value = new Unity.Mathematics.Random(math.max(1u, authoring.Seed))
                });

                AddComponent(entity, new AsteroidSpawnerData
                {
                    BaseSpawnInterval = authoring.BaseSpawnInterval,
                    FastSpawnInterval = authoring.FastSpawnInterval,
                    CooldownTimer = 0.5f,
                    DensityRadius = authoring.DensityRadius,
                    DensityRadiusSq = authoring.DensityRadius * authoring.DensityRadius,
                    TargetLocalDensityMin = authoring.TargetLocalDensityMin,
                    TargetLocalDensityMax = authoring.TargetLocalDensityMax
                });

                AddComponent(entity, new AsteroidPrefabsConfig
                {
                    LargePrefab = GetEntity(authoring.LargeAsteroidPrefab, TransformUsageFlags.Dynamic),
                    MediumPrefab = GetEntity(authoring.MediumAsteroidPrefab, TransformUsageFlags.Dynamic),
                    SmallPrefab = GetEntity(authoring.SmallAsteroidPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
```

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `random-in-ecs` & `entity-spawning`
*Source: `concepts/random-in-ecs.md`, `concepts/entity-spawning.md`*
* **Frustum Margin**: `ViewportBufferRadius` ($35\text{m}$) ensures entities never spawn within the camera's visible screen boundary (diagonal $\approx 24\text{m}$).
* **Local Density Gating**: Always evaluate density using squared distance against `DensityRadiusSq` to prevent unbounded accumulation around a stationary or slow player.
* **Burst Randomness**: Use `Unity.Mathematics.Random` with a non-zero initial seed (`math.max(1u, seed)`).

### Anti-Pattern Warnings
* ⛔ **DO NOT RELY ON GLOBAL ENTITY COUNT**: In an infinite world, counting all entities across the entire world breaks when entities are scattered hundreds of meters away. Always measure **local density**.
* ⛔ **DO NOT SPAWN INSIDE CAMERA VIEW**: The minimum spawn radius must strictly exceed camera viewport bounds.
* ⛔ **NO `math.sqrt` IN DENSITY LOOP**: Compare `math.distancesq` directly with `DensityRadiusSq`.

---

## Section 4: Developer Definition of Done (DoD)
- [x] Code implemented strictly within Section 2 target paths.
- [x] Spawner respects `TargetLocalDensityMax` (halts spawns when $\ge 16$ asteroids within $70\text{m}$).
- [x] Spawner accelerates cadence to `FastSpawnInterval` when density $< 8$.
- [x] Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = **0 errors**).
- [x] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [ ] **Density Guard**: Spawning halts when asteroid count reaches `TargetLocalDensityMax`.
- [ ] **Burst Compliance**: `AsteroidSpawnSystem` is fully Burst-compiled (`[BurstCompile]`).
- [ ] **Zero Popping**: Spawn positions are strictly beyond the camera frustum diagonal.

---

## Actionable Implementation Checklist
- [x] Step 1: Create `GlobalRandom.cs`, `AsteroidPrefabsConfig.cs`, and `AsteroidSpawnerData.cs` in `Assets/Scripts/Components/`.
- [x] Step 2: Implement `AsteroidSpawnSystem.cs` in `Assets/Scripts/Systems/`.
- [x] Step 3: Implement `AsteroidSpawnerAuthoring.cs` in `Assets/Scripts/Authoring/`.
- [x] Step 4: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.
