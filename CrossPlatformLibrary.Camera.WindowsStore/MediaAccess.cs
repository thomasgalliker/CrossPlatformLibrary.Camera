using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossPlatformLibrary.IoC;
using Windows.Devices.Enumeration;

namespace CrossPlatformLibrary.Camera
{
    public class MediaAccess : IMediaAccess
    {
        private readonly Dictionary<string, Tuple<ICamera, DeviceInformation>> cameras = new Dictionary<string, Tuple<ICamera, DeviceInformation>>();
        private readonly DeviceWatcher watcher;
        private Task init;
        private bool isCameraAvailable;

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
                            var devices = t.Result;
                            foreach (DeviceInformation deviceInformation in devices)
                            {
                                if (this.cameras.ContainsKey(deviceInformation.Id))
                                {
                                    this.cameras.Remove(deviceInformation.Id);
                                }

                                this.cameras.Add(deviceInformation.Id, CreateCamera(deviceInformation));
                            }

                            this.UpdateCameraAvailability();
                        }

                        this.init = null;
                    });
        }

        private static Tuple<ICamera, DeviceInformation> CreateCamera(DeviceInformation device)
        {
            // TODO GATH: check if we can read video/phot capabilities somewhere
            var camera = new VideoCamera(device.ToCameraFacingDirection(), device.IsEnabled, device.Name);

            return new Tuple<ICamera, DeviceInformation>(camera, device);
        }

        /// <inheritdoc />
        ////public bool IsCameraAvailable
        ////{
        ////    get
        ////    {
        ////        if (this.init != null)
        ////        {
        ////            this.init.Wait();
        ////        }

        ////        return this.isCameraAvailable;
        ////    }
        ////}

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
        {
            lock (this.cameras)
            {
                this.cameras.Add(device.Id, CreateCamera(device));
                this.UpdateCameraAvailability();
            }
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            ////object value;
            ////if (!deviceInformationUpdate.Properties.TryGetValue("System.Devices.InterfaceEnabled", out value))
            ////{
            ////    return;
            ////}

            lock (this.cameras)
            {
                if (this.cameras.ContainsKey(deviceInformationUpdate.Id))
                {
                    var deviceInformation = this.cameras[deviceInformationUpdate.Id].Item2;
                    deviceInformation.Update(deviceInformationUpdate);

                    ////this.cameras[deviceInformationUpdate.Id] = CreateCamera(deviceInformation);

                    if (this.cameras.ContainsKey(deviceInformationUpdate.Id))
                    {
                        this.cameras.Remove(deviceInformationUpdate.Id);
                    }

                    this.cameras.Add(deviceInformationUpdate.Id, CreateCamera(deviceInformation));
                }

                this.UpdateCameraAvailability();
            }
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            lock (this.cameras)
            {
                this.cameras.Remove(update.Id);
                this.UpdateCameraAvailability();
            }
        }

        private void UpdateCameraAvailability()
        {
            this.isCameraAvailable = this.cameras.Any(device => device.Value.Item2.IsEnabled);
        }

        public IEnumerable<ICamera> Cameras
        {
            get
            {
                return this.cameras.Values.Select(x => x.Item1);
            }
        }

        public IMediaPicker MediaPicker
        {
            get
            {
                return SimpleIoc.Default.GetInstance<IMediaPicker>();
            }
        }

        ////public event EventHandler<ICamera> CameraAdded;

        ////public event EventHandler<ICamera> CameraUpdated;

        ////public event EventHandler<ICamera> CameraRemoved;

        ////protected virtual void OnCameraAdded(ICamera camera)
        ////{
        ////    var handler = this.CameraAdded;
        ////    if (handler != null)
        ////    {
        ////        handler(this, camera);
        ////    }
        ////}

        ////protected virtual void OnCameraUpdated(ICamera camera)
        ////{
        ////    var handler = this.CameraUpdated;
        ////    if (handler != null)
        ////    {
        ////        handler(this, camera);
        ////    }
        ////}

        ////protected virtual void OnCameraRemoved(ICamera camera)
        ////{
        ////    var handler = this.CameraRemoved;
        ////    if (handler != null)
        ////    {
        ////        handler(this, camera);
        ////    }
        ////}
    }
}