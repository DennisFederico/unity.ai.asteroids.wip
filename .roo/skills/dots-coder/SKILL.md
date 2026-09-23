---
name: dots-coder
description: >-
  Use when implementing C# ECS components, ISystem structs, IJobEntity jobs, or Bakers from an approved
  task contract, verifying C# compilation status in Unity, or resolving Unity DOTS compiler errors and Burst warnings.
user-invocable: true
allowed-tools: view_file write_to_file replace_file_content run_command call_mcp_tool
---

# Unity DOTS Coder (Implementer)

Guide the Implementer persona to execute an approved atomic task contract with surgical precision and verify clean compilation via Unity MCP.

## 1. Ingestion & Contract Review
1. Read the target task contract (`tasks/task-XXX.md`).
2. Review Section 2 (API Contracts) and Section 3 (Injected OKF Patterns).
3. If any API signature, component field, or behavior is ambiguous:
   - Query the knowledge base via `ecs-knowledge`.
   - Check `concepts/common-compilation-errors.md` for known gotchas.
   - **Ask the user or architect for clarification rather than hallucinating interfaces.**

## 2. Implementation Rules (Entities 1.4+)
All code must be written strictly within `Assets/Scripts/`:

1. **Systems (`ISystem`)**:
   - Declare as `public partial struct MySystem : ISystem`.
   - Decorate with `[BurstCompile]`.
   - Use `SystemAPI.Query<RefRO<T>, RefRW<U>>()` directly inside `foreach`. Never assign queries to local variables.
   - Use `SystemBase` only if managed GameObject operations are explicitly required by the contract.
2. **Components (`IComponentData`)**:
   - Must be `public struct` containing only unmanaged blittable types.
   - For components without spatial/transform presence, use `TransformUsageFlags.None` in Bakers.
   - **NEVER** use `IAspect` (marked `[Obsolete]` in Entities 1.4+).
3. **Structural Changes**:
   - Never add/remove components or instantiate/destroy entities inside job loops.
   - Record commands into an `EntityCommandBuffer` (`ECB`) retrieved from `BeginSimulationEntityCommandBufferSystem.Singleton` or `EndSimulationEntityCommandBufferSystem.Singleton`.
   - **ECB Lifecycle (Record-and-Forget)**: System-managed command buffers are strictly append-only recordings. Unity automatically manages playback and disposal at system group boundaries. Simply record your commands and exit the method. **NEVER** call `ecb.Playback()` or `ecb.Dispose()` on system-managed ECBs (causes fatal runtime exceptions).
4. **Zero Allocations**:
   - Absolutely no `new`, `string`, LINQ, or managed collections inside `ISystem.OnUpdate` or jobs.

## 3. Mandatory Definition of Done (DoD) Verification
Before declaring the task complete, verify compilation via the `unity-compile-check` skill workflow:

* ⛔ **Do not interact with the Unity Editor directly or using the filesystem** (never run `Unity -batchmode` or inspect `Editor.log`).
* ✅ **Always use the MCP**:

1. **Editor Port Discovery & Selection**:
   - Discover active editor instances:
     ```json
     { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_list_instances", "Arguments": {} }
     ```
   - Select target instance by project name or port:
     ```json
     { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_select_instance", "Arguments": { "projectName": "<ProjectName>" } }
     ```
2. **Trigger Script Compilation**:
   - If auto-refresh has not fired, call:
     ```json
     { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_execute_menu_item", "Arguments": { "menu_item": "Assets/Refresh" } }
     ```
3. **Check Diagnostics**:
   - Call `unity_get_compilation_errors`:
     ```json
     { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_get_compilation_errors", "Arguments": { "severity": "all" } }
     ```
     Confirm **0 compiler errors** and **0 Burst warnings**.
   - Call `unity_console_log` with `log_types: ["Error", "Exception"]` to ensure no domain reload exceptions.
4. **Remediation via OKF Knowledge**:
   - If any compilation error or Burst warning appears, **do NOT blindly guess fixes or improvise APIs**.
   - Immediately query the knowledge base via `ecs-knowledge` (or read `concepts/common-compilation-errors.md`) using the error code or keyword (e.g., `CS0246`, `CS0208`, `UnityObjectRef`, `IEnableableComponent`, `SystemAPI.Query` redundancy, `FromObject`).
   - Apply the canonical fix documented in the troubleshooting card.
   - Re-poll `unity_get_compilation_errors` to confirm resolution.
   - Do NOT hand off code with unresolved compilation errors.

## 4. Progress Tracking & Handoff
1. Mark completed items in the task's actionable checklist (`- [x]`). If using a native todo tool (e.g. ZooCode Todo), keep it synchronized.
2. Once the code compiles cleanly (0 errors, 0 Burst warnings) and all checklist items are checked, report task completion to the user/orchestrator with a summary of changes and compile verification status.
3. Do not attempt to automatically switch modes or execute scene modifications.

