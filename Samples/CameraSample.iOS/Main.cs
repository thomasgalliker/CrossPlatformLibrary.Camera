using System.Linq;

using CrossPlatformLibrary.Camera;

using UIKit;

namespace CameraSample.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            var x = new MediaAccess();

            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
