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
                        }

                        this.init = null;
                    });
        }

        private static Tuple<ICamera, DeviceInformation> CreateCamera(DeviceInformation deviceInformation)
        {
            var camera = new VideoCamera(deviceInformation);
            return new Tuple<ICamera, DeviceInformation>(camera, deviceInformation);
        }

        private void WaitUntilInitializationHasFinished()
        {
            if (this.init != null)
            {
                this.init.Wait();
            }
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            lock (this.cameras)
            {
                if (this.cameras.ContainsKey(deviceInformation.Id))
                {
                    this.cameras.Remove(deviceInformation.Id);
                }

                this.cameras.Add(deviceInformation.Id, CreateCamera(deviceInformation));
            }
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            lock (this.cameras)
            {
                if (this.cameras.ContainsKey(deviceInformationUpdate.Id))
                {
                    var deviceInformation = this.cameras[deviceInformationUpdate.Id].Item2;
                    deviceInformation.Update(deviceInformationUpdate);

                    if (this.cameras.ContainsKey(deviceInformationUpdate.Id))
                    {
                        this.cameras.Remove(deviceInformationUpdate.Id);
                    }

                    this.cameras.Add(deviceInformationUpdate.Id, CreateCamera(deviceInformation));
                }
            }
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            lock (this.cameras)
            {
                this.cameras.Remove(update.Id);
            }
        }

        public IEnumerable<ICamera> Cameras
        {
            get
            {
                this.WaitUntilInitializationHasFinished();

                return this.cameras.Values.Select(x => x.Item1);
            }
        }

        public IMediaLibrary MediaLibrary
        {
            get
            {
                return SimpleIoc.Default.GetInstance<IMediaLibrary>();
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