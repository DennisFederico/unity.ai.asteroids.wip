namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Kinematic motion vectors for asteroid drift and tumbling.
    /// Separated from AsteroidData for maximum chunk memory packing and SIMD cache locality.
    /// Linear velocity is constrained to the XZ gameplay plane (Y = 0).
    /// </summary>
    public struct DriftVelocity : IComponentData
    {
        public float3 Linear;  // Translation velocity on XZ plane (Y = 0)
        public float3 Angular; // 3-axis Euler angular velocity in radians per second
    }
}
