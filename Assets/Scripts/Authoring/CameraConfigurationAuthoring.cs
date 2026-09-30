namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// MonoBehaviour authoring component for global isometric camera configuration.
    /// Lives in a SubScene and programmatically finds the Main Camera (tagged "MainCamera")
    /// from the Main Scene during baking. This enables designers to visually frame the camera
    /// in the Main Scene's Scene View and have those values baked into the ECS world.
    /// </summary>
    public class CameraConfigurationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Baker that finds the Main Camera GameObject via tag and samples its
        /// position and rotation during baking. Uses DependsOn to ensure reactivity
        /// when the Main Camera's transform changes.
        /// </summary>
        class Baker : Baker<CameraConfigurationAuthoring>
        {
            public override void Bake(CameraConfigurationAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                // Find the Main Camera programmatically via tag lookup.
                // This is necessary because the authoring component lives in a SubScene
                // while the Main Camera lives in the Main Scene — cross-scene serialized
                // references cannot be baked directly.
                GameObject mainCameraGO = GameObject.FindWithTag("MainCamera");
                if (mainCameraGO == null)
                {
                    Debug.LogWarning("[CameraConfigurationBaker] No GameObject found with tag 'MainCamera'. Camera configuration will use identity defaults.");
                    AddComponent(entity, new CameraConfiguration
                    {
                        offset = float3.zero,
                        pitch = 45f,
                        yaw = 45f
                    });
                    AddComponent<IsCameraConfigTag>(entity);
                    return;
                }

                Transform cameraTransform = mainCameraGO.transform;

                // Record dependency on the camera's transform for reactive rebaking.
                // This ensures that when the Main Camera is moved or rotated in the
                // Main Scene's Edit Mode, the ECS config entity is automatically rebaked.
                DependsOn(cameraTransform);

                // Calculate offset as the camera's world position relative to origin
                // (where the player ship starts).
                float3 offset = cameraTransform.position;

                // Convert camera rotation to Euler angles for pitch (X) and yaw (Y).
                float3 cameraEuler = cameraTransform.rotation.eulerAngles;
                float pitch = cameraEuler.x;
                float yaw = cameraEuler.y;

                AddComponent(entity, new CameraConfiguration
                {
                    offset = offset,
                    pitch = pitch,
                    yaw = yaw
                });

                AddComponent<IsCameraConfigTag>(entity);
            }
        }
    }
}
