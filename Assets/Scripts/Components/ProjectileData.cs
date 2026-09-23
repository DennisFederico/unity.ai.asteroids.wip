namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Kinematic motion and bounding volume for collision.
    /// Stores velocity vector, scalar speed, and bounding radius.
    /// </summary>
    public struct ProjectileData : IComponentData
    {
        /// <summary>
        /// Velocity vector (speed * direction) for movement.
        /// </summary>
        public float3 Velocity;

        /// <summary>
        /// Scalar speed magnitude (e.g., 35.0f).
        /// </summary>
        public float Speed;

        /// <summary>
        /// Bounding radius for sphere intersection tests (e.g., 0.3f).
        /// </summary>
        public float Radius;
    }
}
