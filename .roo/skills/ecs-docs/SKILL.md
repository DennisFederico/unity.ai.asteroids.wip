---
name: ecs-docs
description: >-
  Use when official Unity 6 / Entities 1.4+ documentation, manual pages, API signatures, or Netcode specifications
  are needed to ground architecture, verify method signatures, or resolve compiler diagnostics not fully covered in OKF concepts.
user-invocable: true
allowed-tools: view_file call_mcp_tool
---

# Unity ECS & Netcode Documentation Grounding (`context7`)

This skill guides agents to retrieve official, verified Unity 6.6 and Entities 1.4+ documentation using the `context7` MCP server without picking up obsolete pre-1.0 packages.

---

## 1. The Cardinal Direct-Query Rule

* ⛔ **DO NOT CALL `resolve-library-id`**:
  - The `resolve-library-id` tool searches arbitrary community and legacy repositories that often return obsolete Entities 0.17/0.51 packages.
  - Calling it wastes a tool round-trip and introduces hallucination risks.
* ✅ **CALL `query-docs` DIRECTLY**:
  - Context7 officially allows direct calls to `query-docs` when provided with a canonical `/org/project` ID.
  - Match your requirement against the pre-approved libraries list below and query `query-docs` immediately.

---

## 2. Pre-Approved Library Registry

Inspect the local registry at `references/context7_libraries.yaml`:

| Target Need | `libraryId` | Description |
| :--- | :--- | :--- |
| **Entities Manual & Concepts** | `/websites/unity3d_packages_com_unity_entities_6_5_manual` | Architecture, Subscenes, Baker workflows, ECB lifecycles, and TransformUsageFlags. |
| **Entities API Reference** | `/websites/unity3d_packages_com_unity_entities_1_4` | Exact C# struct definitions, method signatures, `SystemAPI.Query` filters, and compiler error resolution. |
| **Netcode for Entities (DOTS)** | `/websites/unity3d_packages_com_unity_netcode_1_9` | Multiplayer DOTS, client-server prediction, ghost snapshot interpolation, and RPCs. |
| **Netcode for GameObjects (NGO)** | `/websites/unity3d_packages_com_unity_netcode_gameobjects_2_11` | Hybrid multiplayer, NetworkBehaviour, and managed GameObject networking. |

---

## 3. Tool Invocation Format

### In Antigravity / Generic MCP Client:
```json
{
  "ServerName": "context7",
  "ToolName": "query-docs",
  "Arguments": {
    "libraryId": "/websites/unity3d_packages_com_unity_entities_6_5_manual",
    "query": "EntityCommandBuffer record and forget lifecycle in SimulationSystemGroup"
  }
}
```

### In ZooCode / Roo Code:
```json
{
  "tool": "use_mcp_tool",
  "server_name": "context7",
  "tool_name": "query-docs",
  "arguments": {
    "libraryId": "/websites/unity3d_packages_com_unity_entities_1_4",
    "query": "SystemAPI.Query RefRO RefRW method signatures"
  }
}
```

---

## 4. Grounding Precedence
1. **Primary Grounding**: Always search the Open Knowledge Format repository via `ecs-knowledge` (`kiso-okf`) first for verified internal patterns.
2. **Documentation Fallback**: When exact API signatures or specific Unity 6 manual pages are needed, invoke this skill (`ecs-docs`) to query `context7:query-docs`.
3. **Web Search Fallback**: Use `ddg-websearch` only if both `ecs-knowledge` and `ecs-docs` do not resolve the issue.

