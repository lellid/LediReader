// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using LediReader.Speech;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace LediReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowController _controller;
        SpeechWorker _speech;

        /// <summary>
        /// The last text element that is somehow marked by the user. If the user turns a page, this element is set to null, in order to ensure that 
        /// this element is always on a page visible to the user.
        /// </summary>
        TextElement _lastTextElementConsidered;

        public MainWindow()
        {
            _controller = new MainWindowController();
            try
            {
                _controller.LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load settings\r\nThe message is:\r\n{ex.Message}");
            }

            var ss = _controller.Settings.StartupSettings;

            if (ss.IsFullScreen || ss.IsMaximized)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                this.Left = ss.Left;
                this.Top = ss.Top;
                this.Width = ss.Width;
                this.Height = ss.Height;
            }

            InitializeComponent();
            this.DataContext = _controller;
            Loaded += EhLoaded;
        }

        private void EhLoaded(object sender, RoutedEventArgs e)
        {
            _speech = new SpeechWorker();
            _speech.ApplySettings(_controller.Settings.SpeechSettings);
            _speech.SpeechCompleted += EhSpeechCompleted;

            _guiDictionary.Controller.LoadDictionariesUsingSettings(_controller.Settings.DictionarySettings);

            var document = _controller.ReopenEbook();
            _guiViewer.Zoom = _controller.Settings.BookSettings.Zoom;
            ShowFlowDocument(document);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);

            bool navigated = false;
            if (_controller.Settings.BookSettings.Bookmark != null)
            {
                var flowDocument = (FlowDocument)_guiViewer.Document;
                var textElement = HtmlToFlowDocument.Rendering.WpfHelper.GetTextElementFromBookmark(flowDocument, _controller.Settings.BookSettings.Bookmark);
                if (null != textElement)
                {
                    textElement.BringIntoView();
                    navigated = true;
                }
            }

            if (!navigated)
            {
                if (_guiViewer.CanGoToPage(_controller.Settings.BookSettings.PageNumber))
                {
                    _guiViewer.GoToPage(_controller.Settings.BookSettings.PageNumber);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Update file paths
            _controller.UpdateDictionaryFileNames(_guiDictionary.Controller.Dictionaries.Select(x => x.FileName));

            if (_speech.IsSpeechSynthesizingActive)
            {
                _speech.StopSpeech();

                for (int i = 0; i < 100 && _speech.IsSpeechSynthesizingActive; ++i)
                {
                    System.Threading.Thread.Sleep(100);
                }

                // save the last word spoken

                _lastTextElementConsidered = _speech.LastSpokenElement;
            }

            if (null == _lastTextElementConsidered)
            {
                _lastTextElementConsidered = GetTextElementAtStartOfCurrentPage();
            }

            if (null != _lastTextElementConsidered)
            {
                var bookmark = HtmlToFlowDocument.Rendering.WpfHelper.GetBookmarkFromTextElement(_lastTextElementConsidered);
                _controller.Settings.BookSettings.Bookmark = bookmark;
            }


            // save the current page
            _controller.Settings.BookSettings.PageNumber = _guiViewer.MasterPageNumber;
            _controller.Settings.BookSettings.Zoom = _guiViewer.Zoom;
            _speech.GetSettings(_controller.Settings.SpeechSettings);

            // save windows state
            {
                var ss = _controller.Settings.StartupSettings;
                ss.Left = this.Left;
                ss.Top = this.Top;
                ss.Width = this.ActualWidth;
                ss.Height = this.ActualHeight;
                ss.IsMaximized = this.WindowState == WindowState.Maximized;
                ss.IsFullScreen = false;
            }

            _controller.SaveSettings();
            base.OnClosed(e);
        }


        private void EhOpenBook(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Epub files|*.epub" +
                            "|All Files|*.*",
                Multiselect = false,
                Title = "Open Ebook (Epub format)"
            };

            if (true == dlg.ShowDialog(this))
            {
                var flowDocument = _controller.OpenEbook(dlg.FileName);
                ShowFlowDocument(flowDocument);
            }
        }

        private void ShowFlowDocument(HtmlToFlowDocument.Dom.FlowDocument flowDocument)
        {
            if (null != flowDocument)
            {
                var renderer = new HtmlToFlowDocument.Rendering.WpfRenderer() { InvertColors = false, AttachDomAsTags = true };
                var flowDocumentE = renderer.Render(flowDocument);

                flowDocumentE.IsColumnWidthFlexible = false;
                flowDocumentE.ColumnWidth = _guiViewer.ActualWidth;
                _guiViewer.Document = flowDocumentE;
            }
            else
            {
                _guiViewer.Document = null;
            }
        }

        private void EhSettings(object sender, RoutedEventArgs e)
        {
            var controller = new Gui.SpeechSettingsController() { Synthesizer = _speech.Synthesizer };
            var control = new Gui.SpeechSettingsControl(controller);


            var window = new Window();
            window.Content = control;
            window.Show();
        }






        private void EhMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _speech.StopSpeech();
            var document = _guiViewer.Document;
            var selection = _guiViewer.Selection;
            if (!selection.IsEmpty)
            {
                ShowDictionaryPage(selection.Text);
            }
        }

        private void EhStartSpeech(object sender, RoutedEventArgs e)
        {
            if (!_guiViewer.Selection.IsEmpty && _guiViewer.Selection.Start.Parent is TextElement te1)
            {
                _guiViewer.Selection.Select(_guiViewer.Selection.Start, _guiViewer.Selection.Start);
                _speech.StartSpeech(te1);
            }
            else if (null != _lastTextElementConsidered)
            {
                _speech.StartSpeech(_lastTextElementConsidered);
            }
            else if (_guiViewer.Selection.IsEmpty)
            {
                var te2 = GetTextElementAtStartOfCurrentPage();
                if (null != te2)
                {
                    _speech.StartSpeech(te2);
                }
            }
        }

        private TextElement GetTextElementAtStartOfCurrentPage()
        {
            var screenPoint = _guiViewer.PointToScreen(new Point(0, 0));
            TextPointer pointer = DocumentExtensions.ScreenPointToTextPointer((FlowDocument)_guiViewer.Document, screenPoint);
            if (null != pointer && pointer.Parent is TextElement te)
                return te;
            else if (null != pointer && pointer.Parent is FlowDocument doc)
                return doc.Blocks.FirstBlock;
            else
                return null;
        }

        private void EhSpeechCompleted(TextElement lastSpokenElement)
        {
            if (null != lastSpokenElement)
            {
                _lastTextElementConsidered = lastSpokenElement;
                _guiViewer.Selection.Select(lastSpokenElement.ContentStart, lastSpokenElement.ContentEnd);
            }
        }

        private void EhStopSpeech(object sender, RoutedEventArgs e)
        {
            _speech.StopSpeech();
        }

        private void ShowDictionaryPage(string phrase)
        {
            if (_guiDictionary.Controller.Dictionaries.Count > 0)
            {
                _guiDictionary.Visibility = Visibility.Visible;
                _guiDictionary.Controller.ShowContentForUntrimmedKey(phrase);
            }
        }

        private void EhImportTeiFile(object sender, RoutedEventArgs e)
        {
            SlobViewer.GuiActions.ImportTeiFile(_guiDictionary.Controller, this);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
        }

        private void EhImportTUChemnitzFile(object sender, RoutedEventArgs e)
        {
            SlobViewer.GuiActions.ImportTUChemnitzFile(_guiDictionary.Controller, this);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
        }

        private void EhOpenSlobFile(object sender, RoutedEventArgs e)
        {
            SlobViewer.GuiActions.OpenSlobFile(_guiDictionary.Controller, this);
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
        }

        private void EhGotoPage(object sender, RoutedEventArgs e)
        {
            _speech.StopSpeech();

            var control = new Gui.GoToPageControl();
            control.Controller.MaxPageNumber = _guiViewer.PageCount;
            control.Controller.PageNumber = _guiViewer.MasterPageNumber;

            var dlg = new Gui.DialogShellViewWpf(control);

            dlg.ButtonApplyPressed += () => _guiViewer.GoToPage(control.Controller.PageNumber);

            if (dlg.ShowDialog() == true)
            {
                _guiViewer.GoToPage(control.Controller.PageNumber);
                _lastTextElementConsidered = null;
            }
        }

        private void GotoNextPage()
        {
            _guiViewer.NextPage();
            _lastTextElementConsidered = null;
        }

        private void GotoPreviousPage()
        {
            _guiViewer.PreviousPage();
            _lastTextElementConsidered = null;
        }

        private void EhMainWindowMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void EhPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (object.ReferenceEquals(sender, _guiViewerBackground))
            {
                var pos = e.GetPosition(_guiViewerBackground);
                if (pos.X <= _guiViewer.Margin.Left)
                {
                    GotoPreviousPage();
                    e.Handled = true;
                }
                else if (pos.X > _guiViewerBackground.ActualWidth - _guiViewer.Margin.Right)
                {
                    GotoNextPage();
                    e.Handled = true;
                }
            }
        }



        // https://stackoverflow.com/questions/2981884/how-can-i-get-a-textpointer-from-a-mouse-click-in-a-flowdocument
    }
}
