using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Phone.Tasks;
using Windows.Storage;

namespace CrossPlatformLibrary.Camera
{
    public class PhotoCamera : IPhotoCamera
    {
        private static TaskCompletionSource<MediaFile> completionSource;
        private readonly CameraCaptureTask cameraCapture;

        public PhotoCamera(CameraFacingDirection cameraFacingDirection, bool isEnabled, string name)
        {
            this.CameraFacingDirection = cameraFacingDirection;
            this.IsEnabled = isEnabled;
            this.Name = name;

            ////this.cameraCapture = new CameraCaptureTask();
            ////this.cameraCapture.Completed += this.OnPhotoChosen;
        }

        public CameraFacingDirection CameraFacingDirection { get; private set; }

        public bool IsEnabled { get; private set; }

        public string Name { get; private set; }

        /// <inheritdoc />
        public async Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options)
        {
            if (!this.IsEnabled)
            {
                throw new NotSupportedException();
            }

            options.VerifyOptions();

            var capture = new CameraCaptureUI();
            var result = await capture.CaptureFileAsync(CameraCaptureUIMode.Photo, options);
            if (result == null)
            {
                return null;
            }

            StorageFolder folder = ApplicationData.Current.LocalFolder;

            string path = options.GetFilePath(folder.Path);
            var directoryFull = Path.GetDirectoryName(path);
            var newFolder = directoryFull.Replace(folder.Path, string.Empty);
            if (!string.IsNullOrWhiteSpace(newFolder))
            {
                await folder.CreateFolderAsync(newFolder, CreationCollisionOption.OpenIfExists);
            }

            folder = await StorageFolder.GetFolderFromPathAsync(directoryFull);

            string filename = Path.GetFileName(path);

            var file = await result.CopyAsync(folder, filename, NameCollisionOption.GenerateUniqueName).AsTask();
            return new MediaFile(file.Path, () => file.OpenStreamForReadAsync().Result);
        }

        ////private void OnPhotoChosen(object sender, PhotoResult photoResult)
        ////{
        ////    var tcs = Interlocked.Exchange(ref completionSource, null);

        ////    if (photoResult.TaskResult == TaskResult.Cancel)
        ////    {
        ////        tcs.SetResult(null);
        ////        return;
        ////    }

        ////    string path = photoResult.OriginalFileName;

        ////    long pos = photoResult.ChosenPhoto.Position;
        ////    var options = tcs.Task.AsyncState as StoreCameraMediaOptions;
        ////    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
        ////    {
        ////        path = options.GetUniqueFilepath((options == null) ? "temp" : null, p => store.FileExists(p));

        ////        string dir = Path.GetDirectoryName(path);
        ////        if (!String.IsNullOrWhiteSpace(dir))
        ////        {
        ////            store.CreateDirectory(dir);
        ////        }

        ////        using (var fs = store.CreateFile(path))
        ////        {
        ////            byte[] buffer = new byte[20480];
        ////            int len;
        ////            while ((len = photoResult.ChosenPhoto.Read(buffer, 0, buffer.Length)) > 0)
        ////            {
        ////                fs.Write(buffer, 0, len);
        ////            }

        ////            fs.Flush(flushToDisk: true);
        ////        }
        ////    }

        ////    Action<bool> dispose = null;
        ////    if (options == null)
        ////    {
        ////        dispose = d => { using (var store = IsolatedStorageFile.GetUserStoreForApplication()) store.DeleteFile(path); };
        ////    }

        ////    switch (photoResult.TaskResult)
        ////    {
        ////        case TaskResult.OK:
        ////            photoResult.ChosenPhoto.Position = pos;
        ////            tcs.SetResult(new MediaFile(path, () => photoResult.ChosenPhoto, dispose: dispose));
        ////            break;

        ////        case TaskResult.None:
        ////            photoResult.ChosenPhoto.Dispose();
        ////            if (photoResult.Error != null)
        ////            {
        ////                tcs.SetResult(null);
        ////            }

        ////            break;
        ////    }
        ////}
    }
}