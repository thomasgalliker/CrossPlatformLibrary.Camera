namespace CrossPlatformLibrary.Camera
{
    public class PickMediaOptions
    {
        const int MaxPhotoSize = 100;
        int customPhotoSize = 100;

        const int MaxQuality = 100;
        int quality = 100;

        public PickMediaOptions()
        {
            this.PhotoSize = PhotoSize.Full;
        }

        /// <summary>
        ///     Gets or sets the size of the photo.
        /// </summary>
        /// <value>The size of the photo.</value>
        public PhotoSize PhotoSize { get; set; }

        /// <summary>
        ///     The custom photo size to use, 100 full size (same as Full),
        ///     and 1 being smallest size at 1% of original
        ///     Default is 100
        /// </summary>
        public int CustomPhotoSize
        {
            get
            {
                return this.customPhotoSize;
            }
            set
            {
                if (value > MaxPhotoSize)
                {
                    this.customPhotoSize = MaxPhotoSize;
                }
                else if (value < 1)
                {
                    this.customPhotoSize = 1;
                }
                else
                {
                    this.customPhotoSize = value;
                }
            }
        }

        /// <summary>
        ///     The compression quality to use, 0 is the maximum compression (worse quality),
        ///     and 100 minimum compression (best quality)
        ///     Default is 100
        /// </summary>
        public int CompressionQuality
        {
            get
            {
                return this.quality;
            }
            set
            {
                if (value > MaxQuality)
                {
                    this.quality = MaxQuality;
                }
                else if (value < 0)
                {
                    this.quality = 0;
                }
                else
                {
                    this.quality = value;
                }
            }
        }
    }
}