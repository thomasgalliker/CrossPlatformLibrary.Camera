using System;
using System.Collections.Generic;
using System.Linq;

using CrossPlatformLibrary.IoC;

using Microsoft.Devices;

namespace CrossPlatformLibrary.Camera
{
    public class MediaAccess : IMediaAccess
    {
        private readonly IList<ICamera> cameras;

        public MediaAccess()
        {
            try
            {
                this.cameras = new List<ICamera>(2);

                foreach (var cameraType in Enum.GetValues(typeof(CameraType)).OfType<CameraType>())
                {
                    if (Microsoft.Devices.Camera.IsCameraTypeSupported(cameraType))
                    {
                        this.cameras.Add(new PhotoCamera(cameraType.ToCameraFacingDirection(), true, cameraType.ToString()));
                    }
                }

                if (!this.cameras.Any())
                {
                    throw new InvalidOperationException("Could not find any cameras. Unable to use MediaAccess if no camera is present.");
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

        public IMediaLibrary MediaLibrary
        {
            get
            {
                return SimpleIoc.Default.GetInstance<IMediaLibrary>();
            }
        }
    }
}
