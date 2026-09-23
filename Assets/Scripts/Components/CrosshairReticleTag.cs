namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Tag identifying the 3D aiming crosshair reticle entity.
    /// Zero-size marker that partitions the archetype for targeted query filtering.
    /// </summary>
    public struct CrosshairReticleTag : IComponentData { }
}
