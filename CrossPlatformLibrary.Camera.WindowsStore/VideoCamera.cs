using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Media.Capture;

namespace CrossPlatformLibrary.Camera
{
    public class VideoCamera : PhotoCamera, IVideoCamera
    {
        public VideoCamera(CameraFacingDirection cameraFacingDirection)
            : base(cameraFacingDirection)
        {
        }

        public bool IsCameraAvailable { get; private set; }

        /// <summary>
        ///     Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public async Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            if (!this.IsCameraAvailable)
            {
                throw new NotSupportedException();
            }

            options.VerifyOptions();

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
