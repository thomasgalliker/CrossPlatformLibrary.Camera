using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CrossPlatformLibrary.Tracing;
using CrossPlatformLibrary.Utils;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace CrossPlatformLibrary.Camera
{
    public class MediaLibrary : IMediaLibrary
    {
        private static readonly IEnumerable<string> SupportedVideoFileTypes = new List<string> { ".mp4", ".wmv", ".avi" };
        private static readonly IEnumerable<string> SupportedImageFileTypes = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };
        
        private readonly ITracer tracer;

        public MediaLibrary(ITracer tracer)
        {
            Guard.ArgumentNotNull(() => tracer);

            this.tracer = tracer;
        }

        /// <inheritdoc />
        public async Task SaveToCameraRoll(Stream stream)
        {
            string fileName = string.Format("{0:yyyy-MM-dd-HH-mm-ss}.jpg", DateTime.Now);

            this.tracer.Debug("SaveToCameraRoll with fileName {0}", fileName);

            try
            {
                var cameraRollFolder = KnownFolders.CameraRoll;
                var newFile = await cameraRollFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                using (var outputstream = await newFile.OpenStreamForWriteAsync())
                {
                    await stream.CopyToAsync(outputstream);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException("Make sure that you add the picturesLibrary capability in your Package.appxmanifest.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<MediaFile> PickPhotoAsync()
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;
            foreach (var filter in SupportedImageFileTypes)
            {
                picker.FileTypeFilter.Add(filter);
            }

            var result = await picker.PickSingleFileAsync();
            if (result == null)
            {
                return null;
            }

            return new MediaFile(result.Path, () => result.OpenStreamForReadAsync().Result);
        }

        /// <inheritdoc />
        public async Task<MediaFile> PickVideoAsync()
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;
            foreach (var filter in SupportedVideoFileTypes)
            {
                picker.FileTypeFilter.Add(filter);
            }

            var result = await picker.PickSingleFileAsync();
            if (result == null)
            {
                return null;
            }

            return new MediaFile(result.Path, () => result.OpenStreamForReadAsync().Result);
        }
    }
}