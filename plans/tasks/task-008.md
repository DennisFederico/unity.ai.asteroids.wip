# Task-008: Aim Crosshair Reticle on Isometric Plane

**Status:** `READY_FOR_IMPLEMENTATION`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---\n
## Section 1: Objective & Scope
* **Objective:** Implement the 3D aiming crosshair reticle tracking system that positions `SM_Wep_Crosshair_04` directly at `PlayerInput.AimWorldPosition` on the isometric $XZ$ plane ($Y=0.05f$) with zero GC allocation.
* **In Scope:**
  - Unmanaged component: `CrosshairReticleTag`.
  - Burst-compiled unmanaged `ReticleTrackingSystem` (`ISystem`) in `SimulationSystemGroup` synchronizing the reticle's `LocalTransform` with the player's aim target.
  - `ReticleAuthoring` `MonoBehaviour` and Baker attached to `SM_Wep_Crosshair_04.prefab`.
* **Out of Scope:**
  - Mouse raycast calculations (already computed in `PlayerInputBridgeSystem` in `task-001`).

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/CrosshairReticleTag.cs`
* `Assets/Scripts/Systems/ReticleTrackingSystem.cs`
* `Assets/Scripts/Authoring/ReticleAuthoring.cs`

### Data Contracts (Components)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;

    // Tag identifying the 3D aiming crosshair reticle entity
    public struct CrosshairReticleTag : IComponentData { }
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
    public partial struct ReticleTrackingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CrosshairReticleTag>();
            state.RequireForUpdate<PlayerInput>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInput>();
            float3 targetPos = playerInput.AimWorldPosition;
            targetPos.y = 0.05f; // Slight elevation to avoid Z-fighting with ground plane

            foreach (var transform in 
                SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<CrosshairReticleTag>())
            {
                transform.ValueRW.Position = targetPos;
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

    public class ReticleAuthoring : MonoBehaviour
    {
        class Baker : Baker<ReticleAuthoring>
        {
            public override void Bake(ReticleAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CrosshairReticleTag());
            }
        }
    }
}
```

### Visual & Scene Rigging Contracts
*(Defines how the reticle entity is assembled in the SubScene)*
* **Target Scope**: `SubScene` (`Assets/Scenes/Asteroids_entities.unity`)
* **Entity Visual Type**:
  - [x] **Visual Entity (Prefab-Backed)**:
    - **Visual Prefab Asset Path**: `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/Weapons/SM_Wep_Crosshair_04.prefab`
    - **Rigging Mode**: `Prefab Instance (Root)`
    - **Initial Transform**: Position `(0, 0.05, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`
    - **Authoring Component**: `ReticleAuthoring` attached to `AimCrosshair` instance in `Asteroids_entities.unity`
  - [ ] **Pure Data / Non-Visual Entity (Explicitly Empty)**:

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `singletons` & `baking`
*Source: `concepts/singletons.md`, `concepts/baking.md`*
* **Dynamic Reticle**: Reticle moves every frame with mouse movements; bake with `TransformUsageFlags.Dynamic`.\n* **Guard Checks**: `state.RequireForUpdate<CrosshairReticleTag>()` and `state.RequireForUpdate<PlayerInput>()` prevent updating if either entity is missing.

### Anti-Pattern Warnings
* ⛔ **DO NOT USE `IAspect`**: Direct query via `SystemAPI.Query<RefRW<LocalTransform>>().WithAll<CrosshairReticleTag>()`.
* ⛔ **NO `Camera.main` IN `ISystem`**: Raycasting occurs exclusively in `PlayerInputBridgeSystem` (managed `SystemBase`); `ReticleTrackingSystem` only reads unmanaged `PlayerInput`.

---

## Section 4: Developer Definition of Done (DoD)
- [x] Code implemented strictly within Section 2 target paths.
- [x] Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = **0 errors**).\n- [x] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [ ] **Burst Compilation**: Struct and methods annotated with `[BurstCompile]`.
- [ ] **Single Responsibility**: System only updates transform position from `PlayerInput.AimWorldPosition`.
- [ ] **Visual & Rigging Audit**: Verified `SM_Wep_Crosshair_04.prefab` is instantiated in `Asteroids_entities.unity` with `ReticleAuthoring` and `TransformUsageFlags.Dynamic`.

---

## Actionable Implementation Checklist
- [x] Step 1: Create `CrosshairReticleTag.cs` in `Assets/Scripts/Components/`.
- [x] Step 2: Implement `ReticleTrackingSystem.cs` in `Assets/Scripts/Systems/`.
- [x] Step 3: Implement `ReticleAuthoring.cs` in `Assets/Scripts/Authoring/`.
- [x] Step 4: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.

---

## Reviewer Sign-off & Verdict
* **Review Date:** `YYYY-MM-DD`
* **Verdict:** `PENDING`
* **Findings:**
  * [Notes or verification logs here]
