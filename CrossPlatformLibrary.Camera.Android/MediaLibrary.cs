using System;
using System.IO;
using System.Threading.Tasks;

namespace CrossPlatformLibrary.Camera
{
    public class MediaLibrary : IMediaLibrary
    {
        public Task SaveToCameraRoll(Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task<MediaFile> PickPhotoAsync()
        {
            throw new NotImplementedException();
        }

        public Task<MediaFile> PickVideoAsync()
        {
            throw new NotImplementedException();
        }
    }
}