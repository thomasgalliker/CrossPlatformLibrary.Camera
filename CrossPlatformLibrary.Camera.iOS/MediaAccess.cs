using System;
using System.Collections.Generic;
using System.Linq;

using CrossPlatformLibrary.IoC;

using UIKit;

namespace CrossPlatformLibrary.Camera
{
    public class MediaAccess : IMediaAccess
    {
        private readonly ICollection<ICamera> cameras;

        public MediaAccess()
        {
            try
            {
                var isEnabled = UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera);
                var availableMediaTypes = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.Camera);
                if (isEnabled && availableMediaTypes == null)
                {
                    this.cameras = new List<ICamera>();
                }
                else
                {
                    this.cameras = new List<ICamera>(availableMediaTypes.Length);

                    foreach (var cameraType in availableMediaTypes)
                    {
                        if (cameraType == Constants.TypeMovie)
                        {
                            this.cameras.Add(new PhotoCamera(CameraFacingDirection.Front, isEnabled, "Front"));
                            this.cameras.Add(new PhotoCamera(CameraFacingDirection.Rear, isEnabled, "Rear"));
                        }
                        else if (cameraType == Constants.TypeImage)
                        {
                            this.cameras.Add(new VideoCamera(CameraFacingDirection.Front, isEnabled, "Front"));
                            this.cameras.Add(new VideoCamera(CameraFacingDirection.Rear, isEnabled, "Rear"));
                        }
                    }
                }

                if (!this.cameras.Any())
                {
                    throw new InvalidOperationException("Could not find any cameras. Unable to use MediaAccess if no camera is present.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                //throw new Exception("You must set the ID_CAP_ISV_CAMERA permission.", ex);
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