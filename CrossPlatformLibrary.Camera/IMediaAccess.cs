using System.Collections.Generic;

namespace CrossPlatformLibrary.Camera
{
    public interface IMediaAccess
    {
        IEnumerable<ICamera> Cameras { get; }

        ////event EventHandler<ICamera> CameraAdded;

        ////event EventHandler<ICamera> CameraUpdated;

        ////event EventHandler<ICamera> CameraRemoved;

        IMediaPicker MediaPicker { get; }
    }
}
