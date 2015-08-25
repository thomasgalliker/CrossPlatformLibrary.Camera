using System;
using System.IO;

namespace CrossPlatformLibrary.Camera
{
    public sealed class MediaFile : IDisposable
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="path"></param>
        /// <param name="streamGetter"></param>
        /// <param name="deletePathOnDispose"></param>
        /// <param name="dispose"></param>
        public MediaFile(string path, Func<Stream> streamGetter, bool deletePathOnDispose = false, Action<bool> dispose = null)
        {
            this.dispose = dispose;
            this.streamGetter = streamGetter;
            this.path = path;
            this.deletePathOnDispose = deletePathOnDispose;
        }

        public string Path
        {
            get
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException(null);
                }

                return this.path;
            }
        }

        /// <summary>
        ///     Get stream if not already disposed.
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(null);
            }

            return this.streamGetter();
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;
        private readonly Action<bool> dispose;
        private readonly Func<Stream> streamGetter;
        private readonly string path;
        private readonly bool deletePathOnDispose;

        private void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            if (this.dispose != null)
            {
                this.dispose(disposing);
            }
        }

        ~MediaFile()
        {
            this.Dispose(false);
        }
    }
}