using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace CameraControls
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CameraCaptureUIPage : Page
    {
        private readonly DisplayOrientations previous;

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = this.previous;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        public CameraCaptureUIPage()
        {
            this.previous = DisplayInformation.AutoRotationPreferences;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            this.Loaded += this.CameraCaptureUIPage_Loaded;
            this.Unloaded += this.CameraCaptureUIPage_Unloaded;
            this.InitializeComponent();
        }

        private void CameraCaptureUIPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var app = Application.Current;
            app.Suspending -= this.MyCCUCtrl.AppSuspending;
            app.Resuming -= this.MyCCUCtrl.AppResuming;
            DisplayInformation.AutoRotationPreferences = this.previous;
        }

        private void CameraCaptureUIPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        ///     Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">
        ///     Event data that describes how this page was reached.
        ///     This parameter is typically used to configure the page.
        /// </param>
        internal CameraCaptureUI MyCCUCtrl { get; set; }
    }
}