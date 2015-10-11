using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;

namespace CrossPlatformLibrary.Camera
{
    /// <summary>
    ///     Implementation for Feature
    /// </summary>
    [Preserve(AllMembers = true)]
    public class Media : IMedia
    {
        /// <summary>
        ///     Implementation
        /// </summary>
        public Media()
        {
            this.context = Application.Context;
            this.IsCameraAvailable = this.context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread)
            {
                this.IsCameraAvailable |= this.context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront);
            }
        }

        /// <inheritdoc />
        public bool IsCameraAvailable { get; private set; }

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
        /// </summary>
        /// <returns></returns>
        public Intent GetPickPhotoUI()
        {
            int id = this.GetRequestId();
            return this.CreateMediaIntent(id, "image/*", Intent.ActionPick, null, tasked: false);
        }

        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Intent GetTakePhotoUI(StoreMediaOptions options)
        {
            if (!this.IsCameraAvailable)
            {
                throw new NotSupportedException();
            }

            int id = this.GetRequestId();
            return this.CreateMediaIntent(id, "image/*", MediaStore.ActionImageCapture, options, tasked: false);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Intent GetPickVideoUI()
        {
            int id = this.GetRequestId();
            return this.CreateMediaIntent(id, "video/*", Intent.ActionPick, null, tasked: false);
        }

        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Intent GetTakeVideoUI(StoreVideoOptions options)
        {
            if (!this.IsCameraAvailable)
            {
                throw new NotSupportedException();
            }

            return this.CreateMediaIntent(this.GetRequestId(), "video/*", MediaStore.ActionVideoCapture, options, tasked: false);
        }

        /// <summary>
        ///     Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
        public Task<MediaFile> PickPhotoAsync()
        {
            return this.TakeMediaAsync("image/*", Intent.ActionPick, null);
        }

        /// <summary>
        ///     Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public Task<MediaFile> TakePhotoAsync(StoreMediaOptions options)
        {
            if (!this.IsCameraAvailable)
            {
                throw new NotSupportedException();
            }

            return this.TakeMediaAsync("image/*", MediaStore.ActionImageCapture, options);
        }

        /// <summary>
        ///     Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
        public Task<MediaFile> PickVideoAsync()
        {
            return this.TakeMediaAsync("video/*", Intent.ActionPick, null);
        }

        /// <summary>
        ///     Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            if (!this.IsCameraAvailable)
            {
                throw new NotSupportedException();
            }

            return this.TakeMediaAsync("video/*", MediaStore.ActionVideoCapture, options);
        }

        private readonly Context context;
        private int requestId;
        private TaskCompletionSource<MediaFile> completionSource;

        private Intent CreateMediaIntent(int id, string type, string action, StoreMediaOptions options, bool tasked = true)
        {
            Intent pickerIntent = new Intent(this.context, typeof(MediaPickerActivity));
            pickerIntent.PutExtra(MediaPickerActivity.ExtraId, id);
            pickerIntent.PutExtra(MediaPickerActivity.ExtraType, type);
            pickerIntent.PutExtra(MediaPickerActivity.ExtraAction, action);
            pickerIntent.PutExtra(MediaPickerActivity.ExtraTasked, tasked);

            if (options != null)
            {
                pickerIntent.PutExtra(MediaPickerActivity.ExtraPath, options.Directory);
                pickerIntent.PutExtra(MediaStore.Images.ImageColumns.Title, options.Name);

                var vidOptions = (options as StoreVideoOptions);
                if (vidOptions != null)
                {
                    pickerIntent.PutExtra(MediaStore.ExtraDurationLimit, (int)vidOptions.DesiredLength.TotalSeconds);
                    pickerIntent.PutExtra(MediaStore.ExtraVideoQuality, (int)vidOptions.Quality);
                }
            }
            //pickerIntent.SetFlags(ActivityFlags.ClearTop);
            pickerIntent.SetFlags(ActivityFlags.NewTask);
            return pickerIntent;
        }

        private int GetRequestId()
        {
            int id = this.requestId;
            if (this.requestId == Int32.MaxValue)
            {
                this.requestId = 0;
            }
            else
            {
                this.requestId++;
            }

            return id;
        }

        private Task<MediaFile> TakeMediaAsync(string type, string action, StoreMediaOptions options)
        {
            int id = this.GetRequestId();

            var ntcs = new TaskCompletionSource<MediaFile>(id);
            if (Interlocked.CompareExchange(ref this.completionSource, ntcs, null) != null)
            {
                throw new InvalidOperationException("Only one operation can be active at a time");
            }

            this.context.StartActivity(this.CreateMediaIntent(id, type, action, options));

            EventHandler<MediaPickedEventArgs> handler = null;
            handler = (s, e) =>
                {
                    var tcs = Interlocked.Exchange(ref this.completionSource, null);

                    MediaPickerActivity.MediaPicked -= handler;

                    if (e.RequestId != id)
                    {
                        return;
                    }

                    if (e.Error != null)
                    {
                        tcs.SetResult(null);
                    }
                    else if (e.IsCanceled)
                    {
                        tcs.SetResult(null);
                    }
                    else
                    {
                        tcs.SetResult(e.Media);
                    }
                };

            MediaPickerActivity.MediaPicked += handler;

            return ntcs.Task;
        }
    }
}