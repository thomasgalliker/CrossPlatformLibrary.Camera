using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Phone.Tasks;
using Windows.Storage;

namespace CrossPlatformLibrary.Camera
{
    public class PhotoCamera : IPhotoCamera
    {
        private static TaskCompletionSource<MediaFile> completionSource;
        private readonly CameraCaptureTask cameraCapture;

        public PhotoCamera(CameraFacingDirection cameraFacingDirection, bool isEnabled, string name)
        {
            this.CameraFacingDirection = cameraFacingDirection;
            this.IsEnabled = isEnabled;
            this.Name = name;
        }

        /// <inheritdoc />
        public CameraFacingDirection CameraFacingDirection { get; private set; }

        /// <inheritdoc />
        public bool IsEnabled { get; private set; }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public async Task<MediaFile> TakePhotoAsync(StoreMediaOptions options)
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

            options.VerifyOptions();

            var capture = new CameraCaptureUI(this.CameraFacingDirection);
            var result = await capture.CaptureFileAsync();
            if (result == null)
            {
                return null;
            }

            StorageFolder folder = ApplicationData.Current.LocalFolder;

            string path = options.GetFilePath(folder.Path);
            var directoryFull = Path.GetDirectoryName(path);
            var newFolder = directoryFull.Replace(folder.Path, string.Empty);
            if (!string.IsNullOrWhiteSpace(newFolder))
            {
                await folder.CreateFolderAsync(newFolder, CreationCollisionOption.OpenIfExists);
            }

            folder = await StorageFolder.GetFolderFromPathAsync(directoryFull);

            string filename = Path.GetFileName(path);

            var file = await result.CopyAsync(folder, filename, NameCollisionOption.GenerateUniqueName).AsTask().ConfigureAwait(false);

            Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);
            return new MediaFile(file.Path, () => stream);
        }
    }
}