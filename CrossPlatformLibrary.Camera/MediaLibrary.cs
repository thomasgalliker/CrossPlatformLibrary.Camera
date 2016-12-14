using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public class MediaLibrary : IMediaLibrary
    {
        public Task SaveToCameraRoll(MediaFile mediafile, bool overwrite = true)
        {
            throw new NotImplementedInReferenceAssemblyException();
        }

        public Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null)
        {
            throw new NotImplementedInReferenceAssemblyException();
        }

        public Task<MediaFile> PickVideoAsync()
        {
            throw new NotImplementedInReferenceAssemblyException();
        }
    }
}