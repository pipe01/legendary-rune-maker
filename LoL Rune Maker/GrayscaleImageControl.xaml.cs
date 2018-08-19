using LoL_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LoL_Rune_Maker
{
    /// <summary>
    /// Interaction logic for GrayscaleImageControl.xaml
    /// </summary>
    public partial class GrayscaleImageControl : UserControl
    {
        public bool Selected
        {
            get => (bool)GetValue(SelectedProperty);
            set { SetValue(SelectedProperty, value); SetSelected(); }
        }
        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(bool), typeof(GrayscaleImageControl));

        public Rune Rune { get; }

        private BitmapSource Normal, Gray;

        public GrayscaleImageControl(Rune rune)
        {
            this.Rune = rune;
            this.DataContext = this;

            InitializeComponent();

            try
            {

                Description.Document = SetRTF(rune.RichLongDesc);
            }
            catch (Exception)
            {
            }
            var paragraph = Description.Document.Blocks.First() as Paragraph;

            var inlines = paragraph.Inlines.ToList();
            paragraph.Inlines.Clear();

            for (int i = 0; i < inlines.Count; i++)
            {
                var item = inlines[i];

                if (item is InlineUIContainer)
                {
                    var ruler = new Line { X1 = 0, Y1 = 0, X2 = 1000, Y2 = 0, Stroke = new SolidColorBrush(Color.FromRgb(81, 82, 80)), StrokeThickness = 2 };
                    ruler.Margin = new Thickness(0, 5, 0, 5);
                    
                    paragraph.Inlines.Add(new InlineUIContainer(ruler, Description.CaretPosition.GetInsertionPosition(LogicalDirection.Forward)));
                }
                else
                {
                    paragraph.Inlines.Add(item);
                }
            }
        }

        private static FlowDocument SetRTF(string xamlString)
        {
            try
            {
                return XamlReader.Parse(xamlString) as FlowDocument;
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        private async void UserControl_Initialized(object sender, EventArgs e)
        {
            this.Normal = await ImageCache.Instance.Get(Riot.ImageEndpoint + this.Rune.IconURL);
            this.Gray = await ImageCache.Instance.GetGrayscale(Riot.ImageEndpoint + this.Rune.IconURL);

            View.Source = this.Gray;
        }

        private void SetSelected()
        {
            View.Source = Selected ? this.Normal : this.Gray;
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Selected = true;
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!Selected)
                View.Source = this.Normal;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!Selected)
                View.Source = this.Gray;
        }
    }
}
