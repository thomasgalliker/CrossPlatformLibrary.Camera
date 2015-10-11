using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Media.Capture;

namespace CrossPlatformLibrary.Camera
{
    public class VideoCamera : PhotoCamera, IVideoCamera
    {
        public VideoCamera(DeviceInformation deviceInformation)
            : base(deviceInformation)
        {
        }

        /// <inheritdoc />
        public async Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            var capture = new CameraCaptureUI();
            capture.VideoSettings.MaxResolution = options.Quality.GetResolutionFromQuality();
            capture.VideoSettings.MaxDurationInSeconds = (float)options.DesiredLength.TotalSeconds;
            capture.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;

            var result = await capture.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (result == null)
            {
                return null;
            }

            return new MediaFile(result.Path, () => result.OpenStreamForReadAsync().Result);
        }
    }
}