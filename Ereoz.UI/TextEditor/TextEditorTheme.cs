using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ereoz.UI.TextEditor
{
    public class TextEditorTheme
    {
        public ElementTheme Main { get; set; }
        public ElementTheme H1 { get; set; }
        public ElementTheme H2 { get; set; }
        public ElementTheme H3 { get; set; }
        public ElementTheme Quote { get; set; }
        public ElementTheme Block { get; set; }
        public ElementTheme Code { get; set; }

        public TextEditorTheme()
        {
            Main = new ElementTheme
            {
                FontSize = 17.0,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            H1 = new ElementTheme
            {
                FontSize = 29.0,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            H2 = new ElementTheme
            {
                FontSize = 25.0,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            H3 = new ElementTheme
            {
                FontSize = 21.0,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            Quote = new ElementTheme
            {
                FontSize = 15.0,
                FontFamily = new FontFamily("Times New Roman"),
                Foreground = Brushes.Black,
                Background = Brushes.LightGray,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(8.0, 0, 0, 0),
                Padding = new Thickness(8.0, 0, 0, 0)
            };

            Block = new ElementTheme
            {
                FontSize = 15.0,
                FontFamily = new FontFamily("Lucida Console"),
                Foreground = Brushes.WhiteSmoke,
                Background = Brushes.DimGray,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8.0)
            };

            Code = new ElementTheme
            {
                FontSize = 15.0,
                FontFamily = new FontFamily("Courier New"),
                Foreground = Brushes.DimGray,
                Background = Brushes.WhiteSmoke
            };
        }
    }

    public class ElementTheme
    {
        public double FontSize { get; set; }
        public FontFamily FontFamily { get; set; }
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
        public Brush BorderBrush { get; set; }
        public Thickness BorderThickness { get; set; }
        public Thickness Padding { get; set; }

        public void SetTheme(Paragraph paragraph)
        {
            paragraph.FontSize = FontSize;
            paragraph.FontFamily = FontFamily;
            paragraph.Foreground = Foreground;
            paragraph.Background = Background;
            paragraph.BorderBrush = BorderBrush;
            paragraph.BorderThickness = BorderThickness;
            paragraph.Padding = Padding;
        }

        public void SetTheme(Run run)
        {
            run.FontSize = FontSize;
            run.FontFamily = FontFamily;
            run.Foreground = Foreground;
            run.Background = Background;
        }
    }
}
