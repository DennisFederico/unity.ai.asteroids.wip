namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    public class InfiniteMapAuthoring : MonoBehaviour
    {
        [Header("Spawn Ring Dimensions")]
        [Tooltip("Minimum safe spawn distance outside the camera frustum")]
        public float ViewportBufferRadius = 35.0f;

        [Tooltip("Outer limit of the spawn ring")]
        public float SpawnOuterRadius = 50.0f;

        [Header("Out-of-Bounds Culling")]
        [Tooltip("Area multiplier for out-of-bounds destruction (default: 50x area)")]
        public float OutOfBoundsAreaMultiplier = 50.0f;

        class Baker : Baker<InfiniteMapAuthoring>
        {
            public override void Bake(InfiniteMapAuthoring authoring)
            {
                // Pure data singleton entity
                Entity entity = GetEntity(TransformUsageFlags.None);

                // Area = pi * r^2. If Area_oob = multiplier * Area_spawn,
                // then r_oob = sqrt(multiplier) * r_spawn.
                float despawnRadius = Mathf.Sqrt(authoring.OutOfBoundsAreaMultiplier) * authoring.SpawnOuterRadius;

                AddComponent(entity, new InfiniteMapConfig
                {
                    ViewportBufferRadius = authoring.ViewportBufferRadius,
                    SpawnOuterRadius = authoring.SpawnOuterRadius,
                    DespawnRadius = despawnRadius,
                    DespawnRadiusSq = despawnRadius * despawnRadius
                });
            }
        }
    }
}
