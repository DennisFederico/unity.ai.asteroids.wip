namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    // Kinematics and control configuration
    public struct PlayerMovementData : IComponentData
    {
        public float ThrustAcceleration; // e.g., 30.0f
        public float MaxSpeed;           // e.g., 12.0f
        public float Drag;               // e.g., 2.0f
        public float RotationDamping;    // e.g., 18.0f
        public float3 CurrentVelocity;   // Accumulated linear velocity
    }
}
