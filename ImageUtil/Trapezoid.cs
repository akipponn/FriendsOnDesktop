using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Drawing.Imaging;

namespace ImageUtils
{
    public static class Trapezoid
    {
        public static bool transform(ref Bitmap bmp, TrapezoidSmallSide ss, double percent, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Console.WriteLine("* PIXEL FORMAT ERROR" + bmp.PixelFormat);
                return false;
            }
            if ((percent > 100d) | (percent < 1d))
            {
                Console.WriteLine("* PERCENT ERROR" + percent);
                return false;
            }

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 3; // margin for interpolations

            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2,
                                                PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            Bitmap tmp2 = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            percent = percent / 100d;
            double cw = (double)w / 2d;
            double ch = (double)h / 2d;
            double p = 1d;
            double xx, yy;

            BmpProc32 src = new BmpProc32(tmp);
            BmpProc32 dst = new BmpProc32(tmp2);

            switch (ss)
            {

                case TrapezoidSmallSide.ssUpper:
                    for (int y = 0; y < h; y++)
                    {
                        p = (1d - percent) * (double)y / (double)(h - 1) + percent;

                        for (int x = 0; x < w; x++)
                        {
                            xx = cw + ((double)x - cw) / p;
                            yy = y;

                            if (rct.Contains(new PointF((float)xx, (float)yy)))
                            {
                                xx = xx + mg;
                                yy = yy + mg;

                                ImageUtils.intBicubic32(dst, src, x, y, xx, yy);
                            }
                        }
                    }
                    break;

                case TrapezoidSmallSide.ssLower:
                    for (int y = 0; y < h; y++)
                    {
                        p = (percent - 1d) * (double)y / (double)(h - 1) + 1d;

                        for (int x = 0; x < w; x++)
                        {
                            xx = cw + ((double)x - cw) / p;
                            yy = y;

                            if (rct.Contains(new PointF((float)xx, (float)yy)))
                            {
                                xx = xx + mg;
                                yy = yy + mg;

                                ImageUtils.intBicubic32(dst, src, x, y, xx, yy);
                            }
                        }
                    }
                    break;

                case TrapezoidSmallSide.ssLeft:
                    for (int x = 0; x < w; x++)
                    {
                        p = (1d - percent) * (double)x / (double)(w - 1) + percent;

                        for (int y = 0; y < h; y++)
                        {
                            xx = x;
                            yy = ch + ((double)y - ch) / p;

                            if (rct.Contains(new PointF((float)xx, (float)yy)))
                            {
                                xx = xx + mg;
                                yy = yy + mg;

                                ImageUtils.intBicubic32(dst, src, x, y, xx, yy);
                            }
                        }
                    }
                    break;

                case TrapezoidSmallSide.ssRight:
                    for (int x = 0; x < w; x++)
                    {
                        p = (percent - 1d) * (double)x / (double)(w - 1) + 1d;

                        for (int y = 0; y < h; y++)
                        {
                            xx = x;
                            yy = ch + ((double)y - ch) / p;

                            if (rct.Contains(new PointF((float)xx, (float)yy)))
                            {
                                xx = xx + mg;
                                yy = yy + mg;

                                ImageUtils.intBicubic32(dst, src, x, y, xx, yy);
                            }
                        }
                    }
                    break;

            }

            ImageUtils.CallDispose(dst, src, tmp);

            if (bkColor == Color.Transparent)
            {
                bmp.Dispose();
                bmp = tmp2;
            }
            else
            {
                g = Graphics.FromImage(bmp);
                g.Clear(bkColor);
                g.DrawImageUnscaled(tmp2, 0, 0);
                g.Dispose();
                tmp2.Dispose();
            }

            return true;
        }

        // 上の辺だけでとりあえず OK
        public static bool transform(ref Bitmap bmp, double percent, int centerX, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb) return false;

            if ((percent > 100d) | (percent < 1d)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 3; // margin for interpolations (= 補完)

            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);  // -1 指定できるのか・・・
            if (Math.Abs((double)w * percent / 100d / 2d) < Math.Abs(centerX))
            {
                double d = Math.Abs( (double)centerX - (double)w * percent / 100d / 2d);
                if (centerX > 0)    // x 軸正方向へのずれ
                {  
                    rct = new RectangleF(-1, -1, w + (float)d + 1, h + 1);
                }
                else
                {
                    rct = new RectangleF(-1 -(float)d, -1, w + 1, h + 1);
                }
            }

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            Bitmap tmp2 = new Bitmap( (int)rct.Width, h, PixelFormat.Format32bppArgb);

            percent = percent / 100d;
            double cw = (double)w / 2d;
            double ch = (double)h / 2d;
            double p = 1d;
            double xx, yy;

            BmpProc32 src = new BmpProc32(tmp);
            BmpProc32 dst = new BmpProc32(tmp2);


            for (int y = 0; y < h; y++)
            {
                p = (1d - percent) * (double)y / (double)(h - 1) + percent;

                for (int x = 0; x < w; x++)
                {
                    xx = cw + ((double)x - cw - centerX * ((double)(h - y) / (double)h)) / p;
                    yy = y;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        ImageUtils.intBicubic32(dst, src, x, y, xx, yy);
                    }
                }
            }

            Console.WriteLine("src size: {0},{1} / dest size {2},{3}", tmp.Width, tmp.Height, tmp2.Width, tmp2.Height);

            ImageUtils.CallDispose(dst, src, tmp);

            if (bkColor == Color.Transparent)
            {
                bmp.Dispose();
                bmp = tmp2;
            }
            else
            {
                g = Graphics.FromImage(bmp);
                g.Clear(bkColor);
                g.DrawImageUnscaled(tmp2, 0, 0);
                g.Dispose();
                tmp2.Dispose();
            }

            return true;
        }

        public enum TrapezoidSmallSide { ssUpper, ssLower, ssLeft, ssRight }
    }
}
