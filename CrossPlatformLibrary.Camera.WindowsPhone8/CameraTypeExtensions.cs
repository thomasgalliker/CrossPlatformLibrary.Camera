using System;

using Microsoft.Devices;

namespace CrossPlatformLibrary.Camera
{
    public static class CameraTypeExtensions
    {
        public static CameraFacingDirection ToCameraFacingDirection(this CameraType cameraType)
        {
            switch (cameraType)
            {
                case CameraType.FrontFacing:
                    return CameraFacingDirection.Front;

                case CameraType.Primary:
                    return CameraFacingDirection.Rear;

                default:
                    throw new InvalidOperationException(string.Format("Could not find mapping for CameraType {0}.", cameraType));
            }
        }

        public static CameraType ToCameraType(this CameraFacingDirection cameraFacingDirection)
        {
            switch (cameraFacingDirection)
            {
                case CameraFacingDirection.Front:
                    return CameraType.FrontFacing;

                case CameraFacingDirection.Rear:
                    return CameraType.Primary;

                default:
                    throw new InvalidOperationException(string.Format("Could not find mapping for CameraFacingDirection {0}.", cameraFacingDirection));
            }
        }
    }
}
