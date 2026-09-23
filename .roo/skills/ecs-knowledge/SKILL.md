---
name: ecs-knowledge
description: >-
  Use when designing or implementing Unity ECS/DOTS systems, checking Entities 1.4+ API syntax,
  verifying architectural constraints, looking up anti-patterns, resolving compilation errors (e.g. CS0246, CS0208),
  or whenever a task contract references an OKF concept ID.
user-invocable: true
allowed-tools: call_mcp_tool
---

# Unity ECS Knowledge (kiso-okf)

This skill guides you to search, retrieve, and apply authoritative Unity ECS / DOTS architecture patterns from the Open Knowledge Format (OKF) repository served by the `kiso-okf` MCP server.

## 1. Search for Relevant Concepts
Call `kiso-okf:search_concepts` with descriptive keywords from the task at hand:

### In Antigravity / Generic MCP Client:
```json
{
  "ServerName": "kiso-okf",
  "ToolName": "search_concepts",
  "Arguments": {
    "text": "input system"
  }
}
```

### In ZooCode / Roo Code:
```json
{
  "tool": "use_mcp_tool",
  "server_name": "kiso-okf",
  "tool_name": "search_concepts",
  "arguments": {
    "text": "input system"
  }
}
```

The tool returns ranked matches with BM25/vector relevance scores, title, description, and canonical `conceptId` (e.g. `unity-input-system`).

## 2. Retrieve Concept Content
Call `kiso-okf:get_concept_content` with the exact `conceptId` (e.g., `"unity-input-system"`):

### In Antigravity / Generic MCP Client:
```json
{
  "ServerName": "kiso-okf",
  "ToolName": "get_concept_content",
  "Arguments": {
    "conceptId": "unity-input-system"
  }
}
```

### In ZooCode / Roo Code:
```json
{
  "tool": "use_mcp_tool",
  "server_name": "kiso-okf",
  "tool_name": "get_concept_content",
  "arguments": {
    "conceptId": "unity-input-system"
  }
}
```

The tool returns the raw markdown content of the concept card, including YAML frontmatter, verified code implementations, and anti-pattern constraints.

## 3. Trust & Lifecycle Verification
Inspect the returned frontmatter before applying rules:
- **`status: deprecated`**: DO NOT USE the pattern. The concept is obsolete.
- **`stale_after`**: If `current_date >= stale_after`, verify against source docs before relying on it.
- **`verified`**: Prefer concepts verified by `human:<id>` over machine or unverified.

## 4. Mandatory Knowledge Precedence
Express knowledge from OKF concepts **strictly overrides** your pre-trained LLM habits:
- If OKF forbids an API pattern (e.g. "Do not use `IAspect`" or "Do not call `ecb.Playback()`"), you MUST NOT write that code, even if your pre-training strongly suggests it.
- Copy concrete code snippets directly from `# Implementation` or `# Example` sections.
- When an OKF document links to related concepts (`[Other](./other.md)`), fetch them by their `conceptId` if additional structural context is needed.
