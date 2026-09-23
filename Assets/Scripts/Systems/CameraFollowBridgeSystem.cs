namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class CameraFollowBridgeSystem : SystemBase
    {
        private Vector3 _cameraOffset;
        private bool _offsetInitialized;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnUpdate()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            foreach (var transform in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<PlayerTag>())
            {
                Vector3 playerPos = transform.ValueRO.Position;

                if (!_offsetInitialized)
                {
                    // Compute initial isometric offset relative to ship starting point
                    _cameraOffset = mainCam.transform.position - playerPos;
                    if (_cameraOffset.sqrMagnitude < 0.01f)
                    {
                        // Default high isometric angle if camera started at origin
                        _cameraOffset = new Vector3(-20f, 30f, -20f);
                    }
                    _offsetInitialized = true;
                }

                // Smoothly update camera position tracking the player
                Vector3 targetCamPos = playerPos + _cameraOffset;
                mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, targetCamPos, SystemAPI.Time.DeltaTime * 15f);
            }
        }
    }
}
