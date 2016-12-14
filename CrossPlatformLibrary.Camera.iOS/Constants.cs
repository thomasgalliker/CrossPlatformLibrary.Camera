using UIKit;

namespace CrossPlatformLibrary.Camera
{
    public static class Constants
    {
        internal const string TypeImage = "public.image";
        internal const string TypeMovie = "public.movie";

        static Constants()
        {
            StatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;
        }

        internal static UIStatusBarStyle StatusBarStyle { get; set; }
    }
}