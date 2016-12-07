using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public interface IPhotoCamera : ICamera
    {

        /// <summary>
        ///     Take a photo async with default storage options.
        /// </summary>
        /// <returns>Media file of photo or null if canceled.</returns>
        ////Task<MediaFile> TakePhotoAsync();

        /// <summary>
        ///     Take a photo async with specified options.
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled.</returns>
        Task<MediaFile> TakePhotoAsync(StoreMediaOptions options);
    }
}