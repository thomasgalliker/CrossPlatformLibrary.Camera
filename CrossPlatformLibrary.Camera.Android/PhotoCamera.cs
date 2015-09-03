using System;
using System.Threading;
using System.Threading.Tasks;

using Android.Content;
using Android.Provider;

using CrossPlatformLibrary.Utils;

namespace CrossPlatformLibrary.Camera
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class PhotoCamera : IPhotoCamera
    {
        private readonly Context context;
        private int requestId;
        private TaskCompletionSource<MediaFile> completionSource;

        public PhotoCamera(CameraFacingDirection cameraFacingDirection, bool isEnabled, string name, Context context)
        {
            Guard.ArgumentNotNull(() => context);

            this.CameraFacingDirection = cameraFacingDirection;
            this.IsEnabled = isEnabled;
            this.Name = name;
            this.context = context;
        }

        /// <inheritdoc />
        public CameraFacingDirection CameraFacingDirection { get; private set; }

        /// <inheritdoc />
        public bool IsEnabled { get; private set; }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public async Task<MediaFile> TakePhotoAsync(StoreMediaOptions options)
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

            options.VerifyOptions();

            return await this.TakeMediaAsync("image/*", MediaStore.ActionImageCapture, options);
        }

        private Task<MediaFile> TakeMediaAsync(string type, string action, StoreMediaOptions options)
        {
            int id = this.GetRequestId();

            var ntcs = new TaskCompletionSource<MediaFile>(id);
            if (Interlocked.CompareExchange(ref this.completionSource, ntcs, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

            this.context.StartActivity(this.CreateMediaIntent(id, type, action, options));

            EventHandler<MediaPickedEventArgs> handler = null;
            handler = (s, e) =>
            {
                var tcs = Interlocked.Exchange(ref this.completionSource, null);

                MediaPickerActivity.MediaPicked -= handler;

                if (e.RequestId != id)
                    return;

                if (e.Error != null)
                    tcs.SetResult(null);
                else if (e.IsCanceled)
                    tcs.SetResult(null);
                else
                    tcs.SetResult(e.Media);
            };

            MediaPickerActivity.MediaPicked += handler;

            return ntcs.Task;
        }

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
                this.requestId = 0;
            else
                this.requestId++;

            return id;
        }
    }
}