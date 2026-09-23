namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// MonoBehaviour authoring component for the density-regulated asteroid ring spawner.
    /// Exposes inspector fields for prefab references, density thresholds, and cadence pacing.
    /// </summary>
    public class AsteroidSpawnerAuthoring : MonoBehaviour
    {
        [Header("Prefab References")]
        public GameObject LargeAsteroidPrefab;
        public GameObject MediumAsteroidPrefab;
        public GameObject SmallAsteroidPrefab;

        [Header("Density Regulation")]
        [Tooltip("Sampling radius around player to regulate hazard density (meters)")]
        public float DensityRadius = 70.0f;

        [Tooltip("Minimum desired asteroid count in density radius; spawns accelerate if below this")]
        public int TargetLocalDensityMin = 8;

        [Tooltip("Maximum allowed asteroid count in density radius; spawns halt if reached")]
        public int TargetLocalDensityMax = 16;

        [Header("Spawn Cadence (Seconds)")]
        public float BaseSpawnInterval = 2.5f;
        public float FastSpawnInterval = 0.8f;
        public uint Seed = 1337u;

        /// <summary>
        /// Nested Baker converting MonoBehaviour fields to unmanaged ECS singletons.
        /// Uses TransformUsageFlags.None since the spawner is a data-only singleton.
        /// </summary>
        class Baker : Baker<AsteroidSpawnerAuthoring>
        {
            public override void Bake(AsteroidSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                // Initialize deterministic random generator with non-zero seed
                AddComponent(entity, new GlobalRandom
                {
                    Value = new Unity.Mathematics.Random(math.max(1u, authoring.Seed))
                });

                // Bake density regulation and cadence configuration
                AddComponent(entity, new AsteroidSpawnerData
                {
                    BaseSpawnInterval = authoring.BaseSpawnInterval,
                    FastSpawnInterval = authoring.FastSpawnInterval,
                    CooldownTimer = 0.5f,
                    DensityRadius = authoring.DensityRadius,
                    DensityRadiusSq = authoring.DensityRadius * authoring.DensityRadius,
                    TargetLocalDensityMin = authoring.TargetLocalDensityMin,
                    TargetLocalDensityMax = authoring.TargetLocalDensityMax
                });

                // Convert prefab GameObject references to Entity references with Dynamic transform
                AddComponent(entity, new AsteroidPrefabsConfig
                {
                    LargePrefab = GetEntity(authoring.LargeAsteroidPrefab, TransformUsageFlags.Dynamic),
                    MediumPrefab = GetEntity(authoring.MediumAsteroidPrefab, TransformUsageFlags.Dynamic),
                    SmallPrefab = GetEntity(authoring.SmallAsteroidPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
