using System;
using System.IO;


namespace CrossPlatformLibrary.Camera
{
    public class StoreMediaOptions
    {
        public StoreMediaOptions(string name = null, string directory = "")
        {
            if (Path.IsPathRooted(directory))
            {
                throw new ArgumentException("Directory must be a relative path", "directory");
            }

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
            get; private set;
        }

        public string Name
        {
            get; private set;
        }

        private bool IsPhoto
        {
            get
            {
                return !(this is StoreVideoOptions);
            }
        }

        public string GetFilePath(string rootPath)
        {
            if (!Path.IsPathRooted(rootPath))
            {
                throw new ArgumentException("Directory must be a root path", "rootPath");
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
    }
}