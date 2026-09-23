# Architecting Asteroids Game with UNITY ECS Prompt

We are building a 3D Asteroids game clone with an Isometric camera perspective (giving a clean 2D arcade gameplay feel), engineered strictly using Unity 6.6 and Entities / DOTS (Entities 1.4+).

## Project Context & Visual Assets
* **Player Ship Prefab**: `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Ship_Fighter_02.prefab`
* **Asteroid Prefabs**: `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Environment/`
* **Aiming Reticle**: `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/Weapons/SM_Wep_Crosshair_04.prefab`
* **Laser FX**: `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab`
* **Pipeline & Display**: Universal Render Pipeline (URP), 16:9 aspect ratio (1920x1080).

## Core Gameplay Mechanics
1. **Movement & Input**:
    - Player ship accelerates with WASD thrust; yaw smoothly faces the mouse cursor on the isometric plane.
    - Use the modern Unity Input System (`com.unity.inputsystem`). Strictly implement the ECS bridge pattern in `concepts/unity-input-system.md` (managed `SystemBase` in `InitializationSystemGroup` writing to a singleton component).
2. **Asteroids & Destruction**:
    - Asteroids drift and rotate across 3 axes. Larger asteroids split into smaller fragments when hit.
    - Entity creation, destruction, and fragment spawning MUST use an `EntityCommandBuffer` (`BeginSimulationEntityCommandBufferSystem.Singleton`). Follow `concepts/ecb-best-practices.md`.
3. **Lasers & Projectiles**:
    - High-velocity laser projectiles with lifetime expiration.
    - Hit detection triggers asteroid splitting and destroys the projectile via ECB.
4. **Hybrid Bridges (UI & Audio)**:
    - Use Unity UI (uGUI) with a screen-space Canvas for the score counter. Use a managed system (`SystemBase`) to sync ECS score data to the UI text element.
    - Companion patterns for laser particle instantiation (`concepts/hybrid-ecs.md`).

---

## Architect Directives

Operate strictly as our **Lead Systems Architect** (`personas/architect.md`) adhering to `rules/AGENTS.md` and using the installed `dots-architect`, `ecs-knowledge`, and `ecs-docs` skills.
Your knowledge of Unity ECS used during training is outdated (if previous to com.unity.entities version 1.4.x). Always use the latest ECS documentation and best practices.

### Step 1: Authoritative Knowledge Retrieval
1. Invoke `ecs-knowledge` to query the `kiso-okf` MCP server, start with:
    - Input System bridge $\rightarrow$ `conceptId: "unity-input-system"`
    - ECB best practices $\rightarrow$ `conceptId: "ecb-best-practices"`
    - Hybrid UI bridges $\rightarrow$ `conceptId: "hybrid-ecs"`
    - 
2. If syntax details are missing, invoke the `ecs-docs` skill (or call `context7:query-docs` directly using the pre-approved libraries in `.agents/references/context7_libraries.yaml`; NEVER call `resolve-library-id`).

3. Ask the user for any clarification or disambiguation.

### Step 2: System Architecture & Overall Roadmap
Author the overarching `IMPLEMENTATION_PLAN.md` of the game in the `plans` folder:
- Define the components with thier memory layouts (blittable unmanaged structs, singleton configurations).
- Map out system with in their execution groups (`InitializationSystemGroup`, `SimulationSystemGroup`).
  - Document the record-and-forget ECB lifecycle for spawning and destruction.
  - Decompose the game into short low-effort atomic tasks and list them with their primary goal. Examples:
      * `task-001`: Implement Player Ship Movement & Input (Input bridge, movement components, unmanaged `ISystem`, Baker).
      * `task-002`: Asteroid Spawner & Drift/Rotation (Movement system, random tumble, splitting via ECB)
      *  etc...

### Step 3: Definition and design of each task
For each identified task in the implementation plan, define and design the task in a separate markdown file in the `plans/tasks` folder. Use the `templates/task-contract-template.md` as a template for each task contract.
The 5-section contract for each task includes:
* **Section 1**: Objective & Scope (eg. strictly player input, movement data, Baker, and movement system).
* **Section 2**: Target paths (eg. `Assets/Scripts/Components/`, `Assets/Scripts/Systems/`, `Assets/Scripts/Authoring/`), namespaces, and exact C# struct definitions.
* **Section 3**: Injected verbatim OKF patterns (`unity-input-system`, `ecb-best-practices`, etc). **IMPORTANT**: these OKF patterns are valuable hints for the coding agent that will implement the task.
* **Section 4**: Definition of Done (DoD). **IMPORTANT**: Always require compilation verification via `anklebreaker-unity-mcp` (0 errors, 0 Burst warnings), in addition to other DoD.
* **Section 5**: Reviewer checklist and actionable implementation checklist (`- [ ]`).

#### Step 4: Output Handoff Prompt for ZooCode
Conclude by outputting the exact ready-to-run prompt to paste into **ZooCode** (running local Qwen in Unity Coder mode) to execute the first task to implement.

> [!CAUTION]
> **Strict Role Boundary**: As the Architect, you MUST NEVER write C# implementation code in `Assets/Scripts/`. You produce explicit technical contracts for the Coder to execute.

