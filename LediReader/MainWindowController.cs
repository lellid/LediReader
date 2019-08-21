using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VersOne.Epub;

namespace LediReader
{
    public class MainWindowController
    {
        Settings _settings;

        public EpubContent _bookContent;

        public MainWindowController()
        {
            _imageProvider = new ImageSource(this);
            _settings = new Settings();
        }

        public void LoadSettings(Action<string, bool> LoadDictionary)
        {
            // Load the settings

            var appPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            appPath = System.IO.Path.Combine(appPath, "LediReader");
            var appSettingsFileName = System.IO.Path.Combine(appPath, "Settings.xml");

            try
            {
                using (var str = new FileStream(appSettingsFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var xmlSettings = new System.Xml.XmlReaderSettings() { CloseInput = true, IgnoreWhitespace = true };
                    using (var tr = System.Xml.XmlReader.Create(str, xmlSettings))
                    {
                        tr.MoveToContent();
                        _settings.LoadXml(tr);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            if (_settings.DictionaryFileNames.Count >= 1)
            {
                for (int i = _settings.DictionaryFileNames.Count - 1; i >= 0; --i)
                    LoadDictionary(_settings.DictionaryFileNames[i], i == 0);
            }
        }

        public void UpdateDictionaryFileNames(IEnumerable<string> dictionaryFileNames)
        {
            // Update dictionaries currently in use
            _settings.DictionaryFileNames.Clear();
            _settings.DictionaryFileNames.AddRange(dictionaryFileNames);
        }

        public void SaveSettings()
        {



            // save the settings file
            var appPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            appPath = System.IO.Path.Combine(appPath, "LediReader");
            var appSettingsFileName = System.IO.Path.Combine(appPath, "Settings.xml");

            try
            {
                System.IO.Directory.CreateDirectory(appPath);
                using (var str = new FileStream(appSettingsFileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    var xmlSettings = new System.Xml.XmlWriterSettings() { CloseOutput = true, Indent = true };
                    using (var tw = System.Xml.XmlWriter.Create(str, xmlSettings))
                    {
                        _settings.SaveXml(tw);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public HtmlToFlowDocument.Dom.FlowDocument ReopenEbook()
        {
            if (!string.IsNullOrEmpty(_settings.BookFileName))
                return OpenEbook(_settings.BookFileName);
            else
                return null;
        }

        public HtmlToFlowDocument.Dom.FlowDocument OpenEbook(string fileName)
        {

            var epubBook = EpubReader.ReadBook(fileName);
            _bookContent = epubBook.Content;

            Dictionary<string, EpubTextContentFile> htmlFiles = _bookContent.Html;
            Dictionary<string, EpubTextContentFile> cssFiles = _bookContent.Css;

            string GetStyleSheet(string name, string htmlFileNameReferencedFrom)
            {
                var absoluteName = HtmlToFlowDocument.CssStylesheet.GetAbsoluteCssFileName(name, htmlFileNameReferencedFrom);
                if (cssFiles.TryGetValue(absoluteName, out var cssFile1))
                    return cssFile1.Content;

                if (cssFiles.TryGetValue(name, out var cssFile2))
                    return cssFile2.Content;

                throw new ArgumentException($"CssFile {name} was not found!", nameof(name));
            }

            // Entire HTML content of the book
            var converter = new HtmlToFlowDocument.Converter() { AttachSourceAsTags = true };
            var flowDocument = new HtmlToFlowDocument.Dom.FlowDocument();
            foreach (EpubTextContentFile htmlFile in htmlFiles.Values)
            {
                string htmlContent = htmlFile.Content;
                var textElement = converter.Convert(htmlContent, false, GetStyleSheet, htmlFile.FileName); // create sections
                flowDocument.AppendChild(textElement); // and add them to the flow document
            }
            _settings.BookFileName = fileName;
            return flowDocument;
        }

        #region Image provider
        ImageSource _imageProvider;
        public ImageSource ImageProvider
        {
            get
            {
                return _imageProvider;
            }
        }



        public class ImageSource
        {
            MainWindowController _contentManager;

            public ImageSource(MainWindowController contentManager)
            {
                _contentManager = contentManager;
            }

            public object this[string relativeFileName]
            {
                get
                {
                    if (_contentManager._bookContent.Images.TryGetValue(relativeFileName, out var contentFile))
                    {
                        using (var stream = new MemoryStream(contentFile.Content))
                        {
                            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            return bitmap;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        #endregion ImageProvider
    }
}
