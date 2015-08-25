using System.Collections.Generic;

namespace CrossPlatformLibrary.Camera
{
    public interface IMediaAccess
    {
        IEnumerable<ICamera> Cameras { get; }
    }
}
