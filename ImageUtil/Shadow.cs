using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using System.Linq;

namespace ImageUtils
{
    public static class Shadow
    {
        private static int shadowSize = 5;

        static Image shadowDownRight    = new Bitmap( ImageUtil.Properties.Resources.tshadowdownright);
        static Image shadowDownLeft     = new Bitmap( ImageUtil.Properties.Resources.tshadowdownleft);
        static Image shadowDown         = new Bitmap( ImageUtil.Properties.Resources.tshadowdown);
        static Image shadowRight        = new Bitmap( ImageUtil.Properties.Resources.tshadowright);
        static Image shadowTopRight     = new Bitmap( ImageUtil.Properties.Resources.tshadowtopright);
        
        public static Bitmap dropShadow(Bitmap original)
        {
            Bitmap dest = new Bitmap(original.Width + shadowSize, original.Height + shadowSize);
            Graphics g = Graphics.FromImage(dest);
            
            TextureBrush shadowRightBrush = new TextureBrush(shadowRight, WrapMode.Tile);
            TextureBrush shadowDownBrush = new TextureBrush(shadowDown, WrapMode.Tile);

            shadowRightBrush.TranslateTransform(original.Width - shadowSize, 0);   
            shadowDownBrush.TranslateTransform(0, original.Height - shadowSize);

            Rectangle shadowDownRectangle = new Rectangle(
                shadowSize,                               // X
                original.Height, //- shadowSize,                            // Y
                original.Width - shadowSize,// - (shadowSize * 2 + shadowMargin),        // width (stretches)
                shadowSize                                               // height
                );

            Rectangle shadowRightRectangle = new Rectangle(
                original.Width,// - shadowSize,                             // X
                shadowSize, // + shadowMargin,                      // Y
                shadowSize,                                     // width
                original.Height -shadowSize // - (shadowSize * 2 + shadowMargin)        // height (stretches)
                );

            // And draw the shadow on the right and at the bottom.

            g.FillRectangle(shadowDownBrush, shadowDownRectangle);
            g.FillRectangle(shadowRightBrush, shadowRightRectangle);

            // 隅っこ三つ
            g.DrawImage(shadowTopRight, new Rectangle(original.Width, 0, shadowSize, shadowSize));
            g.DrawImage(shadowDownRight, new Rectangle(original.Width, original.Height, shadowSize, shadowSize));
            g.DrawImage(shadowDownLeft, new Rectangle(0, original.Height, shadowSize, shadowSize));

            g.DrawImage(original, new Rectangle(new Point(0, 0), new Size(original.Width, original.Height)));

            shadowDownBrush.Dispose();
            shadowRightBrush.Dispose();

            shadowDownBrush = null;
            shadowRightBrush = null;

            return dest;
        }
    }
}
