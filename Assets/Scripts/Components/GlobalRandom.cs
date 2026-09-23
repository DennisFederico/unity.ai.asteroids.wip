namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Deterministic pseudo-random generator singleton for Burst-compatible
    /// spawners and systems. Holds a Unity.Mathematics.Random value-type.
    /// </summary>
    public struct GlobalRandom : IComponentData
    {
        public Unity.Mathematics.Random Value;
    }
}
