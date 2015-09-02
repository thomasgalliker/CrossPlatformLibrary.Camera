using System;
using System.Threading.Tasks;
using CrossPlatformLibraryCameraControl;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Windows.Phone.Media.Capture;
using Windows.Storage;

namespace CrossPlatformLibrary.Camera
{
    public partial class ViewFinderPage : PhoneApplicationPage
    {
        public ViewFinder viewFinder;

        public ViewFinderPage()
        {
            this.InitializeComponent();
            ////this.viewFinder.SensorLocation = CameraSensorLocation.Back;

            this.Loaded += (a, b) =>
                {
                    if (!(Microsoft.Devices.PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing) && Microsoft.Devices.PhotoCamera.IsCameraTypeSupported(CameraType.Primary)))
                    {
                        this.ApplicationBar.Buttons.RemoveAt(0);
                    }
                };

            ////this.viewFinder.StartPreview();

            ////CameraButtons.ShutterKeyHalfPressed += async (s, e) => { await this.viewFinder.FocusAsync(); };
            ////CameraButtons.ShutterKeyPressed += async (s, e) =>
            ////    {
            ////        var file = await this.viewFinder.TakePicture();
            ////        if (file == null)
            ////        {
            ////            return;
            ////        }
            ////        var bmp = new BitmapImage();
            ////        bmp.SetSource(file.AsStream());
            ////    };
        }

        private void ToggleCameraButtonTapped(object sender, EventArgs e)
        {
            if (Microsoft.Devices.PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing) && Microsoft.Devices.PhotoCamera.IsCameraTypeSupported(CameraType.Primary))
            {
                this.viewFinder.SensorLocation = this.viewFinder.SensorLocation == CameraSensorLocation.Front ? CameraSensorLocation.Back : CameraSensorLocation.Front;
            }
        }

        private void TakePictureButtonTapped(object sender, EventArgs e)
        {
            this.viewFinder.TakePicture();
        }

        public async Task<StorageFile> CaptureFileAsync()
        {
            return await this.viewFinder.CaptureFileAsync();
        }
    }
}