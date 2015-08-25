using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Media.Capture;
using Windows.Storage;

namespace CrossPlatformLibrary.Camera
{
    public class PhotoCamera : IPhotoCamera
    {
        public PhotoCamera(CameraFacingDirection cameraFacingDirection, bool isEnabled, string name)
        {
            this.CameraFacingDirection = cameraFacingDirection;
            this.IsEnabled = isEnabled;
            this.Name = name;
        }

        public CameraFacingDirection CameraFacingDirection { get; private set; }

        public bool IsEnabled { get; private set; }

        public string Name { get; private set; }

        /// <inheritdoc />
        public async Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options)
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

            options.VerifyOptions();

            var capture = new CameraCaptureUI();
            capture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            capture.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;

            var result = await capture.CaptureFileAsync(CameraCaptureUIMode.Photo);
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

            var file = await result.CopyAsync(folder, filename, NameCollisionOption.GenerateUniqueName).AsTask();
            return new MediaFile(file.Path, () => file.OpenStreamForReadAsync().Result);
        }
    }
}