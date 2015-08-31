﻿using System;
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

            options.VerifyOptions();

            var capture = new CameraCaptureUI(this.CameraFacingDirection);
            ////capture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            ////capture.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;

            var result = await capture.CaptureFileAsync(this.deviceInformation.Id);
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