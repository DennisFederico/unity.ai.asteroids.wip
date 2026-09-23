namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Time to live in seconds. Decrement each frame; destroy when <= 0.
    /// </summary>
    public struct Lifetime : IComponentData
    {
        /// <summary>
        /// Remaining lifetime in seconds.
        /// </summary>
        public float Value;
    }
}
