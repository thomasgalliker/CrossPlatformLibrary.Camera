using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public interface IMediaLibrary
    {
        /// <summary>
        /// Saves the given stream as jpeg to the camera roll.
        /// </summary>
        Task SaveToCameraRoll(MediaFile mediafile, bool overwrite = true);

        /// <summary>
        ///     Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
        Task<MediaFile> PickPhotoAsync();

        /// <summary>
        ///     Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
        Task<MediaFile> PickVideoAsync();
    }
}