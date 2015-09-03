using System;

using Android.Content;
using Android.Util;
using Android.Views;

namespace Camera2Basic
{
    public class AutoFitTextureView : TextureView
    {
        private int mRatioWidth = 0;
        private int mRatioHeight = 0;

        public AutoFitTextureView(Context context)
            : this(context, null)
        {
        }

        public AutoFitTextureView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public AutoFitTextureView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
        }

        public void SetAspectRatio(int width, int height)
        {
            if (width == 0 || height == 0)
            {
                throw new ArgumentException("Size cannot be negative.");
            }
            this.mRatioWidth = width;
            this.mRatioHeight = height;
            this.RequestLayout();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasureSpec.GetSize(heightMeasureSpec);
            if (0 == this.mRatioWidth || 0 == this.mRatioHeight)
            {
                this.SetMeasuredDimension(width, height);
            }
            else
            {
                if (width < (float)height * this.mRatioWidth / (float)this.mRatioHeight)
                {
                    this.SetMeasuredDimension(width, width * this.mRatioHeight / this.mRatioWidth);
                }
                else
                {
                    this.SetMeasuredDimension(height * this.mRatioWidth / this.mRatioHeight, height);
                }
            }
        }
    }
}