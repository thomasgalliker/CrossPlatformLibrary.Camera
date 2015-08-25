﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace CrossPlatformLibrary.Camera
{
    public class Media : IMedia
    {
        private static TaskCompletionSource<MediaFile> completionSource;
        private static readonly IEnumerable<string> SupportedVideoFileTypes = new List<string> { ".mp4", ".wmv", ".avi" };
        private static readonly IEnumerable<string> SupportedImageFileTypes = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };

        public Media()
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

                        lock (this.devices)
                        {
                            foreach (DeviceInformation device in t.Result)
                            {
                                if (device.IsEnabled)
                                {
                                    this.devices.Add(device.Id);
                                }
                            }

                            this.isCameraAvailable = (this.devices.Count > 0);
                        }

                        this.init = null;
                    });
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
                return false;
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
        ///     Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public async Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options)
        {
            if (!this.IsCameraAvailable)
            {
                throw new NotSupportedException();
            }

            options.VerifyOptions();

            var capture = new CameraCaptureUI();
            var result = await capture.CaptureFileAsync(CameraCaptureUIMode.Photo, options);
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

        /// <summary>
        ///     Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
        public Task<MediaFile> PickPhotoAsync()
        {
            var ntcs = new TaskCompletionSource<MediaFile>();
            if (Interlocked.CompareExchange(ref completionSource, ntcs, null) != null)
            {
                throw new InvalidOperationException("Only one operation can be active at at time");
            }

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;
            foreach (var filter in SupportedImageFileTypes)
            {
                picker.FileTypeFilter.Add(filter);
            }

            picker.PickSingleFileAndContinue();
            return ntcs.Task;
        }

        /// <summary>
        ///     Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
        public Task<MediaFile> PickVideoAsync()
        {
            var ntcs = new TaskCompletionSource<MediaFile>();
            if (Interlocked.CompareExchange(ref completionSource, ntcs, null) != null)
            {
                throw new InvalidOperationException("Only one operation can be active at at time");
            }

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;
            foreach (var filter in SupportedVideoFileTypes)
            {
                picker.FileTypeFilter.Add(filter);
            }

            picker.PickSingleFileAndContinue();
            return ntcs.Task;
        }

        private Task init;
        private readonly HashSet<string> devices = new HashSet<string>();
        private readonly DeviceWatcher watcher;
        private bool isCameraAvailable;

        /// <summary>
        ///     OnFilesPicked
        /// </summary>
        /// <param name="args"></param>
        public static void OnFilesPicked(IActivatedEventArgs args)
        {
            var tcs = Interlocked.Exchange(ref completionSource, null);

            IReadOnlyList<StorageFile> files;
            var fopArgs = args as FileOpenPickerContinuationEventArgs;
            if (fopArgs != null)
            {
                // Pass the picked files to the subscribed event handlers
                // In a real world app you could also use a Messenger, Listener or any other subscriber-based model
                if (fopArgs.Files.Any())
                {
                    files = fopArgs.Files;
                }
                else
                {
                    tcs.SetResult(null);
                    return;
                }
            }
            else
            {
                tcs.SetResult(null);
                return;
            }

            // Check if video or image and pick first file to show
            var imageFile = files.FirstOrDefault(f => SupportedImageFileTypes.Contains(f.FileType.ToLower()));
            if (imageFile != null)
            {
                tcs.SetResult(new MediaFile(imageFile.Path, () => imageFile.OpenStreamForReadAsync().Result));
                return;
            }

            var videoFile = files.FirstOrDefault(f => SupportedVideoFileTypes.Contains(f.FileType.ToLower()));
            if (videoFile != null)
            {
                tcs.SetResult(new MediaFile(videoFile.Path, () => videoFile.OpenStreamForReadAsync().Result));
                return;
            }

            tcs.SetResult(null);
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            object value;
            if (!update.Properties.TryGetValue("System.Devices.InterfaceEnabled", out value))
            {
                return;
            }

            lock (this.devices)
            {
                if ((bool)value)
                {
                    this.devices.Add(update.Id);
                }
                else
                {
                    this.devices.Remove(update.Id);
                }

                this.isCameraAvailable = this.devices.Count > 0;
            }
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            lock (this.devices)
            {
                this.devices.Remove(update.Id);
                if (this.devices.Count == 0)
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

            lock (this.devices)
            {
                this.devices.Add(device.Id);
                this.isCameraAvailable = true;
            }
        }
    }
}