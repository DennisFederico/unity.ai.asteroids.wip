namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;

    /// <summary>
    /// Authoring component for the 3D crosshair reticle prefab.
    /// Bakes the CrosshairReticleTag with Dynamic transform flags.
    /// </summary>
    public class ReticleAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Baker that converts the reticle GameObject into an ECS entity
        /// with the CrosshairReticleTag and Dynamic transform usage.
        /// </summary>
        class Baker : Baker<ReticleAuthoring>
        {
            public override void Bake(ReticleAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CrosshairReticleTag());
            }
        }
    }
}
