using CrossPlatformLibrary.Bootstrapping;

using Xamarin.Forms;

namespace CameraSample
{
    public class App : Application
    {
        private readonly IBootstrapper bootstrapper;

        public App()
        {
            this.bootstrapper = new Bootstrapper();
            this.bootstrapper.Startup();

            this.MainPage = new NavigationPage(new CameraPage());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
            this.bootstrapper.Sleep();
        }

        protected override void OnResume()
        {
            this.bootstrapper.Resume();
        }
    }
}