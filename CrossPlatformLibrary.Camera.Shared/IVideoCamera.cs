using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public interface IVideoCamera : IPhotoCamera
    {
        /// <summary>
        ///     Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        Task<MediaFile> TakeVideoAsync(StoreVideoOptions options);
    }
}