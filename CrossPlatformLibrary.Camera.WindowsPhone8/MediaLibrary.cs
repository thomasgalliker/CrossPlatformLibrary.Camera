using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Guards;
using Tracing;
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
        public async Task SaveToCameraRoll(MediaFile mediafile, bool overwrite = true)
        {
            string targetFilename = mediafile.Filename;
            this.tracer.Debug("SaveToCameraRoll with targetFilename={0}, overwrite={1}", targetFilename, overwrite);

            try
            {
                using (var mediaLibrary = new Microsoft.Xna.Framework.Media.MediaLibrary())
                {
                    mediaLibrary.SavePictureToCameraRoll(targetFilename, mediafile.GetStream());
                }

            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException("Make sure that you add the ID_CAP_MEDIALIB_PHOTO capability in your WmAppManifest.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null)
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