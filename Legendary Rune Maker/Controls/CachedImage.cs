using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker.Controls
{
    public class CachedImage : Image
    {
        public static readonly DependencyProperty ImageURLProperty = DependencyProperty.Register("ImageURL", typeof(string), typeof(CachedImage));
        public string ImageURL
        {
            get { return (string)GetValue(ImageURLProperty); }
            set { SetValue(ImageURLProperty, value); }
        }

        static CachedImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CachedImage), new FrameworkPropertyMetadata(typeof(CachedImage)));
        }

        public CachedImage()
        {
            this.Initialized += this.CachedImage_Initialized;
        }

        private async void CachedImage_Initialized(object sender, EventArgs e)
        {
            if (this.ImageURL != null)
                this.Source = await ImageCache.Instance.Get(this.ImageURL);
        }
    }
}
