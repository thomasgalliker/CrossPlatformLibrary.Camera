using System;
using System.Collections.Generic;
using System.Linq;

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

using Tracing;

using Java.IO;
using Java.Lang;
using Java.Nio;
using Java.Util.Concurrent;

using Debug = System.Diagnostics.Debug;
using Exception = System.Exception;
using Math = System.Math;
using Object = Java.Lang.Object;
using Orientation = Android.Content.Res.Orientation;

namespace CrossPlatformLibrary.Camera.ViewFinder
{
    /// <summary>
    ///     Source:
    ///     http://developer.android.com/samples/Camera2Basic/src/com.example.android.camera2basic/Camera2BasicFragment.html
    ///     https://gist.github.com/anonymous/be03bb6f5fa5287bc52b
    /// </summary>
    [Preserve(AllMembers = true)]
    public class Camera2BasicFragment : Fragment
    {
        internal const string ExtraPath = "path";
        internal const string ExtraFilename = "filename";
        internal const string ExtraCameraFacingDirection = "cameraFacingDirection";
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

        // The size of the camera preview
        private Size previewSize;

        // CameraDevice.StateListener is called when a CameraDevice changes its state
        private CameraStateListener cameraStateListener;

        /**
        * A {@link Semaphore} to prevent the app from exiting before closing the camera.
        */
        private readonly Semaphore mCameraOpenCloseLock = new Semaphore(1);

        private HandlerThread mBackgroundThread;
        private Handler mBackgroundHandler;
        private static readonly Integer LENS_FACING_BACK = (Integer)1;
        private static readonly Integer LENS_FACING_FRONT = (Integer)0;
        private Integer facing = LENS_FACING_BACK;

        private string path;
        private string targetFilename;
        private readonly ITracer tracer = Tracer.Create<Camera2BasicFragment>();

        // TextureView.ISurfaceTextureListener handles several lifecycle events on a TextureView
        private Camera2BasicSurfaceTextureListener mSurfaceTextureListener;
        private int requestId;

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
                ////this.fragment.ConfigureTransform(width, height);
                ////this.fragment.StartPreview();
                this.fragment.OpenCamera();
            }

            public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
            {
                return true;
            }

            public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
            {
                this.fragment.ConfigureTransform(width, height);
                ////this.fragment.StartPreview();
            }

            public void OnSurfaceTextureUpdated(SurfaceTexture surface)
            {
            }
        }

        private class CameraStateListener : CameraDevice.StateCallback
        {
            private readonly ITracer tracer = Tracer.Create<ImageAvailableListener>();
            public Camera2BasicFragment Fragment;

            public override void OnOpened(CameraDevice camera)
            {
                if (this.Fragment != null)
                {
                    this.Fragment.cameraDevice = camera;
                    this.Fragment.StartPreview();

                    this.Fragment.mCameraOpenCloseLock.Release();

                    if (this.Fragment.autoFitTextureView != null)
                    {
                        this.Fragment.ConfigureTransform(this.Fragment.autoFitTextureView.Width, this.Fragment.autoFitTextureView.Height);
                    }
                }
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                if (this.Fragment != null)
                {
                    this.Fragment.mCameraOpenCloseLock.Release();
                    camera.Close();
                    this.Fragment.cameraDevice = null;
                }
            }

            public override void OnError(CameraDevice camera, CameraError error)
            {
                this.tracer.Error("CameraStateListener.OnError: CameraError={0}", error.ToString());

                if (this.Fragment != null)
                {
                    this.Fragment.mCameraOpenCloseLock.Release();
                    camera.Close();
                    this.Fragment.cameraDevice = null;
                    Activity activity = this.Fragment.Activity;

                    if (activity != null)
                    {
                        activity.Finish();
                    }
                }
            }
        }

        private class ImageAvailableListener : Object, ImageReader.IOnImageAvailableListener
        {
            private readonly ITracer tracer = Tracer.Create<ImageAvailableListener>();
            public File File;

            public void OnImageAvailable(ImageReader reader)
            {
                Image image = null;
                try
                {
                    this.tracer.Debug("OnImageAvailable: ImageReader.AcquireLatestImage");
                    image = reader.AcquireLatestImage();
                    ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                    byte[] bytes = new byte[buffer.Capacity()];
                    buffer.Get(bytes);
                    this.Save(bytes);
                    
                }
                catch (Exception ex)
                {
                    this.tracer.Exception(ex, "OnImageAvailable failed with exception.");
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
                this.tracer.Debug("BEFORE Save(byte[] bytes)");
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
                this.tracer.Debug("AFTER Save(byte[] bytes)");
            }
        }

        private class CameraCaptureListener : CameraCaptureSession.CaptureCallback
        {
            private readonly ITracer tracer = Tracer.Create<CameraCaptureListener>();
            public Camera2BasicFragment Fragment;
            public File File;

            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                this.tracer.Debug("OnCaptureCompleted");

                if (this.Fragment != null && this.File != null)
                {
                    Activity activity = this.Fragment.Activity;
                    if (activity != null)
                    {
                        var mediaFile = new MediaFile(this.File.AbsolutePath, () => System.IO.File.OpenRead(this.File.AbsolutePath));
                        this.Fragment.OnMediaPicked(new MediaPickedEventArgs(this.Fragment.requestId, mediaFile));

                        activity.Finish();
                    }
                }
            }
        }

        private void OnMediaPicked(MediaPickedEventArgs e)
        {
            this.tracer.Debug("OnMediaPicked");

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

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Bundle b = (savedInstanceState ?? this.Activity.Intent.Extras);

            this.requestId = b.GetInt(ExtraId, 0);

            var targetFilenameString = b.GetString(ExtraFilename);
            if (!string.IsNullOrEmpty(targetFilenameString))
            {
                this.targetFilename = targetFilenameString;
            }
            else
            {
                this.targetFilename = "_____ccuiphoto.jpg";
            }
            this.tracer.Debug("OnCreate: this.targetFilename={0}", this.targetFilename);

            var pathString = b.GetString(ExtraPath);
            
            if (!string.IsNullOrEmpty(pathString))
            {
                this.path = pathString;
            }
            else
            {
                this.path = null;
            }
            this.tracer.Debug("OnCreate: this.path={0}", this.path);
            
            this.facing = new Integer(b.GetInt(ExtraCameraFacingDirection));
            this.tracer.Debug("OnCreate: this.facing={0} (Integer)", this.facing.ToString());

            this.cameraStateListener = new CameraStateListener { Fragment = this };
            this.mSurfaceTextureListener = new Camera2BasicSurfaceTextureListener(this);

            Orientations.Append((int)SurfaceOrientation.Rotation0, 90);
            Orientations.Append((int)SurfaceOrientation.Rotation90, 0);
            Orientations.Append((int)SurfaceOrientation.Rotation180, 270);
            Orientations.Append((int)SurfaceOrientation.Rotation270, 180);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt(ExtraId, this.requestId);
            outState.PutInt(ExtraCameraFacingDirection, (int)this.facing);
            outState.PutString(ExtraFilename, this.targetFilename);

            if (this.path != null)
            {
                outState.PutString(ExtraPath, this.path);
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

            var takePictureButton = view.FindViewById(Resource.Id.takePictureButton);
            takePictureButton.Click += this.TakePictureButtonOnClick;

            var switchCameraButton = view.FindViewById(Resource.Id.switchCameraButton);
            switchCameraButton.Click += this.SwitchCameraButtonOnClick;
        }

        public override void OnResume()
        {
            base.OnResume();
            this.tracer.Debug("OnResume");
            this.tracer.Debug("OnResume: this.facing={0}", this.facing.ToString());

            this.StartBackgroundThread();
            if (this.autoFitTextureView.IsAvailable)
            {
                this.OpenCamera();
            }
            else
            {
                this.autoFitTextureView.SurfaceTextureListener = this.mSurfaceTextureListener;
            }
        }

        public override void OnPause()
        {
            this.tracer.Debug("OnPause");

            this.CloseCamera();
            this.StopBackgroundThread();
            base.OnPause();
        }

        private void StartBackgroundThread()
        {
            this.mBackgroundThread = new HandlerThread("CameraBackground");
            this.mBackgroundThread.Start();
            this.mBackgroundHandler = new Handler(this.mBackgroundThread.Looper);
        }

        private void StopBackgroundThread()
        {
            this.mBackgroundThread.QuitSafely();
            try
            {
                this.mBackgroundThread.Join();
                this.mBackgroundThread = null;
                this.mBackgroundHandler = null;
            }
            catch (InterruptedException e)
            {
                this.tracer.Exception(e);
            }
        }

        private static CameraFacingDirection ToCameraFacingDirection(Integer lensFacing)
        {
            if (lensFacing.Equals(LENS_FACING_BACK))
            {
                Tracer.Create("ToCameraFacingDirection").Debug("lensFacing == LENS_FACING_BACK");
                return CameraFacingDirection.Rear;
            }

            Tracer.Create("ToCameraFacingDirection").Debug("lensFacing == LENS_FACING_FRONT");
            return CameraFacingDirection.Front;
        }

        internal static Integer ToLensFacingInteger(CameraFacingDirection cameraFacingDirection)
        {
            if (cameraFacingDirection == CameraFacingDirection.Rear)
            {
                return LENS_FACING_BACK;
            }

            return LENS_FACING_FRONT;
        }

        // Opens a CameraDevice. The result is listened to by 'cameraStateListener'.
        private void OpenCamera()
        {
            var activity = this.Activity;
            if (activity == null || activity.IsFinishing)
            {
                return;
            }

            this.tracer.Debug("OpenCamera");
            this.tracer.Debug("OpenCamera: this.facing={0}", this.facing.ToString());

            var cameraManager = (CameraManager)activity.GetSystemService(Context.CameraService);

            try
            {
                if (!this.mCameraOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds))
                {
                    const string ErrorMessage = "Time out waiting to lock camera opening.";
                    this.tracer.Error(ErrorMessage);
                    throw new RuntimeException(ErrorMessage);
                }

                string idForOpen = null;
                string[] camerasIds = cameraManager.GetCameraIdList();
                foreach (string id in camerasIds)
                {
                    CameraCharacteristics cameraCharacteristics = cameraManager.GetCameraCharacteristics(id);
                    var cameraLensFacing = (Integer)cameraCharacteristics.Get(CameraCharacteristics.LensFacing);

                    CameraFacingDirection cameraFacingDirection = ToCameraFacingDirection(cameraLensFacing);
                    CameraFacingDirection configuredFacingDirection = ToCameraFacingDirection(this.facing);

                    this.tracer.Debug("OpenCamera: cameraFacingDirection={0}, configuredFacingDirection={1}", cameraFacingDirection, configuredFacingDirection);
                    if (cameraFacingDirection == configuredFacingDirection)
                    {
                        idForOpen = id;
                        break;
                    }
                }

                var cameraId = idForOpen ?? camerasIds[0];
                this.tracer.Debug("OpenCamera: idForOpen={0}", idForOpen);
                this.tracer.Debug("OpenCamera: idForOpen={0}", idForOpen);
                this.tracer.Debug("OpenCamera: cameraId={0}", cameraId);

                // To get a list of available sizes of camera preview, we retrieve an instance of
                // StreamConfigurationMap from CameraCharacteristics
                CameraCharacteristics characteristics = cameraManager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                this.previewSize = map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[0]; // We assume that the top-most element is the one with the best resolution
                Orientation orientation = this.Resources.Configuration.Orientation;
                if (orientation == Orientation.Landscape)
                {
                    this.autoFitTextureView.SetAspectRatio(this.previewSize.Width, this.previewSize.Height);
                }
                else
                {
                    this.autoFitTextureView.SetAspectRatio(this.previewSize.Height, this.previewSize.Width);
                }

                ////this.ConfigureTransform(this.autoFitTextureView.Width, this.autoFitTextureView.Width);

                // We are opening the camera with a listener. When it is ready, OnOpened of cameraStateListener is called.
                cameraManager.OpenCamera(cameraId, this.cameraStateListener, null);
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
            catch (InterruptedException e)
            {
                const string ErrorMessage = "Interrupted while trying to lock camera opening.";
                this.tracer.Exception(e, ErrorMessage);
                throw new RuntimeException(ErrorMessage);
            }
            catch (Exception ex)
            {
                this.tracer.Exception(ex);
                throw;
            }
        }

        private void CloseCamera()
        {
            this.tracer.Debug("CloseCamera");

            try
            {
                this.tracer.Debug("this.mCameraOpenCloseLock.Acquire()");
                this.mCameraOpenCloseLock.Acquire();
                if (this.cameraDevice != null)
                {
                    this.tracer.Debug("this.cameraDevice.Close();");
                    this.cameraDevice.Close();
                    this.cameraDevice = null;
                }
                ////if (null != mMediaRecorder)
                ////{
                ////    mMediaRecorder.release();
                ////    mMediaRecorder = null;
                ////}
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.");
            }
            finally
            {
                this.mCameraOpenCloseLock.Release();
                this.tracer.Debug(" this.mCameraOpenCloseLock.Release()");
            }
        }

        private void StartPreview()
        {
            if (this.cameraDevice == null || !this.autoFitTextureView.IsAvailable || this.previewSize == null)
            {
                return;
            }

            this.tracer.Debug("StartPreview");

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
                    new CameraCaptureStateListener
                        {
                            OnConfigureFailedAction = (CameraCaptureSession session) =>
                                {
                                    Activity activity = this.Activity;
                                    if (activity != null)
                                    {
                                        this.tracer.Error("OnConfigureFailedAction");
                                    }
                                },
                            OnConfiguredAction = (CameraCaptureSession session) =>
                                {
                                    this.previewSession = session;
                                    this.UpdatePreview();
                                }
                        },
                    this.mBackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                this.tracer.Exception(ex, "Failed to start preview.");
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

            this.tracer.Debug("UpdatePreview");

            try
            {
                // The camera preview can be run in a background thread. This is a Handler for the camere preview
                this.SetUpCaptureRequestBuilder(this.previewBuilder);
                var thread = new HandlerThread("CameraPreview");
                thread.Start();

                // Finally, we start displaying the camera preview
                this.previewSession.SetRepeatingRequest(this.previewBuilder.Build(), null, this.mBackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                this.tracer.Exception(ex, "Failed to execute UpdatePreview.");
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
                float verticalScale = (float)viewHeight / this.previewSize.Height;
                float horizontalScale = (float)viewWidth / this.previewSize.Width;
                float scale = Math.Max(verticalScale, horizontalScale);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
            }
            this.autoFitTextureView.SetTransform(matrix);
        }

        private void TakePictureButtonOnClick(object sender, EventArgs e)
        {
            this.TakePicture();
        }

        private void TakePicture()
        {
            this.tracer.Debug("TakePicture");

            try
            {
                Activity activity = this.Activity;
                if (activity == null || this.cameraDevice == null)
                {
                    return;
                }

                var cameraManager = (CameraManager)activity.GetSystemService(Context.CameraService);

                // Pick the best JPEG size that can be captures with this CameraDevice
                var characteristics = cameraManager.GetCameraCharacteristics(this.cameraDevice.Id);
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

                    this.tracer.Debug("TakePicture: Found {0} jpegSizes. Selected {1}x{2}px.", jpegSizes.Count(), width, height);
                }

                // We use an ImageReader to get a JPEG from CameraDevice
                // Here, we create a new ImageReader and prepare its Surface as an output from the camera
                var reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
                var outputSurfaces = new List<Surface>(2);
                outputSurfaces.Add(reader.Surface);
                outputSurfaces.Add(new Surface(this.autoFitTextureView.SurfaceTexture));

                var captureBuilder = this.cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(reader.Surface);
                this.SetUpCaptureRequestBuilder(captureBuilder);

                // Orientation
                var rotation = activity.WindowManager.DefaultDisplay.Rotation;
                captureBuilder.Set(CaptureRequest.JpegOrientation, new Integer(Orientations.Get((int)rotation)));

                // Output file
                var combined = System.IO.Path.Combine(activity.GetExternalFilesDir(null).AbsolutePath, this.path);
                var folder = new File(combined);
                if (!folder.Exists())
                {
                    if (!folder.Mkdirs())
                    {
                        throw new IOException("Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?");
                    }
                }

                this.tracer.Debug("TakePicture: Path.Combine={0}", combined);
                var file = new File(combined, this.targetFilename);
                this.tracer.Debug("TakePicture: File={0}", file.AbsolutePath);

                // This listener is called when an image is ready in ImageReader 
                var readerListener = new ImageAvailableListener { File = file };

                // We create a Handler since we want to handle the resulting JPEG in a background thread
                var thread = new HandlerThread("CameraPicture");
                thread.Start();
                var backgroundHandler = new Handler(thread.Looper);
                reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

                //This listener is called when the capture is completed
                // Note that the JPEG data is not available in this listener, but in the ImageAvailableListener we created above
                var captureListener = new CameraCaptureListener { Fragment = this, File = file };

                this.cameraDevice.CreateCaptureSession(
                    outputSurfaces,
                    new CameraCaptureStateListener
                        {
                            OnConfiguredAction = (CameraCaptureSession session) =>
                                {
                                    try
                                    {
                                        session.Capture(captureBuilder.Build(), captureListener, backgroundHandler);
                                    }
                                    catch (CameraAccessException ex)
                                    {
                                        this.tracer.Exception(ex, "Capture session error.");
                                    }
                                }
                        },
                    backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                this.tracer.Exception(ex, "Failed to take picture.");
            }
        }

        private bool CanSwitch()
        {
            var manager = (CameraManager)this.Activity.GetSystemService(Context.CameraService);
            try
            {
                int numberOfCameras = manager.GetCameraIdList().Length;
                return numberOfCameras > 1;
            }
            catch (CameraAccessException ex)
            {
                return false;
            }
        }

        private void SwitchCameraButtonOnClick(object sender, EventArgs e)
        {
            this.SwitchCamera();
        }

        private void SwitchCamera()
        {
            if (!this.CanSwitch())
            {
                return;
            }

            if (this.facing.Equals(LENS_FACING_BACK))
            {
                this.facing = LENS_FACING_FRONT;
            }
            else if (this.facing.Equals(LENS_FACING_FRONT))
            {
                this.facing = LENS_FACING_BACK;
            }

            this.RestartCamera();
        }

        private void RestartCamera()
        {
            this.CloseCamera();
            this.OpenCamera();
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