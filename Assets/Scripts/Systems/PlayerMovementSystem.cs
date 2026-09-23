namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    /// <summary>
    /// Burst-compiled unmanaged ISystem that applies ship movement physics
    /// on the isometric XZ plane (Y=0). The input vector is interpreted relative
    /// to the ship's orientation: forward/back becomes thrust along the ship's nose,
    /// left/right becomes strafe (slide). The ship's aim (rotation) always follows
    /// the cursor, while the velocity persists independently (inertial drift).
    /// No drag is applied — the ship continues drifting until thrust changes it.
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
                // 1. Compute target yaw towards AimWorldPosition (cursor)
                float3 toTarget = input.ValueRO.AimWorldPosition - transform.ValueRO.Position;
                toTarget.y = 0f;

                quaternion targetRot = quaternion.identity;
                if (math.lengthsq(toTarget) > 0.01f)
                {
                    float targetAngle = math.atan2(toTarget.x, toTarget.z);
                    targetRot = quaternion.RotateY(targetAngle);
                }

                // 2. Smoothly rotate yaw towards target (ship's aim always follows cursor)
                quaternion newRotation = math.slerp(
                    transform.ValueRO.Rotation,
                    targetRot,
                    math.saturate(moveData.ValueRO.RotationDamping * dt)
                );
                transform.ValueRW.Rotation = newRotation;

                // 3. Apply thrust along ship's aim vectors (ship-relative)
                // Input.y → thrust along ship's nose (forward/back)
                // Input.x → strafe along ship's right (left/right)
                float2 inputMovement = input.ValueRO.Movement;
                if (math.lengthsq(inputMovement) > 0.001f)
                {
                    // Get ship's forward and right vectors in world space (constrained to XZ plane)
                    float3 shipForward = math.forward(newRotation);
                    shipForward.y = 0f;
                    shipForward = math.normalizesafe(shipForward);

                    float3 shipRight = math.normalize(math.cross(new float3(0f, 1f, 0f), shipForward));

                    // Apply thrust directly from input, using ship-relative vectors
                    float3 thrustWorld = shipForward * inputMovement.y + shipRight * inputMovement.x;
                    moveData.ValueRW.CurrentVelocity += thrustWorld * moveData.ValueRO.ThrustAcceleration * dt;
                }

                // 4. Clamp top speed (no drag — ship drifts in space!)
                float currentSpeed = math.length(moveData.ValueRO.CurrentVelocity);
                if (currentSpeed > moveData.ValueRO.MaxSpeed)
                {
                    moveData.ValueRW.CurrentVelocity = (moveData.ValueRO.CurrentVelocity / currentSpeed) * moveData.ValueRO.MaxSpeed;
                }

                // 5. Translate position (constrained to Y = 0)
                float3 newPos = transform.ValueRO.Position + (moveData.ValueRO.CurrentVelocity * dt);
                newPos.y = 0f;
                transform.ValueRW.Position = newPos;
            }
        }
    }
}
