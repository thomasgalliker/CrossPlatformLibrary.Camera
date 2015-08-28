using System;

namespace CrossPlatformLibrary.Camera
{
    public class StoreVideoOptions : StoreMediaOptions
    {
        public StoreVideoOptions()
        {
            this.Quality = VideoQuality.High;
            this.DesiredLength = TimeSpan.FromMinutes(10);
        }

        public TimeSpan DesiredLength
        {
            get;
            set;
        }

        public VideoQuality Quality
        {
            get;
            set;
        }
    }
}