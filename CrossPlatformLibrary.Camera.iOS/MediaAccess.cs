using System.Collections.Generic;


namespace CrossPlatformLibrary.Camera
{
    public class MediaAccess : IMediaAccess
    {
        public const string TypeImage = "public.image";

        public const string TypeMovie = "public.movie";

        public IEnumerable<ICamera> Cameras { get; private set; }

        public IMediaLibrary MediaLibrary { get; private set; }
    }
}