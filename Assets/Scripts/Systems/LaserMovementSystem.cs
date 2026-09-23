namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Burst;

    /// <summary>
    /// Translates laser projectile entities along their velocity trajectory on the XZ plane (Y=0).
    /// Burst-compiled ISystem in SimulationSystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LaserMovementSystem : ISystem
    {
        /// <summary>
        /// No ECB required — pure transform translation.
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalTransform>();
        }

        /// <summary>
        /// Advances each laser entity's position along its velocity vector, clamping Y to 0.
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Query only laser projectile entities with transform and projectile data
            foreach (var (transform, projectile) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<ProjectileData>>()
                         .WithAll<LaserTag>())
            {
                // Translate along velocity trajectory, keeping Y=0 (XZ plane only)
                var pos = transform.ValueRO.Position + (projectile.ValueRO.Velocity * dt);
                pos.y = 0f;
                transform.ValueRW.Position = pos;
            }
        }
    }
}
