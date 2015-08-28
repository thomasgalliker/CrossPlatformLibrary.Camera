using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private static ManualResetEvent cameraCaptureEvent = new ManualResetEvent(true);
        private static ManualResetEvent cameraInitializedEvent = new ManualResetEvent(false);
        private static ManualResetEvent pauseFramesEvent = new ManualResetEvent(true);

        private int cameraHeight = -1;
        private int cameraWidth = -1;
        private bool enableShutterKey = true;
        ////private SoundEffect _cameraShutterSound;
        private bool pumpFrames;
        private Thread pumpFramesThread;

        private Grid mainGrid;
        private readonly PhoneApplicationFrame rootVisual;
        private readonly CameraFacingDirection cameraFacingDirection;
        private UIElementCollection mainGridChildren;

        public CameraCaptureUI(CameraFacingDirection cameraFacingDirection, bool startPumpingFrames = true)
        {
            this.StopFlag = false;
            this.InitializeComponent();

            this.cameraFacingDirection = cameraFacingDirection == CameraFacingDirection.Undefined ? CameraFacingDirection.Front : cameraFacingDirection;
            
            this.Unloaded += this.OnUnloaded;

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

        private CameraCaptureUIPage MyCciPage { get; set; }

        private Microsoft.Devices.PhotoCamera camera;

        public int CameraHeight
        {
            get
            {
                return this.cameraHeight;
            }

            set
            {
                this.cameraHeight = value;
            }
        }

        public int CameraWidth
        {
            get
            {
                return this.cameraWidth;
            }

            set
            {
                this.cameraWidth = value;
            }
        }

        public EventHandler<CameraOperationCompletedEventArgs> CamInitialized { get; set; }

        public EventHandler<ContentReadyEventArgs> NewCameraCaptureImage { get; set; }

        public EventHandler<CameraFrameEventArgs> NewCameraFrame { get; set; }

        public bool EnableShutterKey
        {
            get
            {
                return this.enableShutterKey;
            }

            set
            {
                this.enableShutterKey = value;
                CameraButtons.ShutterKeyPressed -= this.CameraButtonsOnShutterKeyPressed;
                CameraButtons.ShutterKeyHalfPressed -= this.CameraButtonsShutterKeyHalfPressed;
                if (this.enableShutterKey)
                {
                    CameraButtons.ShutterKeyPressed += this.CameraButtonsOnShutterKeyPressed;
                    CameraButtons.ShutterKeyHalfPressed += this.CameraButtonsShutterKeyHalfPressed;
                }
            }
        }

        public bool TakingPhoto { get; private set; }

        private void InitializeCamera()
        {
            cameraInitializedEvent.Reset();

            // Initialize the default camera.
            this.camera = new Microsoft.Devices.PhotoCamera(this.cameraFacingDirection.ToCameraType());

            // Event is fired when the PhotoCamera object has been initialized
            this.camera.Initialized += this.CameraInitialized;
            this.camera.CaptureImageAvailable += this.CameraOnCaptureImageAvailable;
            this.camera.CaptureCompleted += this.CameraOnCaptureCompleted;

            // Adjust mirroring according to camera type
            if (this.camera.CameraType == CameraType.Primary)
            {
                this.viewfinderBrush.RelativeTransform = new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = 90 };
            }
            else
            {
                this.viewfinderBrush.RelativeTransform = new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = 90, ScaleX = -1 };
            }

            // Set the VideoBrush source to the camera
            this.viewfinderBrush = new VideoBrush();
            this.viewfinderBrush.SetSource(this.camera);
            this.videoRectangle.Fill = this.viewfinderBrush;

            // initialize the shutter sound
            // Audio
            ////Stream stream = TitleContainer.OpenStream("shutter.wav");
            ////_cameraShutterSound = SoundEffect.FromStream(stream);

            this.EnableShutterKey = true;
        }

        public void StartPumpingFrames()
        {
            pauseFramesEvent = new ManualResetEvent(true);
            cameraCaptureEvent = new ManualResetEvent(true);
            cameraInitializedEvent = new ManualResetEvent(false);

            this.InitializeCamera();

            // if (pumpFramesThread != null)
            // {
            // if (pumpFramesThread.IsAlive)
            // pumpFramesThread.Abort();
            // pumpFramesThread = null;
            // }
            this.pumpFrames = true;
            if (this.pumpFramesThread == null)
            {
                this.pumpFramesThread = new Thread(this.PumpFrames);
            }

            if (!this.pumpFramesThread.IsAlive)
            {
                this.pumpFramesThread.Start();
            }
        }

        private void PumpFrames()
        {
            cameraInitializedEvent.WaitOne();
            var pixels = new int[this.CameraWidth * this.CameraHeight];

            int numExceptions = 0;
            while (this.pumpFrames)
            {
                pauseFramesEvent.WaitOne();
                cameraCaptureEvent.WaitOne();
                cameraInitializedEvent.WaitOne();

                try
                {
                    this.camera.GetPreviewBufferArgb32(pixels);
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

                pauseFramesEvent.Reset();

                Deployment.Current.Dispatcher.BeginInvoke(
                    () =>
                    {
                        if (this.NewCameraFrame != null && this.pumpFrames)
                        {
                            this.NewCameraFrame(this, new CameraFrameEventArgs { ARGBData = pixels });
                        }

                        pauseFramesEvent.Set();
                    });
            }
        }

        public void StopPumpingFrames()
        {
            this.pumpFrames = false;
            this.pumpFramesThread = null;
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

        /// <summary>
        ///     This is a loop which waits async until the flag has been set.
        /// </summary>
        private async Task IsStopped()
        {
            while (!this.StopFlag)
            {
                await Task.Delay(WaitForClickLoopLength);
            }
        }

        private const short WaitForClickLoopLength = 1000;

        private bool StopFlag;
        private StorageFile file;

        public async Task<StorageFile> CaptureFileAsync()
        {
            var t = this.IsStopped();

            await t;

            // Cleanup
            this.StopPumpingFrames();
            this.rootVisual.GoBack();

            return this.file;
        }

        private void OnShutterButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!this.TakingPhoto)
            {
                cameraInitializedEvent.WaitOne();
                cameraCaptureEvent.WaitOne();
                this.CaptureImageFromCamera();
            }
        }

        private void CameraButtonsOnShutterKeyPressed(object sender, EventArgs eventArgs)
        {
            if (this.enableShutterKey && !this.TakingPhoto)
            {
                cameraInitializedEvent.WaitOne();
                cameraCaptureEvent.WaitOne();
                this.CaptureImageFromCamera();
            }
        }

        private void CaptureImageFromCamera()
        {
            if (this.TakingPhoto)
            {
                return;
            }

            cameraCaptureEvent.Reset();
            FrameworkDispatcher.Update();

            ////_cameraShutterSound.Play();
            this.TakingPhoto = true;
            this.camera.CaptureImage();
        }

        private void CameraButtonsShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (this.camera.IsFocusSupported)
            {
                this.camera.Focus();
            }
        }

        private void CameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
            {
                try
                {
                    // available resolutions are ordered based on number of pixels in each resolution
                    this.CameraWidth = (int)this.camera.PreviewResolution.Width;
                    this.CameraHeight = (int)this.camera.PreviewResolution.Height;

                    if (this.CamInitialized != null)
                    {
                        this.CamInitialized.Invoke(this, e);
                    }

                    cameraInitializedEvent.Set();
                    pauseFramesEvent.Set();
                }
                catch (ObjectDisposedException)
                {
                    // If the camera was disposed, try initializing again
                }
            }
        }

        private void CameraOnCaptureCompleted(object sender, CameraOperationCompletedEventArgs cameraOperationCompletedEventArgs)
        {
            cameraCaptureEvent.Set();
            this.TakingPhoto = false;
        }

        private static async Task<StorageFile> SaveToLocalFolderAsync(Stream stream, string fileName)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile storageFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (Stream outputStream = await storageFile.OpenStreamForWriteAsync())
            {
                await stream.CopyToAsync(outputStream);
            }

            return storageFile;
        }

        private void CameraOnCaptureImageAvailable(object sender, ContentReadyEventArgs contentReadyEventArgs)
        {
            if (this.NewCameraCaptureImage != null)
            {
                this.NewCameraCaptureImage.Invoke(this, contentReadyEventArgs);
            }

            Task.Factory.StartNew(
                async () =>
                    {
                        this.file = await SaveToLocalFolderAsync(contentReadyEventArgs.ImageStream, "_____ccuiphoto.jpg");
                        this.StopFlag = true;
                    });
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            cameraInitializedEvent.Reset();
            CameraButtons.ShutterKeyPressed -= this.CameraButtonsOnShutterKeyPressed;
            CameraButtons.ShutterKeyHalfPressed -= this.CameraButtonsShutterKeyHalfPressed;
        }
    }
}