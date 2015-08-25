
using Windows.Media.Capture;

namespace CrossPlatformLibrary.Camera
{
    public static class VideoQualityExtensions
    {
        public static CameraCaptureUIMaxVideoResolution GetResolutionFromQuality(this VideoQuality quality)
        {
            switch (quality)
            {
                case VideoQuality.High:
                    return CameraCaptureUIMaxVideoResolution.HighestAvailable;
                case VideoQuality.Medium:
                    return CameraCaptureUIMaxVideoResolution.StandardDefinition;
                case VideoQuality.Low:
                    return CameraCaptureUIMaxVideoResolution.LowDefinition;
                default:
                    return CameraCaptureUIMaxVideoResolution.HighestAvailable;
            }
        }
    }
}
