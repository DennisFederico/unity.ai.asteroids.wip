# Asteroids 3D (Isometric) — Overarching DOTS Implementation Plan

**Target Unity Version:** Unity 6.6 (Editor `6000.6.0f1`)  
**Entities Version:** Unity Entities 1.4+ (`6.6.0`)  
**Render Pipeline:** Universal Render Pipeline (URP)  
**Gameplay Model:** Infinite 2D Arcade Simulation in 3D Space (XZ Ground Plane at $Y=0$, Yaw rotation around $Y$-axis, Isometric Camera Tracking Player).

---

## 1. Executive Summary & Core Constraints

This implementation plan establishes the architectural blueprint for an **infinite-map 3D Asteroids game clone** with an isometric camera perspective and **dynamic player-centric density regulation**. The player ship navigates unbounded space on the unmanaged $XZ$ plane ($Y=0$). 

To ensure fair arcade difficulty and eliminate both visual popping and entity crowding:
1. **Camera Frustum Occlusion**: Asteroids spawn strictly outside the active camera frustum (beyond a safe buffer radius of $35\text{m}$), drifting naturally into the player's view from offscreen.
2. **Local Density Interval**: Asteroid population is regulated locally within an engagement radius ($R_{density} = 70\text{m}$) around the player ship:
   - **Target Density Interval**: $[8, 16]$ asteroids.
   - If local density $\ge 16$, spawning is completely suppressed.
   - If local density $< 8$, an accelerated replenishment interval ($0.8\text{s}$) brings the field back into the optimal engagement zone.
   - In the target band $[8, 15]$, a steady interval ($2.5\text{s}$) maintains consistent gameplay challenge.
3. **$50\times$ Out-of-Bounds Culling Bubble**: Asteroids that drift beyond the out-of-bounds radius ($R_{oob} = \sqrt{50} \times R_{spawn\_outer} \approx 7.071 \times 50\text{m} \approx 353.55\text{m}$, representing $50\times$ the area of the spawn zone) are automatically destroyed via `BeginSimulationEntityCommandBufferSystem.Singleton`.

### Non-Negotiable Architectural Rules
1. **Unmanaged Blittable Memory**: All simulation components implement `IComponentData` as unmanaged blittable structs.
2. **Deterministic Record-and-Forget ECB Lifecycle**: All runtime structural modifications (instantiating lasers, culling out-of-bounds asteroids, splitting fragments) are recorded via `BeginSimulationEntityCommandBufferSystem.Singleton`. **Never call `.Playback()` or `.Dispose()` in game systems.**
3. **No `IAspect`**: Direct queries via `SystemAPI.Query<RefRO<T>, RefRW<U>>()` are used everywhere (`IAspect` is obsolete in Entities 1.4+).
4. **Precomputed Squared Distances**: All distance checks in Burst (`math.distancesq`) evaluate against precomputed squared radii (`DensityRadiusSq`, `DespawnRadiusSq`) to avoid expensive `math.sqrt()` calls.

---

## 2. Component Layout & Memory Specifications

All data structs reside in namespace `Asteroids.Core`.

### 2.1 Singleton & Configuration Components
* **`InfiniteMapConfig`**:
  ```csharp
  public struct InfiniteMapConfig : IComponentData
  {
      public float ViewportBufferRadius; // Minimum spawn distance outside frustum (e.g. 35m)
      public float SpawnOuterRadius;     // Outer boundary of spawn ring (e.g. 50m)
      public float DespawnRadius;        // Out-of-bounds cull distance (e.g. sqrt(50) * 50m ≈ 353.55m)
      public float DespawnRadiusSq;      // Precomputed squared distance for Burst culling
  }
  ```
  *Memory footprint:* 16 bytes. Baked with `TransformUsageFlags.None`.
* **`GameScore`**:
  ```csharp
  public struct GameScore : IComponentData
  {
      public int CurrentScore;
  }
  ```
  *Memory footprint:* 4 bytes.
* **`GlobalRandom`**:
  ```csharp
  public struct GlobalRandom : IComponentData
  {
      public Unity.Mathematics.Random Value;
  }
  ```
  *Memory footprint:* 4 bytes. Initialized with non-zero seed `math.max(1u, seed)`.

### 2.2 Player Ship Components
* **`PlayerTag`**: Empty tag struct for query filtering (0 bytes).
* **`PlayerInput`**:
  ```csharp
  public struct PlayerInput : IComponentData
  {
      public float2 Movement;         // WASD / Stick direction on XZ plane
      public float3 AimWorldPosition; // Cursor intersection with Y=0 plane
      public bool FireHeld;           // Continuous trigger
      public bool FireTriggered;      // Transient edge press
  }
  ```
  *Memory footprint:* 24 bytes. Polled in `InitializationSystemGroup`.
* **`PlayerMovementData`**:
  ```csharp
  public struct PlayerMovementData : IComponentData
  {
      public float ThrustAcceleration; // e.g. 35.0f
      public float MaxSpeed;           // e.g. 15.0f
      public float Drag;               // Linear damping (e.g. 1.8f)
      public float RotationDamping;    // Slerp angular rate towards cursor (e.g. 18.0f)
      public float3 CurrentVelocity;   // Accumulated linear velocity vector
  }
  ```
  *Memory footprint:* 28 bytes. Processed in `SimulationSystemGroup`.

### 2.3 Projectile & Weapon Components
* **`LaserTag`**: Empty tag struct for laser identification (0 bytes).
* **`ProjectileData`**:
  ```csharp
  public struct ProjectileData : IComponentData
  {
      public float3 Velocity;
      public float Speed;
      public float Radius; // Bounding radius for sphere collision (e.g. 0.3f)
  }
  ```
  *Memory footprint:* 20 bytes.
* **`Lifetime`**:
  ```csharp
  public struct Lifetime : IComponentData
  {
      public float Value; // Time-to-live in seconds (e.g. 2.0f)
  }
  ```
  *Memory footprint:* 4 bytes.
* **`LaserSpawner`**:
  ```csharp
  public struct LaserSpawner : IComponentData
  {
      public Entity LaserPrefab;
      public float FireRate;       // Rounds per second (e.g. 6.0f)
      public float CooldownTimer;  // Internal countdown
      public float3 MuzzleOffset;  // Local offset from ship center
  }
  ```
  *Memory footprint:* 24 bytes.

### 2.4 Asteroid & Obstacle Components
* **`AsteroidTag`**: Empty tag struct for asteroid query filtering (0 bytes).
* **`AsteroidData`**:
  ```csharp
  public struct AsteroidData : IComponentData
  {
      public int Tier;       // 3 = Large, 2 = Medium, 1 = Small
      public float Radius;   // Large: 2.2f, Medium: 1.1f, Small: 0.5f
      public int ScoreValue; // Large: 20, Medium: 50, Small: 100
      public int SplitCount; // Number of children spawned upon hit (default: 2)
  }
  ```
  *Memory footprint:* 16 bytes.
* **`DriftVelocity`**:
  ```csharp
  public struct DriftVelocity : IComponentData
  {
      public float3 Linear;  // Translation velocity on XZ plane
      public float3 Angular; // 3-axis rotation vector (Euler radians/sec)
  }
  ```
  *Memory footprint:* 24 bytes.
* **`AsteroidPrefabsConfig`**:
  ```csharp
  public struct AsteroidPrefabsConfig : IComponentData
  {
      public Entity LargePrefab;
      public Entity MediumPrefab;
      public Entity SmallPrefab;
  }
  ```
  *Memory footprint:* 24 bytes.
* **`AsteroidSpawnerData`**:
  ```csharp
  public struct AsteroidSpawnerData : IComponentData
  {
      public float BaseSpawnInterval;   // Standard interval when density is in range (e.g. 2.5s)
      public float FastSpawnInterval;   // Accelerated interval when density < min (e.g. 0.8s)
      public float CooldownTimer;       // Current countdown timer
      public float DensityRadius;       // Player-centric sampling radius (e.g. 70m)
      public float DensityRadiusSq;     // Precomputed squared radius (e.g. 4900)
      public int TargetLocalDensityMin; // Minimum desired density in radius (e.g. 8)
      public int TargetLocalDensityMax; // Maximum permitted density in radius (e.g. 16)
  }
  ```
  *Memory footprint:* 28 bytes.

### 2.5 Shared & Utility Components
* **`CrosshairReticleTag`**: Tag for aiming reticle visual indicator entity (0 bytes).

---

## 3. Radial Density Architecture & Anti-Popping Zones

```mermaid
graph TD
    subgraph "Player-Centric Spatial Radial Zones"
        P["Player Ship (Origin)"]
        Frustum["Camera Frustum (0 to ~25m)<br/>Visible on screen"]
        SafetyBuffer["Frustum Buffer (25m to 35m)<br/>Occluded margin"]
        SpawnZone["Spawn Ring (35m to 50m)<br/>New asteroids instantiate here (OFFSCREEN)"]
        DensityZone["Engagement & Density Check Radius (0 to 70m)<br/>Target count: [8 to 16] asteroids"]
        CullZone["Out-of-Bounds Cull Line (353.55m)<br/>Area = 50x Spawn Area"]
    end

    P --- Frustum
    Frustum --- SafetyBuffer
    SafetyBuffer --- SpawnZone
    SpawnZone --- DensityZone
    DensityZone --- CullZone
    CullZone -->|dist > 353.55m| ECB_Destroy["ecb.DestroyEntity()"]
```

### Density Feedback Algorithm (Inside `AsteroidSpawnSystem`)
1. **Count Local Population**: Sum asteroids where $\text{distSq}(asteroid, player) \le R_{density}^2$.
2. **Evaluate Density Gate**:
   - $\text{Count} \ge 16$: **Cap reached**. Skip spawn tick. Set cooldown to `BaseSpawnInterval` ($2.5\text{s}$).
   - $\text{Count} < 8$: **Under-populated**. Instantiate Large asteroid offscreen. Set cooldown to `FastSpawnInterval` ($0.8\text{s}$) to smoothly repopulate.
   - $8 \le \text{Count} < 16$: **Optimal difficulty band**. Instantiate Large asteroid offscreen. Set cooldown to `BaseSpawnInterval` ($2.5\text{s}$).

---

## 4. System Execution Pipeline & Update Ordering

```mermaid
flowchart TD
    subgraph InitializationSystemGroup
        InputBridge["PlayerInputBridgeSystem (SystemBase)"]
    end

    subgraph SimulationSystemGroup
        subgraph PlayerPipeline
            PlayerMove["PlayerMovementSystem (ISystem)"]
            LaserShoot["LaserShootingSystem (ISystem)"]
        end

        subgraph AsteroidPipeline
            LaserMove["LaserMovementSystem (ISystem)"]
            LaserLife["LaserLifetimeSystem (ISystem)"]
            AsteroidDrift["AsteroidDriftSystem (ISystem)"]
            AsteroidSpawn["AsteroidSpawnSystem (ISystem) - Local Density Gated"]
            CollisionDetect["LaserAsteroidCollisionSystem (ISystem)"]
            AsteroidCull["AsteroidCullingSystem (ISystem) - 50x Area Despawn"]
        end

        subgraph VisualPipeline
            ReticleTrack["ReticleTrackingSystem (ISystem)"]
        end
    end

    subgraph PresentationSystemGroup
        CameraFollow["CameraFollowBridgeSystem (SystemBase)"]
        ScoreUIBridge["ScoreUIBridgeSystem (SystemBase)"]
    end

    InputBridge --> PlayerMove
    PlayerMove --> LaserShoot
    LaserShoot --> LaserMove
    LaserMove --> CollisionDetect
    AsteroidDrift --> CollisionDetect
    CollisionDetect --> AsteroidCull
    PlayerMove -.->|Tracks Player Transform| CameraFollow
    CollisionDetect -.->|Updates GameScore| ScoreUIBridge
```

---

## 5. Record-and-Forget ECB Lifecycle

All structural changes occur via `BeginSimulationEntityCommandBufferSystem.Singleton`:

```mermaid
sequenceDiagram
    autonumber
    participant SYS as Simulation Systems (ISystem)
    participant ECB as EntityCommandBuffer (BeginSimulation)
    participant GRP as BeginSimulationEntityCommandBufferSystem

    SYS->>ECB: Instantiate(LaserPrefab) / DestroyEntity(Laser)
    SYS->>ECB: Instantiate(LargeAsteroid) [if Local Density < Max]
    SYS->>ECB: DestroyEntity(OutOfBoundsAsteroid) [Distance > DespawnRadius]
    SYS->>ECB: DestroyEntity(HitAsteroid) + Instantiate(Fragments)
    Note over SYS: System updates complete. ZERO manual Playback/Dispose!
    GRP->>GRP: Playback(EntityManager) at frame sync boundary
    GRP->>GRP: Automatically clean up buffer memory
```

---

## 6. Asteroid Splitting Hierarchy

```mermaid
graph TD
    A["Large Asteroid (Tier 3)<br/>Radius: 2.2 | Score: 20"] -->|Hit by Laser| B1["Medium Asteroid (Tier 2)<br/>Radius: 1.1 | Score: 50"]
    A -->|Hit by Laser| B2["Medium Asteroid (Tier 2)<br/>Radius: 1.1 | Score: 50"]
    B1 -->|Hit by Laser| C1["Small Asteroid (Tier 1)<br/>Radius: 0.5 | Score: 100"]
    B1 -->|Hit by Laser| C2["Small Asteroid (Tier 1)<br/>Radius: 0.5 | Score: 100"]
    B2 -->|Hit by Laser| C3["Small Asteroid (Tier 1)<br/>Radius: 0.5 | Score: 100"]
    B2 -->|Hit by Laser| C4["Small Asteroid (Tier 1)<br/>Radius: 0.5 | Score: 100"]
    C1 -->|Hit by Laser| D1["Destroyed (Score +100)"]
    C2 -->|Hit by Laser| D2["Destroyed (Score +100)"]
    C3 -->|Hit by Laser| D3["Destroyed (Score +100)"]
    C4 -->|Hit by Laser| D4["Destroyed (Score +100)"]
```

---

## 7. Atomic Task Roadmap (Infinite Map)

| Task ID | Title | Scope & Deliverables | Primary Files |
| :--- | :--- | :--- | :--- |
| **`task-001`** | **Player Ship Movement & Input** | Polling `InputSystem_Actions`, computing isometric mouse raycast, unmanaged movement logic with drag and smooth yaw rotation in infinite space, player authoring & baker. | `PlayerInput.cs`, `PlayerMovementData.cs`, `PlayerInputBridgeSystem.cs`, `PlayerMovementSystem.cs`, `PlayerAuthoring.cs` |
| **`task-002`** | **Infinite Map: Culling & Camera Tracking** | `InfiniteMapConfig` singleton ($50\times$ area ratio), `AsteroidCullingSystem` destroying out-of-bounds asteroids via ECB, and `CameraFollowBridgeSystem` tracking the player ship. | `InfiniteMapConfig.cs`, `AsteroidCullingSystem.cs`, `CameraFollowBridgeSystem.cs`, `InfiniteMapAuthoring.cs` |
| **`task-003`** | **Laser Projectiles & Shooting Mechanism** | Laser spawning with rate limiter, forward projectile motion, lifetime countdown, and ECB destruction upon expiry. | `ProjectileData.cs`, `LaserTag.cs`, `Lifetime.cs`, `LaserSpawner.cs`, `LaserShootingSystem.cs`, `LaserMovementSystem.cs`, `LaserLifetimeSystem.cs`, `LaserAuthoring.cs` |
| **`task-004`** | **Asteroids Drift & 3-Axis Random Tumble** | Asteroid identification, tier data, linear velocity, 3-axis rotational angular velocity, unmanaged drift system. | `AsteroidData.cs`, `AsteroidTag.cs`, `DriftVelocity.cs`, `AsteroidDriftSystem.cs`, `AsteroidAuthoring.cs` |
| **`task-005`** | **Density-Regulated Asteroid Ring Spawner** | Dynamic ring generation ($[35\text{m}, 50\text{m}]$) strictly outside camera frustum, local density evaluation within $70\text{m}$ ($[8, 16]$ interval), adaptive cooldown, inward drift trajectory, and `GlobalRandom` singleton. | `GlobalRandom.cs`, `AsteroidSpawnerData.cs`, `AsteroidPrefabsConfig.cs`, `AsteroidSpawnSystem.cs`, `AsteroidSpawnerAuthoring.cs` |
| **`task-006`** | **Laser-Asteroid Collision & Splitting System** | Bounding sphere distance query in `ISystem`, laser despawn, tier-based asteroid splitting (Large $\rightarrow$ 2x Medium, Medium $\rightarrow$ 2x Small, Small $\rightarrow$ destroy), and score reward accumulation. | `GameScore.cs`, `LaserAsteroidCollisionSystem.cs` |
| **`task-007`** | **Score Management & Hybrid uGUI Bridge** | Score singleton management, managed `SystemBase` UI bridge synchronizing score data to uGUI Text / TextMeshPro on Screen-Space Canvas. | `ScoreDisplayView.cs`, `ScoreUIBridgeSystem.cs` |
| **`task-008`** | **Aim Crosshair Reticle on Isometric Plane** | 3D visual crosshair following `PlayerInput.AimWorldPosition` on the $Y=0.05f$ plane. | `CrosshairReticleTag.cs`, `ReticleTrackingSystem.cs`, `ReticleAuthoring.cs` |

---

## 8. Quality Gates & Verification Standards

1. **Compilation Gate**: All code must compile with **0 errors and 0 Burst warnings** verified via `anklebreaker-unity-mcp` (`unity_get_compilation_errors`).
2. **Zero Allocation Gate**: Burst simulation systems run with 0 GC allocations per frame.
3. **Lifecycle Discipline**: Zero manual `.Playback()` or `.Dispose()` calls on system-managed ECBs.
