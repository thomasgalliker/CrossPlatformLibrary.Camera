//
//  Copyright 2012, Xamarin Inc.
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

using System;
using System.IO;
using System.Runtime.InteropServices;

#if ! __UNIFIED__
using MonoTouch.Foundation;
#else
using UIKit;
using Foundation;
#endif

namespace CrossPlatformLibrary.Camera
{
    class NSDataStream : Stream // TODO GATH: Move to base library
    {
        NSData data;
        uint pos;

        public NSDataStream(NSData data)
        {
            this.data = data;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.data != null)
            {
                this.data.Dispose();
                this.data = null;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.pos >= this.data.Length)
            {
                return 0;
            }
            else
            {
#if ! __UNIFIED__
                var len = (int)Math.Min(count, this.data.Length - this.pos);
#else
				var len = (int)Math.Min (count, (double)(data.Length - pos));
#endif

                Marshal.Copy(new IntPtr(this.data.Bytes.ToInt64() + this.pos), buffer, offset, len);
                this.pos += (uint)len;
                return len;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                // override does not allow nint
#if ! __UNIFIED__
                return this.data.Length;
#else
				return (long) data.Length;
#endif
            }
        }

        public override long Position
        {
            get
            {
                return this.pos;
            }
            set
            {
            }
        }
    }
}

