using System;
using System.Collections.Generic;
using System.Linq;

using Android.Content;
using Android.Content.PM;
using Android.OS;

using CrossPlatformLibrary.IoC;

namespace CrossPlatformLibrary.Camera
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class MediaAccess : IMediaAccess
    {
        private readonly IList<ICamera> cameras;

        public MediaAccess()
        {
            try
            {
                var context = Android.App.Application.Context;

                this.cameras = new List<ICamera>(2);

                if (context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera))
                {
                    this.cameras.Add(new PhotoCamera(CameraFacingDirection.Rear, true, "Camera", context));
                }

                if (context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront) &&
                    (Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread))
                {
                    this.cameras.Add(new PhotoCamera(CameraFacingDirection.Front, true, "Front camera", context));
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