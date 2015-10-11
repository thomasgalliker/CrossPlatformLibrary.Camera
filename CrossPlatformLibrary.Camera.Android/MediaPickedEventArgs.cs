using System;
using System.Threading.Tasks;

using Guards;

namespace CrossPlatformLibrary.Camera
{
    internal class MediaPickedEventArgs : EventArgs
    {
        public MediaPickedEventArgs(int id, Exception error)
        {
            Guard.ArgumentNotNull(() => error);

            this.RequestId = id;
            this.Error = error;
        }

        public MediaPickedEventArgs(int id, MediaFile media)
        {
            Guard.ArgumentNotNull(() => media);

            this.RequestId = id;
            this.Media = media;
        }

        public MediaPickedEventArgs(int id, bool isCanceled)
        {
            this.RequestId = id;
            this.IsCanceled = isCanceled;
        }

        public int RequestId { get; private set; }

        public bool IsCanceled { get; private set; }

        public Exception Error { get; private set; }

        public MediaFile Media { get; private set; }

        public Task<MediaFile> ToTask()
        {
            var tcs = new TaskCompletionSource<MediaFile>();

            if (this.IsCanceled)
            {
                tcs.SetResult(null);
            }
            else if (this.Error != null)
            {
                tcs.SetResult(null);
            }
            else
            {
                tcs.SetResult(this.Media);
            }

            return tcs.Task;
        }
    }
}