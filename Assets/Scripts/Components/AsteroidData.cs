namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Hierarchy tier, collision geometry, and gameplay values for asteroid entities.
    /// Blittable IComponentData struct with fields ordered for optimal cache alignment.
    /// </summary>
    public struct AsteroidData : IComponentData
    {
        public int Tier;       // 3 = Large, 2 = Medium, 1 = Small
        public float Radius;   // Bounding sphere radius: Large ~2.2f, Medium ~1.1f, Small ~0.5f
        public int ScoreValue; // Points awarded: Large 20, Medium 50, Small 100
        public int SplitCount; // Number of children spawned when destroyed (2 for Large/Medium, 0 for Small)
    }
}
