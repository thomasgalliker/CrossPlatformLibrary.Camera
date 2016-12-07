using System.Collections.Generic;

namespace CrossPlatformLibrary.Camera
{
    public class MediaAccess : IMediaAccess
    {
        public IEnumerable<ICamera> Cameras
        {
            get
            {
                throw new NotImplementedInReferenceAssemblyException();
            }
        }

        public IMediaLibrary MediaLibrary
        {
            get
            {
                throw new NotImplementedInReferenceAssemblyException();
            }
        }
    }
}