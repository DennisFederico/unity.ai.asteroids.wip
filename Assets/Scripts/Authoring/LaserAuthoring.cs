namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// MonoBehaviour authoring for the laser prefab (FX_Laser_Bullet_01).
    /// Serialized in the Inspector; baked into ECS components at edit/build time.
    /// </summary>
    public class LaserAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Projectile travel speed (default 35.0f).
        /// </summary>
        public float Speed = 35.0f;

        /// <summary>
        /// Time to live in seconds before despawn (default 1.8f).
        /// </summary>
        public float LifetimeDuration = 1.8f;

        /// <summary>
        /// Collision bounding radius (default 0.3f).
        /// </summary>
        public float CollisionRadius = 0.3f;

        /// <summary>
        /// Nested Baker that converts this MonoBehaviour into unmanaged ECS components.
        /// Uses TransformUsageFlags.Dynamic for runtime transform updates.
        /// </summary>
        class Baker : Baker<LaserAuthoring>
        {
            public override void Bake(LaserAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Add tag for system filtering
                AddComponent(entity, new LaserTag());

                // Add projectile data — Velocity set to zero at bake time, filled by LaserShootingSystem
                AddComponent(entity, new ProjectileData
                {
                    Velocity = float3.zero,
                    Speed = authoring.Speed,
                    Radius = authoring.CollisionRadius
                });

                // Add lifetime for automatic despawn
                AddComponent(entity, new Lifetime
                {
                    Value = authoring.LifetimeDuration
                });
            }
        }
    }
}
