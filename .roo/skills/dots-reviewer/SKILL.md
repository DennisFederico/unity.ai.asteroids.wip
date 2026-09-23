---
name: dots-reviewer
description: >-
  Use when auditing completed ECS code for anti-patterns and Burst compliance, formulating automated headless
  test plans (Unity.Entities.Testing), executing PlayMode runtime sanity checks, or gating task completion.
user-invocable: true
allowed-tools: view_file write_to_file replace_file_content run_command call_mcp_tool
---

# Unity DOTS Reviewer & Gatekeeper

Guide the Reviewer persona to audit code, formulate test strategies, run Play Mode sanity checks via Unity MCP, and gate task handoffs.

## 1. Task Ingestion & Audit Scope
1. Read the target task contract (`tasks/task-XXX.md`).
2. Review Section 2 (API Contracts), Section 4 (Coder DoD status), and Section 5 (Reviewer Checklist).
3. Confirm that the Coder has already verified clean compilation (0 compiler errors, 0 Burst warnings) before initiating the review.

## 2. Static Code & DOTS 1.4+ Compliance Audit
Inspect the implemented C# files under `Assets/Scripts/`:

1. **Burst Compilation & Safety**:
   - Ensure unmanaged `ISystem` structs and jobs are marked with `[BurstCompile]`.
   - Verify zero managed leaks (`string`, `class`, `GameObject`, boxed interfaces) inside Burst contexts.
2. **Zero GC Allocations**:
   - Verify that `OnUpdate` and job execution bodies contain zero GC allocations (no `new`, no closures/lambdas, no LINQ, no string concatenations).
3. **Structural Changes & Sync Points**:
   - Verify that all entity mutations (create/destroy, add/remove components) are properly recorded via an `EntityCommandBuffer` (`ECB`).
   - Check the ECB playback system (e.g., `BeginSimulationEntityCommandBufferSystem`) to ensure it does not introduce unnecessary sync points (`concepts/ecb-structural.md`).
   - Strictly audit for and reject any code calling `.Playback()` or `.Dispose()` on system-managed command buffers (fails static audit).
4. **Anti-Pattern Scans**:
   - Query `concepts/aspects.md` to ensure zero usage of `[Obsolete]` `IAspect`.
   - Query `concepts/common-compilation-errors.md` to catch subtle runtime traps (e.g. redundant query constraints, incorrect buffer lookups).

## 3. Test Planning & Automated Testing (UTF)
1. **Formulate Test Plan**:
   - Document a concise test specification:
     - **Automated Tests**: Unit tests asserting deterministic state mutations, system ticks, or component values.
     - **Manual Sanity Steps**: Quick in-editor checks if automated testing is impractical.
2. **Automated Headless ECS Testing**:
   - When writing automated tests under `Assets/Tests/`, prefer headless `Unity.Entities.Testing`:
     - Create an isolated test `World`.
     - Spawn entities with required components.
     - Tick the system: `system.Update(world.Unmanaged);`.
     - Assert expected component data using standard NUnit assertions (`Assert.AreEqual`, `Assert.IsTrue`).
   - Run tests and confirm clean passage.

## 4. Runtime Play Mode Sanity Check (When Applicable)
When scene-level or visual verification is required:
1. Clear existing console logs:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_console_clear", "Arguments": {} }
   ```
2. Enter Play Mode:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_play_mode", "Arguments": { "action": "play" } }
   ```
3. Inspect runtime logs for unexpected exceptions, Burst aborts, or error spam:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_console_log", "Arguments": { "log_types": ["Error", "Exception", "Warning"] } }
   ```
4. Exit Play Mode:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_play_mode", "Arguments": { "action": "stop" } }
   ```

## 5. Zero-Error Gatekeeping Report
Append your review verdict to the bottom of the task contract:
- **Verdict**: `PASSED` or `CHANGES_REQUESTED`.
- **Static Audit Summary**: Burst compliance, zero allocations, ECB safety.
- **Test Results**: Passing automated unit tests or manual verification observations.
- **Diagnostics**: Console log status (0 exceptions, 0 runtime errors).
- **Remediation Details**: If `CHANGES_REQUESTED`, provide exact file paths, line numbers, and actionable remediation notes. Never approve a task with active runtime errors.

