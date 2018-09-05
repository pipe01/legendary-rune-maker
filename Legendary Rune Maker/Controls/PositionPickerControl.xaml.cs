using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Legendary_Rune_Maker.Controls
{
    /// <summary>
    /// Interaction logic for PositionPickerControl.xaml
    /// </summary>
    public partial class PositionPickerControl : UserControl
    {
        private IDictionary<FadeImage, Position> Positions = new Dictionary<FadeImage, Position>();

        private FadeImage PreviousSelected;

        private Position _Selected;
        public Position Selected
        {
            get => _Selected;
            set
            {
                if (_Selected == value && value != Position.Fill)
                {
                    Selected = Position.Fill;
                    return;
                }
                else
                {
                    _Selected = value;

                    SelectedChanged?.Invoke(this, EventArgs.Empty);
                }
                
                if (PreviousSelected != null)
                {
                    SetImage(PreviousSelected, Positions[PreviousSelected]);
                }

                if (Selected != Position.Fill)
                {
                    var img = Positions.Keys.ElementAt(Positions.Values.ToList().IndexOf(value));
                    SetImage(img, Position.Fill);

                    PreviousSelected = img;
                }
                else
                {
                    PreviousSelected = null;
                }
            }
        }
        
        public event EventHandler SelectedChanged;

        public PositionPickerControl()
        {
            InitializeComponent();

            Positions[PosTop] = Position.Top;
            Positions[PosJungle] = Position.Jungle;
            Positions[PosMid] = Position.Mid;
            Positions[PosSupport] = Position.Support;
            Positions[PosBottom] = Position.Bottom;
        }

        private void PositionPicker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var image = sender as FadeImage;

            if (image == PreviousSelected)
            {
                Selected = Positions[PreviousSelected];
            }
            else
            {
                Selected = Positions[image];
            }
        }

        private void SetImage(FadeImage image, Position pos)
        {
            image.Source = PositionImage(pos);
        }

        private ImageSource PositionImage(Position pos)
        {
            return (ImageSource)Application.Current.FindResource(pos.ToString());
        }

        private void FadeImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
