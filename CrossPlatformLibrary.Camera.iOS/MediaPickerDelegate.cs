﻿using System;
using System.IO;
using System.Threading.Tasks;

using CoreGraphics;

using Foundation;

using UIKit;

using NSAction = System.Action;

namespace CrossPlatformLibrary.Camera
{
    internal class MediaPickerDelegate : UIImagePickerControllerDelegate
    {
        private UIDeviceOrientation? orientation;
        private readonly NSObject observer;
        private readonly UIViewController viewController;
        private readonly UIImagePickerControllerSourceType source;
        private TaskCompletionSource<MediaFile> tcs = new TaskCompletionSource<MediaFile>();
        private readonly StoreMediaOptions options;

        internal MediaPickerDelegate(UIViewController viewController, UIImagePickerControllerSourceType sourceType, StoreMediaOptions options)
        {
            this.viewController = viewController;
            this.source = sourceType;
            this.options = options ?? new StoreMediaOptions();

            if (viewController != null)
            {
                UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
                this.observer = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, this.DidRotate);
            }
        }

        public UIPopoverController Popover { get; set; }

        public UIView View
        {
            get
            {
                return this.viewController.View;
            }
        }

        public Task<MediaFile> Task
        {
            get
            {
                return this.tcs.Task;
            }
        }

        public override async void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
        {
            this.RemoveOrientationChangeObserverAndNotifications();

            MediaFile mediaFile;
            switch ((NSString)info[UIImagePickerController.MediaType])
            {
                case Constants.TypeImage:
                    mediaFile = await this.GetPictureMediaFile(info);
                    break;

                case Constants.TypeMovie:
                    mediaFile = await this.GetMovieMediaFile(info);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                UIApplication.SharedApplication.SetStatusBarStyle(Constants.StatusBarStyle, false);
            }

            this.Dismiss(picker, () => { this.tcs.TrySetResult(mediaFile); });
        }

        public override void Canceled(UIImagePickerController picker)
        {
            this.RemoveOrientationChangeObserverAndNotifications();

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                UIApplication.SharedApplication.SetStatusBarStyle(Constants.StatusBarStyle, false);
            }

            this.Dismiss(picker, () => { this.tcs.SetResult(null); });
        }

        public void DisplayPopover(bool hideFirst = false)
        {
            if (this.Popover == null) return;

            var swidth = UIScreen.MainScreen.Bounds.Width;
            var sheight = UIScreen.MainScreen.Bounds.Height;

            nfloat width = 400;
            nfloat height = 300;

            if (this.orientation == null)
            {
                if (IsValidInterfaceOrientation(UIDevice.CurrentDevice.Orientation))
                {
                    this.orientation = UIDevice.CurrentDevice.Orientation;
                }
                else
                {
                    this.orientation = GetDeviceOrientation(this.viewController.InterfaceOrientation);
                }
            }

            nfloat x, y;
            if (this.orientation == UIDeviceOrientation.LandscapeLeft ||
                this.orientation == UIDeviceOrientation.LandscapeRight)
            {
                y = (swidth / 2) - (height / 2);
                x = (sheight / 2) - (width / 2);
            }
            else
            {
                x = (swidth / 2) - (width / 2);
                y = (sheight / 2) - (height / 2);
            }

            if (hideFirst && this.Popover.PopoverVisible)
            {
                this.Popover.Dismiss(animated: false);
            }

            this.Popover.PresentFromRect(new CGRect(x, y, width, height), this.View, 0, animated: true);
        }

        private bool IsCaptured
        {
            get
            {
                return this.source == UIImagePickerControllerSourceType.Camera;
            }
        }

        private void Dismiss(UIImagePickerController picker, NSAction onDismiss)
        {
            if (this.viewController == null)
            {
                onDismiss();
                this.tcs = new TaskCompletionSource<MediaFile>();
            }
            else
            {
                if (this.Popover != null)
                {
                    this.Popover.Dismiss(animated: true);
                    this.Popover.Dispose();
                    this.Popover = null;

                    onDismiss();
                }
                else
                {
                    picker.DismissViewController(true, onDismiss);
                    picker.Dispose();
                }
            }
        }

        private void RemoveOrientationChangeObserverAndNotifications()
        {
            if (this.viewController != null)
            {
                UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
                NSNotificationCenter.DefaultCenter.RemoveObserver(this.observer);
                this.observer.Dispose();
            }
        }

        private void DidRotate(NSNotification notice)
        {
            UIDevice device = (UIDevice)notice.Object;
            if (!IsValidInterfaceOrientation(device.Orientation) || this.Popover == null) return;
            if (this.orientation.HasValue && IsSameOrientationKind(this.orientation.Value, device.Orientation)) return;

            if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
            {
                if (!this.GetShouldRotate6(device.Orientation)) return;
            }
            else if (!this.GetShouldRotate(device.Orientation)) return;

            UIDeviceOrientation? co = this.orientation;
            this.orientation = device.Orientation;

            if (co == null) return;

            this.DisplayPopover(hideFirst: true);
        }

        private bool GetShouldRotate(UIDeviceOrientation orientation)
        {
            UIInterfaceOrientation iorientation = UIInterfaceOrientation.Portrait;
            switch (orientation)
            {
                case UIDeviceOrientation.LandscapeLeft:
                    iorientation = UIInterfaceOrientation.LandscapeLeft;
                    break;

                case UIDeviceOrientation.LandscapeRight:
                    iorientation = UIInterfaceOrientation.LandscapeRight;
                    break;

                case UIDeviceOrientation.Portrait:
                    iorientation = UIInterfaceOrientation.Portrait;
                    break;

                case UIDeviceOrientation.PortraitUpsideDown:
                    iorientation = UIInterfaceOrientation.PortraitUpsideDown;
                    break;

                default:
                    return false;
            }

            return this.viewController.ShouldAutorotateToInterfaceOrientation(iorientation);
        }

        private bool GetShouldRotate6(UIDeviceOrientation orientation)
        {
            if (!this.viewController.ShouldAutorotate()) return false;

            UIInterfaceOrientationMask mask = UIInterfaceOrientationMask.Portrait;
            switch (orientation)
            {
                case UIDeviceOrientation.LandscapeLeft:
                    mask = UIInterfaceOrientationMask.LandscapeLeft;
                    break;

                case UIDeviceOrientation.LandscapeRight:
                    mask = UIInterfaceOrientationMask.LandscapeRight;
                    break;

                case UIDeviceOrientation.Portrait:
                    mask = UIInterfaceOrientationMask.Portrait;
                    break;

                case UIDeviceOrientation.PortraitUpsideDown:
                    mask = UIInterfaceOrientationMask.PortraitUpsideDown;
                    break;

                default:
                    return false;
            }

            return this.viewController.GetSupportedInterfaceOrientations().HasFlag(mask);
        }

        private async Task<MediaFile> GetPictureMediaFile(NSDictionary info)
        {
            var image = (UIImage)info[UIImagePickerController.EditedImage];
            if (image == null)
            {
                image = (UIImage)info[UIImagePickerController.OriginalImage];
            }

            string path = GetOutputPath(Constants.TypeImage, this.options.Directory ?? ((this.IsCaptured) ? string.Empty : "temp"), this.options.Name);

            if (this.options.PhotoSize != PhotoSize.Full)
            {
                try
                {
                    var percent = 1.0f;
                    switch (this.options.PhotoSize)
                    {
                        case PhotoSize.Large:
                            percent = .75f;
                            break;
                        case PhotoSize.Medium:
                            percent = .5f;
                            break;
                        case PhotoSize.Small:
                            percent = .25f;
                            break;
                        case PhotoSize.Custom:
                            percent = (float)this.options.CustomPhotoSize / 100f;
                            break;
                    }

                    //calculate new size
                    var width = (image.CGImage.Width * percent);
                    var height = (image.CGImage.Height * percent);

                    //begin resizing image
                    image = image.ResizeImageWithAspectRatio(width, height);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to compress image: {ex}");
                }
            }

            //iOS quality is 0.0-1.0
            var quality = (this.options.CompressionQuality / 100f);
            image.AsJPEG(quality).Save(path, true);

            Action<bool> dispose = null;
            if (this.source != UIImagePickerControllerSourceType.Camera)
            {
                dispose = d => File.Delete(path);
            }

            return new MediaFile(path, () => File.OpenRead(path), dispose: dispose);
        }

        private async Task<MediaFile> GetMovieMediaFile(NSDictionary info)
        {
            NSUrl url = (NSUrl)info[UIImagePickerController.MediaURL];

            string path = GetOutputPath(Constants.TypeMovie, this.options.Directory ?? ((this.IsCaptured) ? string.Empty : "temp"), this.options.Name ?? Path.GetFileName(url.Path));

            File.Move(url.Path, path);

            Action<bool> dispose = null;
            if (this.source != UIImagePickerControllerSourceType.Camera)
            {
                dispose = d => File.Delete(path);
            }

            return new MediaFile(path, () => File.OpenRead(path), dispose: dispose);
        }

        private static string GetUniquePath(string type, string path, string name)
        {
            bool isPhoto = type == Constants.TypeImage;
            string ext = Path.GetExtension(name);
            if (ext == string.Empty)
            {
                ext = isPhoto ? ".jpg" : ".mp4";
            }

            name = Path.GetFileNameWithoutExtension(name);

            string nname = name + ext;
            int i = 1;
            while (File.Exists(Path.Combine(path, nname))) nname = name + "_" + (i++) + ext;

            return Path.Combine(path, nname);
        }

        private static string GetOutputPath(string type, string path, string name)
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path);
            Directory.CreateDirectory(path);

            if (string.IsNullOrWhiteSpace(name))
            {
                string timestamp = DateTime.Now.ToString("yyyMMdd_HHmmss");
                if (type == Constants.TypeImage) name = "IMG_" + timestamp + ".jpg";
                else name = "VID_" + timestamp + ".mp4";
            }

            return Path.Combine(path, GetUniquePath(type, path, name));
        }

        private static bool IsValidInterfaceOrientation(UIDeviceOrientation self)
        {
            return (self != UIDeviceOrientation.FaceUp && self != UIDeviceOrientation.FaceDown && self != UIDeviceOrientation.Unknown);
        }

        private static bool IsSameOrientationKind(UIDeviceOrientation o1, UIDeviceOrientation o2)
        {
            if (o1 == UIDeviceOrientation.FaceDown || o1 == UIDeviceOrientation.FaceUp)
            {
                return (o2 == UIDeviceOrientation.FaceDown || o2 == UIDeviceOrientation.FaceUp);
            }
            if (o1 == UIDeviceOrientation.LandscapeLeft || o1 == UIDeviceOrientation.LandscapeRight)
            {
                return (o2 == UIDeviceOrientation.LandscapeLeft || o2 == UIDeviceOrientation.LandscapeRight);
            }
            if (o1 == UIDeviceOrientation.Portrait || o1 == UIDeviceOrientation.PortraitUpsideDown)
            {
                return (o2 == UIDeviceOrientation.Portrait || o2 == UIDeviceOrientation.PortraitUpsideDown);
            }

            return false;
        }

        private static UIDeviceOrientation GetDeviceOrientation(UIInterfaceOrientation orientation)
        {
            switch (orientation)
            {
                case UIInterfaceOrientation.LandscapeLeft:
                    return UIDeviceOrientation.LandscapeLeft;
                case UIInterfaceOrientation.LandscapeRight:
                    return UIDeviceOrientation.LandscapeRight;
                case UIInterfaceOrientation.Portrait:
                    return UIDeviceOrientation.Portrait;
                case UIInterfaceOrientation.PortraitUpsideDown:
                    return UIDeviceOrientation.PortraitUpsideDown;
                default:
                    throw new NotSupportedException($"UIInterfaceOrientation.{orientation} is not supported.");
            }
        }
    }
}