namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Burst;

    /// <summary>
    /// Burst-compiled collision detection system that performs bounding sphere
    /// intersection tests between lasers and asteroids, destroys colliding entities,
    /// splits multi-tier asteroids, and increments the GameScore singleton.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LaserMovementSystem))]
    [UpdateAfter(typeof(AsteroidDriftSystem))]
    public partial struct LaserAsteroidCollisionSystem : ISystem
    {
        private EntityQuery _laserQuery;
        private EntityQuery _asteroidQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AsteroidPrefabsConfig>();
            state.RequireForUpdate<GameScore>();

            _laserQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<LaserTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<ProjectileData>()
            );

            _asteroidQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AsteroidTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<AsteroidData>(),
                ComponentType.ReadOnly<DriftVelocity>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int laserCount = _laserQuery.CalculateEntityCount();
            int asteroidCount = _asteroidQuery.CalculateEntityCount();

            if (laserCount == 0 || asteroidCount == 0)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var prefabs = SystemAPI.GetSingleton<AsteroidPrefabsConfig>();
            ref var score = ref SystemAPI.GetSingletonRW<GameScore>().ValueRW;

            using var laserEntities = _laserQuery.ToEntityArray(Allocator.Temp);
            using var laserTransforms = _laserQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using var laserProjectiles = _laserQuery.ToComponentDataArray<ProjectileData>(Allocator.Temp);

            using var asteroidEntities = _asteroidQuery.ToEntityArray(Allocator.Temp);
            using var asteroidTransforms = _asteroidQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using var asteroidData = _asteroidQuery.ToComponentDataArray<AsteroidData>(Allocator.Temp);
            using var asteroidDrifts = _asteroidQuery.ToComponentDataArray<DriftVelocity>(Allocator.Temp);

            // Track hit entities in this tick to prevent multi-hit race conditions
            var destroyedLasers = new NativeParallelHashSet<Entity>(laserCount, Allocator.Temp);
            var destroyedAsteroids = new NativeParallelHashSet<Entity>(asteroidCount, Allocator.Temp);

            for (int l = 0; l < laserCount; l++)
            {
                Entity laserEntity = laserEntities[l];
                if (destroyedLasers.Contains(laserEntity))
                    continue;

                float3 laserPos = laserTransforms[l].Position;
                float laserRadius = laserProjectiles[l].Radius;

                for (int a = 0; a < asteroidCount; a++)
                {
                    Entity asteroidEntity = asteroidEntities[a];
                    if (destroyedAsteroids.Contains(asteroidEntity))
                        continue;

                    float3 asteroidPos = asteroidTransforms[a].Position;
                    float asteroidRadius = asteroidData[a].Radius;

                    float totalRadius = laserRadius + asteroidRadius;
                    if (math.distancesq(laserPos, asteroidPos) <= (totalRadius * totalRadius))
                    {
                        // 1. Mark both as destroyed in this pass
                        destroyedLasers.Add(laserEntity);
                        destroyedAsteroids.Add(asteroidEntity);

                        // 2. Record laser destruction
                        ecb.DestroyEntity(laserEntity);

                        // 3. Award score
                        score.CurrentScore += asteroidData[a].ScoreValue;

                        // 4. Handle splitting
                        int currentTier = asteroidData[a].Tier;
                        Entity childPrefab = Entity.Null;

                        if (currentTier == 3)
                            childPrefab = prefabs.MediumPrefab;
                        else if (currentTier == 2)
                            childPrefab = prefabs.SmallPrefab;

                        if (childPrefab != Entity.Null && asteroidData[a].SplitCount > 0)
                        {
                            float3 parentVel = asteroidDrifts[a].Linear;
                            float baseSpeed = math.max(2.5f, math.length(parentVel)) * 1.25f;

                            for (int i = 0; i < asteroidData[a].SplitCount; i++)
                            {
                                float angleOffset = (i == 0) ? 0.75f : -0.75f;
                                quaternion rotOffset = quaternion.RotateY(angleOffset);
                                float3 childDir = math.normalize(math.mul(rotOffset, parentVel));

                                Entity child = ecb.Instantiate(childPrefab);
                                ecb.SetComponent(child, LocalTransform.FromPositionRotationScale(
                                    asteroidPos,
                                    asteroidTransforms[a].Rotation,
                                    1f
                                ));
                                ecb.SetComponent(child, new DriftVelocity
                                {
                                    Linear = childDir * baseSpeed,
                                    Angular = asteroidDrifts[a].Angular * 1.4f
                                });
                            }
                        }

                        // 5. Record parent asteroid destruction
                        ecb.DestroyEntity(asteroidEntity);
                        break; // Laser hit one asteroid, proceed to next laser
                    }
                }
            }
        }
    }
}
