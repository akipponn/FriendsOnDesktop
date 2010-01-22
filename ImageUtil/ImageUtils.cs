/*
 *  Image Processing Utilities for C#2.0 (VC# 2005)
 *  
 *         Copyright junki, Jan, 2006 -
 * 
 *  http://junki.main.jp/
 *  http://code.junki.main.jp/
 * 
 *  how to use : see http://junki.main.jp/csgr/006Library1.htm
 * 
 * This library is free for any non commercial usage. In the case of
 * modifying and/or redistributing the code, it's obligate to retain
 * the original copyright notice.
 * 
 */

using System;
using System.Drawing;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ImageUtils
{
    public static class ImageUtils
    {

        public static byte AdjustByte(int value)
        {
            if (value < 0) return 0; else if (value > 255) return 255;
            return (byte)value;
        }

        public static byte AdjustByte(double value)
        {
            if (value < 0) return 0; else if (value > 255) return 255;
            return (byte)value;
        }

        public static void CallDispose(params IDisposable[] obj)
        {
            if (obj.Length != 0)
            {
                for (int i = 0; i < obj.Length; i++)
                    obj[i].Dispose();
            }
        }

        public static Bitmap GetPalette(Bitmap bmp)
        {
            const int r = 10; // rect size of entry
            const int m = 3;  // margin

            ColorPalette pal = bmp.Palette;
            Bitmap palb = new Bitmap(r * 16 + m * 2 + 1, r * 16 + m * 2 + 1, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(palb);
            g.Clear(Color.FloralWhite);

            SolidBrush br = new SolidBrush(Color.AliceBlue);
            Rectangle rect = new Rectangle(0, 0, r, r);

            for (int i = 0; i < pal.Entries.Length; i++)
            {
                br.Color = pal.Entries[i];
                rect.X = m + (i % 16) * r;
                rect.Y = m + (i / 16) * r;
                g.FillRectangle(br, rect);
            }

            br.Dispose();

            Pen p = new Pen(Color.Silver, 1f);
            for (int y = 0; y < 17; y++)
                g.DrawLine(p, m, m + r * y, m + r * 16, m + r * y);
            for (int x = 0; x < 17; x++)
                g.DrawLine(p, m + r * x, m, m + r * x, m + r * 16);
            p.Dispose();

            g.Dispose();

            return palb;
        }

        public static bool BrightnessHistogram(Bitmap bmp, out Bitmap bmpHist)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                bmpHist = null;
                return false;
            }

            int w = bmp.Width;
            int h = bmp.Height;

            int[] hist = new int[256];
            hist.Initialize();

            int ir, indx;

            BmpProc24 bd = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ir = bd.IndexR(x, y);
                    indx = (int)(bd[ir] * 0.299 + bd[ir - 1] * 0.587 +
                                                           bd[ir - 2] * 0.114);
                    hist[indx]++;
                }

            bd.Dispose();

            int max = -1;
            for (int i = 0; i < 256; i++)
                if (hist[i] > max) max = hist[i];

            for (int i = 0; i < 256; i++)
                hist[i] = hist[i] * 140 / max;

            bmpHist = new Bitmap(275, 160, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(bmpHist);
            g.Clear(Color.AntiqueWhite);

            Pen pen = new Pen(Color.Gray, 1F);

            for (int i = 0; i < 256; i++)
                g.DrawLine(pen, 10 + i, 150, 10 + i, 150 - hist[i]);

            pen.Color = Color.Black;

            g.DrawLine(pen, 10, 150, 10, 10);

            for (int i = 0; i <= 20; i++)
                if ((i % 2) == 0)
                    g.DrawLine(pen, 10, 150 - 7 * i, 6, 150 - 7 * i);
                else
                    g.DrawLine(pen, 10, 150 - 7 * i, 8, 150 - 7 * i);

            g.DrawLine(pen, 10, 150, 10 + 255, 150);

            for (int i = 0; i <= 51; i++)
                if ((i % 2) == 0)
                    g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 154);
                else
                    g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 152);

            g.Dispose();

            return true;
        }

        public static bool SetGrayPalette(Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                return false;

            ColorPalette pal = bmp.Palette;

            for (int i = 0; i < pal.Entries.Length; i++)
            {
                pal.Entries[i] = Color.FromArgb(i, i, i);
            }

            bmp.Palette = pal;

            return true;
        }

        public static bool GrayScale(ref Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

            if (!SetGrayPalette(tmp))
            {
                tmp.Dispose();
                return false;
            }

            int srcR;

            BmpProc24 src = new BmpProc24(bmp);
            BmpProc8 dst = new BmpProc8(tmp);

            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                {
                    srcR = src.IndexR(x, y);       // current

                    dst[x, y] = (byte)(src[srcR] * 0.299 +
                                       src[srcR - 1] * 0.587 + src[srcR - 2] * 0.114);

                }

            CallDispose(dst, src, bmp);

            bmp = tmp;

            return true;
        }

        public delegate void P2PFunc(ref byte r, ref byte g, ref byte b, 
                                                        params double[] param);

        public static bool P2PCore(ref Bitmap bmp, P2PFunc func, params double[] param)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            byte r, g, b;
            int ir;

            BmpProc24 bd = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ir = bd.IndexR(x, y);

                    r = bd[ir]; g = bd[ir - 1]; b = bd[ir - 2];

                    func(ref r, ref g, ref b, param);

                    bd[ir] = r; bd[ir - 1] = g; bd[ir - 2] = b;
                }

            bd.Dispose();

            return true;
        }

        private static void InvertFunc(ref byte r, ref byte g, ref byte b, 
                                                                 params double[] param)
        {
            r = (byte)(255 - r);
            g = (byte)(255 - g);
            b = (byte)(255 - b);
        }

        public static bool Invert(ref Bitmap bmp)
        {
            P2PFunc func = new P2PFunc(InvertFunc);

            return P2PCore(ref bmp, func);
        }

        private static void GrayScale24Func(ref byte r, ref byte g, ref byte b,
                                                                  params double[] param)
        {
            g = b = r = (byte)(r * 0.299 + g * 0.587 + b * 0.114);
        }

        public static bool GrayScale24(ref Bitmap bmp)
        {
            P2PFunc func = new P2PFunc(GrayScale24Func);

            return P2PCore(ref bmp, func);
        }

        public static bool TwoValued1(Bitmap bmp24, int threshold, out Bitmap bmp1)
        {
            bmp1 = null;

            if (bmp24.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp24.Width;
            int h = bmp24.Height;

            bmp1 = new Bitmap(w, h, PixelFormat.Format1bppIndexed);

            int srcR;

            BmpProc24 src = new BmpProc24(bmp24);
            BmpProc1 dst = new BmpProc1(bmp1);

            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                {
                    srcR = src.IndexR(x, y);       // current

                    dst[x, y] = ((src[srcR] * 0.299 +
                          src[srcR - 1] * 0.587 + src[srcR - 2] * 0.114) > threshold);

                }

            CallDispose(dst, src);

            return true;
        }

        public static bool TwoValued2(Bitmap bmp24, Color BKcolor, out Bitmap bmp1)
        {
            bmp1 = null;

            if (bmp24.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp24.Width;
            int h = bmp24.Height;

            byte r = BKcolor.R;
            byte g = BKcolor.G;
            byte b = BKcolor.B;

            bmp1 = new Bitmap(w, h, PixelFormat.Format1bppIndexed);

            int ir;

            BmpProc24 src = new BmpProc24(bmp24);
            BmpProc1 dst = new BmpProc1(bmp1);

            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                {
                    ir = src.IndexR(x, y);       // current

                    dst[x, y] = ((src[ir] == r) && (src[ir - 1] == g) && (src[ir - 2] == b));

                }

            CallDispose(dst, src);

            return true;
        }

        private static void BrightnessFunc(ref byte r, ref byte g, ref byte b,
                                                            params double[] prms)
        {
            r = AdjustByte(r + prms[0]);
            g = AdjustByte(g + prms[0]);
            b = AdjustByte(b + prms[0]);
        }

        public static bool Brightness(ref Bitmap bmp, double factor)
        {
            if ((factor < 0) || (factor > 2)) return false;

            factor = (factor - 1) * 255;

            P2PFunc func = new P2PFunc(BrightnessFunc);

            return P2PCore(ref bmp, func, factor);
        }

        private static void ColorFilterFunc(ref byte r, ref byte g, ref byte b,
                                                                  params double[] prms)
        {
            r = AdjustByte(r * prms[0]);
            g = AdjustByte(g * prms[1]);
            b = AdjustByte(b * prms[2]);
        }

        public static bool ColorFilter(ref Bitmap bmp,
                                            double fRed, double fGreen, double fBlue)
        {
            P2PFunc func = new P2PFunc(ColorFilterFunc);

            return P2PCore(ref bmp, func, fRed, fGreen, fBlue);
        }

        private static void ContrastFunc(ref byte r, ref byte g, ref byte b,
                                                            params double[] prms)
        {
            r = (byte)prms[r];
            g = (byte)prms[g];
            b = (byte)prms[b];
        }

        public static bool Contrast(ref Bitmap bmp, double factor)
        {
            if ((factor > 1) || (factor < -1)) return false;

            double value = (1 + factor) * (1 + factor);
            double[] d = new double[256];

            for (int i = 0; i < 256; i++)
                d[i] = AdjustByte((i - 127.5) * value + 127.5);

            P2PFunc func = new P2PFunc(ContrastFunc);

            return P2PCore(ref bmp, func, d);
        }

        public static bool GrayScale8(ref Bitmap bmp8)
        {
            if (bmp8.PixelFormat != PixelFormat.Format8bppIndexed)
                return false;

            int w = bmp8.Width;
            int h = bmp8.Height;

            ColorPalette pal = bmp8.Palette;

            byte[] cl = new byte[pal.Entries.Length];

            Color c;

            for (int i = 0; i < pal.Entries.Length; i++)
            {
                c = pal.Entries[i];
                cl[i] = (byte)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
            }

            BmpProc8 bd = new BmpProc8(bmp8);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    bd[x, y] = cl[bd[x, y]];

            bd.Dispose();

            return SetGrayPalette(bmp8);
        }

        public static bool ConvolutionCore(ref Bitmap bmp,
                          int[,] mask, double divfactor, double offset)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int row = mask.GetLength(0);
            int col = mask.GetLength(1);

            int xzone = (row - 1) / 2;
            int yzone = (col - 1) / 2;

            int ix, iy, xx, yy, mx, my;

            Bitmap tmp = (Bitmap)(bmp.Clone());

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);
            {
                int r, g, b;
                int srcR, dstR;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        r = g = b = 0;
                        for (iy = y - yzone; iy <= y + yzone; iy++)
                        {
                            for (ix = x - xzone; ix <= x + xzone; ix++)
                            {
                                mx = ix - x + xzone;
                                my = iy - y + yzone;

                                if (mask[mx, my] == 0) continue;

                                xx = ix;
                                yy = iy;
                                if ((iy < 0) || (iy > h - 1)) yy = y;
                                if ((ix < 0) || (ix > w - 1)) xx = x;

                                srcR = src.IndexR(xx, yy);
                                r += src[srcR] * mask[mx, my];
                                g += src[srcR - 1] * mask[mx, my];
                                b += src[srcR - 2] * mask[mx, my];
                            }
                        }

                        dstR = dst.IndexR(x, y);
                        dst[dstR] = AdjustByte((double)r / divfactor + offset);
                        dst[dstR - 1] = AdjustByte((double)g / divfactor + offset);
                        dst[dstR - 2] = AdjustByte((double)b / divfactor + offset);
                    }
                }
            }
            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Emboss1(ref Bitmap bmp, bool fGrayScale)
        {
            int[,] mask = { {2,  0,  0},
                            {0, -1,  0},
                            {0,  0, -1} };


            if (fGrayScale)
            {
                if (ConvolutionCore(ref bmp, mask, 1, 127))
                    return GrayScale(ref bmp);
                else
                    return false;
            }
            else
                return ConvolutionCore(ref bmp, mask, 1, 127);
        }

        public static bool Emboss2(ref Bitmap bmp, bool fGrayScale)
        {
            int[,] mask = { {0,  0,  0},
                            {0,  1,  0},
                            {0,  0, -1} };

            if (fGrayScale)
            {
                if (ConvolutionCore(ref bmp, mask, 1, 127))
                    return GrayScale(ref bmp);
                else
                    return false;
            }
            else
                return ConvolutionCore(ref bmp, mask, 1, 127);
        }

        public static bool Emboss3(ref Bitmap bmp, bool fGrayScale)
        {
            int[,] mask = { {0,  0,  0},
                            {0,  4, -1},
                            {0, -1, -2} };

            if (fGrayScale)
            {
                if (ConvolutionCore(ref bmp, mask, 1, 127))
                    return GrayScale(ref bmp);
                else
                    return false;
            }
            else
                return ConvolutionCore(ref bmp, mask, 1, 127);
        }

        public static bool SetTwoColorGrayPalette(Bitmap bmp, Color Dark, Color Bright)
        {
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                return false;

            byte dr = Dark.R; byte dg = Dark.G; byte db = Dark.B;
            byte br = Bright.R; byte bg = Bright.G; byte bb = Bright.B;

            ColorPalette pal = bmp.Palette;

            byte r, g, b;

            for (int i = 0; i < pal.Entries.Length; i++)
            {
                r = (byte)(dr + (br - dr) * i / 255);
                g = (byte)(dg + (bg - dg) * i / 255);
                b = (byte)(db + (bb - db) * i / 255);
                pal.Entries[i] = Color.FromArgb(r, g, b);
            }

            bmp.Palette = pal;

            return true;
        }

        public static bool TwoColorGrayScale(ref Bitmap bmp, Color Dark, Color Bright)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

            if (!SetTwoColorGrayPalette(tmp, Dark, Bright))
            {
                tmp.Dispose();
                return false;
            }

            BmpProc24 src = new BmpProc24(bmp);
            BmpProc8 dst = new BmpProc8(tmp);

            int ir;

            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                {
                    ir = src.IndexR(x, y);

                    dst[x, y] = (byte)(src[ir] * 0.299 +
                                         src[ir - 1] * 0.587 + src[ir - 2] * 0.114);

                }
            CallDispose(dst, src, bmp);

            bmp = tmp;
            return true;
        }

        public static bool Blur(ref Bitmap bmp, int zone)
        {
            if ((zone < 1) || (zone > 10)) return false;

            int len = zone * 2 + 1;

            int[,] mask = new int[len, len];

            for (int row = 0; row < len; row++)
                for (int col = 0; col < len; col++)
                    mask[row, col] = 1;

            return ConvolutionCore(ref bmp, mask, len * len, 0);
        }

        public static int CountColors(Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return -1;

            int w = bmp.Width;
            int h = bmp.Height;
            int num = w * h;

            int[] colors = new int[num];

            int indx = 0;
            int ir;

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ir = src.IndexR(x, y);
                    colors[indx] =
                      (int)((((src[ir] << 8) | src[ir - 1]) << 8) | src[ir - 2]);
                    indx++;
                }

            src.Dispose();

            Array.Sort(colors);

            int count = 1;

            for (int i = 1; i < num; i++)
                if (colors[i - 1] != colors[i]) count++;

            return count;
        }

        private static void SaturationFunc(ref byte r, ref byte g, ref byte b, 
                                                                  params double[] prms)
        {
            prms[4] = r; prms[5] = g; prms[6] = b;

            r = AdjustByte(prms[4] * (prms[0] + prms[3]) + prms[5] * prms[1] +
                                                             prms[6] * prms[2]);
            g = AdjustByte(prms[4] * prms[0] + prms[5] * (prms[1] + prms[3]) +
                                                             prms[6] * prms[2]);
            b = AdjustByte(prms[4] * prms[0] + prms[5] * prms[1] +
                                                 prms[6] * (prms[2] + prms[3]));
        }

        public static bool Saturation(ref Bitmap bmp, double percent)
        {
            double level = 1.0 + percent / 100;
            double baseF = 1.0 - level;
            double rF = 0.3086 * baseF;
            double gF = 0.6094 * baseF;
            double bF = 0.0820 * baseF;

            P2PFunc func = new P2PFunc(SaturationFunc);

            return P2PCore(ref bmp, func, rF, gF, bF, level, 0, 0, 0);
        }

        private static void GammaFunc(ref byte r, ref byte g, ref byte b,
                                                                  params double[] prms)
        {
            r = AdjustByte(prms[r]);
            g = AdjustByte(prms[g]);
            b = AdjustByte(prms[b]);
        }

        public static bool Gamma(ref Bitmap bmp, double dGamma)
        {
            double[] gammaf = new double[256];

            for (int i = 0; i < 256; i++)
                gammaf[i] = Math.Min(255.0,
                    Math.Pow((double)(i / 255.0), (1.0 / dGamma)) * 255 + 0.5);

            P2PFunc func = new P2PFunc(GammaFunc);

            return P2PCore(ref bmp, func, gammaf);
        }

        public static Bitmap ResizeBitmap(Bitmap bmp, float ratio, InterpolationMode im)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap((int)(w * ratio), (int)(h * ratio), bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.InterpolationMode = im;
            g.DrawImage(bmp, new RectangleF(0f, 0f, w * ratio, h * ratio));
            g.Dispose();

            return tmp;
        }

        public static bool OilPaint(ref Bitmap bmp, int zone)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int[] rh = new int[16];
            int[] gh = new int[16];
            int[] bh = new int[16];

            int rmax, gmax, bmax, rindx, gindx, bindx;
            int r, g, b, countr, countg, countb;

            Bitmap tmp = (Bitmap)bmp.Clone();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    rmax = gmax = bmax = -1;
                    rindx = gindx = bindx = 0;
                    for (int i = 0; i <= 15; i++)
                    { rh[i] = gh[i] = bh[i] = 0; }

                    for (int iy = y - zone; iy <= y + zone; iy++)
                        for (int ix = x - zone; ix <= x + zone; ix++)
                        {
                            if ((iy < 0) | (iy > h - 1)) continue;
                            if ((ix < 0) | (ix > w - 1)) continue;

                            src.SetXY(ix, iy);
                            rh[src.R >> 4]++;
                            gh[src.G >> 4]++;
                            bh[src.B >> 4]++;
                        }

                    for (int i = 0; i <= 15; i++)
                    {
                        if (rmax < rh[i]) { rmax = rh[i]; rindx = i; }
                        if (gmax < gh[i]) { gmax = gh[i]; gindx = i; }
                        if (bmax < bh[i]) { bmax = bh[i]; bindx = i; }
                    }

                    r = g = b = countr = countg = countb = 0;

                    for (int iy = y - zone; iy <= y + zone; iy++)
                        for (int ix = x - zone; ix <= x + zone; ix++)
                        {
                            if ((iy < 0) | (iy > h - 1)) continue;
                            if ((ix < 0) | (ix > w - 1)) continue;

                            src.SetXY(ix, iy);
                            if ((src.R >> 4) == rindx) { r += src.R; countr++; }
                            if ((src.G >> 4) == gindx) { g += src.G; countg++; }
                            if ((src.B >> 4) == bindx) { b += src.B; countb++; }
                        }

                    dst.SetXY(x, y);
                    dst.R = (byte)(r / countr);
                    dst.G = (byte)(g / countg);
                    dst.B = (byte)(b / countb);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static Bitmap BitmapRotate(Bitmap bmp, float angle, Color bkColor)
        {
            int w = bmp.Width + 2;
            int h = bmp.Height + 2;

            PixelFormat pf;

            if (bkColor == Color.Transparent)
                pf = PixelFormat.Format32bppArgb;
            else
                pf = bmp.PixelFormat;

            Bitmap tmp = new Bitmap(w, h, pf);
            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, 1, 1);
            g.Dispose();

            GraphicsPath path = new GraphicsPath();
            path.AddRectangle(new RectangleF(0f, 0f, w, h));
            Matrix mtrx = new Matrix();
            mtrx.Rotate(angle);
            RectangleF rct = path.GetBounds(mtrx);

            Bitmap dst = new Bitmap((int)rct.Width, (int)rct.Height, pf);
            g = Graphics.FromImage(dst);
            g.Clear(bkColor);
            g.TranslateTransform(-rct.X, -rct.Y);
            g.RotateTransform(angle);
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.DrawImageUnscaled(tmp, 0, 0);
            g.Dispose();

            tmp.Dispose();

            return dst;
        }

        public static bool PaletteHistogram(Bitmap bmp, out Bitmap bmpHist)
        {
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                bmpHist = null;
                return false;
            }

            int w = bmp.Width;
            int h = bmp.Height;

            int[] hist = new int[256];


            BmpProc8 bd = new BmpProc8(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    hist[bd[x, y]]++;

            bd.Dispose();

            int max = -1;
            for (int i = 0; i < 256; i++)
                if (hist[i] > max) max = hist[i];

            for (int i = 0; i < 256; i++)
                hist[i] = hist[i] * 140 / max;

            bmpHist = new Bitmap(275, 180, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(bmpHist);
            g.Clear(Color.AntiqueWhite);

            Pen pen = new Pen(Color.Gray, 1F);

            for (int i = 0; i < 256; i++)
                g.DrawLine(pen, 10 + i, 150, 10 + i, 150 - hist[i]);

            pen.Color = Color.Black;

            g.DrawLine(pen, 8, 150, 8, 10);

            for (int i = 0; i <= 20; i++)
                if ((i % 2) == 0)
                    g.DrawLine(pen, 8, 150 - 7 * i, 4, 150 - 7 * i);
                else
                    g.DrawLine(pen, 8, 150 - 7 * i, 6, 150 - 7 * i);

            g.DrawLine(pen, 10, 150, 10 + 255, 150);

            for (int i = 0; i <= 51; i++)
                if ((i % 2) == 0)
                    g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 154);
                else
                    g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 152);

            ColorPalette pal = bmp.Palette;

            for (int i = 0; i < 256; i++)
            {
                pen.Color = pal.Entries[i];
                g.DrawLine(pen, 10 + i, 175, 10 + i, 158);
            }

            g.Dispose();

            return true;
        }

        public static bool GaussianBlur(ref Bitmap bmp, int zone)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 1) | (zone > 30)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int range = zone * 3;

            double[] gf = new double[range + 1];

            for (int i = 0; i <= range; i++)
                gf[i] = Math.Exp(-i * i / (2 * zone * zone));

            double count, sr, sg, sb, gauss;
            int ir;

            Bitmap tmp = (Bitmap)bmp.Clone();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            // dst --> src  x-direction average

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    count = sr = sg = sb = 0;

                    for (int ix = x - range; ix <= x + range; ix++)
                    {
                        if ((ix < 0) | (ix > w - 1)) continue;
                        ir = dst.IndexR(ix, y);
                        gauss = gf[Math.Abs(ix - x)];
                        sr += dst[ir] * gauss;
                        sg += dst[ir - 1] * gauss;
                        sb += dst[ir - 2] * gauss;
                        count += gauss;
                    }

                    ir = src.IndexR(x, y);
                    src[ir] = AdjustByte(sr / count);
                    src[ir - 1] = AdjustByte(sg / count);
                    src[ir - 2] = AdjustByte(sb / count);
                }

            // src --> dst  y-direction average

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    count = sr = sg = sb = 0;

                    for (int iy = y - range; iy <= y + range; iy++)
                    {
                        if ((iy < 0) | (iy > h - 1)) continue;
                        ir = src.IndexR(x, iy);
                        gauss = gf[Math.Abs(iy - y)];
                        sr += src[ir] * gauss;
                        sg += src[ir - 1] * gauss;
                        sb += src[ir - 2] * gauss;
                        count += gauss;
                    }

                    ir = dst.IndexR(x, y);
                    dst[ir] = AdjustByte(sr / count);
                    dst[ir - 1] = AdjustByte(sg / count);
                    dst[ir - 2] = AdjustByte(sb / count);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool HistoStretch(ref Bitmap graybmp, params double[] limit)
        {
            if (graybmp.PixelFormat != PixelFormat.Format8bppIndexed)
                return false;

            int w = graybmp.Width;
            int h = graybmp.Height;

            double stretchfactor = 1.00;

            int threshold = (int)(w * h * 0.015);

            if (limit.Length != 0) threshold = (int)(w * h * limit[0] / 100);

            int[] hist = new int[256];

            BmpProc8 src = new BmpProc8(graybmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    hist[src[x, y]]++;

            int lt = 0;
            for (int i = 0; i < 256; i++)
            {
                lt += hist[i];
                if (lt > threshold)
                {
                    lt = i;
                    break;
                }
            }

            int ht = 0;
            for (int i = 255; i >= 0; i--)
            {
                ht += hist[i];
                if (ht > threshold)
                {
                    ht = i;
                    break;
                }
            }

            double originalrange = ht - lt + 1;
            double stretchedrange = originalrange + stretchfactor * (255 - originalrange);
            double scalefactor = stretchedrange / originalrange;

            for (int i = 0; i < 256; i++)
                hist[i] = AdjustByte(scalefactor * (i - lt));

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    src[x, y] = (byte)hist[src[x, y]];

            src.Dispose();

            return true;
        }

        public static bool Laplacian(ref Bitmap bmp, bool fGrayScale)
        {
            int[,] mask = { { 0, -1,  0},
                            {-1,  4, -1},
                            { 0, -1,  0} };

            if (fGrayScale)
            {
                if (ConvolutionCore(ref bmp, mask, 1, 127))
                    return GrayScale(ref bmp);
                else
                    return false;
            }
            else
                return ConvolutionCore(ref bmp, mask, 1, 127);
        }

        public static bool EdgeEnhance(ref Bitmap bmp, int percent)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = (Bitmap)(bmp.Clone());

            Laplacian(ref tmp, false);

            double f = (double)(percent) / 100;

            int ird, irs;

            BmpProc24 dst = new BmpProc24(bmp);
            BmpProc24 src = new BmpProc24(tmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ird = dst.IndexR(x, y);
                    irs = src.IndexR(x, y);

                    dst[ird] = AdjustByte(dst[ird] + (src[irs] - 127) * f);
                    dst[ird - 1] = AdjustByte(dst[ird - 1] + (src[irs - 1] - 127) * f);
                    dst[ird - 2] = AdjustByte(dst[ird - 2] + (src[irs - 2] - 127) * f);
                }
            CallDispose(src, dst, tmp);

            return true;
        }

        public static bool Pixelate(ref Bitmap bmp, int zone)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 30)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int ir, count, sumr, sumg, sumb;

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = 0; y < h; y += zone)
                for (int x = 0; x < w; x += zone)
                {
                    count = sumr = sumg = sumb = 0;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;

                            count++;

                            ir = src.IndexR(ix, iy);
                            sumr += src[ir];
                            sumg += src[ir - 1];
                            sumb += src[ir - 2];
                        }

                    sumr = sumr / count;
                    sumg = sumg / count;
                    sumb = sumb / count;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;

                            ir = src.IndexR(ix, iy);
                            src[ir] = (byte)sumr;
                            src[ir - 1] = (byte)sumg;
                            src[ir - 2] = (byte)sumb;
                        }
                }

            src.Dispose();

            return true;
        }

        public static bool Pixelate2(ref Bitmap bmp, int zone)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 30)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int ir, count, sumr, sumg, sumb;

            byte[,] rr = new byte[w / zone + 1, h / zone + 1];
            byte[,] gg = new byte[w / zone + 1, h / zone + 1];
            byte[,] bb = new byte[w / zone + 1, h / zone + 1];

            int countX, countY;

            byte rrr, ggg, bbb;

            countY = 0;

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = 0; y < h; y += zone)
            {
                countX = 0;

                for (int x = 0; x < w; x += zone)
                {
                    count = sumr = sumg = sumb = 0;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;

                            count++;

                            ir = src.IndexR(ix, iy);
                            sumr += src[ir];
                            sumg += src[ir - 1];
                            sumb += src[ir - 2];
                        }

                    sumr = sumr / count;
                    sumg = sumg / count;
                    sumb = sumb / count;

                    rr[countX, countY] = (byte)sumr;
                    gg[countX, countY] = (byte)sumg;
                    bb[countX, countY] = (byte)sumb;

                    if (sumr > 127)
                    { rrr = (byte)(sumr * 0.8); }
                    else
                    { rrr = (byte)(sumr * 1.2); }

                    if (sumg > 127)
                    { ggg = (byte)(sumg * 0.8); }
                    else
                    { ggg = (byte)(sumg * 1.2); }

                    if (sumb > 127)
                    { bbb = (byte)(sumb * 0.8); }
                    else
                    { bbb = (byte)(sumb * 1.2); }

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;

                            ir = src.IndexR(ix, iy);
                            src[ir] = rrr;
                            src[ir - 1] = ggg;
                            src[ir - 2] = bbb;
                        }

                    countX++;

                }

                countY++;
            }

            src.Dispose();

            countY = 0;

            SolidBrush br = new SolidBrush(Color.Black);

            Rectangle rct = new Rectangle(0, 0, zone, zone);

            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int y = 0; y < h; y += zone)
            {
                countX = 0;

                for (int x = 0; x < w; x += zone)
                {
                    br.Color = Color.FromArgb(rr[countX, countY], gg[countX, countY],
                                                                   bb[countX, countY]);
                    rct.X = x; rct.Y = y;
                    g.FillEllipse(br, rct);

                    countX++;
                }

                countY++;
            }

            g.Dispose();
            br.Dispose();

            return true;
        }

        public static bool Pixelate3(ref Bitmap bmp, int zone)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 30)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int ir, count, sumr, sumg, sumb;

            byte[,] rr = new byte[w / zone + 1, h / zone + 1];
            byte[,] gg = new byte[w / zone + 1, h / zone + 1];
            byte[,] bb = new byte[w / zone + 1, h / zone + 1];

            int countX, countY;

            byte rrr, ggg, bbb;

            countY = 0;

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = 0; y < h; y += zone)
            {
                countX = 0;

                for (int x = 0; x < w; x += zone)
                {
                    count = sumr = sumg = sumb = 0;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;

                            count++;

                            ir = src.IndexR(ix, iy);
                            sumr += src[ir];
                            sumg += src[ir - 1];
                            sumb += src[ir - 2];
                        }

                    sumr = sumr / count;
                    sumg = sumg / count;
                    sumb = sumb / count;

                    rr[countX, countY] = (byte)sumr;
                    gg[countX, countY] = (byte)sumg;
                    bb[countX, countY] = (byte)sumb;

                    rrr = (byte)sumr;
                    ggg = (byte)sumg;
                    bbb = (byte)sumb;

                    if ((sumr < 230) & (sumr > 15)) { rrr = (byte)(sumr - 15); }
                    if ((sumg < 230) & (sumg > 15)) { ggg = (byte)(sumg - 15); }
                    if ((sumb < 230) & (sumb > 15)) { bbb = (byte)(sumb - 15); }


                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;

                            ir = src.IndexR(ix, iy);
                            src[ir] = rrr;
                            src[ir - 1] = ggg;
                            src[ir - 2] = bbb;
                        }

                    countX++;

                }

                countY++;
            }

            src.Dispose();

            countY = 0;

            SolidBrush br = new SolidBrush(Color.Black);

            Point[] pt = new Point[3];

            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int y = 0; y < h; y += zone)
            {
                countX = 0;

                for (int x = 0; x < w; x += zone)
                {
                    rrr = rr[countX, countY]; ggg = gg[countX, countY];
                    bbb = bb[countX, countY];
                    if ((rrr < 230) & (rrr > 15)) { rrr = (byte)(rrr + 15); }
                    if ((ggg < 230) & (ggg > 15)) { ggg = (byte)(ggg + 15); }
                    if ((bbb < 230) & (bbb > 15)) { bbb = (byte)(bbb + 15); }
                    br.Color = Color.FromArgb(rrr, ggg, bbb);
                    pt[0].X = x; pt[0].Y = y;
                    pt[1].X = x + zone; pt[1].Y = y;
                    pt[2].X = x; pt[2].Y = y + zone;
                    g.FillPolygon(br, pt);

                    countX++;
                }

                countY++;
            }

            g.Dispose();
            br.Dispose();

            return true;
        }

        // hue 0..360  saturation 0..255  luminance 0..255 for r,g,b 0..255

        public static void HSLToRGB(int h, int s, int l,
                                             out byte rr, out byte gg, out byte bb)
        {
            int hh = h;
            double ss = s / 255d;
            double ll = l / 255d;

            double r, g, b, maxc, minc;

            if (s == 0)
            {
                r = g = b = ll;
            }
            else
            {
                if (ll <= 0.5) maxc = ll * (1 + ss); else maxc = ll * (1 - ss) + ss;
                minc = 2 * ll - maxc;

                int hhh = hh + 120;
                if (hhh >= 360) hhh = hhh - 360;
                if (hhh < 60) r = minc + (maxc - minc) * hhh / 60;
                else if (hhh < 180) r = maxc;
                else if (hhh < 240) r = minc + (maxc - minc) * (240 - hhh) / 60;
                else r = minc;

                hhh = hh;
                if (hhh < 60) g = minc + (maxc - minc) * hhh / 60;
                else if (hhh < 180) g = maxc;
                else if (hhh < 240) g = minc + (maxc - minc) * (240 - hhh) / 60;
                else g = minc;

                hhh = hh - 120;
                if (hhh < 0) hhh = hhh + 360;
                if (hhh < 60) b = minc + (maxc - minc) * hhh / 60;
                else if (hhh < 180) b = maxc;
                else if (hhh < 240) b = minc + (maxc - minc) * (240 - hhh) / 60;
                else b = minc;
            }
            rr = (byte)(r * 255);
            gg = (byte)(g * 255);
            bb = (byte)(b * 255);
        }

        public static void RGBToHSL(byte rr, byte gg, byte bb,
                                                  out int h, out int s, out int l)
        {
            double r = rr / 255d;
            double g = gg / 255d;
            double b = bb / 255d;

            double maxc = Math.Max(Math.Max(r, g), b);
            double minc = Math.Min(Math.Min(r, g), b);

            double ll = (maxc + minc) / 2;
            double ss, hh;

            if ((maxc - minc) < 0.000001)
                ss = hh = 0;
            else
            {
                if (ll <= 0.5)
                    ss = (maxc - minc) / (maxc + minc);
                else
                    ss = (maxc - minc) / (2 - maxc - minc);

                double cr = (maxc - r) / (maxc - minc);
                double cg = (maxc - g) / (maxc - minc);
                double cb = (maxc - b) / (maxc - minc);

                if (maxc == r)
                    hh = cb - cg;
                else if (maxc == g) hh = 2 + cr - cb;
                else hh = 4 + cg - cr;

                hh = 60 * hh;
                if (hh < 0) hh = hh + 360;
            }

            h = (int)hh;
            s = (int)(ss * 255);
            l = (int)(ll * 255);
        }

        public static bool RGBHistogram(Bitmap bmp, out Bitmap bmpHist)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                bmpHist = null;
                return false;
            }

            int w = bmp.Width;
            int h = bmp.Height;

            int[] hist = new int[256];
            int[] rh = new int[256];
            int[] gh = new int[256];
            int[] bh = new int[256];

            int ir, indx;

            BmpProc24 bd = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ir = bd.IndexR(x, y);
                    rh[bd[ir]]++;
                    gh[bd[ir - 1]]++;
                    bh[bd[ir - 2]]++;
                    indx = (int)(bd[ir] * 0.299 + bd[ir - 1] * 0.587 +
                                                           bd[ir - 2] * 0.114);
                    hist[indx]++;
                }

            bd.Dispose();

            int max = -1;
            for (int i = 0; i < 256; i++)
            {
                if (rh[i] > max) max = rh[i];
                if (gh[i] > max) max = gh[i];
                if (bh[i] > max) max = bh[i];
                if (hist[i] > max) max = hist[i];
            }

            PointF[] pt = new PointF[256];

            bmpHist = new Bitmap(275, 160, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(bmpHist);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.AntiqueWhite);

            Pen pen = new Pen(Color.Red, 1.5F);
            for (int i = 0; i < 256; i++)
            {
                pt[i].X = 10 + i;
                pt[i].Y = (float)(150 - rh[i] * 140f / max);
            }

            g.DrawLines(pen, pt);

            pen.Color = Color.ForestGreen;
            for (int i = 0; i < 256; i++)
                pt[i].Y = (float)(150 - gh[i] * 140f / max);

            g.DrawLines(pen, pt);

            pen.Color = Color.Blue;
            for (int i = 0; i < 256; i++)
                pt[i].Y = (float)(150 - bh[i] * 140f / max);

            g.DrawLines(pen, pt);

            pen.Color = Color.Black;
            for (int i = 0; i < 256; i++)
                pt[i].Y = (float)(150 - hist[i] * 140f / max);

            g.DrawLines(pen, pt);

            pen.Color = Color.Black;

            g.DrawLine(pen, 8, 150, 8, 10);

            for (int i = 0; i <= 20; i++)
                if ((i % 2) == 0)
                    g.DrawLine(pen, 8, 150 - 7 * i, 4, 150 - 7 * i);
                else
                    g.DrawLine(pen, 8, 150 - 7 * i, 6, 150 - 7 * i);

            g.DrawLine(pen, 10, 150, 10 + 255, 150);

            for (int i = 0; i <= 51; i++)
                if ((i % 2) == 0)
                    g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 154);
                else
                    g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 152);

            g.Dispose();

            return true;
        }

        public static bool Edge(ref Bitmap bmp, bool fGrayScale)
        {
            int[,] mask = { {-1, -1, -1},
                            {-1,  8, -1},
                            {-1, -1, -1} };

            if (fGrayScale)
            {
                if (ConvolutionCore(ref bmp, mask, 1, 127))
                    return GrayScale(ref bmp);
                else
                    return false;
            }
            else
                return ConvolutionCore(ref bmp, mask, 1, 127);
        }

        public static bool Sharpen(ref Bitmap bmp, int percent)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = (Bitmap)(bmp.Clone());

            Edge(ref tmp, false);

            double f = (double)(percent) / 100;

            int ird, irs;

            BmpProc24 dst = new BmpProc24(bmp);
            BmpProc24 src = new BmpProc24(tmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ird = dst.IndexR(x, y);
                    irs = src.IndexR(x, y);

                    dst[ird] = AdjustByte(dst[ird] + (src[irs] - 127) * f);
                    dst[ird - 1] = AdjustByte(dst[ird - 1] + (src[irs - 1] - 127) * f);
                    dst[ird - 2] = AdjustByte(dst[ird - 2] + (src[irs - 2] - 127) * f);
                }
            CallDispose(src, dst, tmp);

            return true;
        }

        public static bool Mosaic(ref Bitmap bmp, int zone, Rectangle rct)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 30)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int ir, count, sumr, sumg, sumb;

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = rct.Y; y < rct.Y + rct.Height; y += zone)
            {
                if ((y < 0) | (y > h - 1)) continue;

                for (int x = rct.X; x < rct.X + rct.Width; x += zone)
                {
                    if ((x < 0) | (x > w - 1)) continue;

                    count = sumr = sumg = sumb = 0;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;
                            if ((iy > rct.Y + rct.Height - 1) |
                                                        (ix > rct.X + rct.Width - 1))
                                continue;

                            count++;

                            ir = src.IndexR(ix, iy);
                            sumr += src[ir];
                            sumg += src[ir - 1];
                            sumb += src[ir - 2];
                        }

                    sumr = sumr / count;
                    sumg = sumg / count;
                    sumb = sumb / count;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;
                            if ((iy > rct.Y + rct.Height - 1) | (ix > rct.X + rct.Width - 1))
                                continue;

                            ir = src.IndexR(ix, iy);
                            src[ir] = (byte)sumr;
                            src[ir - 1] = (byte)sumg;
                            src[ir - 2] = (byte)sumb;
                        }
                }
            }

            src.Dispose();

            return true;
        }

        public static bool Median(ref Bitmap bmp, int area)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((area < 1) | (area > 5)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = (2 * area + 1) * (2 * area + 1);
            int mid = num / 2; // mid-index of zero starting array

            byte[] rr = new byte[num];
            byte[] gg = new byte[num];
            byte[] bb = new byte[num];

            int indx;

            Bitmap tmp = (Bitmap)bmp.Clone();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    indx = 0;

                    for (int iy = y - area; iy <= y + area; iy++)
                        for (int ix = x - area; ix <= x + area; ix++)
                        {
                            if ((ix < 0) | (ix > w - 1)) continue;
                            if ((iy < 0) | (iy > h - 1)) continue;

                            src.SetXY(ix, iy);
                            rr[indx] = src.R;
                            gg[indx] = src.G;
                            bb[indx] = src.B;

                            indx++;
                        }

                    Array.Sort(rr, 0, indx);
                    Array.Sort(gg, 0, indx);
                    Array.Sort(bb, 0, indx);

                    if (indx == num) indx = mid; else indx = indx / 2;

                    dst.SetXY(x, y);
                    dst.R = rr[indx];
                    dst.G = gg[indx];
                    dst.B = bb[indx];
                }

            CallDispose(dst, src, tmp);

            return true;

        }

        private static void HueFunc(ref byte r, ref byte g, ref byte b,
                                                             params double[] param)
        {
            int h, s, l;

            RGBToHSL(r, g, b, out h, out s, out l);
            h = h + (int)param[0];
            if (h > 360) h -= 360;
            if (h < 0) h += 360;
            HSLToRGB(h, s, l, out r, out g, out b);
        }

        public static bool Hue(ref Bitmap bmp, int angle)
        {
            if ((angle < -359) | (angle > 359)) return false;

            P2PFunc func = new P2PFunc(HueFunc);

            return P2PCore(ref bmp, func, angle);
        }

        private static void LuminanceFunc(ref byte r, ref byte g, ref byte b,
                                                                 params double[] param)
        {
            int h, s, l;

            RGBToHSL(r, g, b, out h, out s, out l);
            l = AdjustByte(l + param[0]);
            HSLToRGB(h, s, l, out r, out g, out b);
        }

        public static bool Luminance(ref Bitmap bmp, int percent)
        {
            double ll = 255d * percent / 100;

            P2PFunc func = new P2PFunc(LuminanceFunc);

            return P2PCore(ref bmp, func, ll);
        }

        public static void CenterRotation(ref Bitmap bmp, double angle,
                                                  Color bkColor, Interpolation ip)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 2; // margin for interpolations

            double cx = w / 2d;
            double cy = h / 2d;

            angle = angle * Math.PI / 180; // degree --> radian

            double r, a, xx, yy;

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            g = Graphics.FromImage(bmp);
            g.Clear(bkColor);
            g.Dispose();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx);

                    a = a - angle; // resampling for angle

                    xx = r * Math.Cos(a) + cx;
                    yy = r * Math.Sin(a) + cy;

                    if ((xx > -1) & (xx < w) & (yy > -1) & (yy < h))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        switch (ip)
                        {
                            case Interpolation.ipNearest:
                                intNearest(dst, src, x, y, xx, yy);
                                break;
                            case Interpolation.ipBilinear:
                                intBilinear(dst, src, x, y, xx, yy);
                                break;
                            case Interpolation.ipBicubic:
                                intBicubic(dst, src, x, y, xx, yy);
                                break;
                            case Interpolation.ipLagrange:
                                intLagrange(dst, src, x, y, xx, yy);
                                break;
                            case Interpolation.ipMitchell:
                                intMitchell(dst, src, x, y, xx, yy);
                                break;
                            case Interpolation.ipLanczos:
                                intLanczos(dst, src, x, y, xx, yy);
                                break;
                        }
                    }
                }

            CallDispose(dst, src, tmp);

        }

        public static void intNearest(BmpProc24 dst, BmpProc24 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int ix = (int)(srcX + 0.5); // nearest point
            int iy = (int)(srcY + 0.5);
            dst.SetXY(dstX, dstY);
            src.SetXY(ix, iy);
            dst.R = src.R;
            dst.G = src.G;
            dst.B = src.B;
        }

        public static void intBilinear(BmpProc24 dst, BmpProc24 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int x1 = (int)Math.Floor(srcX);
            int x2 = x1 + 1;
            double fx2 = srcX - x1;
            double fx1 = 1 - fx2;

            int y1 = (int)Math.Floor(srcY);
            int y2 = y1 + 1;
            double fy2 = srcY - y1;
            double fy1 = 1 - fy2;

            dst[dstX, dstY, eRGB.r] =
                (byte)(src[x1, y1, eRGB.r] * fx1 * fy1 +
                       src[x2, y1, eRGB.r] * fx2 * fy1 +
                       src[x1, y2, eRGB.r] * fx1 * fy2 +
                       src[x2, y2, eRGB.r] * fx2 * fy2);

            dst[dstX, dstY, eRGB.g] =
                (byte)(src[x1, y1, eRGB.g] * fx1 * fy1 +
                       src[x2, y1, eRGB.g] * fx2 * fy1 +
                       src[x1, y2, eRGB.g] * fx1 * fy2 +
                       src[x2, y2, eRGB.g] * fx2 * fy2);

            dst[dstX, dstY, eRGB.b] =
                (byte)(src[x1, y1, eRGB.b] * fx1 * fy1 +
                       src[x2, y1, eRGB.b] * fx2 * fy1 +
                       src[x1, y2, eRGB.b] * fx1 * fy2 +
                       src[x2, y2, eRGB.b] * fx2 * fy2);
        }

        public static void intBicubic(BmpProc24 dst, BmpProc24 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int xi = (int)Math.Floor(srcX);
            int yi = (int)Math.Floor(srcY);

            double rr, gg, bb;
            double dx, dy, wx, wy;

            rr = gg = bb = 0;
            for (int iy = yi - 1; iy < yi + 3; iy++)
                for (int ix = xi - 1; ix < xi + 3; ix++)
                {
                    dx = Math.Abs(srcX - ix);
                    dy = Math.Abs(srcY - iy);

                    if (dx < 1) wx = (dx - 1d) * (dx * dx - dx - 1d);
                    else wx = -(dx - 1d) * (dx - 2d) * (dx - 2d);

                    if (dy < 1) wy = (dy - 1d) * (dy * dy - dy - 1d);
                    else wy = -(dy - 1d) * (dy - 2d) * (dy - 2d);

                    rr += src[ix, iy, eRGB.r] * wx * wy;
                    gg += src[ix, iy, eRGB.g] * wx * wy;
                    bb += src[ix, iy, eRGB.b] * wx * wy;
                }

            dst[dstX, dstY, eRGB.r] = AdjustByte(rr);
            dst[dstX, dstY, eRGB.g] = AdjustByte(gg);
            dst[dstX, dstY, eRGB.b] = AdjustByte(bb);
        }

        public static void intLagrange(BmpProc24 dst, BmpProc24 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int xi = (int)Math.Floor(srcX);
            int yi = (int)Math.Floor(srcY);

            double rr, gg, bb;
            double dx, dy, wx, wy;

            rr = gg = bb = 0;
            for (int iy = yi - 1; iy < yi + 3; iy++)
                for (int ix = xi - 1; ix < xi + 3; ix++)
                {
                    dx = Math.Abs(srcX - ix);
                    dy = Math.Abs(srcY - iy);

                    if (dx < 1) wx = 0.5d * (dx - 2d) * (dx + 1d) * (dx - 1d);
                    else wx = -(dx - 3d) * (dx - 2d) * (dx - 1d) / 6d;

                    if (dy < 1) wy = 0.5d * (dy - 2d) * (dy + 1d) * (dy - 1d);
                    else wy = -(dy - 3d) * (dy - 2d) * (dy - 1d) / 6d;

                    rr += src[ix, iy, eRGB.r] * wx * wy;
                    gg += src[ix, iy, eRGB.g] * wx * wy;
                    bb += src[ix, iy, eRGB.b] * wx * wy;
                }

            dst[dstX, dstY, eRGB.r] = AdjustByte(rr);
            dst[dstX, dstY, eRGB.g] = AdjustByte(gg);
            dst[dstX, dstY, eRGB.b] = AdjustByte(bb);
        }

        public static void intMitchell(BmpProc24 dst, BmpProc24 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int xi = (int)Math.Floor(srcX);
            int yi = (int)Math.Floor(srcY);

            double rr, gg, bb;
            double dx, dy, wx, wy;

            rr = gg = bb = 0;
            for (int iy = yi - 1; iy < yi + 3; iy++)
                for (int ix = xi - 1; ix < xi + 3; ix++)
                {
                    dx = Math.Abs(srcX - ix);
                    dy = Math.Abs(srcY - iy);

                    if (dx < 1) wx = 7d * dx * dx * dx / 6d - 2d * dx * dx + 8d / 9d;
                    else wx = 2d * dx * dx -
                                  10d * dx / 3d - 7d * dx * dx * dx / 18d + 16d / 9d;

                    if (dy < 1) wy = 7d * dy * dy * dy / 6d - 2d * dy * dy + 8d / 9d;
                    else wy = 2d * dy * dy -
                                  10d * dy / 3d - 7d * dy * dy * dy / 18d + 16d / 9d;

                    rr += src[ix, iy, eRGB.r] * wx * wy;
                    gg += src[ix, iy, eRGB.g] * wx * wy;
                    bb += src[ix, iy, eRGB.b] * wx * wy;
                }

            dst[dstX, dstY, eRGB.r] = AdjustByte(rr);
            dst[dstX, dstY, eRGB.g] = AdjustByte(gg);
            dst[dstX, dstY, eRGB.b] = AdjustByte(bb);
        }

        public static void intLanczos(BmpProc24 dst, BmpProc24 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int xi = (int)Math.Floor(srcX);
            int yi = (int)Math.Floor(srcY);

            double rr, gg, bb;
            double pi = Math.PI;
            double dx, dy, wx, wy;

            rr = gg = bb = 0;
            for (int iy = yi - 1; iy < yi + 3; iy++)
                for (int ix = xi - 1; ix < xi + 3; ix++)
                {
                    dx = Math.Abs(srcX - ix);
                    dy = Math.Abs(srcY - iy);

                    if (dx == 0) wx = 1d;
                    else wx = Math.Sin(pi * dx) *
                                Math.Sin(0.5d * pi * dx) / (0.5d * pi * pi * dx * dx);

                    if (dy == 0) wy = 1d;
                    else wy = Math.Sin(pi * dy) *
                                Math.Sin(0.5d * pi * dy) / (0.5d * pi * pi * dy * dy);

                    rr += src[ix, iy, eRGB.r] * wx * wy;
                    gg += src[ix, iy, eRGB.g] * wx * wy;
                    bb += src[ix, iy, eRGB.b] * wx * wy;
                }

            dst[dstX, dstY, eRGB.r] = AdjustByte(rr);
            dst[dstX, dstY, eRGB.g] = AdjustByte(gg);
            dst[dstX, dstY, eRGB.b] = AdjustByte(bb);
        }

        public static bool Swirl(ref Bitmap bmp, double factor,
                                       int cx, int cy, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 2; // margin for interpolations

            double c = factor;
            if ((cx == 0) & (cy == 0)) { cx = w / 2; cy = h / 2; }

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            g = Graphics.FromImage(bmp);
            g.Clear(bkColor);
            g.Dispose();

            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);

            double r, a, xx, yy;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx);                // radian
                    a = a + c * r;
                    xx = r * Math.Cos(a) + cx;
                    yy = r * Math.Sin(a) + cy;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBilinear(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool PartialGaussianBlur(ref Bitmap bmp, int zone, Rectangle rct)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 1) | (zone > 30)) return false;

            Bitmap tmp = new Bitmap(rct.Width, rct.Height, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(tmp);
            g.DrawImage(bmp, 0, 0, rct, GraphicsUnit.Pixel);
            g.Dispose();

            GaussianBlur(ref tmp, zone);

            g = Graphics.FromImage(bmp);
            g.DrawImageUnscaled(tmp, rct.Left, rct.Top);
            g.Dispose();

            tmp.Dispose();

            return true;
        }

        public static void JAlphaBlend(Graphics g, Bitmap bmp, float alpha,
                                               int dstX, int dstY, Rectangle srcRct)
        {
            int w = srcRct.Width;
            int h = srcRct.Height;

            ColorMatrix cm = new ColorMatrix();
            cm.Matrix33 = alpha;

            ImageAttributes im = new ImageAttributes();
            im.SetColorMatrix(cm);

            g.DrawImage(bmp, new Rectangle(dstX, dstY, w, h),
                                srcRct.Left, srcRct.Top, w, h, GraphicsUnit.Pixel, im);

            im.Dispose();
        }

        public static void JAlphaBlend(Graphics g, Bitmap bmp, float alpha,
                                                                int dstX, int dstY)
        {
            Rectangle rct = new Rectangle(0, 0, bmp.Width, bmp.Height);

            JAlphaBlend(g, bmp, alpha, dstX, dstY, rct);
        }

        public static bool SetFramedAlpha(ref Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            double pi = Math.PI;
            double f;

            BmpProc32 dst = new BmpProc32(tmp);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    f = Math.Sin(y * pi / h) * Math.Sin(x * pi / w);
                    dst[x, y, eRGB.a] = (byte)(255 * f);
                }
            }

            dst.Dispose();

            bmp.Dispose();
            bmp = tmp;

            return true;
        }

        public static bool DrawFramedAlpha(Bitmap dstBmp, Bitmap srcBmp,
                                                              int dstX, int dstY)
        {
            if (dstBmp.PixelFormat != PixelFormat.Format24bppRgb) return false;

            if (srcBmp.PixelFormat != PixelFormat.Format24bppRgb) return false;

            int dw = dstBmp.Width;
            int dh = dstBmp.Height;

            int sw = srcBmp.Width;
            int sh = srcBmp.Height;

            double pi = Math.PI;
            double f;

            BmpProc24 dst = new BmpProc24(dstBmp);
            BmpProc24 src = new BmpProc24(srcBmp);

            for (int y = 0; y < sh; y++)
            {
                if ((y + dstY < 0) | (y + dstY > dh - 1)) continue;

                for (int x = 0; x < sw; x++)
                {
                    if ((x + dstX < 0) | (x + dstX > dw - 1)) continue;

                    f = Math.Sin(y * pi / sh) * Math.Sin(x * pi / sw);
                    dst.SetXY(x + dstX, y + dstY);
                    src.SetXY(x, y);

                    dst.R = (byte)((1f - f) * dst.R + f * src.R);
                    dst.G = (byte)((1f - f) * dst.G + f * src.G);
                    dst.B = (byte)((1f - f) * dst.B + f * src.B);
                }
            }

            CallDispose(src, dst);

            return true;
        }

        public static bool AlphaSpots(ref Bitmap bmp, byte offset,
                                                               params RadiusPos[] rp)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (rp.Length == 0) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            double cx, cy, rr, r, f;

            BmpProc32 src = new BmpProc32(tmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    src[x, y, eRGB.a] = offset;

            for (int i = 0; i < rp.Length; i++)
            {
                cx = rp[i].PosX; cy = rp[i].PosY;
                rr = rp[i].Radius; rr = rr * rr;

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        r = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                        f = 1.2d * (1d - r / rr);
                        if (f > 1d) f = 1d;
                        if (f > 0)
                            src[x, y, eRGB.a] = Math.Max(src[x, y, eRGB.a], AdjustByte(255 * f));
                    }
            }

            src.Dispose();

            bmp.Dispose();

            bmp = tmp;

            return true;
        }

        public static bool SpotLights(ref Bitmap bmp, Color bkColor, byte offset,
                                                               params RadiusPos[] rp)
        {
            if (!AlphaSpots(ref bmp, offset, rp)) return false;

            Bitmap tmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            bmp.Dispose();

            bmp = tmp;

            return true;
        }

        public static bool AlphaRects(ref Bitmap bmp, byte offset,
                                                       params Rectangle[] rct)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (rct.Length == 0) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            double pi = Math.PI;
            double f;

            BmpProc32 src = new BmpProc32(tmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    src[x, y, eRGB.a] = offset;

            for (int i = 0; i < rct.Length; i++)
            {

                for (int y = rct[i].Y; y < rct[i].Y + rct[i].Height; y++)
                    for (int x = rct[i].X; x < rct[i].X + rct[i].Width; x++)
                    {
                        if ((x < 0) | (x > w - 1) |
                                       (y < 0) | (y > h - 1)) continue;

                        f = 1.2 * Math.Sin((x - rct[i].X) * pi / rct[i].Width) *
                                       Math.Sin((y - rct[i].Y) * pi / rct[i].Height);
                        if (f > 1d) f = 1d;
                        src[x, y, eRGB.a] = Math.Max(src[x, y, eRGB.a], (byte)(f * 255));
                    }

            }

            src.Dispose();

            bmp.Dispose();

            bmp = tmp;

            return true;
        }

        public static bool RectLights(ref Bitmap bmp, Color bkColor, byte offset,
                                                              params Rectangle[] rct)
        {
            if (!AlphaRects(ref bmp, offset, rct)) return false;

            Bitmap tmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            bmp.Dispose();

            bmp = tmp;

            return true;
        }

        public static bool AlphaGradient(ref Bitmap bmp, float[] factor,
                                              float[] position, GradientSide gs)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            Color startColor = Color.White;
            Color endColor = Color.Black;

            Point start = new Point(-1, 0);
            Point end = new Point(0, 0);

            switch (gs)
            {
                case GradientSide.Left:
                    end.X = w;
                    break;

                case GradientSide.Right:
                    start.X = w;
                    break;

                case GradientSide.Upper:
                    end.Y = h;
                    break;

                case GradientSide.Lower:
                    start.Y = h;
                    break;

                case GradientSide.UpperLeft:
                    end.X = w;
                    end.Y = h;
                    break;

                case GradientSide.UpperRight:
                    start.X = w;
                    end.Y = h;
                    break;

                case GradientSide.LowerLeft:
                    start.Y = h;
                    end.X = w;
                    break;

                case GradientSide.LowerRight:
                    start.X = w;
                    start.Y = h;
                    break;
            }

            Blend bl = new Blend();
            bl.Factors = factor;
            bl.Positions = position;

            LinearGradientBrush br = new LinearGradientBrush(
                                        start, end, startColor, endColor);
            br.Blend = bl;
            //br.GammaCorrection = true;

            Rectangle rct = new Rectangle(0, 0, w, h);

            g = Graphics.FromImage(bmp);
            g.FillRectangle(br, rct);
            g.Dispose();

            BmpProc24 src = new BmpProc24(bmp);
            BmpProc32 dst = new BmpProc32(tmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    dst[x, y, eRGB.a] = src[x, y, eRGB.r];

            CallDispose(dst, src, bmp);

            bmp = tmp;

            return true;
        }

        public static bool GradientColor(ref Bitmap bmp, float[] factor,
                                              float[] position, GradientSide gs)
        {
            Bitmap tmp = bmp.Clone() as Bitmap;

            if (!AlphaGradient(ref tmp, factor, position, gs))
            {
                tmp.Dispose();
                return false;
            }

            GrayScale24(ref bmp);

            Graphics g = Graphics.FromImage(bmp);
            g.DrawImageUnscaled(tmp, 0, 0);
            g.Dispose();

            tmp.Dispose();

            return true;
        }

        public static bool Dots(Bitmap bmp, int zone,
                                  Color dark, Color bright, out Bitmap dot)
        {
            dot = null;

            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 3) | (zone > 15)) return false;

            Bitmap tmp = bmp.Clone() as Bitmap;

            GrayScale(ref tmp);

            int w = bmp.Width;
            int h = bmp.Height;

            int count;
            double sum;
            double propc = zone / 28.5d;
            float cx, cy, cl, ct, cw, ch;

            dot = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(dot);
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            SolidBrush br = new SolidBrush(Color.Black);

            BmpProc8 src = new BmpProc8(tmp);

            for (int y = 0; y < h - 1; y += zone)
                for (int x = 0; x < w - 1; x += zone)
                {
                    count = 0;
                    sum = 0;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((iy > h - 1) | (ix > w - 1)) continue;

                            count++;
                            sum += 255 - src[ix, iy];
                        }

                    sum = Math.Sqrt(sum / count) * propc;

                    cx = x + (float)zone / 2f;
                    cy = y + (float)zone / 2f;

                    cl = (float)(cx - sum); ct = (float)(cy - sum);
                    cw = (float)(sum * 2); ch = (float)(sum * 2);

                    g.FillEllipse(br, cl, ct, cw, ch);
                }

            CallDispose(src, tmp);

            br.Dispose();
            g.Dispose();

            GrayScale(ref dot);
            SetTwoColorGrayPalette(dot, dark, bright);

            return true;
        }

        public static bool Caricature(ref Bitmap bmp, double factor,
                                       int cx, int cy, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (factor < 0) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 2; // margin for interpolations

            if ((cx == 0) & (cy == 0)) { cx = w / 2; cy = h / 2; }

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            g = Graphics.FromImage(bmp);
            g.Clear(bkColor);
            g.Dispose();

            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);

            double r, a, xx, yy;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx);                // radian

                    xx = Math.Sqrt(r * factor) * Math.Cos(a) + cx;
                    yy = Math.Sqrt(r * factor) * Math.Sin(a) + cy;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBicubic(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Fisheye(ref Bitmap bmp, double factor,
                                       int cx, int cy, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (factor < 0) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 2; // margin for interpolations

            if ((cx == 0) & (cy == 0)) { cx = w / 2; cy = h / 2; }

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            g = Graphics.FromImage(bmp);
            g.Clear(bkColor);
            g.Dispose();

            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);

            double r2, a, xx, yy;

            double c = factor / 100d;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    a = Math.Atan2(y - cy, x - cx);                // radian

                    xx = c * r2 * Math.Cos(a) + cx;
                    yy = c * r2 * Math.Sin(a) + cy;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBicubic(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Radiance(ref Bitmap bmp, double factor, int cx, int cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double r, r1, a;
            int f, rr, gg, bb, xx, yy, count;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx);                // radian
                    f = (int)(factor * r * 0.1);

                    if (f < 1) continue;

                    rr = gg = bb = count = 0;

                    for (int i = -f; i <= 0; i++)
                    {
                        r1 = r + i;
                        xx = (int)(r1 * Math.Cos(a) + cx);
                        yy = (int)(r1 * Math.Sin(a) + cy);

                        if ((xx < 0) | (xx > w - 1)) continue;
                        if ((yy < 0) | (yy > h - 1)) continue;

                        src.SetXY(xx, yy);

                        rr += src.R;
                        gg += src.G;
                        bb += src.B;
                        count++;
                    }

                    dst.SetXY(x, y);

                    dst.R = (byte)(rr / count);
                    dst.G = (byte)(gg / count);
                    dst.B = (byte)(bb / count);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Cyclone(ref Bitmap bmp, double factor, int cx, int cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            if ((cx == 0) & (cy == 0)) { cx = w / 2; cy = h / 2; }

            double r, a, a1;
            int f, rr, gg, bb, xx, yy, count;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx); // radian

                    f = (int)(Math.Sqrt(r) * factor);

                    if (f < 1) continue;

                    rr = gg = bb = count = 0;

                    for (int i = 0; i <= f; i++)
                    {
                        a1 = a + 0.007 * i;
                        xx = (int)(r * Math.Cos(a1) + cx);
                        yy = (int)(r * Math.Sin(a1) + cy);

                        if ((xx < 0) | (xx > w - 1)) continue;
                        if ((yy < 0) | (yy > h - 1)) continue;

                        src.SetXY(xx, yy);

                        rr += src.R;
                        gg += src.G;
                        bb += src.B;
                        count++;
                    }

                    dst.SetXY(x, y);

                    dst.R = (byte)(rr / count);
                    dst.G = (byte)(gg / count);
                    dst.B = (byte)(bb / count);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Ellipse(ref Bitmap bmp, Color bkColor)
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

                        intBicubic(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp1);

            g = Graphics.FromImage(bmp);
            g.Clear(bkColor);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(tmp2, 0, 0, w, h);
            g.Dispose();

            tmp2.Dispose();

            return true;
        }

        public static bool Sobel(ref Bitmap bmp, bool fGrayScale)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int[,] hmask = { {-1, -2, -1},
                             { 0,  0,  0},
                             { 1,  2,  1}};
            int[,] vmask = { {-1,  0,  1},
                             {-2,  0,  2},
                             {-1,  0,  1}};

            int xx, yy, mx, my;
            double rv, gv, bv, rh, gh, bh;

            Bitmap tmp = (Bitmap)bmp.Clone();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    rv = gv = bv = rh = gh = bh = 0;

                    for (int iy = y - 1; iy <= y + 1; iy++)
                        for (int ix = x - 1; ix <= x + 1; ix++)
                        {
                            mx = ix - x + 1; my = iy - y + 1;
                            xx = ix; yy = iy;
                            if ((ix < 0) | (ix > w - 1)) xx = x;
                            if ((iy < 0) | (iy > h - 1)) yy = y;

                            src.SetXY(xx, yy);

                            rv += src.R * vmask[mx, my];
                            gv += src.G * vmask[mx, my];
                            bv += src.B * vmask[mx, my];

                            rh += src.R * hmask[mx, my];
                            gh += src.G * hmask[mx, my];
                            bh += src.B * hmask[mx, my];
                        }

                    dst.SetXY(x, y);

                    dst.R = AdjustByte(Math.Sqrt(rv * rv + rh * rh));
                    dst.G = AdjustByte(Math.Sqrt(gv * gv + gh * gh));
                    dst.B = AdjustByte(Math.Sqrt(bv * bv + bh * bh));
                }

            CallDispose(dst, src, tmp);

            if (fGrayScale)
                return GrayScale(ref bmp);
            else
                return true;
        }

        public static bool ErrorDiffusionDither(Bitmap bmp, ErrorDiffusion ed, bool fStretch,
                            Color dark, Color bright, byte randomness, out Bitmap bmp1)
        {
            bmp1 = null;

            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((randomness < 0) | (randomness > 30)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = (Bitmap)bmp.Clone();

            GrayScale(ref tmp);

            if (fStretch) HistoStretch(ref tmp);

            double[,] fs = { {       -1,       -1, 7.0/16.0},
                             { 3.0/16.0, 5.0/16.0, 1.0/16.0} };

            double[,] st = { {-1, -1,  -1, 8.0/42.0, 4.0/42.0},
                             {2.0/42.0, 4.0/42.0, 8.0/42.0, 4.0/42.0, 2.0/42.0},
                             {1.0/42.0, 2.0/42.0, 4.0/42.0, 2.0/42.0, 1.0/42.0} };

            double[,] sr = { {-1, -1,  -1, 5.0/32.0, 3.0/32.0},
                             {2.0/32.0, 4.0/32.0, 5.0/32.0, 4.0/32.0, 2.0/32.0},
                             {-1, 2.0/32.0, 3.0/32.0, 2.0/32.0, -1} };

            double[,] jjn = { {-1, -1, -1, 7.0/48.0, 5.0/48.0},
                              {3.0/48.0, 5.0/48.0, 7.0/48.0, 5.0/48.0, 3.0/48.0},
                              {1.0/48.0, 3.0/48.0, 5.0/48.0, 3.0/48.0, 1.0/48.0} };


            bmp1 = new Bitmap(w, h, PixelFormat.Format1bppIndexed);

            byte d8;
            bool d;
            double err;
            int xx, yy;

            byte threshold = 127;

            Random rnd = new Random();
            byte randn = (byte)(randomness * 2 + 1);

            double[,] df;

            df = st;

            switch (ed)
            {
                case ErrorDiffusion.FloydSteinberg: df = fs; break;
                case ErrorDiffusion.Stucci: df = st; break;
                case ErrorDiffusion.Sierra: df = sr; break;
                case ErrorDiffusion.JaJuNi: df = jjn; break;
            }

            int row = df.GetLength(0);
            int col = df.GetLength(1);

            int xrange = (col - 1) / 2;

            BmpProc8 src = new BmpProc8(tmp);
            BmpProc1 dst = new BmpProc1(bmp1);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (randomness > 0)
                        threshold = (byte)(127 + rnd.Next(randn) - randomness);

                    d8 = src[x, y];
                    d = (d8 > threshold);
                    dst[x, y] = d;

                    if (d) { err = d8 - 255; } else { err = d8; }

                    for (int iy = 0; iy < row; iy++)
                        for (int ix = -xrange; ix <= xrange; ix++)
                        {
                            xx = x + ix; if ((xx < 0) | (xx > w - 1)) continue;
                            yy = y + iy; if (yy > h - 1) continue;

                            if (df[iy, ix + xrange] < 0) continue;

                            src[xx, yy] = AdjustByte(src[xx, yy] +
                                                           err * df[iy, ix + xrange]);
                        }
                }

            CallDispose(dst, src, tmp);

            ColorPalette cp = bmp1.Palette;
            cp.Entries[0] = dark;
            cp.Entries[1] = bright;
            bmp1.Palette = cp;

            return true;
        }

        public static Bitmap MakeCheckerBoard(int width, int height, int block,
                                                Color dark, Color bright, bool fRandomDark)
        {
            int xi = width / block + 1;
            int yi = height / block + 1;

            SolidBrush dbr = new SolidBrush(dark);
            SolidBrush bbr = new SolidBrush(bright);
            SolidBrush br;

            byte r, g, b;

            Random rnd = new Random();

            Rectangle rct = new Rectangle(0, 0, block, block);

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            Graphics gr = Graphics.FromImage(bmp);

            for (int x = 0; x < xi; x++)
                for (int y = 0; y < yi; y++)
                {
                    rct.X = block * x;
                    rct.Y = block * y;

                    if ((x + y) % 2 == 0)
                    {
                        if (fRandomDark)
                        {
                            do
                            {
                                r = (byte)rnd.Next(256);
                                g = (byte)rnd.Next(256);
                                b = (byte)rnd.Next(256);
                            }
                            while ((r + g + b) > 330);

                            br = new SolidBrush(Color.FromArgb(r, g, b));
                            gr.FillRectangle(br, rct);
                            br.Dispose();
                        }
                        else
                        {
                            br = dbr;
                            gr.FillRectangle(br, rct);
                        }
                    }
                    else
                    {
                        br = bbr;
                        gr.FillRectangle(br, rct);
                    }
                }

            gr.Dispose();
            bbr.Dispose();
            dbr.Dispose();

            return bmp;
        }

        public static Bitmap MakeCheckerBoard(int width, int height, int block)
        {
            return MakeCheckerBoard(width, height, block, Color.Black, Color.White, false);
        }

        public static Bitmap MakeCheckerBoard(int width, int height, int block,
                                                                 Color dark, Color bright)
        {
            return MakeCheckerBoard(width, height, block, dark, bright, false);
        }

        public static bool Oscillo(ref Bitmap bmp, int width, bool bHorizontal,
                                                        Color dark, Color bright)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((width < 2) | (width > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int interval = width * 2 + 1;

            double sum;
            int count;

            Bitmap tmp = bmp.Clone() as Bitmap;
            GrayScale(ref tmp);
            HistoStretch(ref tmp);

            Pen pn = new Pen(Color.Black, 1.0f);

            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            BmpProc8 src = new BmpProc8(tmp);

            if (bHorizontal)
                for (int y = width; y < h + width; y += interval)
                    for (int x = 0; x < w; x++)
                    {
                        sum = 0; count = 0;
                        for (int iy = y - width; iy <= y + width; iy++)
                        {
                            if (iy > h - 1) break;
                            count++;
                            sum += 255 - src[x, iy];
                        }
                        sum = sum * width / (count * 255d);
                        sum = Math.Max(0.1d, sum);

                        g.DrawLine(pn, (float)x, (float)(y + sum),
                                                   (float)x, (float)(y - sum));
                    }
            else
                for (int x = width; x < w + width; x += interval)
                    for (int y = 0; y < h; y++)
                    {
                        sum = 0; count = 0;
                        for (int ix = x - width; ix <= x + width; ix++)
                        {
                            if (ix > w - 1) break;
                            count++;
                            sum += 255 - src[ix, y];
                        }
                        sum = sum * width / (count * 255d);
                        sum = Math.Max(0.1d, sum);

                        g.DrawLine(pn, (float)(x - sum), (float)y,
                                                  (float)(x + sum), (float)y);
                    }

            src.Dispose();
            g.Dispose();

            GrayScale(ref bmp);

            SetTwoColorGrayPalette(bmp, dark, bright);

            return true;
        }

        public static bool Kuwahara(ref Bitmap bmp, int block)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((block < 1) | (block > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = (block + 1) * (block + 1);

            int[] v = new int[4];
            Point[] evP = new Point[4];
            Rectangle rct = new Rectangle(0, 0, w - 1, h - 1);
            int[] xini = new int[4];
            int[] xend = new int[4];
            int[] yini = new int[4];
            int[] yend = new int[4];

            int r, g, b, maxr, maxg, maxb, minr, ming, minb, indx, min;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {

                    evP[0].X = x - block; evP[0].Y = y - block; // upper-left
                    xini[0] = x - block; xend[0] = x;
                    yini[0] = y - block; yend[0] = y;

                    evP[1].X = x + block; evP[1].Y = y - block; // upper-right
                    xini[1] = x; xend[1] = x + block;
                    yini[1] = y - block; yend[1] = y;

                    evP[2].X = x + block; evP[2].Y = y + block; // lower-right
                    xini[2] = x; xend[2] = x + block;
                    yini[2] = y; yend[2] = y + block;

                    evP[3].X = x - block; evP[3].Y = y + block; // lower-left
                    xini[3] = x - block; xend[3] = x;
                    yini[3] = y; yend[3] = y + block;

                    for (int i = 0; i <= 3; i++)
                    {
                        v[i] = 1000;
                        if (!rct.Contains(evP[i])) continue;

                        maxr = maxg = maxb = 0; minr = ming = minb = 255;
                        for (int ix = xini[i]; ix <= xend[i]; ix++)
                            for (int iy = yini[i]; iy <= yend[i]; iy++)
                            {
                                src.SetXY(ix, iy);
                                if (src.R > maxr) maxr = src.R;
                                if (src.R < minr) minr = src.R;
                                if (src.G > maxg) maxg = src.G;
                                if (src.G < ming) ming = src.G;
                                if (src.B > maxb) maxb = src.B;
                                if (src.B < minb) minb = src.B;
                            }

                        v[i] = (maxr - minr) + (maxg - ming) + (maxb - minb);
                    }

                    min = 1000; indx = 0;
                    for (int i = 0; i <= 3; i++)
                        if (v[i] < min) { min = v[i]; indx = i; }

                    r = g = b = 0;

                    for (int ix = xini[indx]; ix <= xend[indx]; ix++)
                        for (int iy = yini[indx]; iy <= yend[indx]; iy++)
                        {
                            src.SetXY(ix, iy);
                            r += src.R;
                            g += src.G;
                            b += src.B;
                        }

                    dst.SetXY(x, y);

                    dst.R = AdjustByte(r / num);
                    dst.G = AdjustByte(g / num);
                    dst.B = AdjustByte(b / num);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Pattern(ref Bitmap bmp, PatternShape ps, int width, int height)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((width < 5) | (width > 20)) return false;
            if ((height < 5) | (height > 60)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double ww = (double)width;
            double hh = (double)height;

            double[] wd = new double[height];
            int[] iwd = new int[height];

            switch (ps)
            {
                case PatternShape.Brick:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 2d))
                            wd[i] = ww;
                        else
                            wd[i] = 0;
                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;

                case PatternShape.Diamond:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 2d))
                            wd[i] = ww * (double)i * 2d / hh;
                        else
                            wd[i] = ww * 2d - ww * (double)i * 2d / hh;
                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;

                case PatternShape.Hexagon:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 6d))
                            wd[i] = ww * (double)i * 6d / hh;
                        else
                        {
                            if ((double)i < (hh / 2d))
                                wd[i] = ww;
                            else
                            {
                                if ((double)i < (hh * 2d / 3d))
                                    wd[i] = ww * 4d - ww * (double)i * 6d / hh;
                                else
                                    wd[i] = 0;
                            }
                        }

                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;

                case PatternShape.Circle:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 2d))
                            wd[i] = ww * (double)i * 2d / hh
                                    - (ww / 8d) * Math.Sin((double)i * 4d / hh * Math.PI);
                        else
                            wd[i] = ww * 2d - ww * (double)i * 2d / hh
                             + (ww / 8d) * Math.Sin((double)(i - hh / 2d) * 4d / hh * Math.PI);
                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;
            }

            bool eflag = false;

            int r, g, b, count, d, im;
            double rm;

            Bitmap tmp = bmp.Clone() as Bitmap;

            Graphics gr = Graphics.FromImage(bmp);
            gr.SmoothingMode = SmoothingMode.AntiAlias;

            Pen pn = new Pen(Color.Black, 1.0f);

            BmpProc24 src = new BmpProc24(tmp);

            for (int x = 0; x < w - 1 + width; x += width)
            {
                eflag = !eflag;

                for (int y = 0; y < h - 1; y++)
                {
                    r = g = b = count = 0;

                    d = y % height;

                    if (eflag)
                    {

                        im = iwd[d];
                        rm = wd[d];
                    }
                    else
                    {
                        im = width - iwd[d];
                        rm = ww - wd[d];
                    }

                    for (int ix = x - im; ix <= x + im; ix++)
                    {
                        if ((ix < 0) | (ix > w - 1)) continue;

                        src.SetXY(ix, y);
                        r += src.R;
                        g += src.G;
                        b += src.B;
                        count++;
                    }

                    if (count == 0) continue;

                    r /= count;
                    g /= count;
                    b /= count;

                    pn.Color = Color.FromArgb(r, g, b);

                    gr.DrawLine(pn, (float)(x - rm), (float)y,
                                               (float)(x + rm), (float)y);
                }
            }

            CallDispose(src, gr, tmp);

            return true;
        }

        public static bool KuwaharaMedian(ref Bitmap bmp, int block)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((block < 1) | (block > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = (block + 1) * (block + 1);

            int[] v = new int[4];
            Point[] evP = new Point[4];
            Rectangle rct = new Rectangle(0, 0, w - 1, h - 1);
            int[] xini = new int[4];
            int[] xend = new int[4];
            int[] yini = new int[4];
            int[] yend = new int[4];

            int iarr, maxr, maxg, maxb, minr, ming, minb, indx, min;

            byte[] rr = new byte[num];
            byte[] gg = new byte[num];
            byte[] bb = new byte[num];

            int imed = num / 2;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {

                    evP[0].X = x - block; evP[0].Y = y - block; // upper-left
                    xini[0] = x - block; xend[0] = x;
                    yini[0] = y - block; yend[0] = y;

                    evP[1].X = x + block; evP[1].Y = y - block; // upper-right
                    xini[1] = x; xend[1] = x + block;
                    yini[1] = y - block; yend[1] = y;

                    evP[2].X = x + block; evP[2].Y = y + block; // lower-right
                    xini[2] = x; xend[2] = x + block;
                    yini[2] = y; yend[2] = y + block;

                    evP[3].X = x - block; evP[3].Y = y + block; // lower-left
                    xini[3] = x - block; xend[3] = x;
                    yini[3] = y; yend[3] = y + block;

                    for (int i = 0; i <= 3; i++)
                    {
                        v[i] = 1000;
                        if (!rct.Contains(evP[i])) continue;

                        maxr = maxg = maxb = 0; minr = ming = minb = 255;
                        for (int ix = xini[i]; ix <= xend[i]; ix++)
                            for (int iy = yini[i]; iy <= yend[i]; iy++)
                            {
                                src.SetXY(ix, iy);
                                if (src.R > maxr) maxr = src.R;
                                if (src.R < minr) minr = src.R;
                                if (src.G > maxg) maxg = src.G;
                                if (src.G < ming) ming = src.G;
                                if (src.B > maxb) maxb = src.B;
                                if (src.B < minb) minb = src.B;
                            }

                        v[i] = (maxr - minr) + (maxg - ming) + (maxb - minb);
                    }

                    min = 1000; indx = 0;
                    for (int i = 0; i <= 3; i++)
                        if (v[i] < min) { min = v[i]; indx = i; }

                    iarr = 0;

                    for (int ix = xini[indx]; ix <= xend[indx]; ix++)
                        for (int iy = yini[indx]; iy <= yend[indx]; iy++)
                        {
                            src.SetXY(ix, iy);
                            rr[iarr] = src.R;
                            gg[iarr] = src.G;
                            bb[iarr] = src.B;

                            iarr++;
                        }

                    Array.Sort(rr);
                    Array.Sort(gg);
                    Array.Sort(bb);

                    dst.SetXY(x, y);

                    dst.R = rr[imed];
                    dst.G = gg[imed];
                    dst.B = bb[imed];
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool KuwaharaOilPaint(ref Bitmap bmp, int block)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((block < 1) | (block > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = (block + 1) * (block + 1);

            int[] v = new int[4];
            Point[] evP = new Point[4];
            Rectangle rct = new Rectangle(0, 0, w - 1, h - 1);
            int[] xini = new int[4];
            int[] xend = new int[4];
            int[] yini = new int[4];
            int[] yend = new int[4];

            int r, g, b, maxr, maxg, maxb, minr, ming, minb, indx, min;

            int[] rh = new int[16];
            int[] gh = new int[16];
            int[] bh = new int[16];

            int rindx, gindx, bindx, countr, countg, countb;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {

                    evP[0].X = x - block; evP[0].Y = y - block; // upper-left
                    xini[0] = x - block; xend[0] = x;
                    yini[0] = y - block; yend[0] = y;

                    evP[1].X = x + block; evP[1].Y = y - block; // upper-right
                    xini[1] = x; xend[1] = x + block;
                    yini[1] = y - block; yend[1] = y;

                    evP[2].X = x + block; evP[2].Y = y + block; // lower-right
                    xini[2] = x; xend[2] = x + block;
                    yini[2] = y; yend[2] = y + block;

                    evP[3].X = x - block; evP[3].Y = y + block; // lower-left
                    xini[3] = x - block; xend[3] = x;
                    yini[3] = y; yend[3] = y + block;

                    for (int i = 0; i <= 3; i++)
                    {
                        v[i] = 1000;
                        if (!rct.Contains(evP[i])) continue;

                        maxr = maxg = maxb = 0; minr = ming = minb = 255;
                        for (int ix = xini[i]; ix <= xend[i]; ix++)
                            for (int iy = yini[i]; iy <= yend[i]; iy++)
                            {
                                src.SetXY(ix, iy);
                                if (src.R > maxr) maxr = src.R;
                                if (src.R < minr) minr = src.R;
                                if (src.G > maxg) maxg = src.G;
                                if (src.G < ming) ming = src.G;
                                if (src.B > maxb) maxb = src.B;
                                if (src.B < minb) minb = src.B;
                            }

                        v[i] = (maxr - minr) + (maxg - ming) + (maxb - minb);
                    }

                    min = 1000; indx = 0;
                    for (int i = 0; i <= 3; i++)
                        if (v[i] < min) { min = v[i]; indx = i; }

                    maxr = maxg = maxb = -1;
                    for (int i = 0; i <= 15; i++) { rh[i] = gh[i] = bh[i] = 0; }

                    for (int ix = xini[indx]; ix <= xend[indx]; ix++)
                        for (int iy = yini[indx]; iy <= yend[indx]; iy++)
                        {
                            src.SetXY(ix, iy);
                            rh[src.R >> 4]++;
                            gh[src.G >> 4]++;
                            bh[src.B >> 4]++;
                        }

                    rindx = gindx = bindx = 0;

                    for (int i = 0; i <= 15; i++)
                    {
                        if (maxr < rh[i]) { maxr = rh[i]; rindx = i; }
                        if (maxg < gh[i]) { maxg = gh[i]; gindx = i; }
                        if (maxb < bh[i]) { maxb = bh[i]; bindx = i; }
                    }

                    r = g = b = countr = countg = countb = 0;

                    for (int ix = xini[indx]; ix <= xend[indx]; ix++)
                        for (int iy = yini[indx]; iy <= yend[indx]; iy++)
                        {
                            src.SetXY(ix, iy);
                            if ((src.R >> 4) == rindx)
                            { r += src.R; countr++; }
                            if ((src.G >> 4) == gindx)
                            { g += src.G; countg++; }
                            if ((src.B >> 4) == bindx)
                            { b += src.B; countb++; }
                        }

                    dst.SetXY(x, y);

                    dst.R = (byte)(r / countr);
                    dst.G = (byte)(g / countg);
                    dst.B = (byte)(b / countb);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Quad(ref Bitmap bmp, QuadPosition qp)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tgt = new Bitmap(w * 2, h * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tgt);

            switch (qp)
            {
                case QuadPosition.qpUpperLeft:
                    g.DrawImageUnscaled(bmp, 0, 0);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImageUnscaled(bmp, w, 0);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    g.DrawImageUnscaled(bmp, w, h);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImageUnscaled(bmp, 0, h);
                    break;

                case QuadPosition.qpUpperRight:
                    g.DrawImageUnscaled(bmp, w, 0);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    g.DrawImageUnscaled(bmp, w, h);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImageUnscaled(bmp, 0, h);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    g.DrawImageUnscaled(bmp, 0, 0);
                    break;

                case QuadPosition.qpLowerLeft:
                    g.DrawImageUnscaled(bmp, 0, h);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    g.DrawImageUnscaled(bmp, 0, 0);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImageUnscaled(bmp, w, 0);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    g.DrawImageUnscaled(bmp, w, h);
                    break;

                case QuadPosition.qpLowerRight:
                    g.DrawImageUnscaled(bmp, w, h);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImageUnscaled(bmp, 0, h);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    g.DrawImageUnscaled(bmp, 0, 0);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImageUnscaled(bmp, w, 0);
                    break;
            }

            g.Dispose();
            bmp.Dispose();

            bmp = tgt;

            return true;
        }

        public static bool InsideOut(ref Bitmap bmp, double cx, double cy,
                                        double radius, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 2; // margin for interpolations

            if ((cx == 0) & (cy == 0)) { cx = (double)w / 2; cy = (double)h / 2; }

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            g = Graphics.FromImage(bmp);
            if (bkColor != Color.Transparent) g.Clear(bkColor);
            g.Dispose();

            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);

            double r, a, xx, yy;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                    if (r > radius) continue;

                    r = radius - r;
                    a = Math.Atan2(y - cy, x - cx);                // radian

                    xx = r * Math.Cos(a) + cx;
                    yy = r * Math.Sin(a) + cy;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBicubic(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static TextureBrush CreateWeaveBrush(int width, Color bkColor)
        {
            if (width < 10) return null;

            int r2 = (int)(width * 0.15 + 0.5);
            int rr = r2 * 2;
            int w = width * 2 + rr * 2;

            float[] myFactors =   { 0.0f, 0.9f, 1.0f, 1.0f, 0.9f, 0.0f };
            float[] myPositions = { 0.0f, 0.17f, 0.4f, 0.6f, 0.83f, 1.0f };

            Blend myBlend = new Blend();
            myBlend.Factors = myFactors;
            myBlend.Positions = myPositions;

            Rectangle rct = new Rectangle(0, 0, width, width + rr * 2);

            LinearGradientBrush lb = new LinearGradientBrush(
                rct,
                bkColor,
                Color.Transparent,
                LinearGradientMode.Vertical);
            lb.Blend = myBlend;

            Bitmap grd = new Bitmap(width, width + rr * 2, PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(grd);
            g.FillRectangle(lb, rct);
            g.Dispose();

            lb.Dispose();

            Bitmap bmp = new Bitmap(w, w, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(bmp);

            g.DrawImageUnscaled(grd, r2, -r2);
            g.DrawImageUnscaled(grd, r2, w - r2);
            g.DrawImageUnscaled(grd, width + rr + r2, -width - rr - r2);
            g.DrawImageUnscaled(grd, width + rr + r2, width + r2);

            grd.RotateFlip(RotateFlipType.Rotate90FlipNone);

            g.DrawImageUnscaled(grd, -width - rr - r2, r2);
            g.DrawImageUnscaled(grd, width + r2, r2);
            g.DrawImageUnscaled(grd, -r2, width + rr + r2);
            g.DrawImageUnscaled(grd, w - r2, width + rr + r2);

            grd.Dispose();

            Rectangle brect = new Rectangle(-r2, -r2, rr, rr);
            SolidBrush sb = new SolidBrush(bkColor);

            g.FillRectangle(sb, brect);
            brect.X = width + r2;
            g.FillRectangle(sb, brect);
            brect.X = w - r2;
            g.FillRectangle(sb, brect);
            brect.X = -r2; brect.Y = width + r2;
            g.FillRectangle(sb, brect);
            brect.X = width + r2;
            g.FillRectangle(sb, brect);
            brect.X = w - r2;
            g.FillRectangle(sb, brect);
            brect.X = -r2; brect.Y = w - r2;
            g.FillRectangle(sb, brect);
            brect.X = width + r2;
            g.FillRectangle(sb, brect);
            brect.X = w - r2;
            g.FillRectangle(sb, brect);

            sb.Dispose();

            g.Dispose();

            TextureBrush tb = new TextureBrush(bmp);

            return tb;
        }

        public static bool Weave(ref Bitmap bmp, int width,
                                             float angle, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (width < 10) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            TextureBrush tb = CreateWeaveBrush(width, bkColor);
            tb.RotateTransform(angle);

            Graphics g = Graphics.FromImage(bmp);

            g.FillRectangle(tb, new Rectangle(0, 0, w, h));

            g.Dispose();
            tb.Dispose();

            return true;
        }

        public static bool Luca(ref Bitmap bmp, bool bInvert,
                            int outerRadius, int innerRadius, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((outerRadius - innerRadius) < 30) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int radius = outerRadius - innerRadius;

            int mg = 3; // margin for interpolations

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2,
                                                PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            int rr = (radius + innerRadius) * 2 + 2;

            Bitmap dbmp = new Bitmap(rr, rr, PixelFormat.Format24bppRgb);

            double cx = (double)(radius + innerRadius + 1);
            double cy = (double)(radius + innerRadius + 1);

            g = Graphics.FromImage(dbmp);
            g.Clear(bkColor);
            g.Dispose();

            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);

            double r, a, xx, yy;
            double pi2 = Math.PI * 2d;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(dbmp);

            for (int y = 0; y < rr; y++)
                for (int x = 0; x < rr; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx);                // radian

                    r = r - innerRadius;


                    xx = (a + Math.PI) / pi2 * (w - 1);
                    if (bInvert)
                        yy = (1d - r / radius) * (h - 1);
                    else
                        yy = r / radius * (h - 1);

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBicubic(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp, bmp);
            bmp = dbmp;

            if (bInvert)
                bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            else
                bmp.RotateFlip(RotateFlipType.Rotate90FlipX);

            return true;

        }

        public static bool KuwaharaGrayScale(ref Bitmap bmp,
                                                int block, bool bDetail)
        {
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                return false;

            if ((block < 1) | (block > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = (block + 1) * (block + 1);

            int[] v = new int[4];
            Point[] evP = new Point[4];
            Rectangle rct = new Rectangle(0, 0, w - 1, h - 1);
            int[] xini = new int[4];
            int[] xend = new int[4];
            int[] yini = new int[4];
            int[] yend = new int[4];

            int d, max, min, indx;

            double t;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc8 src = new BmpProc8(tmp);
            BmpProc8 dst = new BmpProc8(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {

                    evP[0].X = x - block; evP[0].Y = y - block; // upper-left
                    xini[0] = x - block; xend[0] = x;
                    yini[0] = y - block; yend[0] = y;

                    evP[1].X = x + block; evP[1].Y = y - block; // upper-right
                    xini[1] = x; xend[1] = x + block;
                    yini[1] = y - block; yend[1] = y;

                    evP[2].X = x + block; evP[2].Y = y + block; // lower-right
                    xini[2] = x; xend[2] = x + block;
                    yini[2] = y; yend[2] = y + block;

                    evP[3].X = x - block; evP[3].Y = y + block; // lower-left
                    xini[3] = x - block; xend[3] = x;
                    yini[3] = y; yend[3] = y + block;

                    for (int i = 0; i <= 3; i++)
                    {
                        v[i] = 1000;
                        if (!rct.Contains(evP[i])) continue;

                        max = 0; min = 255;
                        for (int ix = xini[i]; ix <= xend[i]; ix++)
                            for (int iy = yini[i]; iy <= yend[i]; iy++)
                            {
                                d = src[ix, iy];
                                if (d > max) max = d;
                                if (d < min) min = d;
                            }

                        v[i] = max - min;
                    }

                    min = 1000; indx = 0;
                    for (int i = 0; i <= 3; i++)
                        if (v[i] < min) { min = v[i]; indx = i; }

                    d = 0;

                    for (int ix = xini[indx]; ix <= xend[indx]; ix++)
                        for (int iy = yini[indx]; iy <= yend[indx]; iy++)
                            d += src[ix, iy];

                    if (bDetail)
                    {
                        t = Math.Max(0.5d, (255d - (double)v[indx]) / 255d);
                        dst[x, y] = ImageUtils.AdjustByte(t * (d / num) +
                                                        (1d - t) * dst[x, y]);
                    }
                    else
                        dst[x, y] = ImageUtils.AdjustByte(d / num);
                }

            ImageUtils.CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Contour(ref Bitmap bmp, bool bStretch, bool b24bit)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Rectangle rct = new Rectangle(0, 0, w - 1, h - 1);

            double[] dis = new double[4];

            int[] ix = new int[4];
            int[] iy = new int[4];
            double max;
            double dd = 1.732d;
            byte r, g, b;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            SetGrayPalette(tmp);

            BmpProc24 src = new BmpProc24(bmp);
            BmpProc8 dst = new BmpProc8(tmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    src.SetXY(x, y);
                    r = src.R; g = src.G; b = src.B;

                    ix[0] = x + 1; iy[0] = y - 1;   // upper-right
                    ix[1] = x + 1; iy[1] = y;       // right
                    ix[2] = x + 1; iy[2] = y + 1;   // lower-right
                    ix[3] = x; iy[3] = y + 1;   // lower

                    for (int i = 0; i < 4; i++)
                        if (rct.Contains(ix[i], iy[i]))
                        {
                            src.SetXY(ix[i], iy[i]);
                            dis[i] = (src.R - r) * (src.R - r) +
                                     (src.G - g) * (src.G - g) +
                                     (src.B - b) * (src.B - b);
                        }
                        else
                            dis[i] = 0d;

                    max = 0;

                    for (int i = 0; i < 4; i++)
                        if (dis[i] > max) max = dis[i];

                    dst[x, y] = AdjustByte(255d - Math.Sqrt(max) / dd);
                }

            CallDispose(dst, src);

            if (bStretch) HistoStretch(ref tmp);

            if (b24bit)
            {
                Graphics gr = Graphics.FromImage(bmp);
                gr.DrawImageUnscaled(tmp, 0, 0);
                gr.Dispose();
                tmp.Dispose();
            }
            else
            {
                bmp.Dispose();
                bmp = tmp;
            }

            return true;
        }

        public static bool KuwaharaDetail(ref Bitmap bmp, int block)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((block < 1) | (block > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = (block + 1) * (block + 1);

            int[] v = new int[4];
            Point[] evP = new Point[4];
            Rectangle rct = new Rectangle(0, 0, w - 1, h - 1);
            int[] xini = new int[4];
            int[] xend = new int[4];
            int[] yini = new int[4];
            int[] yend = new int[4];
            int[] vr = new int[4];
            int[] vg = new int[4];
            int[] vb = new int[4];

            int r, g, b, maxr, maxg, maxb, minr, ming, minb, indx, min;
            double t;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {

                    evP[0].X = x - block; evP[0].Y = y - block; // upper-left
                    xini[0] = x - block; xend[0] = x;
                    yini[0] = y - block; yend[0] = y;

                    evP[1].X = x + block; evP[1].Y = y - block; // upper-right
                    xini[1] = x; xend[1] = x + block;
                    yini[1] = y - block; yend[1] = y;

                    evP[2].X = x + block; evP[2].Y = y + block; // lower-right
                    xini[2] = x; xend[2] = x + block;
                    yini[2] = y; yend[2] = y + block;

                    evP[3].X = x - block; evP[3].Y = y + block; // lower-left
                    xini[3] = x - block; xend[3] = x;
                    yini[3] = y; yend[3] = y + block;

                    for (int i = 0; i <= 3; i++)
                    {
                        v[i] = 1000;
                        if (!rct.Contains(evP[i])) continue;

                        maxr = maxg = maxb = 0; minr = ming = minb = 255;
                        for (int ix = xini[i]; ix <= xend[i]; ix++)
                            for (int iy = yini[i]; iy <= yend[i]; iy++)
                            {
                                src.SetXY(ix, iy);
                                if (src.R > maxr) maxr = src.R;
                                if (src.R < minr) minr = src.R;
                                if (src.G > maxg) maxg = src.G;
                                if (src.G < ming) ming = src.G;
                                if (src.B > maxb) maxb = src.B;
                                if (src.B < minb) minb = src.B;
                            }

                        vr[i] = maxr - minr;
                        vg[i] = maxg - ming;
                        vb[i] = maxb - minb;

                        v[i] = vr[i] + vg[i] + vb[i];
                    }

                    min = 1000; indx = 0;
                    for (int i = 0; i <= 3; i++)
                        if (v[i] < min) { min = v[i]; indx = i; }

                    r = g = b = 0;

                    for (int ix = xini[indx]; ix <= xend[indx]; ix++)
                        for (int iy = yini[indx]; iy <= yend[indx]; iy++)
                        {
                            src.SetXY(ix, iy);
                            r += src.R;
                            g += src.G;
                            b += src.B;
                        }

                    dst.SetXY(x, y);

                    t = Math.Max(0.6d, (255d - (double)vr[indx]) / 255d);
                    dst.R = AdjustByte(t * (r / num) + (1d - t) * dst.R);

                    t = Math.Max(0.6d, (255d - (double)vg[indx]) / 255d);
                    dst.G = AdjustByte(t * (g / num) + (1d - t) * dst.G);

                    t = Math.Max(0.6d, (255d - (double)vb[indx]) / 255d);
                    dst.B = AdjustByte(t * (b / num) + (1d - t) * dst.B);

                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool KuwaharaAlpha(ref Bitmap bmp, int block,
                                             float alpha, Color bkColor)
        {
            bool ret = KuwaharaDetail(ref bmp, block);

            if (!ret) return false;

            Bitmap tmp = bmp.Clone() as Bitmap;

            Graphics g = Graphics.FromImage(bmp);
            g.Clear(bkColor);
            JAlphaBlend(g, tmp, alpha, 0, 0);

            g.Dispose();
            tmp.Dispose();

            return true;
        }

        public static bool Mirror(ref Bitmap bmp, double cx, double cy,
                                        double angle, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((angle < 0) | (angle > 360)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            if ((cx < 0) | (cx > w - 1) | (cy < 0) | (cy > h - 1))
                return false;

            int mg = 3; // margin for interpolations
            RectangleF rct = new RectangleF(-1, -1, w + 1, h + 1);

            double deg = Math.PI / 180d;

            double a, b, sn, cs;

            if (Math.Abs(Math.Cos(angle * deg)) < 1.0E-20)
                a = 1.0E20;
            else
                a = Math.Sin(angle * deg) / Math.Cos(angle * deg);

            bool fUp = ((90 < angle) & (angle <= 270));

            b = cy - a * cx;

            if (fUp)
            {
                sn = Math.Sin((angle - 90) * deg);
                cs = Math.Cos((angle - 90) * deg);
            }
            else
            {
                sn = Math.Sin((angle + 90) * deg);
                cs = Math.Cos((angle + 90) * deg);
            }

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            double xx, yy, dd;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if ((fUp & (a * x + b > y)) | (!fUp & (a * x + b < y)))
                    {

                        dd = Math.Abs(y - a * x - b) / Math.Sqrt(1 + a * a);

                        if (y < a * x + b)
                        {
                            xx = x + 2 * dd * cs;
                            yy = y + 2 * dd * sn;
                        }
                        else
                        {
                            xx = x - 2 * dd * cs;
                            yy = y - 2 * dd * sn;
                        }

                        if (rct.Contains((float)xx, (float)yy))
                        {
                            xx = xx + mg;
                            yy = yy + mg;

                            ImageUtils.intBicubic(dst, src, x, y, xx, yy);
                        }
                        else
                        {
                            dst[x, y, eRGB.r] = bkColor.R;
                            dst[x, y, eRGB.g] = bkColor.G;
                            dst[x, y, eRGB.b] = bkColor.B;
                        }

                    }
                }

            ImageUtils.CallDispose(dst, src, tmp);

            return true;
        }

        public static bool CircularPixelate(ref Bitmap bmp, int rzone, int azone,
                                                          int cx, int cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            pixelinfo[,] pi = new pixelinfo[w, h];

            if ((cx == 0) & (cy == 0)) { cx = w / 2; cy = h / 2; }

            double degree = 180d / Math.PI;

            double r, a;
            int xx, yy;

            Rectangle rct = new Rectangle(0, 0, w - 1, h - 1);


            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx) * degree;      // degree

                    r = ((int)(r + 0.5) / rzone) * rzone;
                    a = ((int)(a + 0.5) / azone) * azone / degree; // radian

                    xx = (int)(r * Math.Cos(a) + cx);
                    yy = (int)(r * Math.Sin(a) + cy);

                    if (rct.Contains(xx, yy))
                    {
                        src.SetXY(x, y);
                        pi[xx, yy].r += src.R;
                        pi[xx, yy].g += src.G;
                        pi[xx, yy].b += src.B;
                        pi[xx, yy].count += 1;
                    }
                }

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (pi[x, y].count == 0) continue;

                    src.SetXY(x, y);

                    src.R = (byte)(pi[x, y].r / pi[x, y].count);
                    src.G = (byte)(pi[x, y].g / pi[x, y].count);
                    src.B = (byte)(pi[x, y].b / pi[x, y].count);
                }

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx) * degree;      // degree

                    r = ((int)(r + 0.5) / rzone) * rzone;
                    a = ((int)(a + 0.5) / azone) * azone / degree;  // radian

                    xx = (int)(r * Math.Cos(a) + cx);
                    yy = (int)(r * Math.Sin(a) + cy);

                    if (rct.Contains(xx, yy))
                    {
                        dst.SetXY(x, y);
                        src.SetXY(xx, yy);

                        dst.R = src.R; dst.G = src.G; dst.B = src.B;
                    }
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool PatternMosaic(ref Bitmap bmp, PatternShape ps,
                                                     int width, int height)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((width < 5) | (width > 20)) return false;
            if ((height < 5) | (height > 60)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double ww = (double)width;
            double hh = (double)height;

            double[] wd = new double[height];
            int[] iwd = new int[height];

            switch (ps)
            {
                case PatternShape.Brick:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 2d))
                            wd[i] = ww;
                        else
                            wd[i] = 0;
                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;

                case PatternShape.Diamond:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 2d))
                            wd[i] = ww * (double)i * 2d / hh;
                        else
                            wd[i] = ww * 2d - ww * (double)i * 2d / hh;
                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;

                case PatternShape.Hexagon:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 6d))
                            wd[i] = ww * (double)i * 6d / hh;
                        else
                        {
                            if ((double)i < (hh / 2d))
                                wd[i] = ww;
                            else
                            {
                                if ((double)i < (hh * 2d / 3d))
                                    wd[i] = ww * 4d - ww * (double)i * 6d / hh;
                                else
                                    wd[i] = 0;
                            }
                        }

                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;

                case PatternShape.Circle:
                    for (int i = 0; i < height; i++)
                    {
                        if ((double)i < (hh / 2d))
                            wd[i] = ww * (double)i * 2d / hh
                                - (ww / 8d) * Math.Sin((double)i * 4d / hh * Math.PI);
                        else
                            wd[i] = ww * 2d - ww * (double)i * 2d / hh
                                + (ww / 8d) * Math.Sin((double)(i - hh / 2d) *
                                                                    4d / hh * Math.PI);
                        iwd[i] = (int)(wd[i] + 0.5);
                    }
                    break;
            }

            bool eflag = false;

            int r, g, b, count, d, im, yinit;
            double rm;

            Bitmap tmp = bmp.Clone() as Bitmap;

            Graphics gr = Graphics.FromImage(bmp);
            gr.SmoothingMode = SmoothingMode.AntiAlias;

            Pen pn = new Pen(Color.Black, 1.0f);

            BmpProc24 src = new BmpProc24(tmp);

            for (int x = 0; x < w - 1 + width; x += width)
            {
                eflag = !eflag;

                if (eflag) yinit = 0; else yinit = -height / 2;

                for (int y = yinit; y < h - 1; y += height)
                {
                    r = g = b = count = 0;

                    for (int iy = y; iy < y + height; iy++)
                    {
                        if ((iy > h - 1) | (iy < 0)) continue;

                        d = iy % height;

                        if (eflag)
                        {
                            im = iwd[d];
                            rm = wd[d];
                        }
                        else
                        {
                            im = width - iwd[d];
                            rm = ww - wd[d];
                        }

                        for (int ix = x - im; ix <= x + im; ix++)
                        {
                            if ((ix < 0) | (ix > w - 1)) continue;

                            src.SetXY(ix, iy);
                            r += src.R;
                            g += src.G;
                            b += src.B;
                            count++;
                        }
                    }

                    if (count == 0) continue;

                    r /= count;
                    g /= count;
                    b /= count;

                    pn.Color = Color.FromArgb(r, g, b);

                    for (int iy = y; iy < y + height; iy++)
                    {
                        if ((iy > h - 1) | (iy < 0)) continue;

                        d = iy % height;

                        if (eflag)
                        {
                            im = iwd[d];
                            rm = wd[d];
                        }
                        else
                        {
                            im = width - iwd[d];
                            rm = (double)width - wd[d];
                        }

                        gr.DrawLine(pn, (float)(x - rm), (float)iy,
                                                 (float)(x + rm), (float)iy);

                    }
                }
            }

            CallDispose(src, gr, tmp);

            return true;

        }

        public static bool Demon(ref Bitmap bmp, float ratio,
                                                    float cx, float cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            float w = bmp.Width;
            float h = bmp.Height;
            float wh = Math.Min(w, h);

            if ((cx < 0) | (cx > w) | (cy < 0) | (cy > h))
                return false;

            if ((ratio > 1f) | (ratio < 0)) return false;

            Bitmap tmp = bmp.Clone() as Bitmap;

            Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int i = 0;
            float r, rwh;
            RectangleF rctf = new RectangleF(0f, 0f, w, h);

            do
            {
                i++;
                r = (float)Math.Pow(ratio, i);
                rctf.Width = w * r;
                rctf.Height = h * r;
                rctf.X = cx * (1f - r);
                rctf.Y = cy * (1f - r);
                g.DrawImage(tmp, rctf);
                rwh = wh * r;
            } while (rwh > 5d);

            g.Dispose();
            tmp.Dispose();

            return true;
        }

        public static bool SpiralDemon(ref Bitmap bmp, float ratio, float angle,
                                                    float cx, float cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            float w = bmp.Width;
            float h = bmp.Height;
            float wh = Math.Min(w, h);

            if ((cx < 0) | (cx > w) | (cy < 0) | (cy > h))
                return false;

            if ((ratio > 1f) | (ratio < 0)) return false;

            Bitmap tmp = bmp.Clone() as Bitmap;

            Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int i = 0;
            float r, rwh;
            Bitmap ttmp, tttmp;
            RectangleF rctf = new RectangleF(0f, 0f, w, h);

            do
            {
                i++;
                r = (float)Math.Pow(ratio, i);
                ttmp = ResizeBitmap(tmp, r, InterpolationMode.HighQualityBicubic);
                tttmp = BitmapRotate(ttmp, angle * i, Color.Transparent);

                rctf.Width = tttmp.Width;
                rctf.Height = tttmp.Height;
                rctf.X = cx * (1f - r) + (float)(ttmp.Width - tttmp.Width) / 2f;
                rctf.Y = cy * (1f - r) + (float)(ttmp.Height - tttmp.Height) / 2f;
                g.DrawImage(tttmp, rctf);

                CallDispose(ttmp, tttmp);

                rwh = wh * r;
            } while (rwh > 5d);

            g.Dispose();
            tmp.Dispose();

            return true;
        }

        public static bool OrderedDither(Bitmap bmp, DitherPattern dp, ErrorDiffusion ed,
                bool fStretch, Color dark, Color bright, out Bitmap bmp1)
        {
            bmp1 = null;

            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = (Bitmap)bmp.Clone();

            ImageUtils.GrayScale(ref tmp);

            if (fStretch) ImageUtils.HistoStretch(ref tmp);

            byte[,] od22 = { { 51, 204},
                             {153, 102} };

            byte[,] od33 = { {131, 182,  105},
                             {157, 236,  26},
                             { 79, 210,  52} };

            byte[,] od332 = { { 64, 128,  64},
                             {128, 196, 128},
                             { 64, 128,  64} };

            byte[,] od44 = { { 15, 195,  60, 240},
                             {135,  75, 180, 120},
                             { 45, 225,  30, 210},
                             {165, 105, 150,  90} };

            byte[,] od442 = { {  51, 102, 204, 153},
                              { 204, 153,  51, 102},
                              { 153, 204, 102,  51},
                              { 102,  51, 153, 204} };

            byte[,] od443 = { {  51, 102, 204, 153},
                              { 204, 153,  51, 102},
                              { 102,  51, 153, 204},
                              { 153, 204, 102,  51} };

            byte[,] msk = od22;

            switch (dp)
            {
                case DitherPattern.od22: msk = od22; break;
                case DitherPattern.od33: msk = od33; break;
                case DitherPattern.od332: msk = od332; break;
                case DitherPattern.od44: msk = od44; break;
                case DitherPattern.od442: msk = od442; break;
                case DitherPattern.od443: msk = od443; break;
            }

            double[,] fs = { {       -1,       -1, 7.0/16.0},
                             { 3.0/16.0, 5.0/16.0, 1.0/16.0} };

            double[,] st = { {-1, -1,  -1, 8.0/42.0, 4.0/42.0},
                             {2.0/42.0, 4.0/42.0, 8.0/42.0, 4.0/42.0, 2.0/42.0},
                             {1.0/42.0, 2.0/42.0, 4.0/42.0, 2.0/42.0, 1.0/42.0} };

            double[,] sr = { {-1, -1,  -1, 5.0/32.0, 3.0/32.0},
                             {2.0/32.0, 4.0/32.0, 5.0/32.0, 4.0/32.0, 2.0/32.0},
                             {-1, 2.0/32.0, 3.0/32.0, 2.0/32.0, -1} };

            double[,] jjn = { {-1, -1, -1, 7.0/48.0, 5.0/48.0},
                              {3.0/48.0, 5.0/48.0, 7.0/48.0, 5.0/48.0, 3.0/48.0},
                              {1.0/48.0, 3.0/48.0, 5.0/48.0, 3.0/48.0, 1.0/48.0} };

            double[,] df = st;

            switch (ed)
            {
                case ErrorDiffusion.FloydSteinberg: df = fs; break;
                case ErrorDiffusion.Stucci: df = st; break;
                case ErrorDiffusion.Sierra: df = sr; break;
                case ErrorDiffusion.JaJuNi: df = jjn; break;
            }

            byte d8;
            bool d;
            double err;
            int xx, yy;

            int row = df.GetLength(0);
            int col = df.GetLength(1);

            int xrange = (col - 1) / 2;

            bmp1 = new Bitmap(w, h, PixelFormat.Format1bppIndexed);

            int dm = msk.GetLength(0);

            BmpProc8 src = new BmpProc8(tmp);
            BmpProc1 dst = new BmpProc1(bmp1);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    d8 = src[x, y];
                    d = (d8 > msk[y % dm, x % dm]);
                    dst[x, y] = d;

                    if (d) { err = d8 - 255; } else { err = d8; }

                    for (int iy = 0; iy < row; iy++)
                        for (int ix = -xrange; ix <= xrange; ix++)
                        {
                            xx = x + ix; if ((xx < 0) | (xx > w - 1)) continue;
                            yy = y + iy; if (yy > h - 1) continue;

                            if (df[iy, ix + xrange] < 0) continue;

                            src[xx, yy] = ImageUtils.AdjustByte(src[xx, yy] +
                                                           err * df[iy, ix + xrange]);
                        }

                }

            CallDispose(dst, src, tmp);

            ColorPalette cp = bmp1.Palette;
            cp.Entries[0] = dark;
            cp.Entries[1] = bright;
            bmp1.Palette = cp;

            return true;
        }

        public static bool Parallelogram(ref Bitmap bmp, float def,
                            ParallelogramDeformation pd, Color bkColor)
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

        public static bool CircleCrop(ref Bitmap bmp, double cx, double cy,
                                        double radius)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            if ((cx == 0) & (cy == 0)) { cx = (double)w / 2; cy = (double)h / 2; }

            Bitmap tmp = bmp.Clone() as Bitmap;

            RectangleF rct = new RectangleF(0, 0, w - 1, h - 1);

            double r, a, xx, yy;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                    if (r < radius) continue;

                    r = radius;
                    a = Math.Atan2(y - cy, x - cx);                // radian

                    xx = r * Math.Cos(a) + cx;
                    yy = r * Math.Sin(a) + cy;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                        ImageUtils.intBilinear(dst, src, x, y, xx, yy);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool UnsharpMask(ref Bitmap bmp, int zone, int percent)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 0) | (zone > 15)) return false;
            if (percent < 0) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double f = (double)percent / 100d;

            Bitmap tmp = bmp.Clone() as Bitmap;

            GaussianBlur(ref tmp, zone);

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h - 1; y++)
                for (int x = 0; x < w - 1; x++)
                {
                    src.SetXY(x, y);
                    dst.SetXY(x, y);
                    dst.R = AdjustByte(dst.R + f * (dst.R - src.R));
                    dst.G = AdjustByte(dst.G + f * (dst.G - src.G));
                    dst.B = AdjustByte(dst.B + f * (dst.B - src.B));
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static TextureBrush MakePatternBrush(Bitmap bmp, float angle)
        {
            TextureBrush tb = new TextureBrush(bmp);
            tb.RotateTransform(angle);

            return tb;
        }

        public static bool Diacross(ref Bitmap bmp, bool bFine,
                                            Color penColor, Color bkColor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Graphics g;

            GrayScale(ref bmp);
            HistoStretch(ref bmp);

            Bitmap[] pattern = new Bitmap[4];
            TextureBrush[] tb = new TextureBrush[4];

            if (bFine)
            {
                Pen pn = new Pen(penColor, 1.8f);
                pn.Alignment = PenAlignment.Center;

                PointF[] start = { new PointF(0f, 0.5f), new PointF(0.5f, 0F), 
                                            new PointF(0f, 2.5f), new PointF(2.5f, 0f) };
                PointF[] stop = { new PointF(4f, 0.5f), new PointF(0.5f, 4f), 
                                            new PointF(4f, 2.5f), new PointF(2.5f, 4f) };

                float[] angle = { -37f, -53f };

                for (int i = 0; i < 4; i++)
                {
                    pattern[i] = new Bitmap(4, 4, PixelFormat.Format24bppRgb);
                    g = Graphics.FromImage(pattern[i]);
                    g.Clear(Color.White);
                    g.DrawLine(pn, start[i], stop[i]);
                    g.Dispose();

                    tb[i] = MakePatternBrush(pattern[i], angle[i % 2]);
                }

                pn.Dispose();
            }
            else
            {
                Pen pn = new Pen(penColor, 2.2f);
                pn.Alignment = PenAlignment.Center;

                Point[] start = { new Point(0, 1), new Point(1, 0), 
                                        new Point(0, 4), new Point(4, 0) };
                Point[] stop = { new Point(9, 1), new Point(1, 9), 
                                        new Point(9, 4), new Point(4, 9) };

                float[] angle = { -30f, -60f };

                for (int i = 0; i < 4; i++)
                {
                    pattern[i] = new Bitmap(6, 6, PixelFormat.Format32bppArgb);
                    g = Graphics.FromImage(pattern[i]);
                    g.Clear(Color.White);
                    g.DrawLine(pn, start[i], stop[i]);
                    g.Dispose();

                    tb[i] = MakePatternBrush(pattern[i], angle[i % 2]);
                }

                pn.Dispose();
            }

            Bitmap dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            Graphics gdst = Graphics.FromImage(dst);
            gdst.Clear(bkColor);

            byte[] th = { 255, 191, 128, 64 };

            Bitmap tgt;

            BmpProc8 mod = new BmpProc8(bmp);

            for (int i = 0; i < 4; i++)
            {

                tgt = new Bitmap(w, h, PixelFormat.Format32bppArgb);

                g = Graphics.FromImage(tgt);
                g.FillRectangle(tb[i], 0, 0, w, h);
                g.Dispose();
                tb[i].Dispose();
                pattern[i].Dispose();

                BmpProc32 src = new BmpProc32(tgt);

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (mod[x, y] < th[i])
                            src[x, y, eRGB.a] =
                                AdjustByte((double)(255 - src[x, y, eRGB.r]) *
                                    ((1d - ((double)mod[x, y] / (double)th[i]))));
                        else
                            src[x, y, eRGB.a] = 0;
                    }

                src.Dispose();

                gdst.DrawImageUnscaled(tgt, 0, 0);

                tgt.Dispose();
            }

            mod.Dispose();
            bmp.Dispose();
            gdst.Dispose();

            bmp = dst;
            return true;
        }

        public static bool Wave(ref Bitmap bmp, WaveMode wm, double factor,
                                                                double frequency)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 1; // margin for interpolations

            double f = factor;
            double c = frequency;
            double deg = Math.PI / 180d;

            RectangleF rct = new RectangleF(0, 0, w - 1, h - 1);

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(Color.Gray);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            double xx, yy;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    xx = x;
                    yy = y;

                    switch (wm)
                    {
                        case WaveMode.wmHorizontal:
                            yy = y + f * Math.Sin(c * deg * x);
                            break;

                        case WaveMode.wmVertical:
                            xx = x + f * Math.Sin(c * deg * y);
                            break;

                        case WaveMode.wmBoth:
                            xx = x + f * Math.Sin(c * deg * y);
                            yy = y + f * Math.Sin(c * deg * x);
                            break;
                    }

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBilinear(dst, src, x, y, xx, yy);
                    }
                    else
                    {
                        if (xx < 0) xx = 0;
                        if (xx > w - 1) xx = w - 1;
                        if (yy < 0) yy = 0;
                        if (yy > h - 1) yy = h - 1;

                        xx = xx + mg;
                        yy = yy + mg;

                        intBilinear(dst, src, x, y, xx, yy);
                    }

                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Blot(ref Bitmap bmp, double factor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 2; // margin for interpolations

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(Color.Gray);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            RectangleF rct = new RectangleF(0, 0, w - 1, h - 1);

            double xx, yy;
            Random rnd = new Random();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    xx = (double)x + factor * ((rnd.Next(201) - 100) * 0.01d);
                    yy = (double)y + factor * ((rnd.Next(201) - 100) * 0.01d);

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBilinear(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool SetAlpha(ref Bitmap bmp, int percent)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            byte alpha = (byte)(255 * percent / 100);

            BmpProc32 dst = new BmpProc32(tmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    dst[x, y, eRGB.a] = alpha;

            dst.Dispose();
            bmp.Dispose();

            bmp = tmp;

            return true;
        }

        public static bool Panography(ref Bitmap bmp, int pw, int ph, int num)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int off = (int)Math.Min(pw * 0.5f, ph * 0.5f);

            Bitmap tmp = bmp.Clone() as Bitmap;
            SetAlpha(ref tmp, 40);

            float ptx, pty;
            RectangleF rctf = new RectangleF(0f, 0f, (float)pw, (float)ph);
            GraphicsPath pt;
            float angle;
            Matrix mt;

            TextureBrush tb = new TextureBrush(tmp);

            Random rnd = new Random();

            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(244, 244, 244));

            for (int i = 0; i < num; i++)
            {
                ptx = (float)rnd.Next(off, w - off);
                pty = (float)rnd.Next(off, h - off);
                angle = (float)(rnd.Next(10) * 18f);

                rctf.X = ptx - pw / 2;
                rctf.Y = pty - ph / 2;
                pt = new GraphicsPath();
                pt.AddRectangle(rctf);
                mt = new Matrix();
                mt.RotateAt(angle, new PointF(ptx, pty));
                pt.Transform(mt);

                g.FillPath(tb, pt);

                CallDispose(mt, pt);
            }

            CallDispose(g, tb, tmp);

            return true;
        }

        public static bool MotionBlur(ref Bitmap bmp, int range, double angle)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (range < 2) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            angle += 180;

            double sn = Math.Sin(angle * Math.PI / 180d);
            double cs = Math.Cos(angle * Math.PI / 180d);

            int[] dx = new int[range];
            int[] dy = new int[range];

            for (int i = 0; i < range; i++)
            {
                dx[i] = (int)(cs * (i + 1) + 0.5d);
                dy[i] = (int)(sn * (i + 1) + 0.5d);
            }

            int xx, yy, rr, gg, bb, count;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    src.SetXY(x, y);

                    rr = src.R;
                    gg = src.G;
                    bb = src.B;

                    count = 1;

                    for (int i = 1; i <= range; i++)
                    {
                        xx = x + dx[i - 1];
                        yy = y + dy[i - 1];

                        if ((xx < 0) | (xx > w - 1) | (yy < 0) | (yy > h - 1))
                            continue;

                        src.SetXY(xx, yy);
                        rr += src.R;
                        gg += src.G;
                        bb += src.B;
                        count++;
                    }

                    dst.SetXY(x, y);

                    dst.R = (byte)(rr / count);
                    dst.G = (byte)(gg / count);
                    dst.B = (byte)(bb / count);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static Bitmap MakeGradientPattern(int width, int height,
                                                            int period, float angle)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            LinearGradientBrush lb = new LinearGradientBrush(new Point(0, 0),
                                     new Point(0, period), Color.Black, Color.White);
            lb.SetSigmaBellShape(0.5f);
            lb.RotateTransform(angle);

            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(lb, new Rectangle(0, 0, width, height));
            g.Dispose();

            lb.Dispose();

            return bmp;
        }

        public static bool Gravity(ref Bitmap bmp, int period, float angle,
                                        float factor, Color dark, Color bright)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int f = (int)(period * factor);
            double xx, yy, ff;
            double angle90 = angle + 90d;

            double sn = Math.Sin(angle90 * Math.PI / 180d);
            double cs = Math.Cos(angle90 * Math.PI / 180d);

            Bitmap bw = bmp.Clone() as Bitmap;
            ImageUtils.GrayScale(ref bw);
            ImageUtils.HistoStretch(ref bw);

            Bitmap stripe = MakeGradientPattern(w + f * 2 + 2, h + f * 2 + 2,
                                                                period, angle);

            BmpProc8 mod = new BmpProc8(bw);
            BmpProc24 src = new BmpProc24(stripe);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    ff = f * (255d - (double)mod[x, y]) / 255d;
                    xx = x + ff * cs;
                    yy = y + ff * sn;
                    intBilinear(dst, src, x, y, xx + f + 1, yy + f + 1);
                }

            CallDispose(dst, src, mod, stripe, bw);

            GrayScale(ref bmp);
            SetTwoColorGrayPalette(bmp, dark, bright);

            return true;
        }

        public static bool Scramble(ref Bitmap bmp, int zone)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 30)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = zone * zone;

            byte[] rr = new byte[num];
            byte[] gg = new byte[num];
            byte[] bb = new byte[num];
            int[] ra = new int[num];

            int indx;
            Random rnd = new Random();
            int j, t;

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = 0; y < h - 1; y += zone)
                for (int x = 0; x < w - 1; x += zone)
                {

                    indx = 0;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((ix > w - 1) | (iy > h - 1)) continue;
                            src.SetXY(ix, iy);
                            rr[indx] = src.R;
                            gg[indx] = src.G;
                            bb[indx] = src.B;
                            indx++;
                        }

                    for (int i = 0; i < indx; i++) ra[i] = i;

                    for (int i = 0; i < indx - 1; i++)
                    {
                        j = rnd.Next(i, indx);
                        t = ra[i];
                        ra[i] = ra[j];
                        ra[j] = t;
                    }

                    indx = 0;

                    for (int iy = y; iy < y + zone; iy++)
                        for (int ix = x; ix < x + zone; ix++)
                        {
                            if ((ix > w - 1) | (iy > h - 1)) continue;
                            src.SetXY(ix, iy);
                            src.R = rr[ra[indx]];
                            src.G = gg[ra[indx]];
                            src.B = bb[ra[indx]];
                            indx++;
                        }
                }

            src.Dispose();

            return true;
        }

        public static bool Invert8(ref Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            BmpProc8 src = new BmpProc8(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    src[x, y] = (byte)(255 - src[x, y]);

            src.Dispose();

            return true;
        }

        public static bool Edge2(ref Bitmap bmp, bool fGrayScale)
        {
            int[,] mask = { {-1, -1, -1},
                            {-1,  8, -1},
                            {-1, -1, -1} };

            if (fGrayScale)
            {
                if (ConvolutionCore(ref bmp, mask, 1, 255))
                    return GrayScale(ref bmp);
                else
                    return false;
            }
            else
                return ConvolutionCore(ref bmp, mask, 1, 255);
        }

        public static bool Edge3(ref Bitmap bmp, bool fGrayScale)
        {
            int[,] mask = { {-1, -2, -3, -2, -1},
                            {-2, -3, -4, -3, -2},
                            {-3, -4, 60, -4, -3},
                            {-2, -3, -4, -3, -2},
                            {-1, -2, -3, -2, -1} };

            if (fGrayScale)
            {
                if (ConvolutionCore(ref bmp, mask, 1, 255))
                    return GrayScale(ref bmp);
                else
                    return false;
            }
            else
                return ConvolutionCore(ref bmp, mask, 1, 255);
        }

        public static bool Rinkaku(ref Bitmap bmp, eEdge edge, double factor)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double edg, sbl, v;

            double[] ff = new double[256];

            for (int i = 0; i < 256; i++)
                ff[i] = factor * Math.Sin((double)i * Math.PI / 160d);

            Bitmap tmp = bmp.Clone() as Bitmap;
            switch (edge)
            {
                case eEdge.Edge2:
                    Edge2(ref tmp, true);
                    break;

                case eEdge.Edge3:
                    Edge3(ref tmp, true);
                    break;
            }
            HistoStretch(ref tmp);

            Contour(ref bmp, true, false);

            BmpProc8 src = new BmpProc8(tmp);
            BmpProc8 dst = new BmpProc8(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    edg = 255d - src[x, y];
                    sbl = 255d - dst[x, y];
                    v = Math.Sqrt(edg * sbl);
                    v = v - ff[(int)v];
                    dst[x, y] = AdjustByte(255d - v);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool Rank(ref Bitmap bmp, int area, RankOp ro)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((area < 1) | (area > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int num = (2 * area + 1) * (2 * area + 1);
            byte rmax, gmax, bmax, rmin, gmin, bmin;

            Bitmap tmp = (Bitmap)bmp.Clone();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    rmax = gmax = bmax = 0;
                    rmin = gmin = bmin = 255;

                    for (int iy = y - area; iy <= y + area; iy++)
                        for (int ix = x - area; ix <= x + area; ix++)
                        {
                            if ((ix < 0) | (ix > w - 1)) continue;
                            if ((iy < 0) | (iy > h - 1)) continue;

                            src.SetXY(ix, iy);

                            switch (ro)
                            {
                                case RankOp.Max:
                                    rmax = Math.Max(rmax, src.R);
                                    gmax = Math.Max(gmax, src.G);
                                    bmax = Math.Max(bmax, src.B);
                                    break;

                                case RankOp.Mid:
                                    rmax = Math.Max(rmax, src.R);
                                    gmax = Math.Max(gmax, src.G);
                                    bmax = Math.Max(bmax, src.B);
                                    rmin = Math.Min(rmin, src.R);
                                    gmin = Math.Min(gmin, src.G);
                                    bmin = Math.Min(bmin, src.B);
                                    break;

                                case RankOp.Min:
                                    rmin = Math.Min(rmin, src.R);
                                    gmin = Math.Min(gmin, src.G);
                                    bmin = Math.Min(bmin, src.B);
                                    break;
                            }
                        }

                    dst.SetXY(x, y);

                    switch (ro)
                    {
                        case RankOp.Max:
                            dst.R = rmax;
                            dst.G = gmax;
                            dst.B = bmax;
                            break;

                        case RankOp.Mid:
                            dst.R = ImageUtils.AdjustByte((rmax + rmin) / 2);
                            dst.G = ImageUtils.AdjustByte((gmax + gmin) / 2);
                            dst.B = ImageUtils.AdjustByte((bmax + bmin) / 2);
                            break;

                        case RankOp.Min:
                            dst.R = rmin;
                            dst.G = gmin;
                            dst.B = bmin;
                            break;
                    }
                }

            CallDispose(dst, src, tmp);

            return true;

        }

        public static bool Twist(ref Bitmap bmp, double factor, int frequency,
                                                double radius, int cx, int cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (radius < 0) return false;

            if ((frequency < 1) | (frequency > 16)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int q = frequency;

            int mg = 2; // margin for interpolations

            double c = factor;
            if ((cx == 0) & (cy == 0)) { cx = w / 2; cy = h / 2; }

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(Color.LightGray);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            RectangleF rct = new RectangleF(0, 0, w - 1, h - 1);

            double r, a, xx, yy;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                    if ((r > radius) | (r < 1)) continue;

                    a = Math.Atan2(y - cy, x - cx);                // radian
                    a = a + c * Math.Sin(2d * q * r / radius * Math.PI) / r;

                    xx = r * Math.Cos(a) + cx;
                    yy = r * Math.Sin(a) + cy;

                    if (rct.Contains(new PointF((float)xx, (float)yy)))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        intBicubic(dst, src, x, y, xx, yy);
                    }
                    else
                    {
                        if (xx < 0) xx = 0;
                        if (xx > w - 1) xx = w - 1;
                        if (yy < 0) yy = 0;
                        if (yy > h - 1) yy = h - 1;

                        xx = xx + mg;
                        yy = yy + mg;

                        intBicubic(dst, src, x, y, xx, yy);

                    }
                }
            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool BathroomWindow1(ref Bitmap bmp,
                                    int zone, BathroomWindowMode wm)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 100)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int rnd = zone * 2 + 1;

            int xx, yy;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    xx = x; yy = y;

                    switch (wm)
                    {
                        case BathroomWindowMode.bwVertical:
                            xx = x + (x % rnd) - zone;
                            break;
                        case BathroomWindowMode.bwHorizontal:
                            yy = y + (y % rnd) - zone;
                            break;
                        case BathroomWindowMode.bwBoth:
                            xx = x + (x % rnd) - zone;
                            yy = y + (y % rnd) - zone;
                            break;
                    }

                    if ((xx > 0) & (xx < w) & (yy > 0) & (yy < h))
                    {
                        dst.SetXY(x, y);
                        src.SetXY(xx, yy);
                        dst.R = src.R;
                        dst.G = src.G;
                        dst.B = src.B;
                    }
                }
            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool BathroomWindow2(ref Bitmap bmp, int zone,
                                                        int cx, int cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 100)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int rnd = zone;

            if ((cx == 0) & (cy == 0))
            {
                cx = w / 2;
                cy = h / 2;
            }

            int xx, yy;
            double a, r;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx);                // radian
                    xx = x + ((int)(((a + Math.PI) * 180 / Math.PI
                                            + 0.2d * r)) % rnd) + zone;
                    yy = y;

                    if ((xx > 0) & (xx < w) & (yy > 0) & (yy < h))
                    {
                        dst.SetXY(x, y);
                        src.SetXY(xx, yy);
                        dst.R = src.R;
                        dst.G = src.G;
                        dst.B = src.B;
                    }
                }
            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool BathroomWindow4(ref Bitmap bmp, int zone,
                                                       double cx, double cy)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 2) | (zone > 100)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            if ((cx == 0d) & (cy == 0d))
            {
                cx = w / 2;
                cy = h / 2;
            }

            int mg = 1; // margin for interpolations

            int rnd = zone;
            double a, r, xx, yy;

            Bitmap tmp = new Bitmap(w + mg * 2, h + mg * 2, bmp.PixelFormat);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(Color.Gray);
            g.DrawImageUnscaled(bmp, mg, mg);
            g.Dispose();

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    a = Math.Atan2(y - cy, x - cx);                // radian

                    r = r + ((int)r % rnd) - zone;
                    xx = r * Math.Cos(a) + cx;
                    yy = r * Math.Sin(a) + cy;

                    if ((xx > 0) & (xx < w - 1) & (yy > 0) & (yy < h - 1))
                    {
                        xx = xx + mg;
                        yy = yy + mg;

                        ImageUtils.intBilinear(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static Bitmap HexaSampler(Bitmap bmp, int cx, int cy,
                                                int radius, double angle)
        {
            double r = (double)radius * 1.1;
            int cc = (int)r;

            Bitmap tmp = new Bitmap((int)r * 2, (int)r * 2,
                                                PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(Color.Yellow);
            g.DrawImageUnscaled(bmp, cc - cx, cc - cy);
            g.Dispose();

            angle = -angle * 30d;
            Bitmap tmp2 = BitmapRotate(tmp, (float)angle, Color.Green);
            tmp.Dispose();

            Bitmap tmp3 = new Bitmap((int)r * 2, (int)r * 2,
                                                PixelFormat.Format24bppRgb);

            g = Graphics.FromImage(tmp3);
            g.Clear(Color.Blue);
            g.DrawImageUnscaled(tmp2, cc - tmp2.Width / 2, cc - tmp2.Height / 2);
            g.Dispose();

            tmp2.Dispose();

            return tmp3;
        }

        public static Bitmap Kaleidoscope(Bitmap bmp, int cx, int cy,
                                                int radius, double angle)
        {
            Bitmap tmp = HexaSampler(bmp, cx, cy, radius, angle);

            int w = tmp.Width;
            int h = tmp.Height;

            double cx0 = (double)w / 2d;
            double cy0 = (double)h / 2d;

            double dx, dy;

            double r = Math.Min(cx0, cy0) * 0.9d;

            double a1 = 300d;
            double a2 = 60d;
            double a3 = 0;

            Color cl = Color.Gray;

            Mirror(ref tmp, cx0, cy0, a1, cl);
            Mirror(ref tmp, cx0, cy0, a2, cl);
            Mirror(ref tmp, cx0, cy0, a3, cl);

            dx = cx0 + r * Math.Cos(60d * Math.PI / 180d);
            dy = cy0 - r * Math.Sin(60d * Math.PI / 180d);

            Mirror(ref tmp, dx, dy, 240, cl);

            dx = cx0 - r * Math.Cos(60d * Math.PI / 180d);

            Mirror(ref tmp, dx, dy, 180, cl);
            Mirror(ref tmp, dx, dy, 120, cl);

            Mirror(ref tmp, cx0, cy0, 0, cl);

            return tmp;
        }

        public static bool TransparentFrame(ref Bitmap bmp, Color cl,
                                                            params FrameUnit[] fu)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Region rg;
            int ww = 0;
            SolidBrush sb = new SolidBrush(Color.Black);

            Graphics g = Graphics.FromImage(bmp);

            for (int i = 0; i < fu.Length; i++)
            {
                if (fu[i].width < 1) continue;
                if (fu[i].percentAlpha == 0)
                {
                    ww += fu[i].width;
                    continue;
                }

                rg = new Region(new Rectangle(ww, ww, w - ww * 2, h - ww * 2));
                rg.Exclude(new Rectangle(ww + fu[i].width, ww + fu[i].width,
                        w - ww * 2 - fu[i].width * 2, h - ww * 2 - fu[i].width * 2));
                ww += fu[i].width;
                sb.Color = Color.FromArgb(fu[i].percentAlpha * 255 / 100, cl);
                g.FillRegion(sb, rg);
                rg.Dispose();
            }

            sb.Dispose();
            g.Dispose();

            return true;
        }

        public static Bitmap PolySampler(Bitmap bmp, int nPoly, int cx, int cy,
                                                 int radius, double angle)
        {
            double r = (double)radius * 1.05;

            double t = Math.Cos(Math.PI / (double)nPoly);
            int rr = (int)(r * 2d / t);

            Bitmap tmp = new Bitmap(rr, rr, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(tmp);
            g.Clear(Color.Yellow);
            g.DrawImageUnscaled(bmp, tmp.Width / 2 - cx, tmp.Height / 2 - cy);
            g.Dispose();

            angle = -angle * 30d;
            Bitmap tmp2 = ImageUtils.BitmapRotate(tmp, (float)angle, Color.Green);
            tmp.Dispose();

            Bitmap tmp3 = new Bitmap(rr, rr, PixelFormat.Format24bppRgb);

            g = Graphics.FromImage(tmp3);
            g.Clear(Color.Blue);
            g.DrawImageUnscaled(tmp2, (rr - tmp2.Width) / 2, (rr - tmp2.Height) / 2);
            g.Dispose();

            tmp2.Dispose();

            return tmp3;
        }

        public static bool PolyKaleidoscope(ref Bitmap bmp, int nPoly, bool mirrorLeft,
                                                double radius, Color bkColor)
        {
            if ((nPoly < 4) | (nPoly > 20)) return false;

            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double r = Math.Min(w, h) / 2d;

            double centerAngle = Math.PI * 2d / (double)nPoly;

            double cx = (double)w / 2d;
            double cy = (double)h / 2d;

            if (mirrorLeft)
                Mirror(ref bmp, cx, cy, 270d, bkColor);
            else
                Mirror(ref bmp, cx, cy, 90d, bkColor);

            double p1x = cx - radius * Math.Tan(centerAngle / 2d) - 1d;
            double p1y = cy - radius;

            double p2x = cx + radius * Math.Tan(centerAngle / 2d) + 1d;
            double p2y = p1y;

            PointF[] pts = { new PointF((float)cx, (float)cy),
                            new PointF((float)p1x, (float)p1y), 
                            new PointF((float)p2x, (float)p2y)
                          };

            GraphicsPath pt = new GraphicsPath();
            pt.AddPolygon(pts);

            double degAngle = centerAngle * 180d / Math.PI;
            PointF centerP = new PointF((float)cx, (float)cy);

            GraphicsPath rotPt;
            Matrix mt = new Matrix();
            TextureBrush tb = new TextureBrush(bmp);

            Bitmap tmp = new Bitmap(w, h, bmp.PixelFormat);
            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < nPoly; i++)
            {
                mt.Reset();
                mt.RotateAt((float)degAngle * i, centerP);

                rotPt = pt.Clone() as GraphicsPath;
                rotPt.Transform(mt);

                tb.ResetTransform();
                tb.Transform = mt;

                g.FillPath(tb, rotPt);
                rotPt.Dispose();
            }

            CallDispose(g, tb, mt, pt, bmp);

            bmp = tmp;
            return true;
        }

        public static bool FrostedGlass(ref Bitmap bmp, Rectangle rect,
            int alphaPercent, int blurZone)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            PartialGaussianBlur(ref bmp, blurZone, rect);

            SolidBrush br = new SolidBrush(Color.FromArgb(alphaPercent * 255 / 100,
                                                    Color.FromArgb(244, 244, 244)));

            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(br, rect);
            g.Dispose();

            br.Dispose();

            return true;
        }

        public static bool AdditiveColor(Bitmap bmpDst, Bitmap bmpSrc,
            RGBSelection rgb)
        {
            if (bmpDst.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (bmpSrc.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmpDst.Width;
            int h = bmpDst.Height;

            if ((bmpSrc.Width != w) | (bmpSrc.Height != h)) return false;

            BmpProc24 src = new BmpProc24(bmpSrc);
            BmpProc24 dst = new BmpProc24(bmpDst);

            switch (rgb)
            {
                case RGBSelection.all:
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            src.SetXY(x, y);
                            dst.SetXY(x, y);

                            dst.R = AdjustByte(dst.R + src.R);
                            dst.G = AdjustByte(dst.G + src.G);
                            dst.B = AdjustByte(dst.B + src.B);
                        }
                    break;

                case RGBSelection.red:
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            src.SetXY(x, y);
                            dst.SetXY(x, y);

                            dst.R = AdjustByte(dst.R + src.R);
                        }
                    break;

                case RGBSelection.green:
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            src.SetXY(x, y);
                            dst.SetXY(x, y);

                            dst.G = AdjustByte(dst.G + src.G);
                        }
                    break;

                case RGBSelection.blue:
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            src.SetXY(x, y);
                            dst.SetXY(x, y);

                            dst.B = AdjustByte(dst.B + src.B);
                        }
                    break;
            }

            CallDispose(dst, src);

            return true;
        }

        public static bool AdditiveColor(Bitmap bmpDst, Bitmap bmpSrc,
                                    RGBSelection rgb, int dstX, int dstY)
        {
            if (bmpDst.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if (bmpSrc.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            Rectangle dstRect = new Rectangle(0, 0,
                                            bmpDst.Width, bmpDst.Height);
            Rectangle srcRect = new Rectangle(dstX, dstY,
                                            bmpSrc.Width, bmpSrc.Height);

            if (!dstRect.IntersectsWith(srcRect)) return true;

            dstRect.Intersect(srcRect);

            int scanX = dstRect.X;
            int scanY = dstRect.Y;
            int scanEndX = scanX + dstRect.Width;
            int scanEndY = scanY + dstRect.Height;

            BmpProc24 src = new BmpProc24(bmpSrc);
            BmpProc24 dst = new BmpProc24(bmpDst);

            switch (rgb)
            {
                case RGBSelection.all:
                    for (int y = scanY; y < scanEndY; y++)
                        for (int x = scanX; x < scanEndX; x++)
                        {
                            src.SetXY(x - dstX, y - dstY);
                            dst.SetXY(x, y);

                            dst.R = AdjustByte(dst.R + src.R);
                            dst.G = AdjustByte(dst.G + src.G);
                            dst.B = AdjustByte(dst.B + src.B);
                        }
                    break;

                case RGBSelection.red:
                    for (int y = scanY; y < scanEndY; y++)
                        for (int x = scanX; x < scanEndX; x++)
                        {
                            src.SetXY(x - dstX, y - dstY);
                            dst.SetXY(x, y);

                            dst.R = AdjustByte(dst.R + src.R);
                        }
                    break;

                case RGBSelection.green:
                    for (int y = scanY; y < scanEndY; y++)
                        for (int x = scanX; x < scanEndX; x++)
                        {
                            src.SetXY(x - dstX, y - dstY);
                            dst.SetXY(x, y);

                            dst.G = AdjustByte(dst.G + src.G);
                        }
                    break;

                case RGBSelection.blue:
                    for (int y = scanY; y < scanEndY; y++)
                        for (int x = scanX; x < scanEndX; x++)
                        {
                            src.SetXY(x - dstX, y - dstY);
                            dst.SetXY(x, y);

                            dst.B = AdjustByte(dst.B + src.B);
                        }
                    break;
            }

            CallDispose(dst, src);

            return true;
        }

        public static bool PhotoBlur(ref Bitmap bmp, int zone, int baseOffset)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            if ((zone < 1) | (zone > 30)) return false;

            if ((baseOffset < 0) | (baseOffset > 20)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double baseB = 1.010d + (double)baseOffset / 1000d;
            double[] paw = new double[256];

            for (int i = 0; i < 256; i++)
                paw[i] = Math.Pow(baseB, (double)i);

            int[,] mask = new int[zone * 2 + 1, zone * 2 + 1];
            int r = zone * zone;

            for (int i = 0; i < zone * 2 + 1; i++)
                for (int j = 0; j < zone * 2 + 1; j++)
                    if ((i - zone) * (i - zone) + (j - zone) * (j - zone) <= r)
                        mask[i, j] = 1;
                    else
                        mask[i, j] = 0;

            double rr, gg, bb;
            int count;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc24 src = new BmpProc24(tmp);
            BmpProc24 dst = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    count = 0;
                    rr = gg = bb = 0d;

                    for (int iy = y - zone; iy < y + zone + 1; iy++)
                        for (int ix = x - zone; ix < x + zone + 1; ix++)
                        {
                            if ((iy < 0) | (iy > h - 1)) continue;
                            if ((ix < 0) | (ix > w - 1)) continue;

                            src.SetXY(ix, iy);
                            rr += paw[src.R] * mask[ix - x + zone, iy - y + zone];
                            gg += paw[src.G] * mask[ix - x + zone, iy - y + zone];
                            bb += paw[src.B] * mask[ix - x + zone, iy - y + zone];
                            count += mask[ix - x + zone, iy - y + zone];
                        }

                    dst.SetXY(x, y);
                    dst.R = AdjustByte(Math.Log(rr / count, baseB));
                    dst.G = AdjustByte(Math.Log(gg / count, baseB));
                    dst.B = AdjustByte(Math.Log(bb / count, baseB));
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool AlphaSheet(ref Bitmap bmp, bool stretch, bool bInvert)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            Bitmap tmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, 0, 0);
            g.Dispose();

            ImageUtils.GrayScale(ref bmp);
            if (stretch) ImageUtils.HistoStretch(ref bmp);

            BmpProc8 src = new BmpProc8(bmp);
            BmpProc32 dst = new BmpProc32(tmp);

            if (bInvert)
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        dst[x, y, eRGB.a] = (byte)(255 - src[x, y]);
            else
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        dst[x, y, eRGB.a] = src[x, y];

            CallDispose(dst, src, bmp);

            bmp = tmp;

            return true;
        }

        public static bool Blur32(ref Bitmap bmp, int zone)
        {
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
                return false;

            if ((zone < 1) | (zone > 10)) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int suma, sumr, sumg, sumb, count;

            Bitmap tmp = (Bitmap)bmp.Clone();

            BmpProc32 src = new BmpProc32(tmp);
            BmpProc32 dst = new BmpProc32(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    suma = sumr = sumg = sumb = count = 0;

                    for (int iy = y - zone; iy <= y + zone; iy++)
                        for (int ix = x - zone; ix <= x + zone; ix++)
                        {
                            if ((ix < 0) | (ix > w - 1)) continue;
                            if ((iy < 0) | (iy > h - 1)) continue;

                            src.SetXY(ix, iy);

                            suma += src.A;
                            sumr += src.R * src.A;
                            sumg += src.G * src.A;
                            sumb += src.B * src.A;
                            count++;
                        }

                    dst.SetXY(x, y);

                    dst.A = (byte)(suma / count);
                    dst.R = AdjustByte((double)sumr / suma);
                    dst.G = AdjustByte((double)sumg / suma);
                    dst.B = AdjustByte((double)sumb / suma);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool MotionBlur32(ref Bitmap bmp, int range, double angle)
        {
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
                return false;

            if (range < 2) return false;

            int w = bmp.Width;
            int h = bmp.Height;

            angle += 180;

            double sn = Math.Sin(angle * Math.PI / 180d);
            double cs = Math.Cos(angle * Math.PI / 180d);

            int[] dx = new int[range];
            int[] dy = new int[range];

            for (int i = 0; i < range; i++)
            {
                dx[i] = (int)(cs * (i + 1) + 0.5d);
                dy[i] = (int)(sn * (i + 1) + 0.5d);
            }

            int xx, yy, aa, rr, gg, bb, count;

            Bitmap tmp = bmp.Clone() as Bitmap;

            BmpProc32 src = new BmpProc32(tmp);
            BmpProc32 dst = new BmpProc32(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    src.SetXY(x, y);

                    aa = src.A;
                    rr = src.R * src.A;
                    gg = src.G * src.A;
                    bb = src.B * src.A;

                    count = 1;

                    for (int i = 1; i <= range; i++)
                    {
                        xx = x + dx[i - 1];
                        yy = y + dy[i - 1];

                        if ((xx < 0) | (xx > w - 1) | (yy < 0) | (yy > h - 1))
                            continue;

                        src.SetXY(xx, yy);
                        aa += src.A;
                        rr += src.R * src.A;
                        gg += src.G * src.A;
                        bb += src.B * src.A;
                        count++;
                    }

                    dst.SetXY(x, y);

                    dst.A = (byte)(aa / count);
                    dst.R = AdjustByte((double)rr / aa);
                    dst.G = AdjustByte((double)gg / aa);
                    dst.B = AdjustByte((double)bb / aa);
                }

            CallDispose(dst, src, tmp);

            return true;
        }

        public static bool SaturationHistogram(Bitmap bmp, out Bitmap bmpHist)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                bmpHist = null;
                return false;
            }

            int w = bmp.Width;
            int h = bmp.Height;

            int[] hist = new int[256];

            int hue, satu, lumi;
            byte rr, gg, bb;

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    src.SetXY(x, y);

                    rr = src.R; gg = src.G; bb = src.B;
                    ImageUtils.RGBToHSL(rr, gg, bb, out hue, out satu, out lumi);

                    hist[satu]++;
                }

            src.Dispose();


            int max = -1;
            for (int i = 0; i < 256; i++)
                if (hist[i] > max) max = hist[i];

            for (int i = 0; i < 256; i++)
                hist[i] = hist[i] * 140 / max;

            bmpHist = new Bitmap(275, 160, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(bmpHist);
            g.Clear(Color.AntiqueWhite);

            Pen pen = new Pen(Color.Gray, 1F);

            for (int i = 0; i < 256; i++)
                g.DrawLine(pen, 10 + i, 150, 10 + i, 150 - hist[i]);

            pen.Color = Color.Black;

            g.DrawLine(pen, 8, 150, 8, 10);

            for (int i = 0; i <= 20; i++)
                if ((i % 2) == 0)
                    g.DrawLine(pen, 8, 150 - 7 * i, 4, 150 - 7 * i);
                else
                    g.DrawLine(pen, 8, 150 - 7 * i, 6, 150 - 7 * i);

            g.DrawLine(pen, 10, 150, 10 + 255, 150);

            for (int i = 0; i <= 51; i++)
                if ((i % 10) == 0)
                    g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 156);
                else
                    if ((i % 5) == 0)
                        g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 154);
                    else
                        g.DrawLine(pen, 10 + 5 * i, 150, 10 + 5 * i, 152);

            g.Dispose();

            return true;
        }

        public static bool Ellipse2(ref Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int mg = 3; // margin for interpolations

            int q = Math.Max(w, h);

            RectangleF rct = new RectangleF(-1, -1, q + 1, q + 1);

            Bitmap tmp1 = new Bitmap(q + mg * 2, q + mg * 2, PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(tmp1);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmp, mg, mg, q, q);
            g.Dispose();

            Bitmap tmp2 = new Bitmap(q, q, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(tmp2);
            g.Dispose();

            double l, xx, yy;
            double r = (double)q / 2d;

            BmpProc32 src = new BmpProc32(tmp1);
            BmpProc32 dst = new BmpProc32(tmp2);

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

                        intBicubic32(dst, src, x, y, xx, yy);
                    }
                }

            CallDispose(dst, src, tmp1);

            Bitmap tmp3 = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            g = Graphics.FromImage(tmp3);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(tmp2, 0, 0, w, h);
            g.Dispose();

            tmp2.Dispose();

            bmp.Dispose();

            bmp = tmp3;

            return true;
        }

        public static void intBicubic32(BmpProc32 dst, BmpProc32 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int xi = (int)Math.Floor(srcX);
            int yi = (int)Math.Floor(srcY);

            double rr, gg, bb, aa;
            double dx, dy, wx, wy;

            rr = gg = bb = aa = 0;
            for (int iy = yi - 1; iy < yi + 3; iy++)
                for (int ix = xi - 1; ix < xi + 3; ix++)
                {
                    dx = Math.Abs(srcX - ix);
                    dy = Math.Abs(srcY - iy);

                    if (dx < 1) wx = (dx - 1d) * (dx * dx - dx - 1d);
                    else wx = -(dx - 1d) * (dx - 2d) * (dx - 2d);

                    if (dy < 1) wy = (dy - 1d) * (dy * dy - dy - 1d);
                    else wy = -(dy - 1d) * (dy - 2d) * (dy - 2d);

                    src.SetXY(ix, iy);

                    aa += src.A * wx * wy;
                    rr += src.R * src.A * wx * wy;
                    gg += src.G * src.A * wx * wy;
                    bb += src.B * src.A * wx * wy;
                }

            dst[dstX, dstY, eRGB.a] = ImageUtils.AdjustByte(aa);

            if (aa != 0)
            {
                dst.SetXY(dstX, dstY);

                dst.R = AdjustByte(rr / aa);
                dst.G = AdjustByte(gg / aa);
                dst.B = AdjustByte(bb / aa);
            }
        }

        public static void intBilinear32(BmpProc32 dst, BmpProc32 src,
                                int dstX, int dstY, double srcX, double srcY)
        {
            int x1 = (int)Math.Floor(srcX);
            int x2 = x1 + 1;
            double fx2 = srcX - x1;
            double fx1 = 1 - fx2;

            int y1 = (int)Math.Floor(srcY);
            int y2 = y1 + 1;
            double fy2 = srcY - y1;
            double fy1 = 1 - fy2;

            byte aa =
                (byte)(src[x1, y1, eRGB.a] * fx1 * fy1 +
                       src[x2, y1, eRGB.a] * fx2 * fy1 +
                       src[x1, y2, eRGB.a] * fx1 * fy2 +
                       src[x2, y2, eRGB.a] * fx2 * fy2);

            dst[dstX, dstY, eRGB.a] = aa;

            if (aa != 0)
            {

                dst[dstX, dstY, eRGB.r] =
                    (byte)((src[x1, y1, eRGB.r] * src[x1, y1, eRGB.a] * fx1 * fy1 +
                           src[x2, y1, eRGB.r] * src[x2, y1, eRGB.a] * fx2 * fy1 +
                           src[x1, y2, eRGB.r] * src[x1, y2, eRGB.a] * fx1 * fy2 +
                           src[x2, y2, eRGB.r] * src[x2, y2, eRGB.a] * fx2 * fy2) /
                           aa);

                dst[dstX, dstY, eRGB.g] =
                    (byte)((src[x1, y1, eRGB.g] * src[x1, y1, eRGB.a] * fx1 * fy1 +
                           src[x2, y1, eRGB.g] * src[x2, y1, eRGB.a] * fx2 * fy1 +
                           src[x1, y2, eRGB.g] * src[x1, y2, eRGB.a] * fx1 * fy2 +
                           src[x2, y2, eRGB.g] * src[x2, y2, eRGB.a] * fx2 * fy2) /
                           aa);

                dst[dstX, dstY, eRGB.b] =
                    (byte)((src[x1, y1, eRGB.b] * src[x1, y1, eRGB.a] * fx1 * fy1 +
                           src[x2, y1, eRGB.b] * src[x2, y1, eRGB.a] * fx2 * fy1 +
                           src[x1, y2, eRGB.b] * src[x1, y2, eRGB.a] * fx1 * fy2 +
                           src[x2, y2, eRGB.b] * src[x2, y2, eRGB.a] * fx2 * fy2) /
                           aa);

            }
        }

        public static bool HistoStretch24(ref Bitmap bmp, params double[] limit)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            double stretchfactor = 1.00;

            int threshold = (int)(w * h * 0.015);

            if (limit.Length != 0) threshold = (int)(w * h * limit[0] / 100);

            int hue, sat, lum, lt, ht;
            byte rr, gg, bb;

            double originalrange, stretchedrange, scalefactor;


            int[] hist = new int[256];

            BmpProc24 src = new BmpProc24(bmp);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    src.SetXY(x, y);
                    RGBToHSL(src.R, src.G, src.B, out hue, out sat, out lum);
                    hist[lum]++;
                }

            lt = 0;
            for (int i = 0; i < 256; i++)
            {
                lt += hist[i];
                if (lt > threshold)
                {
                    lt = i;
                    break;
                }
            }

            ht = 0;
            for (int i = 255; i >= 0; i--)
            {
                ht += hist[i];
                if (ht > threshold)
                {
                    ht = i;
                    break;
                }
            }

            originalrange = ht - lt + 1;
            stretchedrange = originalrange + stretchfactor * (255 - originalrange);
            scalefactor = stretchedrange / originalrange;

            for (int i = 0; i < 256; i++)
                hist[i] = AdjustByte(scalefactor * (i - lt));

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    src.SetXY(x, y);
                    RGBToHSL(src.R, src.G, src.B, out hue, out sat, out lum);
                    lum = hist[lum];
                    HSLToRGB(hue, sat, lum, out rr, out gg, out bb);
                    src.R = rr;
                    src.G = gg;
                    src.B = bb;
                }

            src.Dispose();

            return true;
        }

    }

    public enum Interpolation
    {
        ipNearest, ipBilinear, ipBicubic, ipLagrange, ipMitchell, ipLanczos
    }

    public enum eRGB
    {
        b, g, r, a
    }

    public class BmpProc32 : IDisposable
    {
        private bool flagDispose = false;

        private Bitmap rbmp;
        private int w, h;
        private BitmapData bmpData;
        private IntPtr ptr;
        private int stride;
        private int bytes;
        private byte[] data;
        private int xyr, xyg, xyb, xya;

        public BmpProc32(Bitmap bmp)
        {
            rbmp = bmp;
            w = bmp.Width;
            h = bmp.Height;
            Rectangle rect = new Rectangle(0, 0, w, h);
            bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite,
                                          PixelFormat.Format32bppArgb);
            ptr = bmpData.Scan0;
            stride = Math.Abs(bmpData.Stride);

            bytes = stride * h;
            data = new byte[bytes];
            Marshal.Copy(ptr, data, 0, bytes);
        }

        public byte this[int x, int y, eRGB rgb]
        {
            get { return data[stride * y + x * 4 + (int)(rgb)]; }
            set { data[stride * y + x * 4 + (int)(rgb)] = value; }
        }

        public byte this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }

        public int IndexR(int x, int y)
        {
            return stride * y + x * 4 + 2; // a <- +1
        }

        public void SetXY(int x, int y)
        {
            xyb = stride * y + x * 4;
            xyg = xyb + 1;
            xyr = xyg + 1;
            xya = xyr + 1;
        }

        public byte R
        {
            get { return data[xyr]; }
            set { data[xyr] = value; }
        }

        public byte G
        {
            get { return data[xyg]; }
            set { data[xyg] = value; }
        }

        public byte B
        {
            get { return data[xyb]; }
            set { data[xyb] = value; }
        }

        public byte A
        {
            get { return data[xya]; }
            set { data[xya] = value; }
        }

        public int DataLength
        {
            get { return bytes; }
        }

        protected virtual void Dispose(bool flag)
        {
            if (!flagDispose)
            {
                if (flag)
                {
                    Marshal.Copy(data, 0, ptr, bytes);
                    rbmp.UnlockBits(bmpData);
                }
                this.flagDispose = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BmpProc32()
        {
            Dispose(false);
        }
    } 

    public class BmpProc24 : IDisposable
    {
        private bool flagDispose = false;

        private Bitmap rbmp;
        private int w, h;
        private BitmapData bmpData;
        private IntPtr ptr;
        private int stride;
        private int bytes;
        private byte[] data;
        private int xyr, xyg, xyb;

        public BmpProc24(Bitmap bmp)
        {
            rbmp = bmp;
            w = bmp.Width;
            h = bmp.Height;
            Rectangle rect = new Rectangle(0, 0, w, h);
            bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite,
                                          PixelFormat.Format24bppRgb);
            ptr = bmpData.Scan0; 
            stride = Math.Abs(bmpData.Stride);

            bytes = stride * h;
            data = new byte[bytes];
            Marshal.Copy(ptr, data, 0, bytes);
        }

        public byte this[int x, int y, eRGB rgb]
        {
            get { return data[stride * y + x * 3 + (int)(rgb)]; }
            set { data[stride * y + x * 3 + (int)(rgb)] = value; }
        }

        public byte this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }

        public int IndexR(int x, int y)
        {
            return stride * y + x*3 + 2;
        }

        public void SetXY(int x, int y)
        {
            xyb = stride * y + x * 3;
            xyg = xyb + 1;
            xyr = xyg + 1;
        }

        public byte R
        {
            get { return data[xyr]; }
            set { data[xyr] = value; }
        }

        public byte G
        {
            get { return data[xyg]; }
            set { data[xyg] = value; }
        }

        public byte B
        {
            get { return data[xyb]; }
            set { data[xyb] = value; }
        }

        public int DataLength
        {
            get { return bytes; }
        }

        protected virtual void Dispose(bool flag)
        {
            if (!flagDispose)
            {
                if (flag)
                {
                    Marshal.Copy(data, 0, ptr, bytes);
                    rbmp.UnlockBits(bmpData);
                }
                this.flagDispose = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BmpProc24()
        {
            Dispose(false);
        }
    }       

    public class BmpProc8 : IDisposable
    {
        private bool flagDispose = false;

        private Bitmap rbmp;
        private int w, h;
        private BitmapData bmpData;
        private IntPtr ptr;
        private int stride;
        private int bytes;
        private byte[] data;

        public BmpProc8(Bitmap bmp)
        {
            rbmp = bmp;
            w = bmp.Width;
            h = bmp.Height;
            Rectangle rect = new Rectangle(0, 0, w, h);
            bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite,
                                          PixelFormat.Format8bppIndexed);
            ptr = bmpData.Scan0;
            stride = Math.Abs(bmpData.Stride);

            bytes = stride * h;
            data = new byte[bytes];
            Marshal.Copy(ptr, data, 0, bytes);
        }

        public byte this[int x, int y]
        {
            get { return data[stride * y + x]; }
            set { data[stride * y + x] = value; }
        }

        public byte this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }

        public int Index(int x, int y)
        {
            return stride * y + x;
        }

        public int DataLength
        {
            get { return bytes; }
        }

        protected virtual void Dispose(bool flag)
        {
            if (!flagDispose)
            {
                if (flag)
                {
                    Marshal.Copy(data, 0, ptr, bytes);
                    rbmp.UnlockBits(bmpData);
                }
                this.flagDispose = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BmpProc8()
        {
            Dispose(false);
        }
    }

    public class BmpProc1 : IDisposable
    {
        private bool flagDispose = false;

        private Bitmap rbmp;
        private int w, h;
        private BitmapData bmpData;
        private IntPtr ptr;
        private int stride;
        private int bytes;
        private byte[] data;

        public BmpProc1(Bitmap bmp)
        {
            rbmp = bmp;
            w = bmp.Width;
            h = bmp.Height;
            Rectangle rect = new Rectangle(0, 0, w, h);
            bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite,
                                          PixelFormat.Format1bppIndexed);
            ptr = bmpData.Scan0;
            stride = Math.Abs(bmpData.Stride);

            bytes = stride * h;
            data = new byte[bytes];
            Marshal.Copy(ptr, data, 0, bytes);
        }

        private byte pd;

        public bool this[int x, int y]
        {
            get
            {
                pd = data[stride * y + x / 8];
                return ((pd >> (7 - (x % 8)) & 1) == 1);
            }

            set
            {
                pd = data[stride * y + x / 8];
                if (value)
                    data[stride * y + x / 8] = (byte)(pd | (1 << (7 - (x % 8))));
                else
                    data[stride * y + x / 8] = (byte)(pd & (~(1 << (7 - (x % 8)))));
            }
        }

        public int DataLength
        {
            get { return bytes; }
        }

        protected virtual void Dispose(bool flag)
        {
            if (!flagDispose)
            {
                if (flag)
                {
                    Marshal.Copy(data, 0, ptr, bytes);
                    rbmp.UnlockBits(bmpData);
                }
                this.flagDispose = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BmpProc1()
        {
            Dispose(false);
        }
    }

    public static class StopWatch
    {
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceCounter(ref long x);

        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceFrequency(ref long x);

        private static double strt;
        public static double time;

        public static void Start()
        {
            long cnt = 0;
            long frq = 0;
            QueryPerformanceCounter(ref cnt);
            QueryPerformanceFrequency(ref frq);
            strt = (double)cnt / (double)frq;
        }

        public static void Stop()
        {
            long cnt = 0;
            long frq = 0;
            QueryPerformanceCounter(ref cnt);
            QueryPerformanceFrequency(ref frq);
            double c = (double)cnt / (double)frq;
            time = (c - strt) * 1000;
        }
    }

    public struct RadiusPos
    {
        public int Radius;
        public int PosX;
        public int PosY;

        public RadiusPos(int radius, int x, int y)
        {
            Radius = radius;
            PosX = x;
            PosY = y;
        }
    }

    public enum GradientSide
    {
        Left, Right, Upper, Lower,
        UpperLeft, UpperRight, LowerLeft, LowerRight
    }

    public enum ErrorDiffusion
    { FloydSteinberg, Stucci, Sierra, JaJuNi }

    public enum PatternShape
    { Brick, Diamond, Hexagon, Circle }

    public enum QuadPosition
    { qpUpperLeft, qpUpperRight, qpLowerLeft, qpLowerRight }

    public struct pixelinfo
    {
        public int r;
        public int g;
        public int b;

        public int count;
    }

    public enum DitherPattern
    { od22, od33, od332, od44, od442, od443 }

    public enum ParallelogramDeformation
    { pdHorizontal, pdVertical }

    public enum WaveMode
    { wmHorizontal, wmVertical, wmBoth }

    public enum eEdge { Edge2, Edge3 }

    public enum RankOp { Max, Mid, Min }

    public enum BathroomWindowMode
    { bwVertical, bwHorizontal, bwBoth }

    public struct FrameUnit
    {
        public int width;
        public int percentAlpha;
    }

    public enum RGBSelection { all, red, green, blue }

}