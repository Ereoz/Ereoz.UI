using Ereoz.UI.TextEditor;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Ereoz.UI
{
    /// <summary>
    /// Логика взаимодействия для FusionTextEditor.xaml
    /// </summary>
    public partial class FusionTextEditor : UserControl
    {
        private TextEditorTheme _theme;

        private bool? _h1State = false;
        private bool? _h2State = false;
        private bool? _h3State = false;

        public FusionTextEditor()
        {
            InitializeComponent();
            DataContext = this;

            _theme = new TextEditorTheme();

            rtb.FontSize = _theme.Main.FontSize;

            BoldCommand = new RelayCommand(BoldCommandImpl, StyleCanExecute);
            ItalicCommand = new RelayCommand(ItalicCommandImpl, StyleCanExecute);
            UnderlineCommand = new RelayCommand(UnderlineCommandImpl, StyleCanExecute);
            H1Command = new RelayCommand(H1CommandImpl, HeadersCanExecute);
            H2Command = new RelayCommand(H2CommandImpl, HeadersCanExecute);
            H3Command = new RelayCommand(H3CommandImpl, HeadersCanExecute);
            BulletListCommand = new RelayCommand(BulletListCommandImpl, ListCanExecute);
            NumberingListCommand = new RelayCommand(NumberingListCommandImpl, ListCanExecute);
            QuoteCommand = new RelayCommand(QuoteCommandImpl, QuoteCanExecute);
            BlockCommand = new RelayCommand(BlockCommandImpl, BlockCanExecute);
            CodeCommand = new RelayCommand(CodeCommandImpl, CodeCanExecute);
            LineBreakCommand = new RelayCommand(LineBreakCommandImpl);
            ImageCommand = new RelayCommand(ImageCommandImpl);
            LeftCommand = new RelayCommand(LeftCommandImpl);
            CenterCommand = new RelayCommand(CenterCommandImpl);
            RightCommand = new RelayCommand(RightCommandImpl);
            JustifyCommand = new RelayCommand(JustifyCommandImpl);
        }

        public ICommand BoldCommand { get; set; }
        public ICommand ItalicCommand { get; set; }
        public ICommand UnderlineCommand { get; set; }
        public ICommand H1Command { get; set; }
        public ICommand H2Command { get; set; }
        public ICommand H3Command { get; set; }
        public ICommand BulletListCommand { get; set; }
        public ICommand NumberingListCommand { get; set; }
        public ICommand QuoteCommand { get; set; }
        public ICommand BlockCommand { get; set; }
        public ICommand CodeCommand { get; set; }
        public ICommand LineBreakCommand { get; set; }
        public ICommand ImageCommand { get; set; }
        public ICommand LeftCommand { get; set; }
        public ICommand CenterCommand { get; set; }
        public ICommand RightCommand { get; set; }
        public ICommand JustifyCommand { get; set; }

        public byte[] GetContent()
        {
            TextRange range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);

            using (Stream stream = new MemoryStream())
            {
                range.Save(stream, DataFormats.XamlPackage);

                using (var reader = new BinaryReader(stream))
                {
                    stream.Position = 0;
                    return reader.ReadBytes((int)reader.BaseStream.Length);
                }
            }
        }

        public void SetContent(byte[] bytes)
        {
            TextRange range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(bytes);
                    writer.Flush();
                    stream.Position = 0;

                    range.Load(stream, DataFormats.XamlPackage);
                }
            }

            FindLinksInDocument(rtb.Document.Blocks);

            rtb.Focus();

            RichTextBox_SelectionChanged(null, null);
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;

            if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflowGrid)
                overflowGrid.Visibility = Visibility.Collapsed;

            if (toolBar.Template.FindName("MainPanelBorder", toolBar) is FrameworkElement mainPanelBorder)
                mainPanelBorder.Margin = new Thickness();
        }

        private void PasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            string[] pastedParagraphs = Clipboard.GetText().Replace("\r\n", "\n").Split('\n').Where(it => !string.IsNullOrWhiteSpace(it)).ToArray();

            if (pastedParagraphs.Length > 0 && !rtb.Selection.IsEmpty)
                rtb.Selection.Text = "";

            for (int i = 0; i < pastedParagraphs.Length; i++)
            {
                if (i == 0)
                {
                    rtb.CaretPosition.InsertTextInRun(pastedParagraphs[i]);

                    if (rtb.CaretPosition.GetPositionAtOffset(pastedParagraphs[i].Length) is TextPointer newPosition)
                        rtb.CaretPosition = newPosition;
                }
                else if (i == pastedParagraphs.Length - 1)
                {
                    rtb.CaretPosition.InsertParagraphBreak().InsertTextInRun(pastedParagraphs[i]);
                    rtb.CaretPosition = rtb.CaretPosition.GetPositionAtOffset(pastedParagraphs[i].Length + 4); // 4 - magic number
                }
                else
                {
                    rtb.CaretPosition.InsertParagraphBreak().InsertTextInRun(pastedParagraphs[i]);
                    rtb.CaretPosition = rtb.CaretPosition.Paragraph.NextBlock.ElementEnd;
                }

                FindLinksInPasted(rtb.CaretPosition.Paragraph);
            }
        }

        private void RichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (rtb.Document.Blocks.FirstOrDefault() == null)
                rtb.Document.Blocks.Add(new Paragraph());

            UpdateBoldButton();
            UpdateItalicButton();
            UpdateUnderlineButton();
            UpdateBulletButton();
            UpdateNumberingButton();
            UpdateCodeButton();
            UpdateAlignButtons();
            UpdateQuoteAndBlockButtons();

            if (_h1State == true)
                H1CommandImpl();
            else
                UpdateH1Button();

            if (_h2State == true)
                H2CommandImpl();
            else
                UpdateH2Button();

            if (_h3State == true)
                H3CommandImpl();
            else
                UpdateH3Button();
        }

        private void RichTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _h1State = btnH1.IsChecked;
                _h2State = btnH2.IsChecked;
                _h3State = btnH3.IsChecked;

                if (btnQuote.IsChecked == true || btnBlock.IsChecked == true)
                {
                    rtb.CaretPosition.InsertLineBreak();
                    rtb.CaretPosition = rtb.CaretPosition.GetNextInsertionPosition(LogicalDirection.Forward);
                    e.Handled = true;
                }

                FindLinkForSelect(rtb.CaretPosition);
            }

            if (e.Key == Key.Space)
            {
                FindLinkForSelect(rtb.CaretPosition);
            }

            if (e.Key == Key.Back)
            {
                FindLinkForRemoveBefore();
            }

            if (e.Key == Key.Delete)
            {
                FindLinkForRemoveAfter();
            }
        }

        private void BoldCommandImpl(object obj)
        {
            EditingCommands.ToggleBold.Execute(null, rtb);
            UpdateBoldButton();
        }

        private void ItalicCommandImpl(object obj)
        {
            EditingCommands.ToggleItalic.Execute(null, rtb);
            UpdateItalicButton();
        }

        private void UnderlineCommandImpl(object obj)
        {
            EditingCommands.ToggleUnderline.Execute(null, rtb);
            UpdateUnderlineButton();
        }

        private void H1CommandImpl(object obj = null)
        {
            _h1State = false;

            if (rtb.Selection.GetPropertyValue(Inline.FontSizeProperty).Equals(_theme.H1.FontSize))
            {
                _theme.Main.SetTheme(rtb.Selection.Start.Paragraph);
                btnH1.IsChecked = false;
            }
            else
            {
                _theme.H1.SetTheme(rtb.Selection.Start.Paragraph);
                btnH1.IsChecked = true;

                //TextRange textRange = new TextRange(rtb.Selection.Start.Paragraph.ElementStart, rtb.Selection.Start.Paragraph.ElementEnd);
                //if (!string.IsNullOrWhiteSpace(textRange.Text))
                //{
                //    string text = textRange.Text;
                //    rtb.Selection.Start.Paragraph.Inlines.Clear();
                //    rtb.Selection.Start.Paragraph.Inlines.Add(text);
                //}
            }

            btnH2.IsChecked = false;
            btnH3.IsChecked = false;

            UpdateH1Button();
        }

        private void H2CommandImpl(object obj = null)
        {
            _h2State = false;

            if (rtb.Selection.GetPropertyValue(Inline.FontSizeProperty).Equals(_theme.H2.FontSize))
            {
                _theme.Main.SetTheme(rtb.Selection.Start.Paragraph);
                btnH2.IsChecked = false;
            }
            else
            {
                _theme.H2.SetTheme(rtb.Selection.Start.Paragraph);
                btnH2.IsChecked = true;

                //TextRange textRange = new TextRange(rtb.Selection.Start.Paragraph.ElementStart, rtb.Selection.Start.Paragraph.ElementEnd);
                //string text = textRange.Text;
                //rtb.Selection.Start.Paragraph.Inlines.Clear();
                //rtb.Selection.Start.Paragraph.Inlines.Add(text);
            }

            btnH1.IsChecked = false;
            btnH3.IsChecked = false;

            UpdateH2Button();
        }

        private void H3CommandImpl(object obj = null)
        {
            _h3State = false;

            if (rtb.Selection.GetPropertyValue(Inline.FontSizeProperty).Equals(_theme.H3.FontSize))
            {
                _theme.Main.SetTheme(rtb.Selection.Start.Paragraph);
                btnH3.IsChecked = false;
            }
            else
            {
                _theme.H3.SetTheme(rtb.Selection.Start.Paragraph);
                btnH3.IsChecked = true;

                //TextRange textRange = new TextRange(rtb.Selection.Start.Paragraph.ElementStart, rtb.Selection.Start.Paragraph.ElementEnd);
                //string text = textRange.Text;
                //rtb.Selection.Start.Paragraph.Inlines.Clear();
                //rtb.Selection.Start.Paragraph.Inlines.Add(text);
            }

            btnH1.IsChecked = false;
            btnH2.IsChecked = false;

            UpdateH3Button();
        }

        private void BulletListCommandImpl(object obj)
        {
            EditingCommands.ToggleBullets.Execute(null, rtb);
            UpdateBulletButton();
        }

        private void NumberingListCommandImpl(object obj)
        {
            EditingCommands.ToggleNumbering.Execute(null, rtb);
            UpdateNumberingButton();
        }

        private void QuoteCommandImpl(object obj)
        {
            if (rtb.Selection.Start.Paragraph.Background?.ToString() == _theme.Quote.Background.ToString())
            {
                _theme.Main.SetTheme(rtb.Selection.Start.Paragraph);
            }
            else
            {
                _theme.Quote.SetTheme(rtb.Selection.Start.Paragraph);

                //TextRange textRange = new TextRange(rtb.Selection.Start.Paragraph.ElementStart, rtb.Selection.Start.Paragraph.ElementEnd);
                //string text = textRange.Text;
                //rtb.Selection.Start.Paragraph.Inlines.Clear();
                //rtb.Selection.Start.Paragraph.Inlines.Add(text);
            }

            UpdateQuoteAndBlockButtons();
        }

        private void BlockCommandImpl(object obj)
        {
            if (rtb.Selection.Start.Paragraph.Background?.ToString() == _theme.Block.Background.ToString())
            {
                _theme.Main.SetTheme(rtb.Selection.Start.Paragraph);
            }
            else
            {
                _theme.Block.SetTheme(rtb.Selection.Start.Paragraph);

                //TextRange textRange = new TextRange(rtb.Selection.Start.Paragraph.ElementStart, rtb.Selection.Start.Paragraph.ElementEnd);
                //string text = textRange.Text;
                //rtb.Selection.Start.Paragraph.Inlines.Clear();
                //rtb.Selection.Start.Paragraph.Inlines.Add(text);
            }

            UpdateQuoteAndBlockButtons();
        }

        private void CodeCommandImpl(object obj)
        {
            if (!SelectionIsCode())
            {
                rtb.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, _theme.Code.FontFamily);
                rtb.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, _theme.Code.FontSize);
                rtb.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, _theme.Code.Foreground);
                rtb.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, _theme.Code.Background);
            }
            else
            {
                rtb.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, _theme.Main.FontFamily);
                rtb.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, _theme.Main.FontSize);
                rtb.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, _theme.Main.Foreground);
                rtb.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, _theme.Main.Background);
            }

            UpdateCodeButton();
        }

        private void LineBreakCommandImpl(object obj)
        {
            rtb.CaretPosition.InsertLineBreak();
            rtb.CaretPosition = rtb.CaretPosition.GetNextInsertionPosition(LogicalDirection.Forward);
        }

        private void ImageCommandImpl(object obj)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                if (rtb.CaretPosition.Paragraph == null)
                {
                    rtb.Document.Blocks.Add(
                        new Paragraph(new InlineUIContainer(
                            new Image()
                            {
                                Source = new BitmapImage(new Uri(openFileDialog.FileName))
                            })));
                }
                else if (string.IsNullOrWhiteSpace(new TextRange(rtb.CaretPosition.Paragraph.ElementStart, rtb.CaretPosition.Paragraph.ElementEnd).Text))
                {
                    rtb.CaretPosition.Paragraph.Inlines.Clear();

                    rtb.CaretPosition.Paragraph.Inlines.Add(
                        new InlineUIContainer(
                            new Image()
                            {
                                Source = new BitmapImage(new Uri(openFileDialog.FileName))
                            }));
                }
                else if(string.IsNullOrWhiteSpace(rtb.CaretPosition.GetTextInRun(LogicalDirection.Backward)))
                {
                    rtb.Document.Blocks.InsertBefore(
                        rtb.CaretPosition.Paragraph,
                        new Paragraph(new InlineUIContainer(
                            new Image()
                            {
                                Source = new BitmapImage(new Uri(openFileDialog.FileName))
                            })));
                }
                else if (string.IsNullOrWhiteSpace(rtb.CaretPosition.GetTextInRun(LogicalDirection.Forward)))
                {
                    rtb.Document.Blocks.InsertAfter(
                        rtb.CaretPosition.Paragraph,
                        new Paragraph(new InlineUIContainer(
                            new Image()
                            {
                                Source = new BitmapImage(new Uri(openFileDialog.FileName))
                            })));
                }
                else
                {
                    rtb.CaretPosition.InsertParagraphBreak();

                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

                    rtb.Document.Blocks.InsertBefore(
                        rtb.CaretPosition.Paragraph,
                        new Paragraph(new InlineUIContainer(
                            new Image()
                            {
                                Source = new BitmapImage(new Uri(openFileDialog.FileName))
                            })));
                }
            }
        }

        private void LeftCommandImpl(object obj)
        {
            EditingCommands.AlignLeft.Execute(null, rtb);
            UpdateAlignButtons();
        }

        private void CenterCommandImpl(object obj)
        {
            EditingCommands.AlignCenter.Execute(null, rtb);
            UpdateAlignButtons();
        }

        private void RightCommandImpl(object obj)
        {
            EditingCommands.AlignRight.Execute(null, rtb);
            UpdateAlignButtons();
        }

        private void JustifyCommandImpl(object obj)
        {
            EditingCommands.AlignJustify.Execute(null, rtb);
            UpdateAlignButtons();
        }

        private void UpdateBoldButton()
        {
            object prop = rtb.Selection.GetPropertyValue(Inline.FontWeightProperty);
            btnBold.IsChecked = prop != null && prop != DependencyProperty.UnsetValue && prop.Equals(FontWeights.Bold);
        }

        private void UpdateItalicButton()
        {
            object prop = rtb.Selection.GetPropertyValue(Inline.FontStyleProperty);
            btnItalic.IsChecked = prop != null && prop != DependencyProperty.UnsetValue && prop.Equals(FontStyles.Italic);
        }

        private void UpdateUnderlineButton()
        {
            object prop = rtb.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            btnUnderline.IsChecked = prop is TextDecorationCollection collection && collection.Count == 1 && collection[0].Location == TextDecorationLocation.Underline;
        }

        private void UpdateH1Button()
        {
            object prop = rtb.Selection.GetPropertyValue(Inline.FontSizeProperty);
            btnH1.IsChecked = prop != null && prop != DependencyProperty.UnsetValue && prop.Equals(_theme.H1.FontSize);
        }

        private void UpdateH2Button()
        {
            object prop = rtb.Selection.GetPropertyValue(Inline.FontSizeProperty);
            btnH2.IsChecked = prop != null && prop != DependencyProperty.UnsetValue && prop.Equals(_theme.H2.FontSize);
        }

        private void UpdateH3Button()
        {
            object prop = rtb.Selection.GetPropertyValue(Inline.FontSizeProperty);
            btnH3.IsChecked = prop != null && prop != DependencyProperty.UnsetValue && prop.Equals(_theme.H3.FontSize);
        }

        private void UpdateBulletButton()
        {
            btnBullet.IsChecked = rtb.CaretPosition.Paragraph?.Parent is ListItem listItem && listItem.List.MarkerStyle == TextMarkerStyle.Disc;
        }

        private void UpdateNumberingButton()
        {
            btnNumbering.IsChecked = rtb.CaretPosition.Paragraph?.Parent is ListItem listItem && listItem.List.MarkerStyle == TextMarkerStyle.Decimal;
        }

        private void UpdateCodeButton()
        {
            btnCode.IsChecked = SelectionIsCode();
        }

        private void UpdateQuoteAndBlockButtons()
        {
            btnQuote.IsChecked = rtb.Selection.Start.Paragraph?.Background?.ToString() == _theme.Quote.Background.ToString();
            btnBlock.IsChecked = rtb.Selection.Start.Paragraph?.Background?.ToString() == _theme.Block.Background.ToString();
        }

        private void UpdateAlignButtons()
        {
            btnLeft.IsChecked = rtb.Selection.Start.Paragraph?.TextAlignment == TextAlignment.Left;
            btnCenter.IsChecked = rtb.Selection.Start.Paragraph?.TextAlignment == TextAlignment.Center;
            btnRight.IsChecked = rtb.Selection.Start.Paragraph?.TextAlignment == TextAlignment.Right;
            btnJustify.IsChecked = rtb.Selection.Start.Paragraph?.TextAlignment == TextAlignment.Justify;
        }

        private bool SelectionIsCode()
        {
            object prop = rtb.Selection.GetPropertyValue(Inline.FontFamilyProperty);
            return prop is FontFamily fontFamily && fontFamily.Source == _theme.Code.FontFamily.Source;
        }

        private void FindLinkForSelect(TextPointer endWord)
        {
            string beforeText = endWord.GetTextInRun(LogicalDirection.Backward);
            int startIndex = beforeText.Length - beforeText.LastIndexOf(' ') - 1;
            TextPointer start = endWord.GetPositionAtOffset(-startIndex);

            var word = new TextRange(start, endWord);
            string text = word.Text;

            if (IsValidURL(text))
            {
                // TODO: Создание ссылки таким образом при определённых сценариях вызывает неотлавливаемое исключение.
                //Hyperlink link = new Hyperlink(start, endWord);

                word.Text = "";
                Hyperlink link = new Hyperlink(new Run(text), start);
                rtb.CaretPosition = link.ElementEnd;

                if (!text.StartsWith("http"))
                    text = "http://" + text;

                link.NavigateUri = new Uri(text);
                link.RequestNavigate += new RequestNavigateEventHandler(LinkRequestNavigate);

                link.ToolTip = link.NavigateUri.ToString();
            }
        }

        private bool IsValidURL(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (text.StartsWith("www."))
                text = "http://" + text;

            return Uri.TryCreate(text, UriKind.Absolute, out Uri uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeFtp);
        }

        private void LinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri.ToString());
        }

        private void FindLinkForRemoveBefore()
        {
            var link = rtb.CaretPosition.Paragraph?.Inlines.Where(
                it => it is Hyperlink hyperlink &&
                hyperlink.ElementStart.CompareTo(rtb.CaretPosition.GetPositionAtOffset(-1)) == -1 &&
                hyperlink.ElementEnd.CompareTo(rtb.CaretPosition.GetPositionAtOffset(-1)) != -1)
                .FirstOrDefault();

            if (link != null)
                rtb.CaretPosition.Paragraph?.Inlines.Remove(link);
        }

        private void FindLinkForRemoveAfter()
        {
            var link = rtb.CaretPosition.Paragraph?.Inlines.Where(
                it => it is Hyperlink hyperlink &&
                hyperlink.ElementStart.CompareTo(rtb.CaretPosition.GetPositionAtOffset(1)) != 1 &&
                hyperlink.ElementEnd.CompareTo(rtb.CaretPosition.GetPositionAtOffset(1)) == 1)
                .FirstOrDefault();

            if (link != null)
                rtb.CaretPosition.Paragraph?.Inlines.Remove(link);
        }

        private void FindLinksInPasted(Paragraph paragraph)
        {
            //foreach (Run run in paragraph.Inlines.Where(it => it is Run))
            //{

            //}
        }

        private void FindLinksInDocument(BlockCollection blocks)
        {
            foreach (var block in blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (Hyperlink link in paragraph.Inlines.Where(it => it is Hyperlink))
                    {
                        link.RequestNavigate += new RequestNavigateEventHandler(LinkRequestNavigate);
                    }
                }
                else if (block is List list)
                {
                    foreach (ListItem listItem in list.ListItems)
                    {
                        FindLinksInDocument(listItem.Blocks);
                    }
                }
            }
        }

        private bool StyleCanExecute(object obj) =>
            btnH1.IsChecked == false &&
            btnH2.IsChecked == false &&
            btnH3.IsChecked == false &&
            btnQuote.IsChecked == false &&
            btnBlock.IsChecked == false &&
            btnCode.IsChecked == false &&
            rtb.Selection.Start.Paragraph != null;

        private bool HeadersCanExecute(object obj) =>
            btnBullet.IsChecked == false &&
            btnNumbering.IsChecked == false &&
            btnQuote.IsChecked == false &&
            btnBlock.IsChecked == false &&
            btnCode.IsChecked == false;

        private bool ListCanExecute(object obj) =>
            btnH1.IsChecked == false &&
            btnH2.IsChecked == false &&
            btnH3.IsChecked == false &&
            btnQuote.IsChecked == false &&
            btnBlock.IsChecked == false &&
            btnCode.IsChecked == false;

        private bool QuoteCanExecute(object obj) =>
            btnH1.IsChecked == false &&
            btnH2.IsChecked == false &&
            btnH3.IsChecked == false &&
            btnBullet.IsChecked == false &&
            btnNumbering.IsChecked == false &&
            btnBlock.IsChecked == false;

        private bool BlockCanExecute(object obj) =>
            btnH1.IsChecked == false &&
            btnH2.IsChecked == false &&
            btnH3.IsChecked == false &&
            btnBullet.IsChecked == false &&
            btnNumbering.IsChecked == false &&
            btnQuote.IsChecked == false;

        private bool CodeCanExecute(object obj) =>
            btnH1.IsChecked == false &&
            btnH2.IsChecked == false &&
            btnH3.IsChecked == false &&
            btnQuote.IsChecked == false &&
            btnBlock.IsChecked == false &&
            rtb.Selection.Start.Paragraph != null;
    }
}
