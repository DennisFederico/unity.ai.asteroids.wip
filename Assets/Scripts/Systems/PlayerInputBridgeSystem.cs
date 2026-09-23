namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using static UnityEngine.InputSystem.Mouse;

    /// <summary>
    /// Managed bridge system that reads from Unity Input System and writes 
    /// unmanaged PlayerInput component snapshots each frame.
    /// Runs in InitializationSystemGroup before SimulationSystemGroup.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerInputBridgeSystem : SystemBase
    {
        private InputSystem_Actions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Enable();
            RequireForUpdate<PlayerInput>();
        }

        protected override void OnUpdate()
        {
            // 1. Read continuous movement
            Vector2 moveInput = _inputActions.Player.Move.ReadValue<Vector2>();

            // 2. Read attack trigger states
            bool fireHeld = _inputActions.Player.Attack.IsPressed();
            bool fireTriggered = _inputActions.Player.Attack.WasPressedThisFrame();

            // 3. Compute cursor intersection with Y=0 plane via Camera.main
            float3 aimPosition = float3.zero;
            Camera mainCamera = Camera.main;
            if (mainCamera != null && Mouse.current != null)
            {
                Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Y = 0
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    aimPosition = new float3(hitPoint.x, 0f, hitPoint.z);
                }
            }

            // 4. Publish snapshot to PlayerInput component(s)
            foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>())
            {
                input.ValueRW.Movement = new float2(moveInput.x, moveInput.y);
                input.ValueRW.AimWorldPosition = aimPosition;
                input.ValueRW.FireHeld = fireHeld;
                input.ValueRW.FireTriggered = fireTriggered;
            }
        }

        protected override void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
            }
        }
    }
}
