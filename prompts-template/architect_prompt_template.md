# Prompt Template: Unity DOTS Architect (Cloud / Antigravity)

Use this prompt template when initiating feature design or architectural planning in **Google Antigravity** (or another high-reasoning Cloud LLM). It grounds the AI to act strictly as the **Lead Systems Architect**, querying authoritative OKF knowledge and breaking down work into atomic contracts without writing premature implementation code.

---

```markdown
We are designing and implementing a new gameplay feature for our Unity 6.6 game using modern Entities / DOTS (Entities 1.4+).

### 1. Game Concept & Technical Context
* **Feature / Game Overview**: [e.g., 3D Isometric Asteroids arcade game / Top-down survival shooter]
* **Camera & Rendering Perspective**: [e.g., Isometric camera, Universal Render Pipeline (URP), 1920x1080 resolution]
* **Target Platforms**: [e.g., Windows PC / Standalone]
* **Control Scheme**: [e.g., Modern Input System (com.unity.inputsystem) - WASD movement + mouse look-at]

### 2. Available Visual & Audio Assets
* **Entity Prefabs / 3D Models**: [e.g., Assets/Prefabs/Vehicles/Ship.prefab, Assets/Prefabs/Enemies/]
* **VFX / Projectiles**: [e.g., Assets/FX/LaserBullet.prefab]
* **UI & Audio**: [e.g., Screen-space uGUI Canvas in Assets/UI/, sound effects]

### 3. Core Mechanics to Architect
1. **[Mechanic 1 Name]**: [Description: e.g., Player ship thrust, inertia damping, and smooth cursor orientation]
2. **[Mechanic 2 Name]**: [Description: e.g., Dynamic asteroid spawning, random rotation, and splitting into fragments]
3. **[Mechanic 3 Name]**: [Description: e.g., Weapon firing, laser projectile velocity, and collision destruction]
4. **[Mechanic 4 Name]**: [Description: e.g., Score tracking and Hybrid UI bridge connecting ECS data to uGUI Canvas]

---

### Antigravity Architect Directives

Operate strictly as our **Lead Systems Architect** (`personas/architect.md`) adhering to `rules/AGENTS.md` and using the installed `dots-architect`, `ecs-knowledge`, and `ecs-docs` skills:

#### Step 1: Authoritative Knowledge Retrieval
1. Invoke `ecs-knowledge` to query the `kiso-okf` MCP server:
   - Use `search_concepts` with relevant keywords (e.g., "input system", "ecb best practices", "hybrid ecs").
   - Use `get_concept_content` with canonical `conceptId`s to retrieve exact, verified syntax patterns.
2. If syntax details are missing, invoke the `ecs-docs` skill (or call `context7:query-docs` directly using the pre-approved libraries in `.agents/references/context7_libraries.yaml`; NEVER call `resolve-library-id`).

#### Step 2: System Architecture & Overall Plan
1. Author an overarching `IMPLEMENTATION_PLAN.md` in the project root detailing:
   - System execution groups (`InitializationSystemGroup`, `SimulationSystemGroup`, `TransformSystemGroup`).
   - Component data structures (blittable unmanaged structs, singleton configurations).
   - EntityCommandBuffer lifecycle strategy (record-and-forget; deferral to `BeginSimulationEntityCommandBufferSystem.Singleton`).
   - Hybrid bridges for managed GameObjects (uGUI UI, companion audio/VFX).
   - **Visual Asset & Prefab Registry**: Centralized table recording all 3D models, prefabs, and VFX from Section 2 of this prompt, mapping each asset to its assigned Task ID, entity name, and authoring mode. Never drop visual asset paths during plan synthesis.
   - Milestone sequence of short, low-effort atomic tasks (`tasks/task-001.md`, `tasks/task-002.md`, etc.).

#### Step 3: Author Initial Task Contract (`tasks/task-001.md`)
Using `templates/task-contract-template.md`, generate the complete 5-section contract for Task 1:
* **Section 1**: Single responsibility scope (what is IN scope, what is explicitly OUT of scope).
* **Section 2**: Exact type signatures, namespaces, struct definitions, field types, target paths (`Assets/Scripts/...`), and **Visual & Scene Rigging Contracts** (explicitly assign the visual prefab asset path OR explicitly designate the entity as `Pure Data / Non-Visual Entity` with rationale).
* **Section 3**: Injected verbatim OKF code snippets and mandatory anti-pattern warnings:
  - ⛔ DO NOT USE `IAspect` (marked `[Obsolete]` in Entities 1.4+).
  - ⛔ NO GC IN BURST (no `new`, `string`, or LINQ in `ISystem` or jobs).
  - ⛔ NO MANUAL ECB PLAYBACK OR DISPOSAL (record-and-forget; never call `.Playback()` or `.Dispose()`).
* **Section 4**: Definition of Done (DoD) requiring compilation verification via MCP (`unity-compile-check`):
  - Explicitly include: *"Do not interact with the Unity Editor directly or using the filesystem; always use the MCP (`unity_list_instances`, `unity_get_compilation_errors`)."*
* **Section 5**: Reviewer checklist and actionable implementation checklist (`- [ ]`).

#### Step 4: Generate Handoff Prompt for Local Coder
Conclude your response by generating the exact prompt to be pasted into **ZooCode** (running local Qwen in Unity Coder mode) to execute Task 1, including the `unity-compile-check` trigger, instance port discovery via `unity_list_instances`, and the concise negative rule against direct filesystem/CLI interaction.

> [!CAUTION]
> **Strict Role Boundary**: As the Architect, you MUST NEVER write C# implementation code in `Assets/Scripts/`. You produce explicit technical contracts for the Coder to execute.
```

