using System;

using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;

using Tracing;

namespace Camera2Basic
{
    [Preserve(AllMembers = true)]
    public class AutoFitTextureView : TextureView
    {
        private int mRatioWidth = 0;
        private int mRatioHeight = 0;
        private readonly ITracer tracer;

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
            this.tracer = Tracer.Create(this);
        }

        /**
        * Sets the aspect ratio for this view. The size of the view will be measured based on the ratio
        * calculated from the parameters. Note that the actual sizes of parameters don't matter, that
        * is, calling SetAspectRatio(2, 3) and SetAspectRatio(4, 6) make the same result.
        */
        public void SetAspectRatio(int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("Size cannot be negative.");
            }
            this.mRatioWidth = width;
            this.mRatioHeight = height;
            this.RequestLayout();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            ////this.tracer.Debug("OnMeasure: widthMeasureSpec={0}, heightMeasureSpec={1}", widthMeasureSpec, heightMeasureSpec);

            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasureSpec.GetSize(heightMeasureSpec);

            ////this.tracer.Debug("OnMeasure: width={0}, height={1}", width, height);

            if (0 == this.mRatioWidth || 0 == this.mRatioHeight)
            {
                ////this.tracer.Debug("SetMeasuredDimension: width={0}, height={1}", width, height);
                this.SetMeasuredDimension(width, height);
            }
            else
            {
                if (width < (float)height * this.mRatioWidth / (float)this.mRatioHeight)
                {
                    var w = width;
                    //var h = width * this.mRatioHeight / this.mRatioWidth;
                    var h = height;
                    ////this.tracer.Debug("SetMeasuredDimension: width={0}, height={1}", w, h);
                    this.SetMeasuredDimension(w, h);
                    this.ConfigureTransform(w, h, this.mRatioWidth, this.mRatioHeight);
                }
                else
                {
                    //var w = height * this.mRatioWidth / this.mRatioHeight;
                    var w = width;
                    var h = height;
                    ////this.tracer.Debug("SetMeasuredDimension: width={0}, height={1}", w, h);
                    this.SetMeasuredDimension(w, h);
                    this.ConfigureTransform(w, h, this.mRatioWidth, this.mRatioHeight);
                }
            }
        }

        private void ConfigureTransform(int viewWidth, int viewHeight, int previewWidth, int previewHeight)
        {
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, previewWidth, previewHeight);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();

            bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
            matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
            float verticalScale = (float)viewHeight / previewHeight;
            //float horizontalScale = (float)viewWidth / previewWidth;
            //float scale = Math.Min(verticalScale, horizontalScale);

            ////this.tracer.Debug("ConfigureTransform: verticalScale={0}", verticalScale);
            ////this.tracer.Debug("ConfigureTransform: horizontalScale={0}", horizontalScale);
            ////this.tracer.Debug("ConfigureTransform: scale={0}", scale);
            ////this.tracer.Debug("ConfigureTransform: centerX={0}", centerX);
            ////this.tracer.Debug("ConfigureTransform: centerY={0}", centerY);

            matrix.PostScale(verticalScale, verticalScale, centerX, centerY);
            //matrix.PostRotate(90 * ((int)this.Rotation - 2), centerX, centerY);

            this.SetTransform(matrix);
        }
    }
}