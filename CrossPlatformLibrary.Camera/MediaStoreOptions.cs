//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.IO;


namespace CrossPlatformLibrary.Camera
{
    /// <summary>
    /// Media Options
    /// </summary>
    public class StoreMediaOptions
    {
        /// <summary>
        /// 
        /// </summary>
        protected StoreMediaOptions()
        {
        }

        /// <summary>
        /// Directory name
        /// </summary>
        public string Directory
        {
            get;
            set;
        }

        /// <summary>
        /// File name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        public void VerifyOptions()
        {
            //if (!Enum.IsDefined (typeof(MediaFileStoreLocation), options.Location))
            //    throw new ArgumentException ("options.Location is not a member of MediaFileStoreLocation");
            //if (options.Location == MediaFileStoreLocation.Local)
            //{
            //if (String.IsNullOrWhiteSpace (options.Directory))
            //	throw new ArgumentNullException ("options", "For local storage, options.Directory must be set");
            if (Path.IsPathRooted(this.Directory))
            {
                throw new ArgumentException("options.Directory must be a relative path", "options");
            }
            //}
        }
    }

    public class StoreCameraMediaOptions
      : StoreMediaOptions
    {
        public CameraFacingDirection DefaultCamera { get; set; }
    }

    public enum VideoQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    public class StoreVideoOptions : StoreCameraMediaOptions
    {
        public StoreVideoOptions()
        {
            this.Quality = VideoQuality.High;
            this.DesiredLength = TimeSpan.FromMinutes(10);
        }

        public TimeSpan DesiredLength
        {
            get;
            set;
        }

        public VideoQuality Quality
        {
            get;
            set;
        }
    }
}