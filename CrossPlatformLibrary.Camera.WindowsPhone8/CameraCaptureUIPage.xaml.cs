
using Microsoft.Phone.Controls;


namespace CrossPlatformLibrary.Camera
{
    public partial class CameraCaptureUIPage : PhoneApplicationPage
    {
        public CameraCaptureUIPage()
        {
            this.InitializeComponent();
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (this.MyCCUCtrl != null)
            {
                this.MyCCUCtrl.UpdateOrientation(e.Orientation);
            }

            base.OnOrientationChanged(e);
        }

        internal CameraCaptureUI MyCCUCtrl { get; set; }
    }
}