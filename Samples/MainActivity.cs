
using System.Linq;

using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Widget;

using CrossPlatformLibrary.Bootstrapping;
using CrossPlatformLibrary.Camera;
using CrossPlatformLibrary.IoC;
using CrossPlatformLibrary.Tracing;

using Resource = AndroidCameraApp.Resource;

namespace MediaAndroidTest
{
    [Activity(Label = "AndroidCameraApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private int count = 1;
        private IMediaLibrary mediaLibrary;
        private IMediaAccess mediaAccess;
        private IMedia media;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var bootstrapper = new Bootstrapper();
            bootstrapper.Startup();
            Tracer.SetFactory(new AndroidLogTracerFactory());

            // Set our view from the "main" layout resource
            this.SetContentView(Resource.Layout.Main);

            this.media = SimpleIoc.Default.GetInstance<IMedia>();
            this.mediaAccess = SimpleIoc.Default.GetInstance<IMediaAccess>();
            this.mediaLibrary = SimpleIoc.Default.GetInstance<IMediaLibrary>();

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = this.FindViewById<Button>(Resource.Id.MyButton);
            var image = this.FindViewById<ImageView>(Resource.Id.imageView1);
            button.Click += async delegate
                {
                    ////var takePhotoAsync = await this.media.TakePhotoAsync(new StoreMediaOptions { Directory = "Sample", Name = "rearcamera.jpg" });
                    ////if (takePhotoAsync != null)
                    ////{
                    ////    var stream = takePhotoAsync.GetStream();
                    ////}

                    var cameras = this.mediaAccess.Cameras.ToList();
                    var frontCamera = cameras.FirstOrDefault() as IPhotoCamera;
                    if (frontCamera != null)
                    {
                        var file = await frontCamera.TakePhotoAsync(new StoreMediaOptions { Directory = "Sample", Name = "rearcamera.jpg" });
                        if (file != null)
                        {
                            var stream = file.GetStream();
                            image.SetImageBitmap(BitmapFactory.DecodeFile(file.Path));
                            ////await this.mediaLibrary.SaveToCameraRoll(stream);
                        }

                        ////if (file != null)
                        ////{
                        ////    var stream = file.GetStream();
                        ////    image.SetImageBitmap(BitmapFactory.DecodeFile(file.Path));
                        ////}
                    }

                    ////var backCamera = cameras.LastOrDefault() as IPhotoCamera;
                    ////if (backCamera != null)
                    ////{
                    ////    var file = await backCamera.TakePhotoAsync(new StoreMediaOptions { Directory = "Sample", Name = "frontcamera.jpg" });
                    ////    if (file != null)
                    ////    {
                    ////        var stream = file.GetStream();
                    ////        ////await this.mediaLibrary.SaveToCameraRoll(stream);
                    ////    }
                    ////    ////if (file != null)
                    ////    ////{
                    ////    ////    image.SetImageBitmap(BitmapFactory.DecodeFile(file.Path));
                    ////    ////}
                    ////}
                };

            ////var pick = FindViewById<Button>(Resource.Id.button1);
            ////pick.Click += async (sender, args) =>
            ////{
            ////    var file = await MediaStore.Audio.Media.Plugin.CrossMedia.Current.PickPhotoAsync();
            ////    if (file == null)
            ////        return;
            ////    image.SetImageBitmap(BitmapFactory.DecodeFile(file.Path));
            ////};

            ////FindViewById<Button>(Resource.Id.button2).Click += async (sender, args) =>
            ////{
            ////    var media = new MediaStore.Audio.Media.Plugin.MediaImplementation();
            ////    var file = await MediaStore.Audio.Media.Plugin.CrossMedia.Current.TakeVideoAsync(new MediaStore.Audio.Media.Plugin.Abstractions.StoreVideoOptions
            ////    {
            ////        Directory = "Sample",
            ////        Name = "test.jpg"
            ////    });
            ////    if (file == null)
            ////        return;
            ////};

            ////FindViewById<Button>(Resource.Id.button3).Click += async (sender, args) =>
            ////{
            ////    var media = new MediaStore.Audio.Media.Plugin.MediaImplementation();
            ////    var file = await MediaStore.Audio.Media.Plugin.CrossMedia.Current.PickVideoAsync();
            ////    if (file == null)
            ////        return;
            ////};
        }
    }
}