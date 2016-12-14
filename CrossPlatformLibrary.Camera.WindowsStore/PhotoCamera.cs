using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Storage;

namespace CrossPlatformLibrary.Camera
{
    public class PhotoCamera : IPhotoCamera
    {
        protected PhotoCamera(DeviceInformation deviceInformation)
        {
            this.CameraFacingDirection = deviceInformation.ToCameraFacingDirection();
            this.IsEnabled = deviceInformation.IsEnabled;
            this.Name = deviceInformation.Name;
        }

        public CameraFacingDirection CameraFacingDirection { get; }

        public bool IsEnabled { get; }

        public string Name { get; }

        /// <inheritdoc />
        public async Task<MediaFile> TakePhotoAsync(StoreMediaOptions options)
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

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