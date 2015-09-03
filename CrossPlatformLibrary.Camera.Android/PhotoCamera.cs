using System;
using System.Threading;
using System.Threading.Tasks;

using Android.Content;
using Android.Runtime;

using Camera2Basic;

using CrossPlatformLibrary.Utils;

namespace CrossPlatformLibrary.Camera
{
    [Preserve(AllMembers = true)]
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

            return await this.TakeMediaAsync(options);
        }

        private Task<MediaFile> TakeMediaAsync(StoreMediaOptions options)
        {
            int id = this.GetRequestId();

            var ntcs = new TaskCompletionSource<MediaFile>(id);
            if (Interlocked.CompareExchange(ref this.completionSource, ntcs, null) != null)
            {
                throw new InvalidOperationException("Only one operation can be active at a time");
            }

            var cameraActivityIntent = this.CreateCameraIntent(options);
            this.context.StartActivity(cameraActivityIntent);

            EventHandler<MediaPickedEventArgs> handler = null;
            handler = (s, e) =>
                {
                    var tcs = Interlocked.Exchange(ref this.completionSource, null);

                    Camera2BasicFragment.MediaPicked -= handler;

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

            Camera2BasicFragment.MediaPicked += handler;

            return ntcs.Task;
        }

        private Intent CreateCameraIntent(StoreMediaOptions options)
        {
            Intent pickerIntent = new Intent(this.context, typeof(CameraActivity));
            pickerIntent.PutExtra(Camera2BasicFragment.ExtraPath, options.Directory);

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
    }
}