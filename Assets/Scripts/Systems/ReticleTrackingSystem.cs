namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    /// <summary>
    /// Burst-compiled system that tracks the player's aim world position
    /// on the isometric XZ plane (Y=0.05f), updating the crosshair reticle transform.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ReticleTrackingSystem : ISystem
    {
        /// <summary>
        /// Requires the reticle tag and PlayerInput singleton before entering OnUpdate.
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CrosshairReticleTag>();
            state.RequireForUpdate<PlayerInput>();
        }

        /// <summary>
        /// Reads AimWorldPosition from the PlayerInput singleton and sets
        /// LocalTransform.Position on all CrosshairReticleTag entities.
        /// Zero GC allocation; fully Burst-compatible.
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInput>();
            float3 targetPos = playerInput.AimWorldPosition;
            targetPos.y = 0.05f; // Slight elevation to avoid Z-fighting with ground plane

            foreach (var transform in
                SystemAPI.Query<RefRW<LocalTransform>>()
                           .WithAll<CrosshairReticleTag>())
            {
                transform.ValueRW.Position = targetPos;
            }
        }
    }
}
