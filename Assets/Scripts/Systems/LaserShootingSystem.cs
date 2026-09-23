namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    /// <summary>
    /// Polls PlayerInput for fire input, enforces firing rate cooldown, and records
    /// laser prefab instantiation with transform, projectile data, and lifetime via ECB.
    /// Burst-compiled ISystem in SimulationSystemGroup, updates after PlayerMovementSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial struct LaserShootingSystem : ISystem
    {
        /// <summary>
        /// Require the ECB singleton for deferred instantiation.
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        /// <summary>
        /// Checks fire input, updates cooldown timer, and instantiates laser projectiles via ECB.
        /// System-managed ECB: record-and-forget, never call .Playback() or .Dispose().
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Get system-managed ECB from singleton — record-and-forget pattern
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Query player entities with spawner config, transform, and input
            foreach (var (spawner, transform, input) in
                SystemAPI.Query<RefRW<LaserSpawner>, RefRO<LocalTransform>, RefRO<PlayerInput>>())
            {
                // Decrement cooldown timer each frame
                spawner.ValueRW.CooldownTimer -= dt;

                // Check fire input and cooldown
                bool wantsToFire = input.ValueRO.FireHeld || input.ValueRO.FireTriggered;
                if (wantsToFire && spawner.ValueRO.CooldownTimer <= 0f && spawner.ValueRO.LaserPrefab != Entity.Null)
                {
                    // Reset cooldown to fire rate interval
                    spawner.ValueRW.CooldownTimer = 1f / math.max(0.01f, spawner.ValueRO.FireRate);

                    // 1. Deferred instantiation via ECB — returns a placeholder entity handle
                    Entity laser = ecb.Instantiate(spawner.ValueRO.LaserPrefab);

                    // 2. Calculate muzzle spawn point on XZ plane (Y=0)
                    float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                    float3 spawnPos = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, spawner.ValueRO.MuzzleOffset);
                    spawnPos.y = 0f;

                    // 3. Chain deferred entity placeholder into subsequent SetComponent calls
                    //    NEVER pass this handle to SystemAPI.GetComponent or EntityManager in the same frame
                    float speed = 35.0f;
                    ecb.SetComponent(laser, LocalTransform.FromPositionRotationScale(
                        spawnPos,
                        transform.ValueRO.Rotation,
                        1f));
                    ecb.SetComponent(laser, new ProjectileData
                    {
                        Velocity = forward * speed,
                        Speed = speed,
                        Radius = 0.3f
                    });
                    ecb.SetComponent(laser, new Lifetime { Value = 1.8f });
                }
            }
        }
    }
}
