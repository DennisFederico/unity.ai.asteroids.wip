namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Tag identifying laser projectile entities.
    /// Zero-size marker component for archetype partitioning.
    /// </summary>
    public struct LaserTag : IComponentData { }
}
