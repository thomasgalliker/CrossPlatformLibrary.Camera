using System;
using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public class VideoCamera : PhotoCamera, IVideoCamera
    {
        public VideoCamera(CameraFacingDirection cameraFacingDirection, bool isEnabled, string name)
            : base(cameraFacingDirection, isEnabled, name)
        {
        }

        public Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            throw new NotImplementedException();
        }
    }
}