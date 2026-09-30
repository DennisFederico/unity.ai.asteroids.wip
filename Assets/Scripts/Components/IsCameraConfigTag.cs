namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Singleton marker tag identifying the unique camera configuration entity in the world.
    /// Used for quick entity lookup by systems that need to read camera settings.
    /// </summary>
    public struct IsCameraConfigTag : IComponentData { }
}
