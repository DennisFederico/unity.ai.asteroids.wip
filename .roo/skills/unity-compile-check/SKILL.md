---
name: unity-compile-check
description: >-
  Use when checking C# compilation status in the Unity Editor, verifying compiler diagnostics or Burst warnings,
  checking domain reload logs, or confirming the Definition of Done (DoD) after writing or modifying scripts.
user-invocable: true
allowed-tools: view_file call_mcp_tool
---

# Unity Editor Compilation & Diagnostic Verification

This skill guides agents to verify C# compilation, domain reload integrity, and Definition of Done (DoD) criteria using the live `anklebreaker-unity-mcp` server.

---

## 1. Cardinal Rule: MCP-First Unity Interaction

* ⛔ **Do not interact with the Unity Editor directly or using the filesystem**:
  - Never execute Unity in batchmode via terminal commands (`Unity -quit -batchmode -projectPath . -logFile -`).
  - Never inspect filesystem log files (`Editor.log`, `upm.log`).
* ✅ **Always use the MCP**:
  - The live Unity Editor is already running and connected via `anklebreaker-unity-mcp`.
  - All compilation diagnostics, error logs, and domain reload checks must be polled via MCP.

---

## 2. Step 1: Discover Editor Instance & Port

When multiple editor instances exist or after editor restarts, identify the target instance port:

1. **List running instances**:
   ```json
   {
     "ServerName": "anklebreaker-unity-mcp",
     "ToolName": "unity_list_instances",
     "Arguments": {}
   }
   ```
2. **Select the target instance**:
   - Identify the instance matching this project's name or port.
   - Call `unity_select_instance`:
     ```json
     {
       "ServerName": "anklebreaker-unity-mcp",
       "ToolName": "unity_select_instance",
       "Arguments": { "projectName": "<ProjectName>" }
     }
     ```
   - *(Optional parallel safety)*: Include `"port": <port_number>` directly on subsequent tool calls when multiple agents share the MCP process.

---

## 3. Step 2: Trigger Refresh & Check Compilation Diagnostics

1. **Trigger Asset Refresh (if domain reload has not fired)**:
   ```json
   {
     "ServerName": "anklebreaker-unity-mcp",
     "ToolName": "unity_execute_menu_item",
     "Arguments": { "menu_item": "Assets/Refresh" }
   }
   ```
2. **Retrieve Compilation Errors & Warnings**:
   ```json
   {
     "ServerName": "anklebreaker-unity-mcp",
     "ToolName": "unity_get_compilation_errors",
     "Arguments": { "severity": "all" }
   }
   ```
   - Confirm **0 compiler errors**.
   - Confirm **0 Burst compiler warnings**.

3. **Check Console for Domain Reload Exceptions**:
   ```json
   {
     "ServerName": "anklebreaker-unity-mcp",
     "ToolName": "unity_console_log",
     "Arguments": { "log_types": ["Error", "Exception"] }
   }
   ```
   - Confirm **0 domain reload exceptions**.

---

## 4. Step 3: Error Remediation via OKF Knowledge

If any compilation error or Burst warning appears:
1. **Do not guess or invent APIs**:
   - Note the diagnostic code (e.g. `CS0246`, `CS0208`, `CS1061`, or Burst error text).
2. **Query the Knowledge Base**:
   - Invoke `ecs-knowledge` (`kiso-okf:get_concept_content` with `conceptId: "common-compilation-errors"`).
   - Apply the canonical fix documented in the troubleshooting card.
3. **Re-poll `unity_get_compilation_errors`**:
   - Repeat until the compiler reports 0 errors.

