using System;
using System.Linq;
using System.Threading.Tasks;

using CrossPlatformLibrary.Camera;
using CrossPlatformLibrary.IoC;
using Tracing;

using Xamarin.Forms;

namespace CameraSample
{
    public partial class CameraPage : ContentPage
    {
        private readonly IMediaAccess mediaAccess;

        public CameraPage()
        {
            this.InitializeComponent();

            this.mediaAccess = SimpleIoc.Default.GetInstance<IMediaAccess>();
        }

        private async void OnTakeFrontPhotoTapped(object sender, EventArgs eventArgs)
        {
            await this.TakeFrontPhotoAsync();
        }

        private async void OnTakeBackPhotoTapped(object sender, EventArgs eventArgs)
        {
            await this.TakeBackPhotoAsync();
        }

        private async Task TakeFrontPhotoAsync()
        {
            try
            {
                var cameras = this.mediaAccess.Cameras.ToList();
                var frontCamera = cameras.FirstOrDefault(c => c.CameraFacingDirection == CameraFacingDirection.Front) as IPhotoCamera;
                if (frontCamera != null)
                {
                    var mediaFile = await frontCamera.TakePhotoAsync(new StoreMediaOptions("testimage" + Guid.NewGuid() + ".jpg", "Front Camera Photos"));
                    if (mediaFile != null)
                    {
                        await this.mediaAccess.MediaLibrary.SaveToCameraRoll(mediaFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.Create<CameraPage>().Exception(ex, ex.Message);
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
                }
            }
        }
    }
}