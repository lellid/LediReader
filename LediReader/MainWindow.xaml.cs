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

namespace LediReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowController _controller;

        Speech _speech;

        public MainWindow()
        {
            _controller = new MainWindowController();
            InitializeComponent();
            this.DataContext = _controller;
            _controller.LoadSettings(this._guiDictionary.Controller.LoadDictionary);
            _speech = new Speech();
            Loaded += EhLoaded;
        }

        private void EhLoaded(object sender, RoutedEventArgs e)
        {
            ShowFlowDocument(_controller.ReopenEbook());
            SlobViewer.GuiActions.UpdateUnloadSubmenus(_guiDictionary.Controller, _guiUnloadMenuItem);
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

        }

        protected override void OnClosed(EventArgs e)
        {
            // Update file paths
            _controller.UpdateDictionaryFileNames(_guiDictionary.Controller.Dictionaries.Select(x => x.FileName));
            _controller.SaveSettings();
            base.OnClosed(e);
        }



        private void EhMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _speech.StopSpeech();
            var document = _guiViewer.Document;

            // var screenPoint = PointToScreen(e.GetPosition(this));
            // TextPointer pointer = DocumentExtensions.ScreenPointToTextPointer((FlowDocument)document, screenPoint);


            var selection = _guiViewer.Selection;
            if (!selection.IsEmpty)
            {
                ShowDictionaryPage(selection.Text);
            }


        }

        private void EhStartSpeech(object sender, RoutedEventArgs e)
        {
            if (!_guiViewer.Selection.IsEmpty && _guiViewer.Selection.Start.Parent is TextElement te)
                _speech.StartSpeech(te);
        }

        private void EhStopSpeech(object sender, RoutedEventArgs e)
        {
            _speech.StopSpeech();
        }

        private void ShowDictionaryPage(string phrase)
        {
            _guiDictionary.Visibility = Visibility.Visible;
            _guiDictionary.Controller.ShowContentForUntrimmedKey(phrase);
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

        // https://stackoverflow.com/questions/2981884/how-can-i-get-a-textpointer-from-a-mouse-click-in-a-flowdocument
    }
}
