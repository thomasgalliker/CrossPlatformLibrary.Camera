using System;
using System.Threading.Tasks;

using Android.Content;

using CrossPlatformLibrary.Tracing;

using Guards;


using Environment = Android.OS.Environment;
using File = Java.IO.File;
using Uri = Android.Net.Uri;

namespace CrossPlatformLibrary.Camera
{
    public class MediaLibrary : IMediaLibrary
    {
        private readonly ITracer tracer;
        private readonly Context context;

        public MediaLibrary(ITracer tracer)
        {
            Guard.ArgumentNotNull(() => tracer);
            this.tracer = tracer;

            this.context = Android.App.Application.Context;
        }

        public async Task SaveToCameraRoll(MediaFile mediafile, bool overwrite = true)
        {
            string targetFilename = mediafile.Filename;
            this.tracer.Debug("SaveToCameraRoll with targetFilename={0}, overwrite={1}", targetFilename, overwrite);

            var picturesDirectory = new File(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).AbsolutePath);
            if (picturesDirectory.Exists())
            {
                this.tracer.Debug("Creating directory {0} since it does not exist.", picturesDirectory.AbsolutePath);
                picturesDirectory.Mkdirs();
            }

            var sourcePath = mediafile.Path;
            var targetPath = System.IO.Path.Combine(picturesDirectory.AbsolutePath, targetFilename);
            this.tracer.Debug("Copying {0} to {1}.", sourcePath, targetPath);
            System.IO.File.Copy(sourcePath, targetPath, overwrite);

            // Make it available in the gallery
            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            Uri contentUri = Uri.FromFile(new File(targetPath));
            this.tracer.Debug("Broadcasting contentUri={0} to media gallery.", contentUri.Path);

            mediaScanIntent.SetData(contentUri);

            this.context.SendBroadcast(mediaScanIntent);    
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