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
    public class Parallelogram
    {
        public static bool Transform(ref Bitmap bmp, float def, ParallelogramDeformation pd, Color bkColor)
        {
            if (def == 0) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            PointF ul = new PointF(0, 0);
            PointF ur = new PointF(w, 0);
            PointF ll = new PointF(0, h);

            Bitmap tmp = null;

            switch (pd)
            {
                case ParallelogramDeformation.pdHorizontal:
                    if (bkColor == Color.Transparent)
                        tmp = new Bitmap((int)(w + Math.Abs(def) + 0.5), h,
                                                   PixelFormat.Format32bppArgb);
                    else
                        tmp = new Bitmap((int)(w + Math.Abs(def) + 0.5), h,
                                                   PixelFormat.Format24bppRgb);
                    if (def > 0)
                    { ul.X += def; ur.X += def; }
                    else
                    { ll.X -= def; }
                    break;

                case ParallelogramDeformation.pdVertical:
                    if (bkColor == Color.Transparent)
                        tmp = new Bitmap(w, (int)(h + Math.Abs(def) + 0.5),
                                                     PixelFormat.Format32bppArgb);
                    else
                        tmp = new Bitmap(w, (int)(h + Math.Abs(def) + 0.5),
                                                     PixelFormat.Format24bppRgb);
                    if (def > 0)
                    { ur.Y += def; }
                    else
                    { ul.Y -= def; ll.Y -= def; }
                    break;

            }

            PointF[] dstP = { ul, ur, ll };

            Graphics g = Graphics.FromImage(tmp);
            if (!(bkColor == Color.Transparent)) g.Clear(bkColor);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmp, dstP);
            g.Dispose();

            bmp.Dispose();

            bmp = tmp;

            return true;
        }

        public enum ParallelogramDeformation { pdHorizontal, pdVertical }
    }
}
