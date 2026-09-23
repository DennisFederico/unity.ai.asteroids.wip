namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    // Snapshot of player input published each frame
    public struct PlayerInput : IComponentData
    {
        public float2 Movement;         // WASD / Stick direction on XZ plane
        public float3 AimWorldPosition; // Cursor intersection with Y=0 plane
        public bool FireHeld;           // Continuous trigger state
        public bool FireTriggered;      // Transient edge press (WasPressedThisFrame)
    }
}
