using System;

namespace CrossPlatformLibrary.Camera
{
    public class MediaFileNotFoundException : Exception
    {
        public MediaFileNotFoundException(string path)
            : base("Unable to locate media file at " + path)
        {
            this.Path = path;
        }

        public MediaFileNotFoundException(string path, Exception innerException)
            : base("Unable to locate media file at " + path, innerException)
        {
            this.Path = path;
        }

        /// <summary>
        ///     Path
        /// </summary>
        public string Path { get; private set; }
    }
}