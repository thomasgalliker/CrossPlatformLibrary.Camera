namespace CrossPlatformLibrary.Camera
{
    public interface ICamera
    {
        /// <summary>
        /// The direction to which the camera is facing to.
        /// </summary>
        CameraFacingDirection CameraFacingDirection { get; }

        /// <summary>
        /// Indicates if the camera is enabled or not.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// The name of the camera device.
        /// </summary>
        string Name { get; }
    }
}