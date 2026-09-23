namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AsteroidCullingSystem : ISystem
    {
        private EntityQuery _playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InfiniteMapConfig>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _playerQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_playerQuery.IsEmpty) return;

            var playerEntity = _playerQuery.GetSingletonEntity();
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
            var config = SystemAPI.GetSingleton<InfiniteMapConfig>();

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                           .WithAll<AsteroidTag>()
                           .WithEntityAccess())
            {
                float distSq = math.distancesq(transform.ValueRO.Position, playerPos);
                if (distSq > config.DespawnRadiusSq)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
