
using Windows.Devices.Enumeration;

namespace CrossPlatformLibrary.Camera
{
    public static class DeviceInformationExtensions
    {
        public static CameraFacingDirection ToCameraFacingDirection(this DeviceInformation deviceInformation)
        {
            if (deviceInformation.EnclosureLocation != null)
            {
                if (deviceInformation.EnclosureLocation.Panel == Panel.Front)
                {
                    return CameraFacingDirection.Front;
                }

                if (deviceInformation.EnclosureLocation.Panel == Panel.Back)
                {
                    return CameraFacingDirection.Rear;
                }
            }

            return CameraFacingDirection.Undefined;
        }
    }
}
