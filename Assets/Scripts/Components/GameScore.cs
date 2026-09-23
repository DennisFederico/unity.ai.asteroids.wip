namespace Asteroids.Core
{
    using Unity.Entities;

    /// <summary>
    /// Singleton component storing the active player score.
    /// Initialized at bake time via GameScoreAuthoring.Baker.
    /// </summary>
    public struct GameScore : IComponentData
    {
        public int CurrentScore;
    }
}
