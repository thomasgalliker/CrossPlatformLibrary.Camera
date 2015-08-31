using System;
using System.Threading.Tasks;

using Android.Content;
using Android.Runtime;

using Uri = Android.Net.Uri;

namespace CrossPlatformLibrary.Camera
{
    /// <summary>
    /// </summary>
    [Preserve(AllMembers = true)]
    public static class MediaFileExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Task<MediaFile> GetMediaFileExtraAsync(this Intent self, Context context)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            string action = self.GetStringExtra("action");
            if (action == null)
            {
                throw new ArgumentException("Intent was not results from MediaLibrary", "self");
            }

            var uri = (Uri)self.GetParcelableExtra("MediaFile");
            bool isPhoto = self.GetBooleanExtra("isPhoto", false);
            var path = (Uri)self.GetParcelableExtra("path");

            return MediaPickerActivity.GetMediaFileAsync(context, 0, action, isPhoto, ref path, uri).ContinueWith(t => t.Result.ToTask()).Unwrap();
        }
    }
}