
using Android.App;
using Android.Content.PM;
using Android.OS;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace CameraSample.Droid
{
    [Activity(Label = "CameraSample", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);
            this.LoadApplication(new App());
        }
    }
}

////using System;
////using System.Collections.Generic;

////using Android.App;
////using Android.Content;
////using Android.Content.PM;
////using Android.OS;
////using Android.Provider;

////using Java.IO;

////namespace CameraSample.Droid
////{
////    using Environment = Android.OS.Environment;
////    using Uri = Android.Net.Uri;

////    [Activity(Label = "Camera App Demo", MainLauncher = true)]
////    public class MainActivity : Activity
////    {
////        private File _dir;
////        private File _file;

////        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
////        {
////            base.OnActivityResult(requestCode, resultCode, data);

////            // make it available in the gallery
////            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
////            Uri contentUri = Uri.FromFile(this._file);
////            mediaScanIntent.SetData(contentUri);
////            this.SendBroadcast(mediaScanIntent);

////        }

////        protected override void OnCreate(Bundle bundle)
////        {
////            base.OnCreate(bundle);
////            SetContentView(Android.Resource.Layout.ListContent);

////            if (this.IsThereAnAppToTakePictures())
////            {
////                this.CreateDirectoryForPictures();

////                this.TakeAPicture();
////            }
////        }

////        private void CreateDirectoryForPictures()
////        {
////            this._dir = new File(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).AbsolutePath);
////            if (!this._dir.Exists())
////            {
////                this._dir.Mkdirs();
////            }
////        }

////        private bool IsThereAnAppToTakePictures()
////        {
////            Intent intent = new Intent(MediaStore.ActionImageCapture);
////            IList<ResolveInfo> availableActivities = this.PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
////            return availableActivities != null && availableActivities.Count > 0;
////        }

////        private void TakeAPicture()
////        {
////            Intent intent = new Intent(MediaStore.ActionImageCapture);

////            this._file = new File(this._dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));

////            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(this._file));

////            this.StartActivityForResult(intent, 0);
////        }
////    }
////}

