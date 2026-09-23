# Task-004: Asteroid Drift, 3-Axis Tumble & Data Layout

**Status:** `READY_FOR_IMPLEMENTATION`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---

## Section 1: Objective & Scope
* **Objective:** Establish the asteroid data schema, 3-axis rotational tumbling kinematics, linear drift on the $XZ$ plane ($Y=0$) through infinite space, and authoring baker for asteroid prefabs.
* **In Scope:**
  - Unmanaged components: `AsteroidTag`, `AsteroidData` (Tier, Radius, ScoreValue, SplitCount), and `DriftVelocity` (3D linear and angular velocity vectors).
  - Burst-compiled unmanaged `AsteroidDriftSystem` (`ISystem`) in `SimulationSystemGroup` translating positions and compounding 3-axis rotation.
  - `AsteroidAuthoring` `MonoBehaviour` and nested `Baker<AsteroidAuthoring>` supporting Large, Medium, and Small asteroid prefabs.
* **Out of Scope:**
  - Dynamic ring spawner (handled in `task-005`).
  - Out-of-bounds culling (handled in `task-002`).
  - Laser hit detection and fragment splitting (handled in `task-006`).

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/AsteroidTag.cs`
* `Assets/Scripts/Components/AsteroidData.cs`
* `Assets/Scripts/Components/DriftVelocity.cs`
* `Assets/Scripts/Systems/AsteroidDriftSystem.cs`
* `Assets/Scripts/Authoring/AsteroidAuthoring.cs`

### Data Contracts (Components)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    // Tag identifying asteroid entities
    public struct AsteroidTag : IComponentData { }

    // Hierarchy tier, collision geometry, and gameplay values
    public struct AsteroidData : IComponentData
    {
        public int Tier;       // 3 = Large, 2 = Medium, 1 = Small
        public float Radius;   // Bounding sphere radius: Large ~2.2f, Medium ~1.1f, Small ~0.5f
        public int ScoreValue; // Points awarded: Large 20, Medium 50, Small 100
        public int SplitCount; // Number of children spawned when destroyed (2 for Large/Medium, 0 for Small)
    }

    // Kinematic motion vectors
    public struct DriftVelocity : IComponentData
    {
        public float3 Linear;  // Translation velocity on XZ plane (Y = 0)
        public float3 Angular; // 3-axis Euler angular velocity in radians per second
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
    public partial struct AsteroidDriftSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, drift) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<DriftVelocity>>()
                         .WithAll<AsteroidTag>())
            {
                // 1. Translate position on XZ plane
                float3 newPos = transform.ValueRO.Position + (drift.ValueRO.Linear * dt);
                newPos.y = 0f;
                transform.ValueRW.Position = newPos;

                // 2. Compounded 3-axis angular rotation
                quaternion deltaRot = quaternion.Euler(drift.ValueRO.Angular * dt);
                transform.ValueRW.Rotation = math.normalize(math.mul(transform.ValueRO.Rotation, deltaRot));
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
    using Unity.Mathematics;

    public class AsteroidAuthoring : MonoBehaviour
    {
        [Tooltip("3 = Large, 2 = Medium, 1 = Small")]
        public int Tier = 3;
        public float Radius = 2.2f;
        public int ScoreValue = 20;
        public int SplitCount = 2;
        public Vector3 InitialLinearVelocity = new Vector3(1.5f, 0f, 1.0f);
        public Vector3 InitialAngularVelocity = new Vector3(0.5f, 0.8f, 0.3f);

        class Baker : Baker<AsteroidAuthoring>
        {
            public override void Bake(AsteroidAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new AsteroidTag());
                AddComponent(entity, new AsteroidData
                {
                    Tier = authoring.Tier,
                    Radius = authoring.Radius,
                    ScoreValue = authoring.ScoreValue,
                    SplitCount = authoring.SplitCount
                });
                AddComponent(entity, new DriftVelocity
                {
                    Linear = new float3(authoring.InitialLinearVelocity.x, 0f, authoring.InitialLinearVelocity.z),
                    Angular = authoring.InitialAngularVelocity
                });
            }
        }
    }
}
```

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `ecs-components` & `baking`
*Source: `concepts/ecs-components.md`, `concepts/baking.md`*
* **Blittable Structs**: Ensure all component data structs are unmanaged blittables with no object or string fields.
* **Component Separation**: Keep `AsteroidData` and `DriftVelocity` in separate component files for cache locality.

### Anti-Pattern Warnings
* ⛔ **DO NOT USE `UnityEngine.Random`**: Dynamic runtime randomization must use `Unity.Mathematics.Random`.
* ⛔ **NO OFF-AXIS Y DRIFT**: Keep `newPos.y = 0f` to prevent isometric plane drift while allowing 3D tumbling orientation.
* ⛔ **DO NOT OMIT `math.normalize` ON QUATERNION COMPOUNDING**: Floating point drift will scale quaternions over time if not normalized.

---

## Section 4: Developer Definition of Done (DoD)
- [ ] Code implemented strictly within Section 2 target paths.
- [ ] Asteroid authoring attaches `AsteroidTag`, `AsteroidData`, and `DriftVelocity`.
- [ ] Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = **0 errors**).
- [ ] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [ ] **Burst Compilation**: `AsteroidDriftSystem` is marked with `[BurstCompile]`.
- [ ] **Zero Garbage**: No heap allocations inside the drift query loop.
- [ ] **Data Locality**: `DriftVelocity` is isolated from `AsteroidData`.

---

## Actionable Implementation Checklist
- [ ] Step 1: Create `AsteroidTag.cs`, `AsteroidData.cs`, and `DriftVelocity.cs` in `Assets/Scripts/Components/`.
- [ ] Step 2: Implement `AsteroidDriftSystem.cs` in `Assets/Scripts/Systems/`.
- [ ] Step 3: Implement `AsteroidAuthoring.cs` in `Assets/Scripts/Authoring/`.
- [ ] Step 4: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.
