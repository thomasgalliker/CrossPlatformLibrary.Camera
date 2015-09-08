using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

using Camera2Basic;

using CrossPlatformLibrary.Tracing;

using Java.IO;
using Java.Lang;
using Java.Nio;

using Debug = System.Diagnostics.Debug;
using Math = System.Math;
using Object = Java.Lang.Object;
using Orientation = Android.Content.Res.Orientation;
using Uri = Android.Net.Uri;

namespace CrossPlatformLibrary.Camera.ViewFinder
{
    [Preserve(AllMembers = true)]
    public class Camera2BasicFragment : Fragment, View.IOnClickListener
    {
        internal const string ExtraPath = "path";
        internal const string ExtraLocation = "location";
        internal const string ExtraType = "type";
        internal const string ExtraId = "id";

        private static readonly SparseIntArray Orientations = new SparseIntArray();
        // An AutoFitTextureView for camera preview
        private AutoFitTextureView autoFitTextureView;

        // A CameraRequest.Builder for camera preview
        private CaptureRequest.Builder previewBuilder;

        // A CameraCaptureSession for camera preview
        private CameraCaptureSession previewSession;

        // A reference to the opened CameraDevice
        private CameraDevice cameraDevice;

        // TextureView.ISurfaceTextureListener handles several lifecycle events on a TextureView
        private Camera2BasicSurfaceTextureListener mSurfaceTextureListener;

        internal static event EventHandler<MediaPickedEventArgs> MediaPicked;

        private class Camera2BasicSurfaceTextureListener : Object, TextureView.ISurfaceTextureListener
        {
            private readonly Camera2BasicFragment fragment;

            public Camera2BasicSurfaceTextureListener(Camera2BasicFragment fragment)
            {
                this.fragment = fragment;
            }

            public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
            {
                this.fragment.ConfigureTransform(width, height);
                this.fragment.StartPreview();
            }

            public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
            {
                return true;
            }

            public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
            {
                this.fragment.ConfigureTransform(width, height);
                this.fragment.StartPreview();
            }

            public void OnSurfaceTextureUpdated(SurfaceTexture surface)
            {
            }
        }

        // The size of the camera preview
        private Size previewSize;

        // True if the app is currently trying to open the camera
        private bool mOpeningCamera;

        // CameraDevice.StateListener is called when a CameraDevice changes its state
        private CameraStateListener mStateListener;

        private class CameraStateListener : CameraDevice.StateCallback
        {
            public Camera2BasicFragment Fragment;

            public override void OnOpened(CameraDevice camera)
            {
                if (this.Fragment != null)
                {
                    this.Fragment.cameraDevice = camera;
                    this.Fragment.StartPreview();
                    this.Fragment.mOpeningCamera = false;
                }
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                if (this.Fragment != null)
                {
                    camera.Close();
                    this.Fragment.cameraDevice = null;
                    this.Fragment.mOpeningCamera = false;
                }
            }

            public override void OnError(CameraDevice camera, CameraError error)
            {
                camera.Close();
                if (this.Fragment != null)
                {
                    this.Fragment.cameraDevice = null;
                    Activity activity = this.Fragment.Activity;
                    this.Fragment.mOpeningCamera = false;
                    if (activity != null)
                    {
                        activity.Finish();
                    }
                }
            }
        }

        private class ImageAvailableListener : Object, ImageReader.IOnImageAvailableListener
        {
            public File File;

            public void OnImageAvailable(ImageReader reader)
            {
                Image image = null;
                try
                {
                    image = reader.AcquireLatestImage();
                    ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                    byte[] bytes = new byte[buffer.Capacity()];
                    buffer.Get(bytes);
                    this.Save(bytes);
                }
                catch (FileNotFoundException ex)
                {
                    Log.WriteLine(LogPriority.Info, "Camera capture session", ex.StackTrace);
                }
                catch (IOException ex)
                {
                    Log.WriteLine(LogPriority.Info, "Camera capture session", ex.StackTrace);
                }
                finally
                {
                    if (image != null)
                    {
                        image.Close();
                    }
                }
            }

            private void Save(byte[] bytes)
            {
                OutputStream output = null;
                try
                {
                    if (this.File != null)
                    {
                        output = new FileOutputStream(this.File);
                        output.Write(bytes);
                    }
                }
                finally
                {
                    if (output != null)
                    {
                        output.Close();
                    }
                }
            }
        }

        private class CameraCaptureListener : CameraCaptureSession.CaptureCallback
        {
            public Camera2BasicFragment Fragment;
            public File File;

            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                if (this.Fragment != null && this.File != null)
                {
                    Activity activity = this.Fragment.Activity;
                    if (activity != null)
                    {
                        var newMediaFile = new MediaFile(this.File.AbsolutePath, () => { return System.IO.File.OpenRead(this.File.AbsolutePath); });
                        this.Fragment.OnMediaPicked(new MediaPickedEventArgs(0, false, newMediaFile));

                        this.Fragment.StartPreview();
                    }
                }
            }
        }

        private void OnMediaPicked(MediaPickedEventArgs e)
        {
            var picked = MediaPicked;
            if (picked != null)
            {
                picked(null, e);
            }
        }

        // This CameraCaptureSession.StateListener uses Action delegates to allow the methods to be defined inline, as they are defined more than once
        private class CameraCaptureStateListener : CameraCaptureSession.StateCallback
        {
            public Action<CameraCaptureSession> OnConfigureFailedAction;

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                if (this.OnConfigureFailedAction != null)
                {
                    this.OnConfigureFailedAction(session);
                }
            }

            public Action<CameraCaptureSession> OnConfiguredAction;

            public override void OnConfigured(CameraCaptureSession session)
            {
                if (this.OnConfiguredAction != null)
                {
                    this.OnConfiguredAction(session);
                }
            }
        }

        private Uri path;
        private ITracer tracer = Tracer.Create<Camera2BasicFragment>();

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Bundle b = (savedInstanceState ?? this.Activity.Intent.Extras);
            this.path = Uri.Parse(b.GetString(ExtraPath));

            this.mStateListener = new CameraStateListener { Fragment = this };
            this.mSurfaceTextureListener = new Camera2BasicSurfaceTextureListener(this);
            Orientations.Append((int)SurfaceOrientation.Rotation0, 90);
            Orientations.Append((int)SurfaceOrientation.Rotation90, 0);
            Orientations.Append((int)SurfaceOrientation.Rotation180, 270);
            Orientations.Append((int)SurfaceOrientation.Rotation270, 180);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            if (this.path != null)
            {
                outState.PutString(ExtraPath, this.path.Path);
            }

            base.OnSaveInstanceState(outState);
        }

        public static Camera2BasicFragment NewInstance()
        {
            var fragment = new Camera2BasicFragment();
            fragment.RetainInstance = true;
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_camera2_basic, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            this.autoFitTextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            this.autoFitTextureView.SurfaceTextureListener = this.mSurfaceTextureListener;
            view.FindViewById(Resource.Id.picture).SetOnClickListener(this);
        }

        public override void OnResume()
        {
            base.OnResume();
            this.OpenCamera();
        }

        public override void OnPause()
        {
            base.OnPause();
            if (this.cameraDevice != null)
            {
                this.cameraDevice.Close();
                this.cameraDevice = null;
            }
        }

        // Opens a CameraDevice. The result is listened to by 'mStateListener'.
        private void OpenCamera()
        {
            var activity = this.Activity;
            if (activity == null || activity.IsFinishing || this.mOpeningCamera)
            {
                return;
            }
            this.mOpeningCamera = true;
            CameraManager cameraManager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                string cameraId = cameraManager.GetCameraIdList()[0];

                // To get a list of available sizes of camera preview, we retrieve an instance of
                // StreamConfigurationMap from CameraCharacteristics
                CameraCharacteristics characteristics = cameraManager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                this.previewSize = map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[0];
                Orientation orientation = this.Resources.Configuration.Orientation;
                if (orientation == Orientation.Landscape)
                {
                    this.autoFitTextureView.SetAspectRatio(this.previewSize.Width, this.previewSize.Height);
                }
                else
                {
                    this.autoFitTextureView.SetAspectRatio(this.previewSize.Height, this.previewSize.Width);
                }

                // We are opening the camera with a listener. When it is ready, OnOpened of mStateListener is called.
                cameraManager.OpenCamera(cameraId, this.mStateListener, null);
            }
            catch (CameraAccessException caex)
            {
                this.tracer.Exception(caex, "Cannot access the camera.");
                Toast.MakeText(activity, "Cannot access the camera.", ToastLength.Short).Show();
                this.Activity.Finish();
            }
            catch (NullPointerException npex)
            {
                this.tracer.Exception(npex, "This device doesn't support Camera2 API.");

                var dialog = new ErrorDialog();
                dialog.Show(this.FragmentManager, "dialog");
            }
        }

        private void StartPreview()
        {
            if (this.cameraDevice == null || !this.autoFitTextureView.IsAvailable || this.previewSize == null)
            {
                return;
            }
            try
            {
                SurfaceTexture texture = this.autoFitTextureView.SurfaceTexture;
                Debug.Assert(texture != null);

                // We configure the size of the default buffer to be the size of the camera preview we want
                texture.SetDefaultBufferSize(this.previewSize.Width, this.previewSize.Height);

                // This is the output Surface we need to start the preview
                Surface surface = new Surface(texture);

                // We set up a CaptureRequest.Builder with the output Surface
                this.previewBuilder = this.cameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                this.previewBuilder.AddTarget(surface);

                // Here, we create a CameraCaptureSession for camera preview.
                this.cameraDevice.CreateCaptureSession(
                    new List<Surface> { surface },
                    new CameraCaptureStateListener {
                            OnConfigureFailedAction = (CameraCaptureSession session) =>
                                {
                                    Activity activity = this.Activity;
                                    if (activity != null)
                                    {
                                        Toast.MakeText(activity, "Failed", ToastLength.Short).Show();
                                    }
                                },
                            OnConfiguredAction = (CameraCaptureSession session) =>
                                {
                                    this.previewSession = session;
                                    this.UpdatePreview();
                                }
                        },
                    null);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Info, "Camera2BasicFragment", ex.StackTrace);
            }
        }

        /// <summary>
        ///     Updates the camera preview, StartPreview() needs to be called in advance
        /// </summary>
        private void UpdatePreview()
        {
            if (this.cameraDevice == null)
            {
                return;
            }

            try
            {
                // The camera preview can be run in a background thread. This is a Handler for the camere preview
                this.SetUpCaptureRequestBuilder(this.previewBuilder);
                HandlerThread thread = new HandlerThread("CameraPreview");
                thread.Start();
                Handler backgroundHandler = new Handler(thread.Looper);

                // Finally, we start displaying the camera preview
                this.previewSession.SetRepeatingRequest(this.previewBuilder.Build(), null, backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Info, "Camera2BasicFragment", ex.StackTrace);
            }
        }

        /// <summary>
        ///     Sets up capture request builder.
        /// </summary>
        /// <param name="builder">Builder.</param>
        private void SetUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        {
            // In this sample, we just let the camera device pick the automatic settings
            builder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));
        }

        /// <summary>
        ///     Configures the necessary transformation to autoFitTextureView.
        ///     This method should be called after the camera preciew size is determined in openCamera, and also the size of
        ///     autoFitTextureView is fixed
        /// </summary>
        /// <param name="viewWidth">The width of autoFitTextureView</param>
        /// <param name="viewHeight">VThe height of autoFitTextureView</param>
        private void ConfigureTransform(int viewWidth, int viewHeight)
        {
            Activity activity = this.Activity;
            if (this.autoFitTextureView == null || this.previewSize == null || activity == null)
            {
                this.tracer.Debug("ConfigureTransform: Could not perform transformation.");
                return;
            }

            SurfaceOrientation rotation = activity.WindowManager.DefaultDisplay.Rotation;
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, this.previewSize.Width, this.previewSize.Height);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            if (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = Math.Max((float)viewHeight / this.previewSize.Height, (float)viewWidth / this.previewSize.Width);
                this.tracer.Debug("ConfigureTransform: scale={0}", scale);
                this.tracer.Debug("ConfigureTransform: centerX={0}", centerX);
                this.tracer.Debug("ConfigureTransform: centerY={0}", centerY);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
            }
            this.autoFitTextureView.SetTransform(matrix);
        }

        private void TakePicture()
        {
            try
            {
                Activity activity = this.Activity;
                if (activity == null || this.cameraDevice == null)
                {
                    return;
                }

                var cameraManager = (CameraManager)activity.GetSystemService(Context.CameraService);

                // Pick the best JPEG size that can be captures with this CameraDevice
                CameraCharacteristics characteristics = cameraManager.GetCameraCharacteristics(this.cameraDevice.Id);
                Size[] jpegSizes = null;
                if (characteristics != null)
                {
                    jpegSizes = ((StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.Jpeg);
                }

                int width = 640;
                int height = 480;
                if (jpegSizes != null && jpegSizes.Length > 0)
                {
                    width = jpegSizes[0].Width;
                    height = jpegSizes[0].Height;
                }

                // We use an ImageReader to get a JPEG from CameraDevice
                // Here, we create a new ImageReader and prepare its Surface as an output from the camera
                ImageReader reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
                List<Surface> outputSurfaces = new List<Surface>(2);
                outputSurfaces.Add(reader.Surface);
                outputSurfaces.Add(new Surface(this.autoFitTextureView.SurfaceTexture));

                CaptureRequest.Builder captureBuilder = this.cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(reader.Surface);
                this.SetUpCaptureRequestBuilder(captureBuilder);
                // Orientation
                SurfaceOrientation rotation = activity.WindowManager.DefaultDisplay.Rotation;
                captureBuilder.Set(CaptureRequest.JpegOrientation, new Integer(Orientations.Get((int)rotation)));

                // Output file
                
                File file = new File(activity.GetExternalFilesDir(null), "DEMO_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg");

                // This listener is called when an image is ready in ImageReader 
                var readerListener = new ImageAvailableListener
                {
                    File = file
                };

                // We create a Handler since we want to handle the resulting JPEG in a background thread
                HandlerThread thread = new HandlerThread("CameraPicture");
                thread.Start();
                Handler backgroundHandler = new Handler(thread.Looper);
                reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

                //This listener is called when the capture is completed
                // Note that the JPEG data is not available in this listener, but in the ImageAvailableListener we created above
                var captureListener = new CameraCaptureListener
                {
                    Fragment = this, 
                    File = file
                };

                this.cameraDevice.CreateCaptureSession(
                    outputSurfaces,
                    new CameraCaptureStateListener {
                            OnConfiguredAction = (CameraCaptureSession session) =>
                                {
                                    try
                                    {
                                        session.Capture(captureBuilder.Build(), captureListener, backgroundHandler);
                                    }
                                    catch (CameraAccessException ex)
                                    {
                                        Log.WriteLine(LogPriority.Info, "Capture Session error: ", ex.ToString());
                                    }
                                }
                        },
                    backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Info, "Taking picture error: ", ex.StackTrace);
            }
        }

        public void OnClick(View v)
        {
            ////switch (v.Id)
            ////{
            ////    case Resource.Id.picture:
            this.TakePicture();
            ////        break;
            ////}
        }

        public class ErrorDialog : DialogFragment
        {
            public override Dialog OnCreateDialog(Bundle savedInstanceState)
            {
                var alert = new AlertDialog.Builder(this.Activity);
                alert.SetMessage("This device doesn't support Camera2 API.");
                alert.SetPositiveButton(Android.Resource.String.Ok, new MyDialogOnClickListener(this));
                return alert.Show();
            }
        }

        private class MyDialogOnClickListener : Object, IDialogInterfaceOnClickListener
        {
            private readonly ErrorDialog er;

            public MyDialogOnClickListener(ErrorDialog e)
            {
                this.er = e;
            }

            public void OnClick(IDialogInterface dialogInterface, int i)
            {
                this.er.Activity.Finish();
            }
        }
    }
}