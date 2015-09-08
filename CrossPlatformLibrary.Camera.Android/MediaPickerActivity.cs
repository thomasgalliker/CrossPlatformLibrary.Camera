using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Runtime;

using CrossPlatformLibrary.Utils;

using Environment = Android.OS.Environment;
using FileNotFoundException = Java.IO.FileNotFoundException;
using Uri = Android.Net.Uri;

namespace CrossPlatformLibrary.Camera
{
    [Activity]
    [Preserve(AllMembers = true)]
    public class MediaPickerActivity : Activity
    {
        internal const string ExtraPath = "path";
        internal const string ExtraLocation = "location";
        internal const string ExtraType = "type";
        internal const string ExtraId = "id";
        internal const string ExtraAction = "action";
        internal const string ExtraTasked = "tasked";

        internal static event EventHandler<MediaPickedEventArgs> MediaPicked;

        private int id;
        private string title;
        private string description;
        private string type;

        /// <summary>
        ///     The user's destination path.
        /// </summary>
        private Uri path;

        private bool isPhoto;
        private string action;

        private int seconds;
        private VideoQuality quality;

        private bool tasked;

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean("ran", true);
            outState.PutString(MediaStore.MediaColumns.Title, this.title);
            outState.PutString(MediaStore.Images.ImageColumns.Description, this.description);
            outState.PutInt(ExtraId, this.id);
            outState.PutString(ExtraType, this.type);
            outState.PutString(ExtraAction, this.action);
            outState.PutInt(MediaStore.ExtraDurationLimit, this.seconds);
            outState.PutInt(MediaStore.ExtraVideoQuality, (int)this.quality);
            outState.PutBoolean(ExtraTasked, this.tasked);

            if (this.path != null)
            {
                outState.PutString(ExtraPath, this.path.Path);
            }

            base.OnSaveInstanceState(outState);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Bundle b = (savedInstanceState ?? this.Intent.Extras);

            bool ran = b.GetBoolean("ran", defaultValue: false);

            this.title = b.GetString(MediaStore.MediaColumns.Title);
            this.description = b.GetString(MediaStore.Images.ImageColumns.Description);

            this.tasked = b.GetBoolean(ExtraTasked);
            this.id = b.GetInt(ExtraId, 0);
            this.type = b.GetString(ExtraType);
            if (this.type == "image/*")
            {
                this.isPhoto = true;
            }

            this.action = b.GetString(ExtraAction);
            Intent pickIntent = null;
            try
            {
                pickIntent = new Intent(this.action);
                if (this.action == Intent.ActionPick)
                {
                    pickIntent.SetType(this.type);
                }
                else
                {
                    if (!this.isPhoto)
                    {
                        this.seconds = b.GetInt(MediaStore.ExtraDurationLimit, 0);
                        if (this.seconds != 0)
                        {
                            pickIntent.PutExtra(MediaStore.ExtraDurationLimit, this.seconds);
                        }
                    }

                    this.quality = (VideoQuality)b.GetInt(MediaStore.ExtraVideoQuality, (int)VideoQuality.High);
                    pickIntent.PutExtra(MediaStore.ExtraVideoQuality, GetVideoQuality(this.quality));

                    if (!ran)
                    {
                        this.path = GetOutputMediaFile(this, b.GetString(ExtraPath), this.title, this.isPhoto);

                        this.Touch();
                        pickIntent.PutExtra(MediaStore.ExtraOutput, this.path);
                    }
                    else
                    {
                        this.path = Uri.Parse(b.GetString(ExtraPath));
                    }
                }

                if (!ran)
                {
                    this.StartActivityForResult(pickIntent, this.id);
                }
            }
            catch (Exception ex)
            {
                OnMediaPicked(new MediaPickedEventArgs(this.id, ex));
            }
            finally
            {
                if (pickIntent != null)
                {
                    pickIntent.Dispose();
                }
            }
        }

        private void Touch()
        {
            if (this.path.Scheme != "file")
            {
                return;
            }

            File.Create(GetLocalPath(this.path)).Close();
        }

        internal static Task<MediaPickedEventArgs> GetMediaFileAsync(Context context, int requestCode, string action, bool isPhoto, ref Uri path, Uri data)
        {
            Task<Tuple<string, bool>> pathFuture;

            string originalPath = null;

            if (action != Intent.ActionPick)
            {
                originalPath = path.Path;

                // Not all camera apps respect EXTRA_OUTPUT, some will instead
                // return a content or file uri from data.
                if (data != null && data.Path != originalPath)
                {
                    originalPath = data.ToString();
                    string currentPath = path.Path;
                    pathFuture = TryMoveFileAsync(context, data, path, isPhoto).ContinueWith(t => new Tuple<string, bool>(t.Result ? currentPath : null, false));
                }
                else
                {
                    pathFuture = TaskFromResult(new Tuple<string, bool>(path.Path, false));
                }
            }
            else if (data != null)
            {
                originalPath = data.ToString();
                path = data;
                pathFuture = GetFileForUriAsync(context, path, isPhoto);
            }
            else
            {
                pathFuture = TaskFromResult<Tuple<string, bool>>(null);
            }

            return pathFuture.ContinueWith(
                t =>
                    {
                        string resultPath = t.Result.Item1;
                        if (resultPath != null && File.Exists(t.Result.Item1))
                        {
                            var mf = new MediaFile(
                                resultPath,
                                () => { return File.OpenRead(resultPath); },
                                deletePathOnDispose: t.Result.Item2,
                                dispose: (dis) =>
                                    {
                                        if (t.Result.Item2)
                                        {
                                            try
                                            {
                                                File.Delete(t.Result.Item1);
                                                // We don't really care if this explodes for a normal IO reason.
                                            }
                                            catch (UnauthorizedAccessException)
                                            {
                                            }
                                            catch (DirectoryNotFoundException)
                                            {
                                            }
                                            catch (IOException)
                                            {
                                            }
                                        }
                                    });
                            return new MediaPickedEventArgs(requestCode, false, mf);
                        }
                        else
                        {
                            return new MediaPickedEventArgs(requestCode, new MediaFileNotFoundException(originalPath));
                        }
                    });
        }

        /// <summary>
        ///     OnActivity Result
        /// </summary>
        /// <param name="requestCode"></param>
        /// <param name="resultCode"></param>
        /// <param name="data"></param>
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (this.tasked)
            {
                Task<MediaPickedEventArgs> future;

                if (resultCode == Result.Canceled)
                {
                    future = TaskFromResult(new MediaPickedEventArgs(requestCode, isCanceled: true));
                }
                else
                {
                    future = GetMediaFileAsync(this, requestCode, this.action, this.isPhoto, ref this.path, (data != null) ? data.Data : null);
                }

                this.Finish();

                future.ContinueWith(t => OnMediaPicked(t.Result));
            }
            else
            {
                if (resultCode == Result.Canceled)
                {
                    this.SetResult(Result.Canceled);
                }
                else
                {
                    Intent resultData = new Intent();
                    resultData.PutExtra("MediaFile", (data != null) ? data.Data : null);
                    resultData.PutExtra("path", this.path);
                    resultData.PutExtra("isPhoto", this.isPhoto);
                    resultData.PutExtra("action", this.action);

                    this.SetResult(Result.Ok, resultData);
                }

                this.Finish();
            }
        }

        private static Task<bool> TryMoveFileAsync(Context context, Uri url, Uri path, bool isPhoto)
        {
            string moveTo = GetLocalPath(path);
            return GetFileForUriAsync(context, url, isPhoto).ContinueWith(
                t =>
                    {
                        if (t.Result.Item1 == null)
                        {
                            return false;
                        }

                        File.Delete(moveTo);
                        File.Move(t.Result.Item1, moveTo);

                        if (url.Scheme == "content")
                        {
                            context.ContentResolver.Delete(url, null, null);
                        }

                        return true;
                    },
                TaskScheduler.Default);
        }

        private static int GetVideoQuality(VideoQuality videoQuality)
        {
            switch (videoQuality)
            {
                case VideoQuality.Medium:
                case VideoQuality.High:
                    return 1;

                default:
                    return 0;
            }
        }

        private static string GetUniquePath(string folder, string name, bool isPhoto)
        {
            string ext = Path.GetExtension(name);
            if (ext == String.Empty)
            {
                ext = ((isPhoto) ? ".jpg" : ".mp4");
            }

            name = Path.GetFileNameWithoutExtension(name);

            string nname = name + ext;
            int i = 1;
            while (File.Exists(Path.Combine(folder, nname)))
            {
                nname = name + "_" + (i++) + ext;
            }

            return Path.Combine(folder, nname);
        }

        private static Uri GetOutputMediaFile(Context context, string subdir, string name, bool isPhoto)
        {
            subdir = subdir ?? String.Empty;

            if (String.IsNullOrWhiteSpace(name))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (isPhoto)
                {
                    name = "IMG_" + timestamp + ".jpg";
                }
                else
                {
                    name = "VID_" + timestamp + ".mp4";
                }
            }

            string mediaType = (isPhoto) ? Environment.DirectoryPictures : Environment.DirectoryMovies;
            using (Java.IO.File mediaStorageDir = new Java.IO.File(context.GetExternalFilesDir(mediaType), subdir))
            {
                if (!mediaStorageDir.Exists())
                {
                    if (!mediaStorageDir.Mkdirs())
                    {
                        throw new IOException("Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?");
                    }

                    // Ensure this media doesn't show up in gallery apps
                    using (Java.IO.File nomedia = new Java.IO.File(mediaStorageDir, ".nomedia")) nomedia.CreateNewFile();
                }

                return Uri.FromFile(new Java.IO.File(GetUniquePath(mediaStorageDir.Path, name, isPhoto)));
            }
        }

        internal static Task<Tuple<string, bool>> GetFileForUriAsync(Context context, Uri uri, bool isPhoto)
        {
            var tcs = new TaskCompletionSource<Tuple<string, bool>>();

            if (uri.Scheme == "file")
            {
                tcs.SetResult(new Tuple<string, bool>(new System.Uri(uri.ToString()).LocalPath, false));
            }
            else if (uri.Scheme == "content")
            {
                Task.Factory.StartNew(
                    () =>
                        {
                            ICursor cursor = null;
                            try
                            {
                                cursor = context.ContentResolver.Query(uri, null, null, null, null);
                                if (cursor == null || !cursor.MoveToNext())
                                {
                                    tcs.SetResult(new Tuple<string, bool>(null, false));
                                }
                                else
                                {
                                    int column = cursor.GetColumnIndex(MediaStore.MediaColumns.Data);
                                    string contentPath = null;

                                    if (column != -1)
                                    {
                                        contentPath = cursor.GetString(column);
                                    }

                                    bool copied = false;

                                    // If they don't follow the "rules", try to copy the file locally
                                    if (contentPath == null || !contentPath.StartsWith("file"))
                                    {
                                        copied = true;
                                        Uri outputPath = GetOutputMediaFile(context, "temp", null, isPhoto);

                                        try
                                        {
                                            using (Stream input = context.ContentResolver.OpenInputStream(uri)) using (Stream output = File.Create(outputPath.Path)) input.CopyTo(output);

                                            contentPath = outputPath.Path;
                                        }
                                        catch (FileNotFoundException)
                                        {
                                            // If there's no data associated with the uri, we don't know
                                            // how to open this. contentPath will be null which will trigger
                                            // MediaFileNotFoundException.
                                        }
                                    }

                                    tcs.SetResult(new Tuple<string, bool>(contentPath, copied));
                                }
                            }
                            finally
                            {
                                if (cursor != null)
                                {
                                    cursor.Close();
                                    cursor.Dispose();
                                }
                            }
                        },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default);
            }
            else
            {
                tcs.SetResult(new Tuple<string, bool>(null, false));
            }

            return tcs.Task;
        }

        private static string GetLocalPath(Uri uri)
        {
            return new System.Uri(uri.ToString()).LocalPath;
        }

        private static Task<T> TaskFromResult<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        private static void OnMediaPicked(MediaPickedEventArgs e)
        {
            var picked = MediaPicked;
            if (picked != null)
            {
                picked(null, e);
            }
        }
    }

    internal class MediaPickedEventArgs : EventArgs
    {
        public MediaPickedEventArgs(int id, Exception error)
        {
            Guard.ArgumentNotNull(() => error);

            this.RequestId = id;
            this.Error = error;
        }

        public MediaPickedEventArgs(int id, bool isCanceled, MediaFile media = null)
        {
            this.RequestId = id;
            this.IsCanceled = isCanceled;
            if (!this.IsCanceled)
            {
                Guard.ArgumentNotNull(() => media);
            }

            this.Media = media;
        }

        public int RequestId { get; private set; }

        public bool IsCanceled { get; private set; }

        public Exception Error { get; private set; }

        public MediaFile Media { get; private set; }

        public Task<MediaFile> ToTask()
        {
            var tcs = new TaskCompletionSource<MediaFile>();

            if (this.IsCanceled)
            {
                tcs.SetResult(null);
            }
            else if (this.Error != null)
            {
                tcs.SetResult(null);
            }
            else
            {
                tcs.SetResult(this.Media);
            }

            return tcs.Task;
        }
    }
}