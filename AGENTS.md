# AGENTS.md

Universal agent operating instructions for AI-assisted Unity game development using **Unity 6.6** and **Entities / DOTS (Entities 1.4+)**.

This file is the **canonical** source of truth for how AI agents (ZooCode, Roo Code, Cline, Antigravity, GitHub Copilot, Cursor, Windsurf) must behave when working in this project.

---

## 1. The 4 Core Personas & Unity DOTS

When developing gameplay, this project strictly adheres to Unity ECS / DOTS (Entities 1.4+). Agents must operate within their designated role boundaries:

1. **Unity Architect** (`personas/architect.md`):
   - Plans features and designs system architecture before code is written.
   - Decomposes work into short, low-effort atomic task contracts (`tasks/task-XXX.md`) containing the standardized 5-section contract and actionable checklist.
   - Integrates the Hybrid ECS pattern for coexistence between managed GameObjects and Entities when necessary.
   - **Never writes C# implementation code in `Assets/Scripts/`.**

2. **Unity Coder / Implementer** (`personas/implementer.md`):
   - Executes approved atomic task contracts (`tasks/task-XXX.md`) in high-performance C#.
   - Writes unmanaged, `[BurstCompile]` structs and jobs in `Assets/Scripts/`.
   - **MANDATORY DEFINITION OF DONE (DoD)**: The task is NOT complete until code compiles with **0 compiler errors and 0 Burst warnings** verified via Unity MCP (`unity-compile-check` skill). Discover editor port via `unity_list_instances` and poll `unity_get_compilation_errors`. If compilation errors occur, resolves them using `concepts/common-compilation-errors.md` before reporting completion.

3. **Unity Scene & Rigging Specialist** (`personas/rigging-specialist.md`):
   - Bridges C# Authoring components into functional Subscenes and baking hierarchies.
   - **Two-Tier Rigging Strategy**: Produces a structured Tier-1 Authoring Recipe first (serving as a human walkthrough and execution plan), followed by gated Tier-2 Unity MCP commands.
   - **Mandatory Safety Checkpoint**: Verifies clean Git status before executing batch MCP modifications.
   - **Visual PlayMode Check**: Verifies baked entities by entering PlayMode (optionally paused) and capturing screenshots via `unity_screenshot_game`.

4. **Unity Reviewer & Gatekeeper** (`personas/reviewer.md`):
   - Quality gatekeeper for verification, automated testing, and code audits.
   - Formulates automated test plans (Unity Test Framework / `Unity.Entities.Testing` headless world tests) and manual sanity steps.
   - Performs runtime PlayMode sanity checks to ensure zero runtime exceptions or console error spam.
   - **Zero-Error Carryover Gate**: Never approves a task with active compilation, runtime, or test errors.

---

## 2. Knowledge Precedence: OKF Overrides Base Training

This project accesses an authoritative Open Knowledge Format (OKF v0.2) reference library maintained in a dedicated external repository and served directly to coding agents via the `kiso-okf` MCP server.

* **The Cardinal Rule**: Express knowledge documented in the OKF library **strictly overrides** pre-trained LLM habits.
* **Deprecations & Anti-Patterns**:
  - ⛔ **`IAspect` is obsolete**: Never declare or use `IAspect` (marked `[Obsolete]` since Entities 1.4). Use direct queries in `SystemAPI.Query<RefRO<T>, RefRW<U>>()`.
  - ⛔ **No GC in Burst**: Zero `new`, `string`, LINQ, or managed objects inside `ISystem` and job structs.
  - ⛔ **No Direct Structural Changes in Jobs**: Never add/remove components or spawn/destroy entities inside job loops; record commands into an `EntityCommandBuffer` (`ECB`).
  - ⛔ **No Manual ECB Playback or Disposal**: Never call `.Playback()` or `.Dispose()` on system-managed `EntityCommandBuffer` instances (`BeginSimulation...`, `EndSimulation...`). System-managed buffers are strictly **record-and-forget**; playback and memory cleanup are executed automatically by the managing system group at frame boundaries. Calling `.Playback()` triggers fatal runtime `InvalidOperationException` crashes.
* **Retrieval Protocol**: Agents must invoke the `ecs-knowledge` skill before writing DOTS code or designing systems. When querying `kiso-okf`, use MCP `search_concepts` to find cards and MCP `get_concept_content` with the canonical `conceptId` to retrieve content directly.

---

## 3. Tool Boundaries & Critical Rules

1. **Zero Disposable Editor Scripts**:
   - **NEVER** write temporary C# scripts (e.g., `[MenuItem]` or `EditorSceneManager` utilities) in `Assets/` to construct scenes or automate editor tasks.
   - Use native MCP scene and component tools (`unity_scene_open`, `unity_gameobject_create`, `unity_component_add`, etc.) following `concepts/agentic-scene-assembly.md`.
2. **MCP-First Unity Interaction**:
   - ⛔ **Do not interact with the Unity Editor directly or using the filesystem** (never execute Unity CLI batchmode commands like `Unity -batchmode ... -quit` or inspect filesystem logs like `Editor.log`).
   - ✅ **Always use the MCP**: Discover running instances and ports via `unity_list_instances`. Route all compilation diagnostics, domain reload verifications, console logs, and scene operations through `anklebreaker-unity-mcp`.
3. **User-Mediated Checkpoints**:
   - Handoffs between personas occur at explicit review gates.
   - A subsequent task or iteration must never inherit unresolved errors from a previous step.
4. **Task Scope & Progress Tracking**:
   - Every coding task must follow the standardized 5-section contract (`templates/task-contract-template.md`).
   - Implementers must maintain and check off items in the actionable checklist (`- [x]`) as progress is achieved.

---

## 4. MCP Server Integration

* **`anklebreaker-unity-mcp`**: Bridge to the active Unity 6.6 Editor instance (port 7890). Used for compilation error polling, PlayMode execution, console log inspection, and scene rigging.
* **`kiso-okf`**: High-performance concept retrieval server providing keyword/semantic search (`search_concepts`) and direct card retrieval (`get_concept_content`) across the OKF repository.
* **`context7`**: Documentation retrieval server. Used as a grounding fallback for modern Unity 6 / Entities 1.4+ manual pages, API signatures, and Netcode references.
  - ⛔ **Banned Tool**: `resolve-library-id` is strictly prohibited (returns unverified and obsolete pre-1.0 libraries).
  - ✅ **Direct Query**: Agents must call `query-docs` directly using the pre-approved `libraryId` entries defined in `.agents/references/context7_libraries.yaml`.
* **`ddg-websearch`**: Strict web search tool for external troubleshooting when local OKF and context7 do not resolve an issue.

