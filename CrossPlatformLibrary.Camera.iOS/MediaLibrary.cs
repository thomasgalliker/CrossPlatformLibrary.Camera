using System;
using System.Threading.Tasks;

using Guards;

using Tracing;

namespace CrossPlatformLibrary.Camera
{
    public class MediaLibrary : IMediaLibrary
    {
        private readonly ITracer tracer;

        public MediaLibrary(ITracer tracer)
        {
            Guard.ArgumentNotNull(tracer, nameof(tracer));

            this.tracer = tracer;
        }

        public async Task SaveToCameraRoll(MediaFile mediafile, bool overwrite = true)
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