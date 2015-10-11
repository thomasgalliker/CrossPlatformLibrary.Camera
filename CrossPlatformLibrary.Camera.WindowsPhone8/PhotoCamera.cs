using System;
using System.IO;
using System.Threading.Tasks;
using CrossPlatformLibraryCameraControl;
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

            //var capture = new CameraCaptureUI(this.CameraFacingDirection);
            var capture = new ViewFinder(this.CameraFacingDirection);
            var result = await capture.CaptureFileAsync();
            if (result == null)
            {
                return null;
            }

            StorageFolder rootFolder = ApplicationData.Current.LocalFolder;

            string targetFilePath = options.GetFilePath(rootFolder.Path);
            var directoryPath = Path.GetDirectoryName(targetFilePath);
            var directoryName = options.Directory;
            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                var exists = Directory.Exists(directoryPath);
                if (!exists)
                {
                    await rootFolder.CreateFolderAsync(directoryName, CreationCollisionOption.ReplaceExisting);
                }
            }

            rootFolder = await StorageFolder.GetFolderFromPathAsync(directoryPath);

            string targetFilename = Path.GetFileName(targetFilePath);

            var file = await result.CopyAsync(rootFolder, targetFilename, NameCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);

            Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);
            return new MediaFile(file.Path, () => stream);
        }
    }
}