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
    public static class Resize
    {
        public static Bitmap resizeImage(Bitmap src, int maxWidth, int maxHeight)
        {
            double hScale = (double)maxHeight / (double)src.Height;
            double wScale = (double)maxWidth / (double)src.Width;

            int destHeight = src.Height;
            int destWidth = src.Width;
            if (hScale > wScale)
            {
                destWidth = (int)(src.Width * wScale);
                destHeight = (int)(src.Height * wScale);
            }
            else
            {
                destWidth = (int)(src.Width * hScale);
                destHeight = (int)(src.Height * hScale);
            }

            Bitmap dest = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(dest);
            g.DrawImage(src, 0, 0, destWidth, destHeight);
            return dest;
        }

        public static Image resizeImageWithScale(Image src, double scale)
        {
            return resizeImageWithScale(src, scale, scale);
        }

        public static Bitmap resizeImageWithScale(Image src, double scaleWidth, double scaleHeight)
        {
            try
            {
                int destWidth = (int)(src.Width * scaleWidth);
                int destHeight = (int)(src.Height * scaleHeight);
                Bitmap dest = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(dest);
                g.DrawImage(src, 0, 0, destWidth, destHeight);
                return dest;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return new Bitmap(1, 1);
            }
        }

        public static Bitmap resizeImageWithScale( Bitmap src, double scaleWidth, double scaleHeight)
        {
            try
            {
                int destWidth = (int)(src.Width * scaleWidth);
                int destHeight = (int)(src.Height * scaleHeight);

                Bitmap dest = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(dest);
                g.DrawImage(src, 0, 0, destWidth, destHeight);
                return dest;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return new Bitmap(1, 1);
            }
        }
    }
}
