namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;

    /// <summary>
    /// MonoBehaviour authoring component for the GameScore singleton.
    /// Exposes the starting score value in the Unity Inspector.
    /// </summary>
    public class GameScoreAuthoring : MonoBehaviour
    {
        public int StartingScore = 0;

        /// <summary>
        /// Nested Baker initializing the GameScore singleton component.
        /// Uses TransformUsageFlags.None because the score entity has no transform needs.
        /// </summary>
        class Baker : Baker<GameScoreAuthoring>
        {
            public override void Bake(GameScoreAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GameScore { CurrentScore = authoring.StartingScore });
            }
        }
    }
}
