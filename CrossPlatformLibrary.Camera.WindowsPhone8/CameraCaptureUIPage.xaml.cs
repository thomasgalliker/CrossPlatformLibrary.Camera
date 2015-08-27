using System.Windows.Navigation;

using Microsoft.Phone.Controls;


namespace CrossPlatformLibrary.Camera
{
    public partial class CameraCaptureUIPage : PhoneApplicationPage
    {
        public CameraCaptureUIPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ////this.cameraViewer.SaveToCameraRoll = true;
            ////this.cameraViewer.StartPumpingFrames();
        }

        internal CameraCaptureUI MyCCUCtrl { get; set; }
    }
}