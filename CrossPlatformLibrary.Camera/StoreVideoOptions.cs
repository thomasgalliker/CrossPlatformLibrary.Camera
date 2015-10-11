using System;

namespace CrossPlatformLibrary.Camera
{
    public class StoreVideoOptions : StoreMediaOptions
    {
        public StoreVideoOptions(string name = null, string directory = "") : base(name, directory)
        {
            this.Quality = VideoQuality.High;
            this.DesiredLength = TimeSpan.FromMinutes(10);
        }

        public TimeSpan DesiredLength
        {
            get; private set;
        }

        public VideoQuality Quality
        {
            get; private set;
        }
    }
}