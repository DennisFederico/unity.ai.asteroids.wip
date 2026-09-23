namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Burst;

    /// <summary>
    /// Decrement Lifetime.Value each frame and destroy expired laser entities via ECB.
    /// Burst-compiled ISystem in SimulationSystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LaserLifetimeSystem : ISystem
    {
        /// <summary>
        /// Require the ECB singleton for deferred entity destruction.
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        /// <summary>
        /// Decrements Lifetime.Value and records ecb.DestroyEntity() upon expiry.
        /// System-managed ECB: record-and-forget, never call .Playback() or .Dispose().
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Get system-managed ECB from singleton — record-and-forget pattern
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Query with entity access for destruction
            foreach (var (lifetime, entity) in
                SystemAPI.Query<RefRW<Lifetime>>()
                         .WithEntityAccess())
            {
                lifetime.ValueRW.Value -= dt;

                if (lifetime.ValueRO.Value <= 0f)
                {
                    // Record destruction to system-managed ECB — never call .Playback() or .Dispose()
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
