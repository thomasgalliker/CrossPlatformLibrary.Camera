using Android.App;
using Android.OS;

using CrossPlatformLibrary.Camera;

namespace Camera2Basic
{
    [Activity(Label = "Camera2Basic", MainLauncher = true, Icon = "@drawable/icon")]
    public class CameraActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.ActionBar.Hide();
            this.SetContentView(Resource.Layout.activity_camera);

            if (bundle == null)
            {
                this.FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2BasicFragment.NewInstance()).Commit();
            }
        }
    }
}