using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Foundation;

using Guards;

using Tracing;

using UIKit;

namespace CrossPlatformLibrary.Camera
{
    public class MediaLibrary : IMediaLibrary
    {
        private readonly ITracer tracer;
        private UIPopoverController popover;
        private UIImagePickerControllerDelegate pickerDelegate;

        public MediaLibrary(ITracer tracer)
        {
            Guard.ArgumentNotNull(tracer, nameof(tracer));

            this.tracer = tracer;
        }

        /// <inheritdoc />
        public Task SaveToCameraRoll(MediaFile mediafile, bool overwrite = true)
        {
            var path = mediafile.Path;
            var someImage = UIImage.FromFile(path);

            var tcs = new TaskCompletionSource<object>();
            someImage.SaveToPhotosAlbum(
                (image, error) =>
                    {
                        if (error != null)
                        {
                            var message = $"SaveToPhotosAlbum failed to save image '{path}'. Error: {error}";
                            tcs.SetException(new Exception(message));
                        }
                        else
                        {
                            tcs.SetResult(null);
                        }
                    });

            return tcs.Task;
        }

        /// <inheritdoc />
        public Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null)
        {
            options = options ?? new PickMediaOptions();

            this.CheckPhotoUsageDescription();

            // TODO Use inheritance here
            var cameraOptions = new StoreMediaOptions
                                    {
                                        PhotoSize = options.PhotoSize, CompressionQuality = options.CompressionQuality
                                    };

            return this.GetMediaAsync(UIImagePickerControllerSourceType.PhotoLibrary, Constants.TypeImage, cameraOptions);
        }

        private void CheckPhotoUsageDescription()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                if (!info.ContainsKey(new NSString("NSPhotoLibraryUsageDescription")))
                    throw new UnauthorizedAccessException(
                              "On iOS 10 and higher you must set NSPhotoLibraryUsageDescription in your Info.plist file to enable Authorization Requests for Photo Library access!");
            }
        }

        private Task<MediaFile> GetMediaAsync(UIImagePickerControllerSourceType sourceType, string mediaType, StoreMediaOptions options = null)
        {
            UIWindow window = UIApplication.SharedApplication.KeyWindow;
            if (window == null) throw new InvalidOperationException("There's no current active window");

            var viewController = window.RootViewController;
            if (viewController == null)
            {
                window = UIApplication.SharedApplication.Windows.OrderByDescending(w => w.WindowLevel).FirstOrDefault(w => w.RootViewController != null);
                if (window == null)
                {
                    throw new InvalidOperationException("Could not find current view controller");
                }

                viewController = window.RootViewController;
            }

            while (viewController.PresentedViewController != null)
            {
                viewController = viewController.PresentedViewController;
            }

            MediaPickerDelegate ndelegate = new MediaPickerDelegate(viewController, sourceType, options);
            var od = Interlocked.CompareExchange(ref this.pickerDelegate, ndelegate, null);
            if (od != null) throw new InvalidOperationException("Only one operation can be active at at time");

            var picker = SetupController(ndelegate, sourceType, mediaType);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad && sourceType == UIImagePickerControllerSourceType.PhotoLibrary)
            {
                ndelegate.Popover = new UIPopoverController(picker);
                ndelegate.Popover.Delegate = new MediaPickerPopoverDelegate(ndelegate, picker);
                ndelegate.DisplayPopover();
            }
            else
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                {
                    picker.ModalPresentationStyle = UIModalPresentationStyle.OverCurrentContext;
                }
                viewController.PresentViewController(picker, true, null);
            }

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

        private static MediaPickerController SetupController(MediaPickerDelegate mpDelegate, UIImagePickerControllerSourceType sourceType, string mediaType)
        {
            var picker = new MediaPickerController(mpDelegate);
            picker.MediaTypes = new[] { mediaType };
            picker.SourceType = sourceType;

            return picker;
        }

        public Task<MediaFile> PickVideoAsync()
        {
            throw new NotImplementedException();
        }
    }
}