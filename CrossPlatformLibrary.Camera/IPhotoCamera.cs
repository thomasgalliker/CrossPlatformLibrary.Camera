using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public interface IPhotoCamera : ICamera
    {
        Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options);
    }
}