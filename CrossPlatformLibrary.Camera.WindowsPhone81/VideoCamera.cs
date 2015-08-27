using System.IO;
using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public class VideoCamera : PhotoCamera, IVideoCamera
    {
        public VideoCamera(CameraFacingDirection cameraFacingDirection, bool isEnabled, string name)
            : base(cameraFacingDirection, isEnabled, name)
        {
        }

        /// <inheritdoc />
        public async Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            options.VerifyOptions();

            var capture = new CameraCaptureUI();
            //capture.VideoSettings.MaxResolution = options.Quality.GetResolutionFromQuality();
            //capture.VideoSettings.MaxDurationInSeconds = (float)options.DesiredLength.TotalSeconds;
            //capture.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;

            var result = await capture.CaptureFileAsync(CameraCaptureUIMode.Video, options);
            if (result == null)
            {
                return null;
            }

            return new MediaFile(result.Path, () => result.OpenStreamForReadAsync().Result);
        }
    }
}