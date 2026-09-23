namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// MonoBehaviour authoring for the player ship prefab.
    /// Serialized in the Inspector; baked into ECS components at edit/build time.
    /// </summary>
    public class PlayerAuthoring : MonoBehaviour
    {
        public float ThrustAcceleration = 35.0f;
        public float MaxSpeed = 12.0f;
        public float Drag = 2.0f;
        public float RotationDamping = 18.0f;

        /// <summary>
        /// Prefab reference for laser projectiles (FX_Laser_Bullet_01.prefab).
        /// </summary>
        public GameObject LaserPrefab;

        /// <summary>
        /// Shots per second (e.g., 5.0f).
        /// </summary>
        public float FireRate = 5.0f;

        /// <summary>
        /// Local offset from player center to muzzle position along forward axis.
        /// </summary>
        public float3 MuzzleOffset = new float3(0f, 0f, 0.5f);

        /// <summary>
        /// Nested Baker that converts this MonoBehaviour into unmanaged ECS components.
        /// </summary>
        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTag());
                AddComponent(entity, new PlayerInput());
                AddComponent(entity, new PlayerMovementData
                {
                    ThrustAcceleration = authoring.ThrustAcceleration,
                    MaxSpeed = authoring.MaxSpeed,
                    Drag = authoring.Drag,
                    RotationDamping = authoring.RotationDamping,
                    CurrentVelocity = float3.zero
                });

                // Bake LaserSpawner if a laser prefab reference is provided
                if (authoring.LaserPrefab != null)
                {
                    AddComponent(entity, new LaserSpawner
                    {
                        LaserPrefab = GetEntity(authoring.LaserPrefab, TransformUsageFlags.Dynamic),
                        FireRate = authoring.FireRate,
                        CooldownTimer = 0f,
                        MuzzleOffset = authoring.MuzzleOffset
                    });
                }
            }
        }
    }
}
