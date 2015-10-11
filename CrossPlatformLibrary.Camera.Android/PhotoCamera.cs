using System;
using System.Threading;
using System.Threading.Tasks;

using Android.Content;
using Android.Runtime;


using CrossPlatformLibrary.Camera.ViewFinder;
using CrossPlatformLibrary.Tracing;
using Guards;



namespace CrossPlatformLibrary.Camera
{
    [Preserve(AllMembers = true)]
    public class PhotoCamera : IPhotoCamera
    {
        private readonly ITracer tracer = Tracer.Create<PhotoCamera>();
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


        // TODO GATH: Consider this way
        // http://developer.xamarin.com/recipes/android/other_ux/camera_intent/take_a_picture_and_save_using_camera_app/
        // http://stackoverflow.com/questions/21957599/xamarin-android-issue-reading-locally-stored-photo-from-camera-no-read-acces
        ////private void TakeAPicture(object sender, EventArgs eventArgs)
        ////{
        ////    Intent intent = new Intent(MediaStore.ActionImageCapture);
        ////    App._file = new File(App._dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));
        ////    intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(App._file));
        ////    StartActivityForResult(intent, 0);
        ////}


        /// <inheritdoc />
        public async Task<MediaFile> TakePhotoAsync(StoreMediaOptions options)
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

            int id = this.GetRequestId();
            this.tracer.Debug("TakePhotoAsync with RequestId={0}", id);

            var ntcs = new TaskCompletionSource<MediaFile>(id);
            if (Interlocked.CompareExchange(ref this.completionSource, ntcs, null) != null)
            {
                throw new InvalidOperationException("Only one operation can be active at a time");
            }

            var cameraActivityIntent = this.CreateCameraIntent(options, id);
            this.context.StartActivity(cameraActivityIntent);

            EventHandler<MediaPickedEventArgs> handler = null;
            handler = (s, e) =>
            {
                this.tracer.Debug("EventHandler<MediaPickedEventArgs> fired");
                var tcs = Interlocked.Exchange(ref this.completionSource, null);

                Camera2BasicFragment.MediaPicked -= handler;

                if (e.RequestId != id)
                {
                    this.tracer.Warning("RequestId={0} does not match with {1}", id, e.RequestId);
                    return;
                }

                if (e.Error != null)
                {
                    tcs.SetResult(null);
                }
                else if (e.IsCanceled)
                {
                    this.tracer.Debug("EventHandler<MediaPickedEventArgs> IsCanceled");
                    tcs.SetResult(null);
                }
                else
                {
                    this.tracer.Debug("EventHandler<MediaPickedEventArgs> media returned: {0}", e.Media.Filename);
                    tcs.SetResult(e.Media);
                }
            };

            Camera2BasicFragment.MediaPicked += handler;

            return await ntcs.Task;
        }

        private Intent CreateCameraIntent(StoreMediaOptions options, int id)
        {
            Intent pickerIntent = new Intent(this.context, typeof(CameraActivity));

            pickerIntent.PutExtra(Camera2BasicFragment.ExtraId, id);
            pickerIntent.PutExtra(Camera2BasicFragment.ExtraPath, options.Directory);
            pickerIntent.PutExtra(Camera2BasicFragment.ExtraFilename, options.Name);
            pickerIntent.PutExtra(Camera2BasicFragment.ExtraCameraFacingDirection, Camera2BasicFragment.ToLensFacingInteger(this.CameraFacingDirection));

            Tracer.Create(this).Debug("CreateCameraIntent: ExtraCameraFacingDirection={0}", Camera2BasicFragment.ToLensFacingInteger(this.CameraFacingDirection));

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