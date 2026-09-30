namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Unmanaged ECS component storing global isometric camera configuration data.
    /// Contains offset vector and rotation angles (pitch/yaw) for a follow-bucket camera.
    /// Baked from CameraConfigurationAuthoring in the Main Scene.
    /// </summary>
    public struct CameraConfiguration : IComponentData
    {
        /// <summary>Distance vector from the camera target (float3).</summary>
        public float3 offset;

        /// <summary>X-axis rotation angle in degrees.</summary>
        public float pitch;

        /// <summary>Y-axis rotation angle in degrees.</summary>
        public float yaw;
    }
}
