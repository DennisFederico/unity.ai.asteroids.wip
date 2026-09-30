namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;

    /// <summary>
    /// Hybrid bridge system that updates the main camera position and rotation
    /// based on the player ship's position and the global CameraConfiguration singleton.
    /// The configuration data is baked from a CameraAnchor transform in the scene,
    /// allowing designers to visually position the camera in Edit Mode.
    /// Operates within PresentationSystemGroup for final-frame camera transforms.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class CameraFollowBridgeSystem : SystemBase
    {
        /// <summary>
        /// Smooth follow damping factor (seconds inverse).
        /// Tuned to a moderate speed so camera tracks the player without jitter.
        /// </summary>
        private const float FollowDamping = 15f;

        /// <summary>
        /// Cached reference to the managed Main Camera for efficient access.
        /// </summary>
        private Camera _mainCamera;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTag>();
            RequireForUpdate<IsCameraConfigTag>();
        }

        protected override void OnUpdate()
        {
            // Cache the main camera reference to avoid repeated lookups
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // Try to retrieve the camera configuration singleton.
            // If no config has been baked yet, skip this frame gracefully.
            if (!SystemAPI.TryGetSingleton<CameraConfiguration>(out CameraConfiguration config))
            {
                return;
            }

            // Find the player entity position
            float3 playerPos = float3.zero;
            bool foundPlayer = false;

            foreach (var ltw in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<PlayerTag>())
            {
                playerPos = ltw.ValueRO.Position;
                foundPlayer = true;
                break;
            }

            if (!foundPlayer) return;

            // Convert ECS float3 offset to Unity Vector3 and apply to player position
            Vector3 offset = new Vector3(config.offset.x, config.offset.y, config.offset.z);
            Vector3 targetCamPos = (Vector3)playerPos + offset;

            // Compute camera rotation from baked pitch and yaw
            float3 euler = math.float3(config.pitch, config.yaw, 0f);
            Quaternion targetCamRot = Quaternion.Euler(euler);

            // Smoothly interpolate camera position and rotation towards target
            float t = SystemAPI.Time.DeltaTime * FollowDamping;
            _mainCamera.transform.position = Vector3.Lerp(
                _mainCamera.transform.position,
                targetCamPos,
                t
            );
            _mainCamera.transform.rotation = Quaternion.Slerp(
                _mainCamera.transform.rotation,
                targetCamRot,
                t
            );
        }
    }
}
