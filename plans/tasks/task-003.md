# Task-003: Laser Projectiles & Shooting Mechanism

**Status:** `READY_FOR_IMPLEMENTATION`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---\n
## Section 1: Objective & Scope
* **Objective:** Implement the laser weapon firing, high-velocity linear movement, and lifetime expiration pipeline using `BeginSimulationEntityCommandBufferSystem.Singleton` for record-and-forget deferred instantiation and destruction.
* **In Scope:**
  - Unmanaged components: `LaserTag`, `ProjectileData`, `Lifetime`, and `LaserSpawner`.
  - Burst-compiled `LaserShootingSystem` (`ISystem`) polling `PlayerInput`, enforcing firing rate, and recording laser prefab instantiation to ECB.
  - Burst-compiled `LaserMovementSystem` (`ISystem`) translating laser entities along forward trajectory on the $XZ$ plane ($Y=0$).
  - Burst-compiled `LaserLifetimeSystem` (`ISystem`) decrementing lifetime and recording destruction to ECB upon expiration.
  - `LaserAuthoring` `MonoBehaviour` and nested Baker attached to `FX_Laser_Bullet_01.prefab`.
  - Updating `PlayerAuthoring` to configure and bake `LaserSpawner`.
* **Out of Scope:**
  - Asteroid collision detection and fragment splitting (handled in `task-006`).
  - Audio playback on firing.

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/LaserTag.cs`
* `Assets/Scripts/Components/ProjectileData.cs`
* `Assets/Scripts/Components/Lifetime.cs`
* `Assets/Scripts/Components/LaserSpawner.cs`
* `Assets/Scripts/Systems/LaserShootingSystem.cs`
* `Assets/Scripts/Systems/LaserMovementSystem.cs`
* `Assets/Scripts/Systems/LaserLifetimeSystem.cs`
* `Assets/Scripts/Authoring/LaserAuthoring.cs`

### Data Contracts (Components)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    // Tag identifying laser projectile entities
    public struct LaserTag : IComponentData { }

    // Kinematic motion and bounding volume for collision
    public struct ProjectileData : IComponentData
    {
        public float3 Velocity;
        public float Speed;
        public float Radius; // Bounding radius for sphere intersection (e.g. 0.3f)
    }

    // Time to live in seconds
    public struct Lifetime : IComponentData
    {
        public float Value;
    }

    // Firing configuration attached to the player ship
    public struct LaserSpawner : IComponentData
    {
        public Entity LaserPrefab;
        public float FireRate;       // Shots per second (e.g. 5.0f)
        public float CooldownTimer;  // Internal countdown
        public float3 MuzzleOffset;  // Local offset relative to ship center
    }
}
```

### System Signatures

#### 1. Shooting System (`LaserShootingSystem.cs`)
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
    public partial struct LaserShootingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (spawner, transform, input) in 
                SystemAPI.Query<RefRW<LaserSpawner>, RefRO<LocalTransform>, RefRO<PlayerInput>>())
            {
                spawner.ValueRW.CooldownTimer -= dt;

                bool wantsToFire = input.ValueRO.FireHeld || input.ValueRO.FireTriggered;
                if (wantsToFire && spawner.ValueRO.CooldownTimer <= 0f && spawner.ValueRO.LaserPrefab != Entity.Null)
                {
                    spawner.ValueRW.CooldownTimer = 1f / math.max(0.01f, spawner.ValueRO.FireRate);

                    // 1. Deferred instantiation via ECB
                    Entity laser = ecb.Instantiate(spawner.ValueRO.LaserPrefab);

                    // 2. Calculate muzzle spawn point & forward trajectory
                    float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                    float3 spawnPos = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, spawner.ValueRO.MuzzleOffset);
                    spawnPos.y = 0f;

                    float speed = 35.0f;
                    ecb.SetComponent(laser, LocalTransform.FromPositionRotationScale(spawnPos, transform.ValueRO.Rotation, 1f));
                    ecb.SetComponent(laser, new ProjectileData
                    {
                        Velocity = forward * speed,
                        Speed = speed,
                        Radius = 0.3f
                    });
                    ecb.SetComponent(laser, new Lifetime { Value = 1.8f });
                }
            }
        }
    }
}
```

#### 2. Movement System (`LaserMovementSystem.cs`)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Burst;

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LaserMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, projectile) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<ProjectileData>>()
                         .WithAll<LaserTag>())
            {
                var pos = transform.ValueRO.Position + (projectile.ValueRO.Velocity * dt);
                pos.y = 0f;
                transform.ValueRW.Position = pos;
            }
        }
    }
}
```

#### 3. Lifetime System (`LaserLifetimeSystem.cs`)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Burst;

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LaserLifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (lifetime, entity) in 
                SystemAPI.Query<RefRW<Lifetime>>()
                         .WithEntityAccess())
            {
                lifetime.ValueRW.Value -= dt;
                if (lifetime.ValueRO.Value <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
```

### Authoring & Bakers

#### `LaserAuthoring.cs`
```csharp
namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    public class LaserAuthoring : MonoBehaviour
    {
        public float Speed = 35.0f;
        public float LifetimeDuration = 1.8f;
        public float CollisionRadius = 0.3f;

        class Baker : Baker<LaserAuthoring>
        {
            public override void Bake(LaserAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new LaserTag());
                AddComponent(entity, new ProjectileData
                {
                    Velocity = float3.zero,
                    Speed = authoring.Speed,
                    Radius = authoring.CollisionRadius
                });
                AddComponent(entity, new Lifetime
                {
                    Value = authoring.LifetimeDuration
                });
            }
        }
    }
}
```

### Visual & Scene Rigging Contracts
*(Defines how the entity and its prefab are assembled)*
* **Target Scope**: Prefab Library / `SubScene` (`Assets/Scenes/Asteroids_entities.unity`)
* **Entity Visual Type**:
  - [x] **Visual Entity (Prefab-Backed)**:
    - **Visual Prefab Asset Path**: `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab`
    - **Rigging Mode**: `Prefab Asset (Dynamic ECB Instantiation)`
    - **Initial Transform**: Spawned dynamically at ship muzzle position facing ship orientation on $Y=0$.
    - **Authoring Component**: `LaserAuthoring` attached directly to `FX_Laser_Bullet_01.prefab`.
  - [ ] **Pure Data / Non-Visual Entity (Explicitly Empty)**:
* **SubScene Wiring Reference**:
  - **Host GameObject**: `PlayerShip` in `Asteroids_entities.unity`
  - **Component**: `PlayerAuthoring`
  - **Serialized Field**: `LaserPrefab` -> `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab`

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `ecb-best-practices`
*Source: `concepts/ecb-best-practices.md`*
* **Record-and-Forget**: Obtain system-managed ECB from `BeginSimulationEntityCommandBufferSystem.Singleton`. Record commands (`Instantiate`, `SetComponent`, `DestroyEntity`) and exit.
* **Deferred Placeholder Chaining**: Deferred entity placeholders returned from `ecb.Instantiate()` are safely passed to subsequent `ecb.SetComponent()` calls within the same buffer.
* **Lifecycle Rules**: Unity's command buffer system executes playback and frees buffer memory automatically at the frame boundary.

### Anti-Pattern Warnings
* ⛔ **DO NOT CALL `.Playback()` OR `.Dispose()`**: Calling `.Playback()` or `.Dispose()` on system-managed command buffers triggers fatal runtime `InvalidOperationException` crashes.
* ⛔ **NO IMMEDIATE COMPONENT READS**: Never call `SystemAPI.GetComponent<T>(laser)` on a deferred placeholder entity inside the same frame.
* ⛔ **NO `EntityManager` MUTATIONS INSIDE QUERIES**: Direct structural changes inside `foreach` queries invalidate chunk enumerators.

---

## Section 4: Developer Definition of Done (DoD)
- [ ] Code implemented strictly within Section 2 target paths.
- [ ] `PlayerAuthoring.cs` extended with `LaserPrefab`, `FireRate`, `MuzzleOffset` fields and `LaserSpawner` component baking.
- [ ] `LaserAuthoring` attached to `FX_Laser_Bullet_01.prefab`.
- [ ] Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = **0 errors**).
- [ ] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [ ] **ECB Safety**: Zero calls to `.Playback()` or `.Dispose()`.
- [ ] **Burst Compilation**: `LaserShootingSystem`, `LaserMovementSystem`, and `LaserLifetimeSystem` all have `[BurstCompile]`.
- [ ] **Execution Group**: All systems update in `SimulationSystemGroup`.
- [ ] **Visual & Rigging Audit**: Verified `FX_Laser_Bullet_01.prefab` has `LaserAuthoring` attached and `TransformUsageFlags.Dynamic` baked.

---

## Actionable Implementation Checklist
- [ ] Step 1: Create `LaserTag.cs`, `ProjectileData.cs`, `Lifetime.cs`, and `LaserSpawner.cs` in `Assets/Scripts/Components/`.
- [ ] Step 2: Implement `LaserMovementSystem.cs` and `LaserLifetimeSystem.cs` in `Assets/Scripts/Systems/`.
- [ ] Step 3: Implement `LaserShootingSystem.cs` in `Assets/Scripts/Systems/`.
- [ ] Step 4: Implement `LaserAuthoring.cs` in `Assets/Scripts/Authoring/`.
- [ ] Step 5: Update `PlayerAuthoring.cs` to bake `LaserSpawner`.
- [ ] Step 6: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.

---

## Reviewer Sign-off & Verdict
* **Review Date:** `YYYY-MM-DD`
* **Verdict:** `PENDING`
* **Findings:**
  * [Notes or verification logs here]
