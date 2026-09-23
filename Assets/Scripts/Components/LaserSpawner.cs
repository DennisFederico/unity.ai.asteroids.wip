namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Firing configuration attached to the player ship entity.
    /// Controls laser prefab reference, fire rate, cooldown, and muzzle position.
    /// </summary>
    public struct LaserSpawner : IComponentData
    {
        /// <summary>
        /// Entity reference to the laser prefab asset.
        /// </summary>
        public Entity LaserPrefab;

        /// <summary>
        /// Shots per second (e.g., 5.0f). Cooldown = 1 / FireRate.
        /// </summary>
        public float FireRate;

        /// <summary>
        /// Internal countdown timer in seconds. Decrements each frame.
        /// </summary>
        public float CooldownTimer;

        /// <summary>
        /// Local offset from player center to muzzle position.
        /// </summary>
        public float3 MuzzleOffset;
    }
}
