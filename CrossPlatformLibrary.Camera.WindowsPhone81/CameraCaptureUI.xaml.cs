using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DMX.Helper;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Panel = Windows.Devices.Enumeration.Panel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CrossPlatformLibrary.Camera
{
    internal enum CameraCaptureUIMode
    {
        PhotoOrVideo,
        Photo,
        Video
    }

    internal sealed partial class CameraCaptureUI : UserControl
    {
        // store the pic here
        private StorageFile file;

        // stop flag - needed to find when to get back to former page

        public bool StopFlag { get; set; }

        // the root grid of our camera ui page
        private readonly Grid mainGrid;

        public MediaCapture MyMediaCapture { get; set; }

        private readonly Frame originalFrame;
        private const short WaitForClickLoopLength = 1000;

        /// <summary>
        ///     Navigates to the CameraCaptureUIPage in a new Frame and show the control
        /// </summary
        public CameraCaptureUI()
        {
            this.StopFlag = false;
            this.InitializeComponent();

            // get current app
            this.app = Application.Current;

            // get current frame
            this.originalFrame = (Frame)Window.Current.Content;

            this.CurrentWindow = Window.Current;
            this.NewCamCapFrame = new Frame();
            this.CurrentWindow.Content = this.NewCamCapFrame;

            // navigate to Capture UI page 
            this.NewCamCapFrame.Navigate(typeof(CameraCaptureUIPage));

            this.Unloaded += this.CameraCaptureUI_Unloaded;
#if WINDOWS_PHONE_APP
            HardwareButtons.BackPressed += this.HardwareButtons_BackPressed;
#endif

            // set references current CCUI page
            this.MyCciPage = ((CameraCaptureUIPage)this.NewCamCapFrame.Content);

            this.MyCciPage.MyCCUCtrl = this;

            this.app.Suspending += this.AppSuspending;
            this.app.Resuming += this.AppResuming;

            // set content
            this.mainGrid = (Grid)(this.MyCciPage).Content;

            // Remove all children, if any exist
            this.mainGridChildren = this.mainGrid.Children;
            foreach (var item in this.mainGridChildren)
            {
                this.mainGrid.Children.Remove(item);
            }

            // Show Ctrl
            this.mainGrid.Children.Add(this);
        }

        public async void AppResuming(object sender, object e)
        {
            // get current frame
            this.NewCamCapFrame = (Frame)Window.Current.Content;

            // make sure you are on CCUIPage
            var ccuipage = this.NewCamCapFrame.Content as CameraCaptureUIPage;
            if (ccuipage != null)
            {
                var ccu = ccuipage.MyCCUCtrl;

                // start captureing again
                await ccu.CaptureFileAsync(CameraCaptureUIMode.Photo, this.options);
            }
            else
            {
                this.app.Resuming -= this.AppResuming;
            }
        }

        public async void AppSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await this.CleanUpAsync();
            deferral.Complete();
        }

        private void CameraCaptureUI_Unloaded(object sender, RoutedEventArgs e)
        {
#if WINDOWS_PHONE_APP
            HardwareButtons.BackPressed -= this.HardwareButtons_BackPressed;
#endif
        }

        private async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            await this.GoBackAsync(e);
        }

        private async Task GoBackAsync(BackPressedEventArgs e)
        {
            await this.CleanUpAsync();

            e.Handled = true;

            this.CurrentWindow.Content = this.originalFrame;
        }

        public async Task CleanUpAsync()
        {
            if (this.myCaptureElement != null)
            {
                this.myCaptureElement.Source = null;
            }

            if (this.MyMediaCapture != null)
            {
                try
                {
                    await this.MyMediaCapture.StopPreviewAsync();
                }
                catch (ObjectDisposedException o)
                {
                    Debug.WriteLine(o.Message);
                }
            }

            if (this.MyMediaCapture != null)
            {
                try
                {
                    this.MyMediaCapture.Dispose();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        private StoreCameraMediaOptions options;

        /// <summary>
        ///     This method takes a picture.
        ///     Right now the parameter is not evaluated.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<StorageFile> CaptureFileAsync(CameraCaptureUIMode mode, StoreCameraMediaOptions options)
        {
            var t = this.IsStopped();
            this.options = options;
            // Create new MediaCapture 
            this.MyMediaCapture = new MediaCapture();
            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var backCamera = videoDevices.FirstOrDefault(item => item.EnclosureLocation != null && item.EnclosureLocation.Panel == Panel.Back);

            var frontCamera = videoDevices.FirstOrDefault(item => item.EnclosureLocation != null && item.EnclosureLocation.Panel == Panel.Front);

            var captureSettings = new MediaCaptureInitializationSettings();
            if (options.DefaultCamera == CameraFacingDirection.Front && frontCamera != null)
            {
                captureSettings.VideoDeviceId = frontCamera.Id;
            }
            else if (options.DefaultCamera == CameraFacingDirection.Rear && backCamera != null)
            {
                captureSettings.VideoDeviceId = backCamera.Id;
            }
            await this.MyMediaCapture.InitializeAsync(captureSettings);

            // Assign to Xaml CaptureElement.Source and start preview
            this.myCaptureElement.Source = this.MyMediaCapture;

            // show preview
            await this.MyMediaCapture.StartPreviewAsync();

            // now wait until stopflag shows that someone took a picture
            await t;

            // picture has been taken
            // stop preview

            await this.CleanUpAsync();

            // go back
            this.CurrentWindow.Content = this.originalFrame;

            this.mainGrid.Children.Remove(this);

            return this.file;
        }

        /// <summary>
        ///     This is a loop which waits async until the flag has been set.
        /// </summary>
        /// <returns></returns>
        private async Task IsStopped()
        {
            while (!this.StopFlag)
            {
                await Task.Delay(WaitForClickLoopLength);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create new file in the pictures library     

            this.file = await ApplicationData.Current.LocalFolder.CreateFileAsync("_____ccuiphoto.jpg", CreationCollisionOption.ReplaceExisting);

            // create a jpeg image
            var imgEncodingProperties = ImageEncodingProperties.CreateJpeg();

            await this.MyMediaCapture.CapturePhotoToStorageFileAsync(imgEncodingProperties, this.file);

            // when pic has been taken, set stopFlag
            this.StopFlag = true;
        }

        public UIElementCollection mainGridChildren { get; set; }

        public bool locker = false;

        public Application app { get; set; }

        public CameraCaptureUIPage MyCciPage { get; set; }

        public Window CurrentWindow { get; set; }

        public Frame NewCamCapFrame { get; set; }
    }
}