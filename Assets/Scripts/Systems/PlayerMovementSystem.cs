namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    /// <summary>
    /// Burst-compiled unmanaged ISystem that applies ship movement physics
    /// on the isometric XZ plane (Y=0). Reads exclusively from PlayerInput
    /// component data — no managed Input System calls.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, moveData, input) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<PlayerMovementData>, RefRO<PlayerInput>>()
                         .WithAll<PlayerTag>())
            {
                // 1. Acceleration along XZ plane
                float3 inputDir = new float3(input.ValueRO.Movement.x, 0f, input.ValueRO.Movement.y);
                if (math.lengthsq(inputDir) > 0.001f)
                {
                    inputDir = math.normalize(inputDir);
                    moveData.ValueRW.CurrentVelocity += inputDir * (moveData.ValueRO.ThrustAcceleration * dt);
                }

                // 2. Apply linear drag damping
                moveData.ValueRW.CurrentVelocity *= math.max(0f, 1f - (moveData.ValueRO.Drag * dt));

                // 3. Clamp top speed
                float currentSpeed = math.length(moveData.ValueRO.CurrentVelocity);
                if (currentSpeed > moveData.ValueRO.MaxSpeed)
                {
                    moveData.ValueRW.CurrentVelocity = (moveData.ValueRO.CurrentVelocity / currentSpeed) * moveData.ValueRO.MaxSpeed;
                }

                // 4. Translate position (constrained to Y = 0)
                float3 newPos = transform.ValueRO.Position + (moveData.ValueRO.CurrentVelocity * dt);
                newPos.y = 0f;
                transform.ValueRW.Position = newPos;

                // 5. Smoothly rotate yaw towards AimWorldPosition
                float3 toTarget = input.ValueRO.AimWorldPosition - transform.ValueRO.Position;
                toTarget.y = 0f;
                if (math.lengthsq(toTarget) > 0.01f)
                {
                    float targetAngle = math.atan2(toTarget.x, toTarget.z);
                    quaternion targetRot = quaternion.RotateY(targetAngle);
                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRO.Rotation,
                        targetRot,
                        math.saturate(moveData.ValueRO.RotationDamping * dt)
                    );
                }
            }
        }
    }
}
