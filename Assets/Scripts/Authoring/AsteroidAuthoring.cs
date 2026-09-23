namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// MonoBehaviour authoring component for asteroid prefabs.
    /// Exposes inspector fields for tier configuration, collision geometry,
    /// scoring values, split count, and initial velocity vectors.
    /// </summary>
    public class AsteroidAuthoring : MonoBehaviour
    {
        [Tooltip("3 = Large, 2 = Medium, 1 = Small")]
        public int Tier = 3;

        public float Radius = 2.2f;

        public int ScoreValue = 20;

        public int SplitCount = 2;

        public Vector3 InitialLinearVelocity = new Vector3(1.5f, 0f, 1.0f);

        public Vector3 InitialAngularVelocity = new Vector3(0.5f, 0.8f, 0.3f);

        /// <summary>
        /// Nested Baker converting MonoBehaviour fields to unmanaged ECS components.
        /// Uses TransformUsageFlags.Dynamic because asteroids translate and rotate every frame.
        /// </summary>
        class Baker : Baker<AsteroidAuthoring>
        {
            public override void Bake(AsteroidAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Tag component for archetype filtering
                AddComponent(entity, new AsteroidTag());

                // Hierarchy metadata and collision geometry
                AddComponent(entity, new AsteroidData
                {
                    Tier = authoring.Tier,
                    Radius = authoring.Radius,
                    ScoreValue = authoring.ScoreValue,
                    SplitCount = authoring.SplitCount
                });

                // Kinematic drift velocity (linear constrained to XZ plane, angular 3D)
                AddComponent(entity, new DriftVelocity
                {
                    Linear = new float3(authoring.InitialLinearVelocity.x, 0f, authoring.InitialLinearVelocity.z),
                    Angular = math.float3(authoring.InitialAngularVelocity.x, authoring.InitialAngularVelocity.y, authoring.InitialAngularVelocity.z)
                });
            }
        }
    }
}
