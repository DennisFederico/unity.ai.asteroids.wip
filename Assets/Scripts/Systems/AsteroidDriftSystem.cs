namespace Asteroids.Core
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Burst-compiled ISystem translating asteroids along the XZ gameplay plane
    /// and compounding continuous 3-axis Euler rotation via normalized quaternion multiplication.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AsteroidDriftSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Guard: only update when at least one asteroid entity exists
            state.RequireForUpdate<AsteroidTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Query entities with LocalTransform, DriftVelocity, and AsteroidTag
            foreach (var (transform, drift) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<DriftVelocity>>()
                    .WithAll<AsteroidTag>())
            {
                // 1. Translate position on XZ plane only (Y = 0)
                float3 newPos = transform.ValueRO.Position + (drift.ValueRO.Linear * dt);
                newPos.y = 0f;
                transform.ValueRW.Position = newPos;

                // 2. Compounded 3-axis angular rotation with normalization
                quaternion deltaRot = quaternion.Euler(drift.ValueRO.Angular * dt);
                transform.ValueRW.Rotation = math.normalize(math.mul(transform.ValueRO.Rotation, deltaRot));
            }
        }
    }
}
