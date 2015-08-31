using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace CameraControls
{
    public sealed class LabeledImage : Control
    {
        public LabeledImage()
        {
            this.DefaultStyleKey = typeof(LabeledImage);
        }

        public ImageSource ImagePath
        {
            get { return (ImageSource)this.GetValue(ImagePathProperty); }
            set { this.SetValue(ImagePathProperty, value); }
        }

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(ImageSource), typeof(LabeledImage), new PropertyMetadata(null));

        public string Label
        {
            get { return (string)this.GetValue(LabelProperty); }
            set { this.SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(LabeledImage), new PropertyMetadata(null));

    }
}
