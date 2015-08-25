using System;
using System.Collections.Generic;

using CrossPlatformLibrary.IoC;

using Microsoft.Devices;

namespace CrossPlatformLibrary.Camera
{
    public class MediaAccess : IMediaAccess
    {
        private readonly IList<ICamera> cameras;

        public MediaAccess()
        {
            this.cameras = new List<ICamera>(2);

            try
            {
                if (Microsoft.Devices.Camera.IsCameraTypeSupported(CameraType.Primary))
                {
                    this.cameras.Add(new PhotoCamera(CameraFacingDirection.Rear, true, "Primary"));
                }

                if (Microsoft.Devices.Camera.IsCameraTypeSupported(CameraType.FrontFacing))
                {
                    this.cameras.Add(new PhotoCamera(CameraFacingDirection.Front, true, "FrontFacing"));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("You must set the ID_CAP_ISV_CAMERA permission.", ex);
            }
        }

        public IEnumerable<ICamera> Cameras
        {
            get
            {
                return this.cameras;
            }
        }

        public IMediaPicker MediaPicker
        {
            get
            {
                return SimpleIoc.Default.GetInstance<IMediaPicker>();
            }
        }
    }
}
