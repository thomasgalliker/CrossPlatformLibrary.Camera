using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

using CrossPlatformLibrary.Camera;

using Java.IO;
using Java.Lang;
using Java.Nio;

using Debug = System.Diagnostics.Debug;
using Math = System.Math;
using Object = Java.Lang.Object;
using Orientation = Android.Content.Res.Orientation;

namespace Camera2Basic
{
    public class Camera2BasicFragment : Fragment, View.IOnClickListener
    {
        internal const string ExtraPath = "path";
        internal const string ExtraLocation = "location";
        internal const string ExtraType = "type";
        internal const string ExtraId = "id";

        private static readonly SparseIntArray ORIENTATIONS = new SparseIntArray();
        // An AutoFitTextureView for camera preview
        private AutoFitTextureView mTextureView;

        // A CameraRequest.Builder for camera preview
        private CaptureRequest.Builder mPreviewBuilder;

        // A CameraCaptureSession for camera preview
        private CameraCaptureSession mPreviewSession;

        // A reference to the opened CameraDevice
        private CameraDevice mCameraDevice;

        // TextureView.ISurfaceTextureListener handles several lifecycle events on a TextureView
        private Camera2BasicSurfaceTextureListener mSurfaceTextureListener;

        internal static event EventHandler<MediaPickedEventArgs> MediaPicked;

        internal void OnMediaPicked(MediaPickedEventArgs e)
        {
            var picked = MediaPicked;
            if (picked != null)
            {
                picked(null, e);
            }
        }

        private class Camera2BasicSurfaceTextureListener : Object, TextureView.ISurfaceTextureListener
        {
            private readonly Camera2BasicFragment Fragment;

            public Camera2BasicSurfaceTextureListener(Camera2BasicFragment fragment)
            {
                this.Fragment = fragment;
            }

            public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
            {
                this.Fragment.ConfigureTransform(width, height);
                this.Fragment.StartPreview();
            }

            public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
            {
                return true;
            }

            public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
            {
                this.Fragment.ConfigureTransform(width, height);
                this.Fragment.StartPreview();
            }

            public void OnSurfaceTextureUpdated(SurfaceTexture surface)
            {
            }
        }

        // The size of the camera preview
        private Size mPreviewSize;

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
                    this.Fragment.mCameraDevice = camera;
                    this.Fragment.StartPreview();
                    this.Fragment.mOpeningCamera = false;
                }
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                if (this.Fragment != null)
                {
                    camera.Close();
                    this.Fragment.mCameraDevice = null;
                    this.Fragment.mOpeningCamera = false;
                }
            }

            public override void OnError(CameraDevice camera, CameraError error)
            {
                camera.Close();
                if (this.Fragment != null)
                {
                    this.Fragment.mCameraDevice = null;
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
                        Toast.MakeText(activity, "Saved: " + this.File.ToString(), ToastLength.Short).Show();
                        this.Fragment.OnMediaPicked(new MediaPickedEventArgs(0, false, new MediaFile(
                            this.File.AbsolutePath,
                            () => { return System.IO.File.OpenRead(this.File.AbsolutePath); }, false)));

                        this.Fragment.StartPreview();
                    }
                }
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

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            this.mStateListener = new CameraStateListener() { Fragment = this };
            this.mSurfaceTextureListener = new Camera2BasicSurfaceTextureListener(this);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);
        }

        public static Camera2BasicFragment NewInstance()
        {
            Camera2BasicFragment fragment = new Camera2BasicFragment();
            fragment.RetainInstance = true;
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_camera2_basic, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            this.mTextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            this.mTextureView.SurfaceTextureListener = this.mSurfaceTextureListener;
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
            if (this.mCameraDevice != null)
            {
                this.mCameraDevice.Close();
                this.mCameraDevice = null;
            }
        }

        // Opens a CameraDevice. The result is listened to by 'mStateListener'.
        private void OpenCamera()
        {
            Activity activity = this.Activity;
            if (activity == null || activity.IsFinishing || this.mOpeningCamera)
            {
                return;
            }
            this.mOpeningCamera = true;
            CameraManager manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                string cameraId = manager.GetCameraIdList()[0];

                // To get a list of available sizes of camera preview, we retrieve an instance of
                // StreamConfigurationMap from CameraCharacteristics
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                this.mPreviewSize = map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[0];
                Orientation orientation = this.Resources.Configuration.Orientation;
                if (orientation == Orientation.Landscape)
                {
                    this.mTextureView.SetAspectRatio(this.mPreviewSize.Width, this.mPreviewSize.Height);
                }
                else
                {
                    this.mTextureView.SetAspectRatio(this.mPreviewSize.Height, this.mPreviewSize.Width);
                }

                // We are opening the camera with a listener. When it is ready, OnOpened of mStateListener is called.
                manager.OpenCamera(cameraId, this.mStateListener, null);
            }
            catch (CameraAccessException ex)
            {
                Toast.MakeText(activity, "Cannot access the camera.", ToastLength.Short).Show();
                this.Activity.Finish();
            }
            catch (NullPointerException)
            {
                var dialog = new ErrorDialog();
                dialog.Show(this.FragmentManager, "dialog");
            }
        }

        /// <summary>
        ///     Starts the camera previe
        /// </summary>
        private void StartPreview()
        {
            if (this.mCameraDevice == null || !this.mTextureView.IsAvailable || this.mPreviewSize == null)
            {
                return;
            }
            try
            {
                SurfaceTexture texture = this.mTextureView.SurfaceTexture;
                Debug.Assert(texture != null);

                // We configure the size of the default buffer to be the size of the camera preview we want
                texture.SetDefaultBufferSize(this.mPreviewSize.Width, this.mPreviewSize.Height);

                // This is the output Surface we need to start the preview
                Surface surface = new Surface(texture);

                // We set up a CaptureRequest.Builder with the output Surface
                this.mPreviewBuilder = this.mCameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                this.mPreviewBuilder.AddTarget(surface);

                // Here, we create a CameraCaptureSession for camera preview.
                this.mCameraDevice.CreateCaptureSession(
                    new List<Surface>() { surface },
                    new CameraCaptureStateListener()
                        {
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
                                    this.mPreviewSession = session;
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
            if (this.mCameraDevice == null)
            {
                return;
            }

            try
            {
                // The camera preview can be run in a background thread. This is a Handler for the camere preview
                this.SetUpCaptureRequestBuilder(this.mPreviewBuilder);
                HandlerThread thread = new HandlerThread("CameraPreview");
                thread.Start();
                Handler backgroundHandler = new Handler(thread.Looper);

                // Finally, we start displaying the camera preview
                this.mPreviewSession.SetRepeatingRequest(this.mPreviewBuilder.Build(), null, backgroundHandler);
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
            // In this sample, w just let the camera device pick the automatic settings
            builder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));
        }

        /// <summary>
        ///     Configures the necessary transformation to mTextureView.
        ///     This method should be called after the camera preciew size is determined in openCamera, and also the size of
        ///     mTextureView is fixed
        /// </summary>
        /// <param name="viewWidth">The width of mTextureView</param>
        /// <param name="viewHeight">VThe height of mTextureView</param>
        private void ConfigureTransform(int viewWidth, int viewHeight)
        {
            Activity activity = this.Activity;
            if (this.mTextureView == null || this.mPreviewSize == null || activity == null)
            {
                return;
            }

            SurfaceOrientation rotation = activity.WindowManager.DefaultDisplay.Rotation;
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, this.mPreviewSize.Width, this.mPreviewSize.Height);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            if (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = Math.Max((float)viewHeight / this.mPreviewSize.Height, (float)viewWidth / this.mPreviewSize.Width);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
            }
            this.mTextureView.SetTransform(matrix);
        }

        /// <summary>
        ///     Takes a picture.
        /// </summary>
        private void TakePicture()
        {
            try
            {
                Activity activity = this.Activity;
                if (activity == null || this.mCameraDevice == null)
                {
                    return;
                }
                CameraManager manager = (CameraManager)activity.GetSystemService(Context.CameraService);

                // Pick the best JPEG size that can be captures with this CameraDevice
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(this.mCameraDevice.Id);
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
                outputSurfaces.Add(new Surface(this.mTextureView.SurfaceTexture));

                CaptureRequest.Builder captureBuilder = this.mCameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(reader.Surface);
                this.SetUpCaptureRequestBuilder(captureBuilder);
                // Orientation
                SurfaceOrientation rotation = activity.WindowManager.DefaultDisplay.Rotation;
                captureBuilder.Set(CaptureRequest.JpegOrientation, new Integer(ORIENTATIONS.Get((int)rotation)));

                // Output file
                File file = new File(activity.GetExternalFilesDir(null), "DEMO_"+DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg");

                // This listener is called when an image is ready in ImageReader 
                // Right click on ImageAvailableListener in your IDE and go to its definition
                ImageAvailableListener readerListener = new ImageAvailableListener { File = file };

                // We create a Handler since we want to handle the resulting JPEG in a background thread
                HandlerThread thread = new HandlerThread("CameraPicture");
                thread.Start();
                Handler backgroundHandler = new Handler(thread.Looper);
                reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

                //This listener is called when the capture is completed
                // Note that the JPEG data is not available in this listener, but in the ImageAvailableListener we created above
                // Right click on CameraCaptureListener in your IDE and go to its definition
                CameraCaptureListener captureListener = new CameraCaptureListener { Fragment = this, File = file };

                this.mCameraDevice.CreateCaptureSession(
                    outputSurfaces,
                    new CameraCaptureStateListener()
                        {
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