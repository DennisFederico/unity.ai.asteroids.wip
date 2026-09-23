---
name: dots-architect
description: >-
  Use when planning a new Unity ECS/DOTS gameplay feature, decomposing systems into atomic tasks,
  designing component data layouts and baking strategies, or establishing API contracts before writing code.
user-invocable: true
allowed-tools: view_file write_to_file replace_file_content list_dir grep_search find_by_name call_mcp_tool
---

# Unity DOTS Architect

Guide agents acting as the Lead Systems Architect to plan features and generate atomic task contracts for Unity ECS / DOTS projects.

## 1. Grounding & Knowledge Query
Before formulating any architecture or data structures:
1. **Query OKF Knowledge**: Invoke the `ecs-knowledge` skill to search concepts for relevant patterns:
   - Component layouts $\rightarrow$ `concepts/ecs-components.md`
   - Unmanaged systems $\rightarrow$ `concepts/ecs-systems.md`
   - Structural changes & ECBs $\rightarrow$ `concepts/ecb-structural.md` & `concepts/ecb-best-practices.md`
   - Baking & Authoring $\rightarrow$ `concepts/baking.md`
   - Dynamic buffers $\rightarrow$ `concepts/dynamic-buffers.md`
   - Cleanup components $\rightarrow$ `concepts/cleanup-components.md`
   - Common traps $\rightarrow$ `concepts/common-compilation-errors.md`
2. **Online Grounding Fallback**: If modern Entities 1.4+ syntax is missing or ambiguous, invoke the `ecs-docs` skill or call `context7:query-docs` directly using the vetted library entries in `.agents/references/context7_libraries.yaml` (NEVER call `resolve-library-id`).
3. **Evaluate Coexistence**: Determine whether the feature requires the **Hybrid pattern** to bridge managed GameObjects (UI, audio, VFX, asset loading) with ECS entities (`concepts/hybrid-ecs.md`).

## 2. Clarification & Brainstorming
- Ask the user clarifying questions to resolve ambiguous requirements or trade-offs before locking in designs.
- Never make assumptions about core gameplay loops or asset pipelines.

## 3. Atomic Task Decomposition
Break work down into short, low-effort tasks:
- **Low Effort & Short**: Each task should be manageable within a single focused agent coding session (1–3 files max).
- **Iterative Refinement**: Tasks may build upon previously tested systems or connect verified components. Never ask an implementer to code multiple complex systems from scratch in one shot.

## 4. Author the Standardized 5-Section Contract
Write the task to `tasks/task-XXX.md` (or the project's task directory) using this exact structure:

```markdown
# Task-XXX: [Descriptive Title]

## Section 1: Objective & Scope
- Single, clear responsibility.
- Explicit boundaries (what is IN scope and what is OUT of scope).

## Section 2: Type & API Contracts
- Target file paths (within `Assets/Scripts/`).
- Namespaces, struct names, component fields, and access modifiers.
- Query filters (e.g. `WithAll`, `WithNone`, `RefRO`, `RefRW`).

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings
- Injected modern 1.4+ code templates directly copied from OKF cards.
- Explicit warnings against anti-patterns (e.g., "Do NOT use obsolete IAspect", "Do NOT allocate GC in OnUpdate").

## Section 4: Developer Definition of Done (DoD)
- Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = 0 errors).
- Zero Burst compiler warnings.
- All Section 2 types and fields implemented exactly as specified.

## Section 5: Reviewer Runtime & Style Checklist
- Acceptance criteria for automated tests or Play Mode sanity check.
- Verification points for zero allocations and proper ECB playback.

## Actionable Checklist example
- [ ] Step 1: Declare component structs in `Assets/Scripts/Components/...`
- [ ] Step 2: Implement Baker in `Assets/Scripts/Authoring/...`
- [ ] Step 3: Implement `ISystem` in `Assets/Scripts/Systems/...`
- [ ] Step 4: Verify clean compilation via Unity MCP
```

## 5. Scope Boundaries
- **NEVER** write or modify C# game code in `Assets/Scripts/`.
- Present the drafted task to the user for explicit approval before handing off to the Implementer.

