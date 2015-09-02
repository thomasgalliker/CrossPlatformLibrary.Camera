using System;
using System.IO;


namespace CrossPlatformLibrary.Camera
{
    public class StoreMediaOptions
    {
        public string Directory
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public void VerifyOptions()
        {
            if (Path.IsPathRooted(this.Directory))
            {
                throw new ArgumentException("options.Directory must be a relative path", "options");
            }
        }

        public string GetFilePath(string rootPath)
        {
            bool isPhoto = !(this is StoreVideoOptions);

            string name = this.Name;
            if (String.IsNullOrWhiteSpace(name))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (isPhoto)
                {
                    name = "IMG_" + timestamp + ".jpg";
                }
                else
                {
                    name = "VID_" + timestamp + ".mp4";
                }
            }

            string ext = Path.GetExtension(name);
            if (ext == String.Empty)
            {
                ext = ((isPhoto) ? ".jpg" : ".mp4");
            }

            name = Path.GetFileNameWithoutExtension(name);

            string folder = Path.Combine(rootPath ?? String.Empty, this.Directory ?? string.Empty);

            return Path.Combine(folder, name + ext);
        }

        public string GetUniqueFilepath(string rootPath, Func<string, bool> checkExists)
        {
            string path = this.GetFilePath(rootPath);
            string folder = Path.GetDirectoryName(path);
            string ext = Path.GetExtension(path);
            string name = Path.GetFileNameWithoutExtension(path);

            string nname = name + ext;
            int i = 1;
            while (checkExists(Path.Combine(folder, nname)))
            {
                nname = name + "_" + (i++) + ext;
            }

            return Path.Combine(folder, nname);
        }
    }
}