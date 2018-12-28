using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Utils;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for GrayscaleImageControl.xaml
    /// </summary>
    public partial class GrayscaleImageControl : UserControl
    {
        public bool Selected
        {
            get => (bool)GetValue(SelectedProperty);
            set { SetValue(SelectedProperty, value); }
        }
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(bool), typeof(GrayscaleImageControl), new PropertyMetadata(OnSelectedChanged));

        public bool ShowSelector
        {
            get { return (bool)GetValue(ShowSelectorProperty); }
            set { SetValue(ShowSelectorProperty, value); }
        }
        public static readonly DependencyProperty ShowSelectorProperty = DependencyProperty.Register("ShowSelector", typeof(bool), typeof(GrayscaleImageControl), new PropertyMetadata(false));
        
        public event EventHandler<bool> SelectedChanged;

        public Rune Rune { get; }

        private BitmapSource Normal, Gray;

        public GrayscaleImageControl(Rune rune)
        {
            this.Rune = rune;
            this.DataContext = this;

            InitializeComponent();
            
            var description = (View.ToolTip as DependencyObject).FindChild<RichTextBox>("Description");
            string descNice = rune.RichLongDesc.Replace("&nbsp;", " ");

            description.Document = XamlReader.Parse(descNice) as FlowDocument;

            var paragraph = description.Document.Blocks.First() as Paragraph;

            var inlines = paragraph.Inlines.ToList();
            paragraph.Inlines.Clear();

            for (int i = 0; i < inlines.Count; i++)
            {
                var item = inlines[i];

                if (item is InlineUIContainer)
                {
                    var ruler = new Line { X1 = 0, Y1 = 0, X2 = 1000, Y2 = 0, Stroke = new SolidColorBrush(Color.FromRgb(81, 82, 80)), StrokeThickness = 2 };
                    ruler.Margin = new Thickness(0, 5, 0, 5);

                    paragraph.Inlines.Add(new InlineUIContainer(ruler, description.CaretPosition.GetInsertionPosition(LogicalDirection.Forward)));
                }
                else
                {
                    paragraph.Inlines.Add(item);
                }
            }
        }
        
        private async void UserControl_Initialized(object sender, EventArgs e)
        {
            this.Normal = await ImageCache.Instance.Get(Riot.ImageEndpoint + this.Rune.IconURL);
            this.Gray = await ImageCache.Instance.GetGrayscale(Riot.ImageEndpoint + this.Rune.IconURL);

            View.Source = this.Gray;
        }
        
        private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;

            var image = d as GrayscaleImageControl;
            image.View.Source = (bool)e.NewValue ? image.Normal : image.Gray;

            if ((bool)e.NewValue)
            {
                ((Storyboard)image.FindResource("FadeIn")).Begin();
            }
            else
            {
                ((Storyboard)image.FindResource("FadeOut")).Begin();
            }

            image.SelectedChanged?.Invoke(image, (bool)e.NewValue);
        }
        
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Selected = true;
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!Selected)
                View.Source = this.Normal;

            if (e.LeftButton == MouseButtonState.Pressed)
                Selected = true;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!Selected)
                View.Source = this.Gray;
        }
    }
}
