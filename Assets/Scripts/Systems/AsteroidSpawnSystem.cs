namespace Asteroids.Core
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Burst-compiled unmanaged ISystem implementing a player-centric dynamic
    /// asteroid ring spawner with local density regulation.
    ///
    /// Counts active asteroids within DensityRadius (70m) using squared distance
    /// (math.distancesq) against DensityRadiusSq. Enforces strict density gating:
    ///   - Suppresses spawning if local count >= TargetLocalDensityMax (16)
    ///   - Accelerates to FastSpawnInterval (0.8s) if count < TargetLocalDensityMin (8)
    ///   - Operates at BaseSpawnInterval (2.5s) if count is in [8, 15]
    ///
    /// Instantiates LargePrefab strictly within [ViewportBufferRadius, SpawnOuterRadius]
    /// (35m to 50m) outside the camera frustum, directing drift velocity toward
    /// the player's vicinity via EntityCommandBuffer.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial struct AsteroidSpawnSystem : ISystem
    {
        private EntityQuery _playerQuery;
        private EntityQuery _asteroidQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalRandom>();
            state.RequireForUpdate<InfiniteMapConfig>();
            state.RequireForUpdate<AsteroidPrefabsConfig>();
            state.RequireForUpdate<AsteroidSpawnerData>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

            _playerQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            _asteroidQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AsteroidTag>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playerQuery.IsEmpty)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            ref var spawner = ref SystemAPI.GetSingletonRW<AsteroidSpawnerData>().ValueRW;
            spawner.CooldownTimer -= dt;

            if (spawner.CooldownTimer > 0f)
                return;

            var playerEntity = _playerQuery.GetSingletonEntity();
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

            // 1. Measure local asteroid density within the player's engagement radius
            int localCount = 0;
            foreach (var asteroidTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<AsteroidTag>())
            {
                if (math.distancesq(asteroidTransform.ValueRO.Position, playerPos) <= spawner.DensityRadiusSq)
                {
                    localCount++;
                }
            }

            // 2. Strict density cap check: prevent overcrowding
            if (localCount >= spawner.TargetLocalDensityMax)
            {
                // Defer next evaluation by the standard interval
                spawner.CooldownTimer = spawner.BaseSpawnInterval;
                return;
            }

            // 3. Adaptive pacing: replenish fast if below min, steady if in optimal band
            if (localCount < spawner.TargetLocalDensityMin)
            {
                spawner.CooldownTimer = spawner.FastSpawnInterval;
            }
            else
            {
                spawner.CooldownTimer = spawner.BaseSpawnInterval;
            }

            ref var rand = ref SystemAPI.GetSingletonRW<GlobalRandom>().ValueRW.Value;
            var mapConfig = SystemAPI.GetSingleton<InfiniteMapConfig>();
            var prefabs = SystemAPI.GetSingleton<AsteroidPrefabsConfig>();

            if (prefabs.LargePrefab == Entity.Null)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 4. Generate random spawn coordinate strictly outside view frustum
            //    within [ViewportBufferRadius, SpawnOuterRadius] (e.g. [35m, 50m])
            float angle = rand.NextFloat(0f, math.PI * 2f);
            float radius = rand.NextFloat(mapConfig.ViewportBufferRadius, mapConfig.SpawnOuterRadius);

            float3 spawnPos = playerPos + new float3(
                math.cos(angle) * radius,
                0f,
                math.sin(angle) * radius
            );
            spawnPos.y = 0f;

            // 5. Direct velocity inward toward or across the player's vicinity
            float3 targetJitter = new float3(rand.NextFloat(-14f, 14f), 0f, rand.NextFloat(-14f, 14f));
            float3 targetPos = playerPos + targetJitter;
            float3 driftDir = math.normalize(targetPos - spawnPos);
            float speed = rand.NextFloat(2.5f, 5.0f);

            // 6. Random 3-axis angular tumble velocity (radians per sec)
            float3 angularVel = rand.NextFloat3(-2.0f, 2.0f);

            // 7. Deferred ECB instantiation (record-and-forget, no .Playback/.Dispose)
            Entity newAsteroid = ecb.Instantiate(prefabs.LargePrefab);
            ecb.SetComponent(newAsteroid, LocalTransform.FromPositionRotationScale(
                spawnPos,
                rand.NextQuaternionRotation(),
                1f
            ));
            ecb.SetComponent(newAsteroid, new DriftVelocity
            {
                Linear = driftDir * speed,
                Angular = angularVel
            });
        }
    }
}
