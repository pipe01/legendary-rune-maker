using LoL_Rune_Maker.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LoL_Rune_Maker.Data
{
    public class ImageCache
    {
        public static ImageCache Instance { get; } = new ImageCache("cache");

        public bool LocalCache => Directory.Exists(CachePath);

        private IDictionary<string, (BitmapSource n, BitmapSource g, byte[] d)> Dicc = new Dictionary<string, (BitmapSource, BitmapSource, byte[])>();

        private readonly string CachePath;

        public ImageCache(string cachePath)
        {
            this.CachePath = cachePath;
        }

        public async Task<BitmapSource> Get(string url)
        {
            if (!Dicc.TryGetValue(url, out var img))
            {
                string file = Path.Combine(CachePath, ToMD5(url));
                byte[] data;

                if (File.Exists(file))
                {
                    data = File.ReadAllBytes(file);
                }
                else
                {
                    data = await new WebClient().DownloadDataTaskAsync(url);
                    
                    if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                    {
                        Directory.CreateDirectory(CachePath);
                        File.WriteAllBytes(file, data);
                    }
                }
                
                Dicc[url] = (RawToBitmapImage(data), null, data);
            }

            return Dicc[url].n;
        }
        
        public async Task<BitmapSource> GetGrayscale(string url)
        {
            if (!Dicc.ContainsKey(url))
                await Get(url);

            var (n, g, d) = Dicc[url];

            if (g == null)
            {
                g = BitmapUtils.Bitmap2BitmapSource(BitmapUtils.Grayscale(BitmapUtils.ToBitmap(d)));
                Dicc[url] = (n, g, d);
            }

            return g;
        }

        private static BitmapSource RawToBitmapImage(byte[] data)
        {
            var stream = new MemoryStream(data);
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            return image;
        }
        
        private static string ToMD5(string txt)
        {
            using (var m = MD5.Create())
            {
                return BitConverter.ToString(m.ComputeHash(Encoding.UTF8.GetBytes(txt))).Replace("-", "");
            }
        }
    }
}
