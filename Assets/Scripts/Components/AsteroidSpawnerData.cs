namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Unmanaged singleton storing local density thresholds and cadence
    /// pacing intervals for the density-regulated asteroid ring spawner.
    /// </summary>
    public struct AsteroidSpawnerData : IComponentData
    {
        public float BaseSpawnInterval;   // Standard cadence within target density band (e.g. 2.5s)
        public float FastSpawnInterval;   // Accelerated cadence when under-populated (e.g. 0.8s)
        public float CooldownTimer;       // Current countdown timer
        public float DensityRadius;       // Local engagement sampling radius (e.g. 70m)
        public float DensityRadiusSq;     // Precomputed 70^2 = 4900 for fast Burst evaluation
        public int TargetLocalDensityMin; // Lower threshold (e.g. 8 asteroids)
        public int TargetLocalDensityMax; // Maximum cap (e.g. 16 asteroids)
    }
}
