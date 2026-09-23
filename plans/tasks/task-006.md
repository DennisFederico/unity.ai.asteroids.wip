# Task-006: Laser-Asteroid Collision & Splitting System

**Status:** `READY_FOR_IMPLEMENTATION`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---

## Section 1: Objective & Scope
* **Objective:** Implement the Burst-compiled collision detection system that performs bounding sphere intersection tests between lasers and asteroids, destroys the colliding laser, splits multi-tier asteroids into fragments via `BeginSimulationEntityCommandBufferSystem.Singleton`, and increments the `GameScore` singleton.
* **In Scope:**
  - Unmanaged component: `GameScore` (singleton).
  - Burst-compiled unmanaged `LaserAsteroidCollisionSystem` (`ISystem`) in `SimulationSystemGroup` executing sphere distance tests.
  - Tier-based splitting hierarchy execution:
    - Tier 3 (Large) $\rightarrow$ Instantiates 2x Tier 2 (Medium) with divergent velocity vectors; awards 20 points.
    - Tier 2 (Medium) $\rightarrow$ Instantiates 2x Tier 1 (Small) with divergent velocity vectors; awards 50 points.
    - Tier 1 (Small) $\rightarrow$ Fully destroyed; awards 100 points.
  - `GameScoreAuthoring` `MonoBehaviour` and Baker with `TransformUsageFlags.None`.
* **Out of Scope:**
  - UI canvas presentation (handled in `task-007`).
  - Audio and particle VFX.

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/GameScore.cs`
* `Assets/Scripts/Systems/LaserAsteroidCollisionSystem.cs`
* `Assets/Scripts/Authoring/GameScoreAuthoring.cs`

### Data Contracts (Components)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;

    // Singleton component storing active score
    public struct GameScore : IComponentData
    {
        public int CurrentScore;
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
    using Unity.Collections;
    using Unity.Burst;

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LaserMovementSystem))]
    [UpdateAfter(typeof(AsteroidDriftSystem))]
    public partial struct LaserAsteroidCollisionSystem : ISystem
    {
        private EntityQuery _laserQuery;
        private EntityQuery _asteroidQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AsteroidPrefabsConfig>();
            state.RequireForUpdate<GameScore>();

            _laserQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<LaserTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<ProjectileData>()
            );

            _asteroidQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AsteroidTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<AsteroidData>(),
                ComponentType.ReadOnly<DriftVelocity>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int laserCount = _laserQuery.CalculateEntityCount();
            int asteroidCount = _asteroidQuery.CalculateEntityCount();

            if (laserCount == 0 || asteroidCount == 0) return;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var prefabs = SystemAPI.GetSingleton<AsteroidPrefabsConfig>();
            ref var score = ref SystemAPI.GetSingletonRW<GameScore>().ValueRW;

            using var laserEntities = _laserQuery.ToEntityArray(Allocator.Temp);
            using var laserTransforms = _laserQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using var laserProjectiles = _laserQuery.ToComponentDataArray<ProjectileData>(Allocator.Temp);

            using var asteroidEntities = _asteroidQuery.ToEntityArray(Allocator.Temp);
            using var asteroidTransforms = _asteroidQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using var asteroidData = _asteroidQuery.ToComponentDataArray<AsteroidData>(Allocator.Temp);
            using var asteroidDrifts = _asteroidQuery.ToComponentDataArray<DriftVelocity>(Allocator.Temp);

            // Track hit entities in this tick to prevent multi-hit race conditions
            var destroyedLasers = new NativeParallelHashSet<Entity>(laserCount, Allocator.Temp);
            var destroyedAsteroids = new NativeParallelHashSet<Entity>(asteroidCount, Allocator.Temp);

            for (int l = 0; l < laserCount; l++)
            {
                Entity laserEntity = laserEntities[l];
                float3 laserPos = laserTransforms[l].Position;
                float laserRadius = laserProjectiles[l].Radius;

                for (int a = 0; a < asteroidCount; a++)
                {
                    Entity asteroidEntity = asteroidEntities[a];
                    if (destroyedAsteroids.Contains(asteroidEntity)) continue;

                    float3 asteroidPos = asteroidTransforms[a].Position;
                    float asteroidRadius = asteroidData[a].Radius;

                    float totalRadius = laserRadius + asteroidRadius;
                    if (math.distancesq(laserPos, asteroidPos) <= (totalRadius * totalRadius))
                    {
                        // 1. Mark both as destroyed in this pass
                        destroyedLasers.Add(laserEntity);
                        destroyedAsteroids.Add(asteroidEntity);

                        // 2. Record laser destruction
                        ecb.DestroyEntity(laserEntity);

                        // 3. Award score
                        score.CurrentScore += asteroidData[a].ScoreValue;

                        // 4. Handle splitting
                        int currentTier = asteroidData[a].Tier;
                        Entity childPrefab = Entity.Null;

                        if (currentTier == 3) childPrefab = prefabs.MediumPrefab;
                        else if (currentTier == 2) childPrefab = prefabs.SmallPrefab;

                        if (childPrefab != Entity.Null && asteroidData[a].SplitCount > 0)
                        {
                            float3 parentVel = asteroidDrifts[a].Linear;
                            float baseSpeed = math.max(2.5f, math.length(parentVel)) * 1.25f;

                            for (int i = 0; i < asteroidData[a].SplitCount; i++)
                            {
                                float angleOffset = (i == 0) ? 0.75f : -0.75f;
                                quaternion rotOffset = quaternion.RotateY(angleOffset);
                                float3 childDir = math.normalize(math.mul(rotOffset, parentVel));

                                Entity child = ecb.Instantiate(childPrefab);
                                ecb.SetComponent(child, LocalTransform.FromPositionRotationScale(
                                    asteroidPos,
                                    asteroidTransforms[a].Rotation,
                                    1f
                                ));
                                ecb.SetComponent(child, new DriftVelocity
                                {
                                    Linear = childDir * baseSpeed,
                                    Angular = asteroidDrifts[a].Angular * 1.4f
                                });
                            }
                        }

                        // 5. Record parent asteroid destruction
                        ecb.DestroyEntity(asteroidEntity);
                        break; // Laser hit one asteroid, proceed to next laser
                    }
                }
            }
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

    public class GameScoreAuthoring : MonoBehaviour
    {
        public int StartingScore = 0;

        class Baker : Baker<GameScoreAuthoring>
        {
            public override void Bake(GameScoreAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GameScore { CurrentScore = authoring.StartingScore });
            }
        }
    }
}
```

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `ecb-best-practices`
*Source: `concepts/ecb-best-practices.md`*
* **Single-Pass Safety**: In unmanaged collision queries, track destroyed entities in a `NativeParallelHashSet<Entity>` with `Allocator.Temp` to avoid recording multiple destructions or splits against the same entity in a single frame.
* **Record-and-Forget**: Use `ecb.DestroyEntity` and `ecb.Instantiate` without calling `.Playback()`.

### Anti-Pattern Warnings
* ⛔ **DO NOT CALL `.Playback()` ON ECB**: Triggers runtime `InvalidOperationException`.
* ⛔ **DO NOT MODIFY ENTITIES DIRECTLY IN QUERIES**: Direct `EntityManager.DestroyEntity()` during system updates causes structural sync syncpoints and invalidates queries.
* ⛔ **REMEMBER TO DISPOSE TEMP COLLECTIONS**: Use `using var` for `NativeArray` allocations.

---

## Section 4: Developer Definition of Done (DoD)
- [ ] Code implemented strictly within Section 2 target paths.
- [ ] Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = **0 errors**).
- [ ] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [ ] **Burst Compilation**: `LaserAsteroidCollisionSystem` is fully Burst-compiled.
- [ ] **Memory Hygiene**: All `NativeArray` and `NativeParallelHashSet` instances are scoped in `using` blocks.
- [ ] **Score Propagation**: `GameScore` singleton is mutated directly via `SystemAPI.GetSingletonRW`.

---

## Actionable Implementation Checklist
- [ ] Step 1: Create `GameScore.cs` in `Assets/Scripts/Components/`.
- [ ] Step 2: Implement `LaserAsteroidCollisionSystem.cs` in `Assets/Scripts/Systems/`.
- [ ] Step 3: Implement `GameScoreAuthoring.cs` in `Assets/Scripts/Authoring/`.
- [ ] Step 4: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.
