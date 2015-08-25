using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public interface IVideoCamera : IPhotoCamera
    {
        Task<MediaFile> TakeVideoAsync(StoreVideoOptions options);
    }
}