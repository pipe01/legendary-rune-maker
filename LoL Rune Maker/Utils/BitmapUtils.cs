using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace LoL_Rune_Maker.Utils
{
    public static class BitmapUtils
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private static ImageConverter Converter = new ImageConverter();

        public static Bitmap ToBitmap(byte[] data) => Converter.ConvertFrom(data) as Bitmap;

        public static Bitmap Filter(Bitmap original, Func<byte, byte, byte, byte, (byte R, byte G, byte B)> filter)
        {
            BitmapData sourceData = original.LockBits(new Rectangle(0, 0, original.Width, original.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);
            original.UnlockBits(sourceData);

            for (int k = 0; k + 4 < pixelBuffer.Length; k += 4)
            {
                var result = filter(pixelBuffer[k], pixelBuffer[k + 1], pixelBuffer[k + 2], pixelBuffer[k + 3]);

                pixelBuffer[k] = result.R;
                pixelBuffer[k + 1] = result.G;
                pixelBuffer[k + 2] = result.B;
            }

            Bitmap resultBitmap = new Bitmap(original.Width, original.Height);
            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(pixelBuffer, 0, resultData.Scan0, pixelBuffer.Length);
            resultBitmap.UnlockBits(resultData);

            return resultBitmap;
        }

        public static Bitmap Grayscale(Bitmap original)
        {
            return Filter(original, (r, g, b, a) =>
            {
                byte x = (byte)(r * 0.3 + g * 0.59 + b * 0.11);
                return (x, x, x);
            });
        }
        
        public static BitmapSource Bitmap2BitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retval;

            try
            {
                retval = Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }

        public static Bitmap BitmapSource2Bitmap(BitmapSource source)
        {
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(source));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
    }
}
