using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageUtils
{
    public class Ellipse
    {
        public static bool Transform(ref Bitmap bmp, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 3; // margin for interpolations

            int q = Math.Max(w, h);

            RectangleF rct = new RectangleF(-1, -1, q + 1, q + 1);

            Bitmap tmp1 = new Bitmap(q + mg * 2, q + mg * 2, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(tmp1);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(bkColor);
            g.DrawImage(bmp, mg, mg, q, q);
            g.Dispose();

            Bitmap tmp2 = new Bitmap(q, q, PixelFormat.Format24bppRgb);
            g = Graphics.FromImage(tmp2);
            g.Clear(bkColor);
            g.Dispose();

            double l, xx, yy;
            double r = (double)q / 2d;

            BmpProc24 src = new BmpProc24(tmp1);
            BmpProc24 dst = new BmpProc24(tmp2);

            for (int y = 1; y < q; y++)
                for (int x = 0; x < q; x++)
                {
                    l = Math.Sqrt(2d * r * y - y * y);
                    if (l == 0) xx = 0; else xx = r * (x - r) / l + r;
                    yy = y;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        ImageUtils.intBicubic(dst, src, x, y, xx, yy);
                    }
                }

            ImageUtils.CallDispose(dst, src, tmp1);

            g = Graphics.FromImage(bmp);
            g.Clear(bkColor);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(tmp2, 0, 0, w, h);
            g.Dispose();

            tmp2.Dispose();

            return true;
        }
    }
}
