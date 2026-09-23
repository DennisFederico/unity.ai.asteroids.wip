namespace Asteroids.Core
{
    using Unity.Entities;

    // Singleton holding infinite map bubble radii
    public struct InfiniteMapConfig : IComponentData
    {
        public float ViewportBufferRadius; // Inner ring edge strictly outside camera frustum (e.g. 35m)
        public float SpawnOuterRadius;     // Outer ring edge (e.g. 50m)
        public float DespawnRadius;        // Out-of-bounds distance (e.g. sqrt(50) * 50m ≈ 353.55m)
        public float DespawnRadiusSq;      // Precomputed for fast Burst comparison (353.55^2 ≈ 125,000)
    }
}
