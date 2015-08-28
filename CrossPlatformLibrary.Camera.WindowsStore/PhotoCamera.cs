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
        private readonly DeviceInformation deviceInformation;

        protected PhotoCamera(DeviceInformation deviceInformation)
        {
            this.deviceInformation = deviceInformation;
        }

        public CameraFacingDirection CameraFacingDirection
        {
            get
            {
                return this.deviceInformation.ToCameraFacingDirection();
            }
        }

        public bool IsEnabled
        {
            get
            {
                return this.deviceInformation.IsEnabled;
            }
        }

        public string Name
        {
            get
            {
                return this.deviceInformation.Name;
            }
        }

        /// <inheritdoc />
        public async Task<MediaFile> TakePhotoAsync(StoreMediaOptions options)
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