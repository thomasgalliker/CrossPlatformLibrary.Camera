using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using CrossPlatformLibrary.Camera;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Windows.Phone.Media.Capture;
using Windows.Storage;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using Size = Windows.Foundation.Size;

namespace CrossPlatformLibraryCameraControl // DO NOT RENAME THIS NAMESPACE // THERE IS A PROBLEM WITH XAML IF THE NAMESPACE CONTAINS A DOT!
{
    public partial class ViewFinder : UserControl
    {
        private const short WaitForClickLoopLength = 1000;
        private readonly PhoneApplicationFrame rootVisual;

        private PhotoCaptureDevice captureDevice;
        private double orientationAngle = 0.0;
        private CameraSensorLocation sensorLocation;
        private bool isPreviewRunning = false;
        private bool commandeRunning = false;
        private bool stopFlag;
        private bool enableShutterKey;
        private Grid mainGrid;
        private UIElementCollection mainGridChildren;
        private ViewFinderPage viewFinderPage;

        public ViewFinder() : this(CameraFacingDirection.Rear)
        {
        }

        public ViewFinder(CameraFacingDirection cameraFacingDirection)
        {
            this.InitializeComponent();
            this.stopFlag = false;
            this.SensorLocation = cameraFacingDirection == CameraFacingDirection.Front ? CameraSensorLocation.Front : CameraSensorLocation.Back;
            this.EnableShutterKey = true;
            this.LayoutUpdated += this.OnLayoutUpdated;

            if (DesignerProperties.IsInDesignTool == false)
            {
                this.rootVisual = (PhoneApplicationFrame)Application.Current.RootVisual;
                this.rootVisual.OrientationChanged += this.OnOrientationChanged;

                this.Tap += this.ViewFinderTap;
                this.Loaded += this.OnViewFinderLoaded;
                this.Unloaded += this.OnViewFinderUnloaded;

                // Navigate to ViewFinderPage
                var pageName = typeof(ViewFinderPage).Name;
                this.rootVisual = (PhoneApplicationFrame)Application.Current.RootVisual;
                this.rootVisual.Navigated += this.OnNavigated;
                this.rootVisual.Navigate(new Uri(string.Format("/CrossPlatformLibrary.Camera.Platform;component/{0}.xaml", pageName), UriKind.Relative));
            }
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            var content = e.Content as ViewFinderPage;
            if (content != null)
            {
                this.StartPreview();

                this.rootVisual.Navigated -= this.OnNavigated;

                // set references current CCUI page
                this.viewFinderPage = ((ViewFinderPage)e.Content);

                this.viewFinderPage.viewFinder = this;

                // set content
                this.mainGrid = (Grid)(this.viewFinderPage).Content;

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

        private async void CameraButtonsShutterKeyHalfPressed(object sender, EventArgs e)
        {
            await this.FocusAsync();
        }

        private void CameraButtonsOnShutterKeyPressed(object sender, EventArgs e)
        {
            this.TakePicture();
        }

        public CameraSensorLocation SensorLocation
        {
            get
            {
                return this.sensorLocation;
            }
            set
            {
                if (!this.commandeRunning)
                {
                    this.sensorLocation = value;
                    if (this.isPreviewRunning)
                    {
                        this.InitCamera();
                    }
                }
            }
        }

        public void StartPreview()
        {
            this.isPreviewRunning = true;
            this.InitCamera();
        }

        public void StopPreview()
        {
            this.isPreviewRunning = false;
            if (this.captureDevice != null)
            {
                this.captureDevice.Dispose();
                this.captureDevice = null;
            }
        }

        private async void ViewFinderTap(object sender, GestureEventArgs e)
        {
            if (this.isPreviewRunning && !this.commandeRunning)
            {
                try
                {
                    this.commandeRunning = true;

                    //compute vector between preview picture center and Inverted transformation center
                    var tmp = this.viewfinderBrush.Transform.Inverse.TransformBounds(new Rect(new Point(), this.viewfinderCanvas.RenderSize));
                    var dx = this.captureDevice.PreviewResolution.Width / 2 - (tmp.X + tmp.Width / 2);
                    var dy = this.captureDevice.PreviewResolution.Height / 2 - (tmp.Y + tmp.Height / 2);

                    //invert tap position
                    var p = e.GetPosition(this);
                    var pInPreview = this.viewfinderBrush.Transform.Inverse.Transform(p);

                    //transform inverted position to picture reference
                    double X = pInPreview.X + dx;
                    double Y = pInPreview.Y + dy;

                    if (X < 0)
                    {
                        X = 0;
                    }
                    if (X >= this.captureDevice.PreviewResolution.Width)
                    {
                        X = this.captureDevice.PreviewResolution.Width - 1;
                    }

                    if (Y >= this.captureDevice.PreviewResolution.Height)
                    {
                        Y = this.captureDevice.PreviewResolution.Height - 1;
                    }
                    if (Y < 0)
                    {
                        Y = 0;
                    }

                    this.captureDevice.FocusRegion = new Windows.Foundation.Rect(new Windows.Foundation.Point(X, Y), new Size());
                    await this.captureDevice.FocusAsync();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    this.commandeRunning = false;
                }
            }
        }

        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (this.captureDevice != null)
            {
                this.captureDevice.Dispose();
                this.captureDevice = null;
            }
        }

        private void OnOrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            this.SetPageOrientation(e.Orientation);
        }

        private void SetPageOrientation(PageOrientation orientation)
        {
            if ((orientation & PageOrientation.Portrait) == PageOrientation.Portrait)
            {
                this.orientationAngle = 0;
            }
            else if ((orientation & PageOrientation.LandscapeLeft) == PageOrientation.LandscapeLeft)
            {
                this.orientationAngle = -90;
            }
            else
            {
                this.orientationAngle = +90;
            }

            this.ComputeVideoBruchTransform();

            if (this.captureDevice != null)
            {
                this.captureDevice.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation, this.orientationAngle);
            }
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            if (this.isPreviewRunning)
            {
                this.ComputeVideoBruchTransform();
            }
        }

        private async void InitCamera()
        {
            if (this.commandeRunning)
            {
                return;
            }
            try
            {
                this.commandeRunning = true;
                if (this.captureDevice != null)
                {
                    this.captureDevice.Dispose();
                    this.captureDevice = null;
                }

                var supportedResolutions = PhotoCaptureDevice.GetAvailableCaptureResolutions(this.sensorLocation).ToArray();
                this.captureDevice = await PhotoCaptureDevice.OpenAsync(this.sensorLocation, supportedResolutions[0]);

                this.viewfinderBrush.SetSource(this.captureDevice);

                this.ComputeVideoBruchTransform();
            }

            finally
            {
                this.commandeRunning = false;
            }
        }

        private void ComputeVideoBruchTransform()
        {
            if (this.captureDevice == null)
            {
                return;
            }

            var tmptransform = new RotateTransform { Angle = this.orientationAngle + this.captureDevice.SensorRotationInDegrees };
            var previewSize = tmptransform.TransformBounds(new Rect(new Point(), new System.Windows.Size(this.captureDevice.PreviewResolution.Width, this.captureDevice.PreviewResolution.Height)));

            double s1 = this.viewfinderCanvas.ActualWidth / previewSize.Width;
            double s2 = this.viewfinderCanvas.ActualHeight / previewSize.Height;

            double scale = Math.Max(s1, s2);

            var t = new TransformGroup();
            if (this.sensorLocation == CameraSensorLocation.Front)
            {
                t.Children.Add(new CompositeTransform
                    {
                        Rotation = -(this.orientationAngle + this.captureDevice.SensorRotationInDegrees),
                        CenterX = this.viewfinderCanvas.ActualWidth / 2,
                        CenterY = this.viewfinderCanvas.ActualHeight / 2,
                        ScaleX = scale,
                        ScaleY = scale
                    });

                t.Children.Add(new ScaleTransform
                    {
                        ScaleX = -1,
                        CenterX = this.viewfinderCanvas.ActualWidth / 2,
                        CenterY = this.viewfinderCanvas.ActualHeight / 2
                    });
            }
            else
            {
                t.Children.Add(
                    new CompositeTransform
                    {
                        Rotation = this.orientationAngle + this.captureDevice.SensorRotationInDegrees,
                        CenterX = this.viewfinderCanvas.ActualWidth / 2,
                        CenterY = this.viewfinderCanvas.ActualHeight / 2,
                        ScaleX = scale,
                        ScaleY = scale
                    });
            }

            this.viewfinderBrush.Transform = t;
        }

        public async Task<CameraFocusStatus> FocusAsync()
        {
            if (this.commandeRunning)
            {
                return CameraFocusStatus.NotLocked;
            }
            try
            {
                this.commandeRunning = true;
                if (this.captureDevice != null && this.sensorLocation == CameraSensorLocation.Back)
                {
                    return await this.captureDevice.FocusAsync();
                }
            }
            finally
            {
                this.commandeRunning = false;
            }
            return CameraFocusStatus.NotLocked;
        }

        private async Task IsStopped()
        {
            while (!this.stopFlag)
            {
                await Task.Delay(WaitForClickLoopLength);
            }
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

        internal void TakePicture()
        {
            this.stopFlag = true;
        }

        public async Task<StorageFile> CaptureFileAsync()
        {
            var takePictureTrigger = this.IsStopped();
            await takePictureTrigger;

            if (this.captureDevice == null || this.commandeRunning)
            {
                return null;
            }

            StorageFile file = null;
            try
            {
                this.commandeRunning = true;
                int angle = (int)(this.orientationAngle + this.captureDevice.SensorRotationInDegrees);

                if (this.sensorLocation == CameraSensorLocation.Front)
                {
                    angle = -angle;
                }

                this.captureDevice.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation, angle);
                this.captureDevice.SetProperty(KnownCameraGeneralProperties.SpecifiedCaptureOrientation, 0);

                var cameraCaptureSequence = this.captureDevice.CreateCaptureSequence(1);
                var stream = new MemoryStream();
                cameraCaptureSequence.Frames[0].CaptureStream = stream.AsOutputStream();
                await this.captureDevice.PrepareCaptureSequenceAsync(cameraCaptureSequence);
                await cameraCaptureSequence.StartCaptureAsync();

                var buffer = stream.GetWindowsRuntimeBuffer();
                file = await SaveToLocalFolderAsync(buffer.AsStream(), "_____ccuiphoto.jpg");
            }
            finally
            {
                this.commandeRunning = false;
            }

            this.StopPreview();
            this.rootVisual.GoBack();

            return file;
        }

        private void OnViewFinderLoaded(object sender, RoutedEventArgs e)
        {
            this.SetPageOrientation(this.rootVisual.Orientation);
        }

        private void OnViewFinderUnloaded(object sender, RoutedEventArgs e)
        {
            this.StopPreview();
            this.EnableShutterKey = false;
        }
    }
}