# Task-[ID]: [Descriptive Feature or System Name]

**Status:** `DRAFT` | `IN_PROGRESS` | `READY_FOR_REVIEW` | `COMPLETED`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---

## Section 1: Objective & Scope
* **Objective:** [Clear, single-responsibility objective in 1–2 sentences]
* **In Scope:**
  - [Specific component, system, or baker to create/modify]
* **Out of Scope:**
  - [Explicitly excluded dependencies, scene rigging, or other systems]

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/[ComponentName].cs`
* `Assets/Scripts/Systems/[SystemName].cs`
* `Assets/Scripts/Authoring/[AuthoringName].cs`

### Data Contracts (Components)
```csharp
namespace Game.Core
{
    // Blittable unmanaged component data
    public struct [ComponentName] : IComponentData
    {
        public float Value;
    }
}
```

### System Signature
```csharp
namespace Game.Core
{
    [BurstCompile]
    public partial struct [SystemName] : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }
    }
}
```

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Code Templates
*Source: `concepts/[relevant-concept].md`*
```csharp
// [Canonical pattern snippet copied from OKF card]
```

### Anti-Pattern Warnings
* ⛔ **DO NOT USE `IAspect`**: Aspects are marked `[Obsolete]` in Entities 1.4+. Use direct queries.
* ⛔ **NO GC IN BURST**: Zero `new`, `string`, or LINQ inside `ISystem` and jobs.
* ⛔ **NO DIRECT STRUCTURAL CHANGES IN JOBS**: Defer all adds/removes/spawns to an `EntityCommandBuffer`.
* ⛔ **NO MANUAL ECB PLAYBACK OR DISPOSAL**: System-managed command buffers are strictly record-and-forget. Unity plays them back automatically at group boundaries. Never call `ecb.Playback()` or `ecb.Dispose()`.
* ⛔ **NO ARBITRARY QUERY ASSIGNMENT**: Write `SystemAPI.Query` directly inside `foreach`.

---

## Section 4: Developer Definition of Done (DoD)
The Implementer cannot declare this task complete without satisfying all criteria:
- [ ] Code implemented strictly within Section 2 paths.
- [ ] Compilation check verified via Unity MCP (`unity-compile-check` skill):
  - ⛔ **Do not interact with the Unity Editor directly or using the filesystem** (never run `Unity -batchmode` or inspect `Editor.log`).
  - ✅ **Always use the MCP**: Discover port via `unity_list_instances`, then call `unity_get_compilation_errors` (**0 errors**, **0 Burst warnings**).
  - [ ] Domain reload verified via `unity_console_log` (**0 exceptions**).
- [ ] Any compiler diagnostic resolved using `concepts/common-compilation-errors.md`.
- [ ] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
The Reviewer executes these checks before approving handoff:
- [ ] **Static Audit**: All unmanaged systems and jobs decorated with `[BurstCompile]`.
- [ ] **Allocation Audit**: Zero GC allocations observed during profiling.
- [ ] **ECB Lifecycle Audit**: Zero calls to `.Playback()` or `.Dispose()` on system-managed command buffers.
- [ ] **Test Plan**: Automated headless tests (`Assets/Tests/`) executed with clean assertions, or PlayMode sanity confirmed.
- [ ] **Zero-Error Gate**: No active compiler, runtime, or test errors.

---

## Actionable Implementation Checklist
*(The Implementer updates `- [x]` as progress is made)*
- [ ] Step 1: Declare component structs in `Assets/Scripts/Components/`
- [ ] Step 2: Implement Baker and Authoring `MonoBehaviour` in `Assets/Scripts/Authoring/`
- [ ] Step 3: Implement `ISystem` logic in `Assets/Scripts/Systems/`
- [ ] Step 4: Run compilation check via MCP (discover port with `unity_list_instances`, verify 0 errors via `unity_get_compilation_errors`)
- [ ] Step 5: Request user-mediated handoff to Reviewer

---

## Reviewer Sign-off & Verdict
* **Review Date:** `YYYY-MM-DD`
* **Verdict:** `PENDING` | `PASSED` | `CHANGES_REQUESTED`
* **Findings:**
  * [Notes or verification logs here]

