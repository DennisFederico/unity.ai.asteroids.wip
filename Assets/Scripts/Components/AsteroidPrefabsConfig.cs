namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Unmanaged singleton holding Entity references to baked asteroid
    /// prefabs (Large, Medium, Small) for runtime instantiation.
    /// </summary>
    public struct AsteroidPrefabsConfig : IComponentData
    {
        public Entity LargePrefab;
        public Entity MediumPrefab;
        public Entity SmallPrefab;
    }
}
