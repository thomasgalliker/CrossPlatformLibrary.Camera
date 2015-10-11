using CameraSample.WinPhone;

using Xamarin.Forms;
using Xamarin.Forms.Platform.WinPhone;

[assembly: ExportRenderer(typeof(MyPage), typeof(MyPageCustom))]

namespace CameraSample.WinPhone
{
    class MyPageCustom : PageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            Page1 p = new Page1();
            this.Children.Add(p);
        }
    }
}