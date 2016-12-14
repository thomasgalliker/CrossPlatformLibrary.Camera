using System;
using System.Linq;
using System.Threading.Tasks;

using CrossPlatformLibrary.Camera;
using CrossPlatformLibrary.IoC;

using Xamarin.Forms;

namespace CameraSample
{
    public partial class CameraPage : ContentPage
    {
        private readonly IMediaAccess mediaAccess;

        public CameraPage()
        {
            this.InitializeComponent();

            try
            {
                this.mediaAccess = SimpleIoc.Default.GetInstance<IMediaAccess>();
            }
            catch (UnauthorizedAccessException)
            {
                this.DisplayAlert("UnauthorizedAccessException", "This app is not authorized to access the camera.", "OK");
            }
            
        }

        private async void OnTakeFrontPhotoTapped(object sender, EventArgs eventArgs)
        {
            await this.TakeFrontPhotoAsync();
        }

        private async void OnTakeBackPhotoTapped(object sender, EventArgs eventArgs)
        {
            await this.TakeBackPhotoAsync();
        }

        private async void OnPickPhotoTapped(object sender, EventArgs e)
        {
            await this.PickPhotoAsync();
        }

        private async Task TakeFrontPhotoAsync()
        {
            var cameras = this.mediaAccess.Cameras.ToList();
            var frontCamera = cameras.FirstOrDefault(c => c.CameraFacingDirection == CameraFacingDirection.Front) as IPhotoCamera;
            if (frontCamera != null)
            {
                var options = new StoreMediaOptions("testimage" + Guid.NewGuid() + ".jpg", "Front Camera Photos");
                options.AllowCropping = true;
                options.PhotoSize = PhotoSize.Small;
                options.CompressionQuality = 10;

                var mediaFile = await frontCamera.TakePhotoAsync(options);
                if (mediaFile != null)
                {
                    await this.mediaAccess.MediaLibrary.SaveToCameraRoll(mediaFile);
                    await this.DisplayAlert("SaveToCameraRoll", $"Your photo has been saved to '{mediaFile.Path}'", "OK");
                    this.SetImageSource(mediaFile);
                }
                else
                {
                    await this.DisplayAlert("TakePhotoAsync", "Failed to take picture.", "OK");
                }
            }
        }

        private async Task TakeBackPhotoAsync()
        {
            var cameras = this.mediaAccess.Cameras.ToList();
            var backCamera = cameras.FirstOrDefault(c => c.CameraFacingDirection == CameraFacingDirection.Rear) as IPhotoCamera;
            if (backCamera != null)
            {
                var mediaFile = await backCamera.TakePhotoAsync(new StoreMediaOptions());
                if (mediaFile != null)
                {
                    await this.mediaAccess.MediaLibrary.SaveToCameraRoll(mediaFile);
                    await this.DisplayAlert("SaveToCameraRoll", $"Your photo has been saved to '{mediaFile.Path}'", "OK");
                    this.SetImageSource(mediaFile);
                }
                else
                {
                    await this.DisplayAlert("TakePhotoAsync", "Failed to take picture.", "OK");
                }
            }
        }

        private void SetImageSource(MediaFile mediaFile)
        {
            this.image.Source = ImageSource.FromStream(
                () =>
                    {
                        using (var m = mediaFile)
                        {
                            return m.GetStream();
                        }
                    });
        }

        private async Task PickPhotoAsync()
        {
            var pickMediaOptions = new PickMediaOptions { PhotoSize = PhotoSize.Full, CompressionQuality = 90 };
            var mediaFile = await this.mediaAccess.MediaLibrary.PickPhotoAsync(pickMediaOptions);
            if (mediaFile != null)
            {
                this.SetImageSource(mediaFile);
            }
            else
            {
                await this.DisplayAlert("PickPhotoAsync", "Failed to open picture.", "OK");
            }
        }
    }
}