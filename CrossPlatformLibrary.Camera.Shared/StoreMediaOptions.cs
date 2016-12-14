using System;
using System.IO;

namespace CrossPlatformLibrary.Camera
{
    public class StoreMediaOptions
    {
        private string directory = "";

        int quality = 100;
        int customPhotoSize = 100;

        public StoreMediaOptions(string name = null, string directory = "")
        {
            this.Directory = directory;

            if (string.IsNullOrWhiteSpace(name))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (this.IsPhoto)
                {
                    name = "IMG_" + timestamp + ".jpg";
                }
                else
                {
                    name = "VID_" + timestamp + ".mp4";
                }
            }

            this.Name = name;
        }

        public string Directory
        {
            get
            {
                return this.directory;
            }
            private set
            {
                if (Path.IsPathRooted(value))
                {
                    throw new ArgumentException("Directory must be a relative path", nameof(this.Directory));
                }

                this.directory = value;
            }
        }

        public string Name { get; }

        private bool IsPhoto
        {
            get
            {
                return !(this is StoreVideoOptions);
            }
        }

        public bool AllowCropping { get; set; }

        public object OverlayViewProvider { get; set; }

        public string GetFilePath(string rootPath)
        {
            if (!Path.IsPathRooted(rootPath))
            {
                throw new ArgumentException("Directory must be a root path", nameof(rootPath));
            }

            string folder = Path.Combine(rootPath ?? string.Empty, this.Directory ?? string.Empty);

            return Path.Combine(folder, this.Name);
        }

        ////public string GetUniqueFilepath(string rootPath, Func<string, bool> checkExists)
        ////{
        ////    string path = this.GetFilePath(rootPath);
        ////    string folder = Path.GetDirectoryName(path);
        ////    string ext = Path.GetExtension(path);
        ////    string name = Path.GetFileNameWithoutExtension(path);

        ////    string nname = name + ext;
        ////    int i = 1;
        ////    while (checkExists(Path.Combine(folder, nname)))
        ////    {
        ////        nname = name + "_" + (i++) + ext;
        ////    }

        ////    return Path.Combine(folder, nname);
        ////}

        /// <summary>
        ///     Gets or sets the size of the photo.
        /// </summary>
        /// <value>The size of the photo.</value>
        public PhotoSize PhotoSize { get; set; } = PhotoSize.Full;

        /// <summary>
        ///     The custom photo size to use, 100 full size (same as Full),
        ///     and 1 being smallest size at 1% of original
        ///     Default is 100
        /// </summary>
        public int CustomPhotoSize
        {
            get
            {
                return this.customPhotoSize;
            }
            set
            {
                if (value > 100) this.customPhotoSize = 100;
                else if (value < 1) this.customPhotoSize = 1;
                else this.customPhotoSize = value;
            }
        }

        /// <summary>
        ///     The compression quality to use, 0 is the maximum compression (worse quality),
        ///     and 100 minimum compression (best quality)
        ///     Default is 100
        /// </summary>
        public int CompressionQuality
        {
            get
            {
                return this.quality;
            }
            set
            {
                if (value > 100) this.quality = 100;
                else if (value < 0) this.quality = 0;
                else this.quality = value;
            }
        }
    }
}