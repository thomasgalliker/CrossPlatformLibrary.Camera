using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;

namespace CrossPlatformLibrary.Camera.ViewFinder
{
    [Activity]
    [Preserve(AllMembers = true)]
    public class CameraActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.Window.AddFlags(WindowManagerFlags.Fullscreen); //to hide

            this.ActionBar.Hide();
            this.SetContentView(Resource.Layout.activity_camera);

            if (bundle == null)
            {
                this.FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2BasicFragment.NewInstance()).Commit();
            }
        }
    }
}