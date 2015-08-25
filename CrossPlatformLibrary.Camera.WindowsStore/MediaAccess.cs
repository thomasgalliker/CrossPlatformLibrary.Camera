using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Storage.Pickers;

namespace CrossPlatformLibrary.Camera
{
    public class MediaAccess : IMediaAccess
    {
        private static readonly IEnumerable<string> SupportedVideoFileTypes = new List<string> { ".mp4", ".wmv", ".avi" };
        private static readonly IEnumerable<string> SupportedImageFileTypes = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };

        public MediaAccess()
        {
            this.watcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);
            this.watcher.Added += this.OnDeviceAdded;
            this.watcher.Updated += this.OnDeviceUpdated;
            this.watcher.Removed += this.OnDeviceRemoved;
            this.watcher.Start();

            this.init = DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask().ContinueWith(
                t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            return;
                        }

                        lock (this.cameras)
                        {
                            foreach (DeviceInformation device in t.Result)
                            {
                                if (device.IsEnabled && this.cameras.ContainsKey(device.Id) == false)
                                {
                                    this.cameras.Add(device.Id, CreateCamera(device));
                                }
                            }

                            this.isCameraAvailable = (this.cameras.Count > 0);
                        }

                        this.init = null;
                    });
        }

        private static Tuple<ICamera, DeviceInformation> CreateCamera(DeviceInformation device)
        {
            return new Tuple<ICamera, DeviceInformation>(new PhotoCamera(ToCameraDevice(device)), device);
        }

        //TODO GATH: Make this method more flexible (Adapter Pattern)
        private static CameraFacingDirection ToCameraDevice(DeviceInformation deviceInformation)
        {
            if (deviceInformation.EnclosureLocation != null)
            {
                if (deviceInformation.EnclosureLocation.Panel == Panel.Front)
                {
                    return CameraFacingDirection.Front;
                }

                if (deviceInformation.EnclosureLocation.Panel == Panel.Back)
                {
                    return CameraFacingDirection.Rear;
                }
            }

            return CameraFacingDirection.Undefined;
        }

        /// <inheritdoc />
        public bool IsCameraAvailable
        {
            get
            {
                if (this.init != null)
                {
                    this.init.Wait();
                }

                return this.isCameraAvailable;
            }
        }

        /// <inheritdoc />
        public bool IsTakePhotoSupported
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc />
        public bool IsPickPhotoSupported
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc />
        public bool IsTakeVideoSupported
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc />
        public bool IsPickVideoSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///     Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
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

        /// <summary>
        ///     Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
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

        private Task init;
        private readonly DeviceWatcher watcher;
        private bool isCameraAvailable;
        private readonly Dictionary<string, Tuple<ICamera, DeviceInformation>> cameras = new Dictionary<string, Tuple<ICamera, DeviceInformation>>();

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            object value;
            if (!update.Properties.TryGetValue("System.Devices.InterfaceEnabled", out value))
            {
                return;
            }

            lock (this.cameras)
            {
                foreach (DeviceInformation deviceInformation in this.cameras.Values.Select(x => x.Item2).ToList())
                {
                    if (deviceInformation.Id == update.Id)
                    {
                        deviceInformation.Update(update);

                        if (!deviceInformation.IsEnabled)
                        {
                            this.cameras.Remove(update.Id);
                        }
                    }
                }

                this.isCameraAvailable = this.cameras.Count > 0;
            }
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            lock (this.cameras)
            {
                this.cameras.Remove(update.Id);
                if (this.cameras.Count == 0)
                {
                    this.isCameraAvailable = false;
                }
            }
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
        {
            if (!device.IsEnabled)
            {
                return;
            }

            lock (this.cameras)
            {
                this.cameras.Add(device.Id, CreateCamera(device));
                this.isCameraAvailable = true;
            }
        }

        public IEnumerable<ICamera> Cameras
        {
            get
            {
                return this.cameras.Values.Select(x => x.Item1);
            }
        }
    }
}