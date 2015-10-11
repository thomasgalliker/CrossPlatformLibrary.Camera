using System;
using System.IO;
using System.Threading.Tasks;
using CameraControls;
using Windows.Devices.Enumeration;
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

        protected string DeviceId
        {
            get
            {
                return this.deviceInformation.Id;
            }
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
        public async Task<MediaFile> TakePhotoAsync(StoreMediaOptions options) // TODO GATH: link from WindowsStore assembly
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

            var capture = new CameraCaptureUI();
            ////capture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            ////capture.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;

            var result = await capture.CaptureFileAsync(this.deviceInformation.Id);
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
                var exists = await FolderExistsAsync(rootFolder, directoryPath);
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

        static async Task<bool> FolderExistsAsync(StorageFolder folder, string folderName)
        {
            try
            {
                await folder.GetFolderAsync(folderName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}