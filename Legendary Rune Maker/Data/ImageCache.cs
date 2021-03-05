using Legendary_Rune_Maker.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

#nullable enable

namespace Legendary_Rune_Maker.Data
{
    public class ImageCache
    {
        public static ImageCache Instance { get; } = new ImageCache("cache");

        private static readonly bool IsDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());

        public bool LocalCache => Directory.Exists(CachePath);

        public string FullCachePath => Path.Combine(Path.GetFullPath("./"), CachePath);

        private readonly IDictionary<string, (BitmapSource Normal, BitmapSource? Grayscale, byte[] Raw)> Dicc = new ConcurrentDictionary<string, (BitmapSource, BitmapSource?, byte[])>();

        private readonly string CachePath;

        public ImageCache(string cachePath)
        {
            this.CachePath = cachePath;
        }

        public async Task<BitmapSource> Get(string url)
        {
            var sw = Stopwatch.StartNew();

            if (!Dicc.TryGetValue(url, out var img))
            {
                string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), CachePath, ToMD5(url));
                byte[] data;

                if (File.Exists(file))
                {
                    data = File.ReadAllBytes(file);
                }
                else
                {
                    data = await new WebClient().DownloadDataTaskAsync(url);

                    if (!IsDesignMode)
                    {
                        Directory.CreateDirectory(CachePath);
                        File.WriteAllBytes(file, data);
                    }
                }

                Dicc[url] = img = (RawToBitmapImage(data), null, data);
            }

            Debug.WriteLine($"Time: {sw.Elapsed} {url}");
            return img.Normal;
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
            image.Freeze();
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
