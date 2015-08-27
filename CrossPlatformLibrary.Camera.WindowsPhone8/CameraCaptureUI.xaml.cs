using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Windows.Storage;

namespace CrossPlatformLibrary.Camera
{
    public class CameraFrameEventArgs : RoutedEventArgs
    {
        public int[] ARGBData { get; set; }
    }

    /// <summary>
    ///     Shows the camera view and raises events when new camera frames are ready
    ///     Uses code from sample at  http://msdn.microsoft.com/en-us/library/hh202982(v=vs.92).aspx
    /// </summary>
    public partial class CameraCaptureUI : UserControl
    {
        private static ManualResetEvent _cameraCaptureEvent = new ManualResetEvent(true);
        private static ManualResetEvent _cameraInitializedEvent = new ManualResetEvent(false);
        private static ManualResetEvent _pauseFramesEvent = new ManualResetEvent(true);

        private int _cameraHeight = -1;

        private int _cameraWidth = -1;

        private bool _photoOnPress;

        ////private SoundEffect _cameraShutterSound;

        private bool _pumpFrames;

        private Thread _pumpFramesThread;

        public CameraCaptureUI(bool startPumpingFrames = true)
        {
            this.InitializeComponent();
            this.Unloaded += this.OnUnloaded;
            this.PhotoOnPress = true;

            var pageName = typeof(CameraCaptureUIPage).Name;

            this.rootVisual = (PhoneApplicationFrame)Application.Current.RootVisual;
            this.rootVisual.Navigated += this.OriginalFrameNavigated;
            this.rootVisual.Navigate(new Uri(string.Format("/CrossPlatformLibrary.Camera.Platform;component/{0}.xaml", pageName), UriKind.Relative));

            if (startPumpingFrames)
            {
                this.StartPumpingFrames();
            }
        }

        private void OriginalFrameNavigated(object sender, NavigationEventArgs e)
        {
            var content = e.Content as CameraCaptureUIPage;
            if (content != null)
            {
                this.rootVisual.Navigated -= this.OriginalFrameNavigated;

                // set references current CCUI page
                this.MyCciPage = ((CameraCaptureUIPage)e.Content);

                this.MyCciPage.MyCCUCtrl = this;

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
        }

        private Grid mainGrid;
        private readonly PhoneApplicationFrame rootVisual;

        private UIElementCollection mainGridChildren { get; set; }

        private CameraCaptureUIPage MyCciPage { get; set; }

        public EventHandler<CameraOperationCompletedEventArgs> CamInitialized { get; set; }

        public Microsoft.Devices.PhotoCamera Camera { get; private set; }

        /// <summary>
        ///     Gets or sets the camera height.
        /// </summary>
        public int CameraHeight
        {
            get
            {
                return this._cameraHeight;
            }

            set
            {
                this._cameraHeight = value;
            }
        }

        /// <summary>
        ///     Gets or sets the camera width.
        /// </summary>
        public int CameraWidth
        {
            get
            {
                return this._cameraWidth;
            }

            set
            {
                this._cameraWidth = value;
            }
        }

        /// <summary>
        ///     Gets or sets the new camera capture image.
        /// </summary>
        public EventHandler<ContentReadyEventArgs> NewCameraCaptureImage { get; set; }

        /// <summary>
        ///     Gets or sets the new camera frame.
        /// </summary>
        public EventHandler<CameraFrameEventArgs> NewCameraFrame { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether photo on press.
        /// </summary>
        public bool PhotoOnPress
        {
            get
            {
                return this._photoOnPress;
            }

            set
            {
                this._photoOnPress = value;
                CameraButtons.ShutterKeyPressed -= this.CameraButtonsOnShutterKeyPressed;
                CameraButtons.ShutterKeyHalfPressed -= this.CameraButtons_ShutterKeyHalfPressed;
                if (this._photoOnPress)
                {
                    CameraButtons.ShutterKeyPressed += this.CameraButtonsOnShutterKeyPressed;
                    CameraButtons.ShutterKeyHalfPressed += this.CameraButtons_ShutterKeyHalfPressed;
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether save to camera roll.
        /// </summary>
        public bool SaveToCameraRoll { get; set; }

        public bool TakingPhoto { get; private set; }

        public void InitializeCamera()
        {
            _cameraInitializedEvent.Reset();

            // Check to see if the camera is available on the device.
            if (Microsoft.Devices.Camera.IsCameraTypeSupported(CameraType.Primary) || Microsoft.Devices.Camera.IsCameraTypeSupported(CameraType.FrontFacing))
            {
                // Initialize the default camera.
                this.Camera = new Microsoft.Devices.PhotoCamera(CameraType.FrontFacing);

                // Event is fired when the PhotoCamera object has been initialized
                this.Camera.Initialized += this.CameraInitialized;
                this.Camera.CaptureImageAvailable += this.CameraOnCaptureImageAvailable;
                this.Camera.CaptureCompleted += this.CameraOnCaptureCompleted;

                // Adjust mirroring according to camera type
                if (this.Camera.CameraType == CameraType.Primary)
                {
                    this.viewfinderBrush.RelativeTransform = new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = 90 };
                }
                else
                {
                    this.viewfinderBrush.RelativeTransform = new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = 90, ScaleX = -1 };
                }

                // Set the VideoBrush source to the camera
                this.viewfinderBrush = new VideoBrush();
                this.viewfinderBrush.SetSource(this.Camera);
                this.videoRectangle.Fill = this.viewfinderBrush;

                // initialize the shutter sound
                // Audio
                ////Stream stream = TitleContainer.OpenStream("shutter.wav");
                ////_cameraShutterSound = SoundEffect.FromStream(stream);
                CameraButtons.ShutterKeyPressed -= this.CameraButtonsOnShutterKeyPressed;
                CameraButtons.ShutterKeyHalfPressed -= this.CameraButtons_ShutterKeyHalfPressed;
                if (this._photoOnPress)
                {
                    CameraButtons.ShutterKeyPressed += this.CameraButtonsOnShutterKeyPressed;
                    CameraButtons.ShutterKeyHalfPressed += this.CameraButtons_ShutterKeyHalfPressed;
                }
            }
            else
            {
                // The camera is not supported on the device.
                MessageBox.Show("Sorry, this sample requires a phone camera and no camera is detected. This application will not show any camera output.");
            }
        }

        public void StartPumpingFrames()
        {
            _pauseFramesEvent = new ManualResetEvent(true);
            _cameraCaptureEvent = new ManualResetEvent(true);
            _cameraInitializedEvent = new ManualResetEvent(false);

            this.InitializeCamera();

            // if (_pumpFramesThread != null)
            // {
            // if (_pumpFramesThread.IsAlive)
            // _pumpFramesThread.Abort();
            // _pumpFramesThread = null;
            // }
            this._pumpFrames = true;
            if (this._pumpFramesThread == null)
            {
                this._pumpFramesThread = new Thread(this.PumpFrames);
            }

            if (!this._pumpFramesThread.IsAlive)
            {
                this._pumpFramesThread.Start();
            }
        }

        public void StopPumpingFrames()
        {
            this._pumpFrames = false;
            this._pumpFramesThread = null;
        }

        /// <summary>
        ///     The take photo.
        /// </summary>
        public void TakePhoto()
        {
            if (this.TakingPhoto)
            {
                return;
            }

            _cameraCaptureEvent.Reset();
            FrameworkDispatcher.Update();

            ////_cameraShutterSound.Play();
            this.TakingPhoto = true;
            this.Camera.CaptureImage();
        }

        public void UpdateOrientation(PageOrientation orientation)
        {
            if (orientation == PageOrientation.PortraitDown)
            {
                this.viewfinderBrush.RelativeTransform = new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = -90 };
            }
            else if (orientation == PageOrientation.PortraitUp)
            {
                this.viewfinderBrush.RelativeTransform = new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = 90 };
            }
            else
            {
                this.viewfinderBrush.RelativeTransform = new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = 0 };
            }
        }

        private void CameraButtonsOnShutterKeyPressed(object sender, EventArgs eventArgs)
        {
            if (this._photoOnPress && !this.TakingPhoto)
            {
                _cameraCaptureEvent.WaitOne();
                this.TakePhoto();
            }
        }

        private void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (this.Camera.IsFocusSupported)
            {
                this.Camera.Focus();
            }
        }

        private void CameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
            {
                try
                {
                    // available resolutions are ordered based on number of pixels in each resolution
                    this.CameraWidth = (int)this.Camera.PreviewResolution.Width;
                    this.CameraHeight = (int)this.Camera.PreviewResolution.Height;

                    if (this.CamInitialized != null)
                    {
                        this.CamInitialized.Invoke(this, e);
                    }

                    _cameraInitializedEvent.Set();
                    _pauseFramesEvent.Set();
                }
                catch (ObjectDisposedException)
                {
                    // If the camera was disposed, try initializing again
                }
            }
        }

        private void CameraOnCaptureCompleted(object sender, CameraOperationCompletedEventArgs cameraOperationCompletedEventArgs)
        {
            _cameraCaptureEvent.Set();
            this.TakingPhoto = false;
        }

        private void CameraOnCaptureImageAvailable(object sender, ContentReadyEventArgs contentReadyEventArgs)
        {
            if (this.NewCameraCaptureImage != null)
            {
                this.NewCameraCaptureImage.Invoke(this, contentReadyEventArgs);
            }
        }

        private WriteableBitmap CreateWriteableBitmap(Stream imageStream, int width, int height)
        {
            var bitmap = new WriteableBitmap(width, height);

            imageStream.Position = 0;
            bitmap.LoadJpeg(imageStream);
            return bitmap;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _cameraInitializedEvent.Reset();
            CameraButtons.ShutterKeyPressed -= this.CameraButtonsOnShutterKeyPressed;
            CameraButtons.ShutterKeyHalfPressed -= this.CameraButtons_ShutterKeyHalfPressed;
        }

        private void PumpFrames()
        {
            _cameraInitializedEvent.WaitOne();
            var pixels = new int[this.CameraWidth * this.CameraHeight];

            int numExceptions = 0;
            while (this._pumpFrames)
            {
                _pauseFramesEvent.WaitOne();
                _cameraCaptureEvent.WaitOne();
                _cameraInitializedEvent.WaitOne();

                try
                {
                    this.Camera.GetPreviewBufferArgb32(pixels);
                }
                catch (Exception e)
                {
                    // If we get an exception try capturing again, do this up to 10 times
                    if (numExceptions >= 10)
                    {
                        throw e;
                    }

                    numExceptions++;
                    continue;
                }

                numExceptions = 0;

                _pauseFramesEvent.Reset();

                Deployment.Current.Dispatcher.BeginInvoke(
                    () =>
                        {
                            if (this.NewCameraFrame != null && this._pumpFrames)
                            {
                                this.NewCameraFrame(this, new CameraFrameEventArgs { ARGBData = pixels });
                            }

                            _pauseFramesEvent.Set();
                        });
            }
        }

        private async void OnShutterButtonClicked(object sender, RoutedEventArgs e)
        {
            // TODO GATH: Implement the take pciture functionality. Signal the CaptureFileAsync method to continue
        }

        public async Task<StorageFile> CaptureFileAsync(CameraCaptureUIMode mode, StoreCameraMediaOptions options)
        {
            await Task.Delay(1000);

            ////this.StopPumpingFrames(); ???
            return new StorageFile(); // TODO GATH: Implement
        }
    }
}