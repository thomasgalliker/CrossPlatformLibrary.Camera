using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AVFoundation;

using Foundation;

using UIKit;

namespace CrossPlatformLibrary.Camera
{
    public class PhotoCamera : IPhotoCamera
    {
        private UIPopoverController popover;
        private UIImagePickerControllerDelegate pickerDelegate;

        public PhotoCamera(CameraFacingDirection cameraFacingDirection, bool isEnabled, string name)
        {
            this.CameraFacingDirection = cameraFacingDirection;
            this.IsEnabled = isEnabled;
            this.Name = name;
        }

        public CameraFacingDirection CameraFacingDirection { get; }

        public bool IsEnabled { get; }

        public string Name { get; }

        public async Task<MediaFile> TakePhotoAsync(StoreMediaOptions options)
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

            if (!await this.AuthorizeCameraUse())
            {
                throw new Exception("Not authorized to access the camera.");
            }
          
            CheckCameraUsageDescription();

            this.VerifyCameraOptions(options);

            return await this.GetMediaAsync(
                       sourceType: UIImagePickerControllerSourceType.Camera, 
                       mediaType: Constants.TypeImage, 
                       cameraFacingDirection: this.CameraFacingDirection, 
                       options: options);
        }

        private static void CheckCameraUsageDescription()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                if (!info.ContainsKey(new NSString("NSCameraUsageDescription"))) throw new UnauthorizedAccessException("On iOS 10 and higher you must set NSCameraUsageDescription in your Info.plist file to enable Authorization Requests for Camera access!");
            }
        }

        private void VerifyOptions(StoreMediaOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
        }

        private void VerifyCameraOptions(StoreMediaOptions options)
        {
            this.VerifyOptions(options);
            //if (!Enum.IsDefined(typeof(CameraFacingDirection), options.DefaultCamera))
            //{
            //    throw new ArgumentException("options.Camera is not a member of CameraDevice");
            //}
        }

        private static MediaPickerController SetupController(MediaPickerDelegate mpDelegate, UIImagePickerControllerSourceType sourceType, string mediaType, CameraFacingDirection cameraFacingDirection, StoreMediaOptions options)
        {
            var picker = new MediaPickerController(mpDelegate);
            picker.MediaTypes = new[] { mediaType };
            picker.SourceType = sourceType;

            if (sourceType == UIImagePickerControllerSourceType.Camera)
            {
                picker.CameraDevice = GetUICameraDevice(cameraFacingDirection);
                picker.AllowsEditing = options.AllowCropping;

                if (options.OverlayViewProvider != null)
                {
                    var overlay = options.OverlayViewProvider;
                    if (overlay is UIView)
                    {
                        picker.CameraOverlayView = overlay as UIView;
                    }
                }
                if (mediaType == Constants.TypeImage)
                {
                    picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo;
                }
                else if (mediaType == Constants.TypeMovie)
                {
                    StoreVideoOptions voptions = (StoreVideoOptions)options;

                    picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Video;
                    picker.VideoQuality = GetQuailty(voptions.Quality);
                    picker.VideoMaximumDuration = voptions.DesiredLength.TotalSeconds;
                }
            }

            return picker;
        }

        private Task<MediaFile> GetMediaAsync(UIImagePickerControllerSourceType sourceType, string mediaType, CameraFacingDirection cameraFacingDirection, StoreMediaOptions options = null)
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            if (window == null)
            {
                throw new InvalidOperationException("There's no current active window");
            }

            var viewController = window.RootViewController;
            if (viewController == null)
            {
                window = UIApplication.SharedApplication.Windows.OrderByDescending(w => w.WindowLevel).FirstOrDefault(w => w.RootViewController != null);
                if (window == null) throw new InvalidOperationException("Could not find current view controller");
                else viewController = window.RootViewController;
            }

            while (viewController.PresentedViewController != null) viewController = viewController.PresentedViewController;

            MediaPickerDelegate ndelegate = new MediaPickerDelegate(viewController, sourceType, options);
            var od = Interlocked.CompareExchange(ref this.pickerDelegate, ndelegate, null);
            if (od != null) throw new InvalidOperationException("Only one operation can be active at at time");

            var picker = SetupController(ndelegate, sourceType, mediaType, cameraFacingDirection, options);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad && sourceType == UIImagePickerControllerSourceType.PhotoLibrary)
            {
                ndelegate.Popover = new UIPopoverController(picker);
                ndelegate.Popover.Delegate = new MediaPickerPopoverDelegate(ndelegate, picker);
                ndelegate.DisplayPopover();
            }
            else viewController.PresentViewController(picker, true, null);

            return ndelegate.Task.ContinueWith(
                t =>
                    {
                        if (this.popover != null)
                        {
                            this.popover.Dispose();
                            this.popover = null;
                        }

                        Interlocked.Exchange(ref this.pickerDelegate, null);
                        return t;
                    }).Unwrap();
        }

        private static UIImagePickerControllerCameraDevice GetUICameraDevice(CameraFacingDirection cameraFacingDirection)
        {
            switch (cameraFacingDirection)
            {
                case CameraFacingDirection.Front:
                    return UIImagePickerControllerCameraDevice.Front;
                case CameraFacingDirection.Rear:
                    return UIImagePickerControllerCameraDevice.Rear;
                default:
                    throw new NotSupportedException();
            }
        }

        private static UIImagePickerControllerQualityType GetQuailty(VideoQuality quality)
        {
            switch (quality)
            {
                case VideoQuality.Low:
                    return UIImagePickerControllerQualityType.Low;
                case VideoQuality.Medium:
                    return UIImagePickerControllerQualityType.Medium;
                default:
                    return UIImagePickerControllerQualityType.High;
            }
        }

        async Task<bool> AuthorizeCameraUse()
        {
            var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);

            // Ask for Camera permissions, if not already authorized
            if (authorizationStatus != AVAuthorizationStatus.Authorized)
            {
                var access = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);

                // Refresh authorization status
                authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);
                return authorizationStatus == AVAuthorizationStatus.Authorized;
            }

            return true;
        }
    }
}