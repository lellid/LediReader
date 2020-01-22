// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using VersOne.Epub;

namespace LediReader.Gui
{
  public class MainWindowController : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    public Settings Settings { get; private set; }

    public EpubContent _bookContent;
    private InstanceStorageService _instanceStorageService;

    public MainWindowController()
    {
      _instanceStorageService = new InstanceStorageService();
      _imageProvider = new ImageSource(this);
      Settings = new Settings();
    }

    #region Bindable properties

    private bool _isBookInDarkMode;

    public bool IsBookInDarkMode
    {
      get
      {
        return _isBookInDarkMode;
      }
      set
      {
        if (!(_isBookInDarkMode == value))
        {
          _isBookInDarkMode = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBookInDarkMode)));
        }
      }
    }

    private bool _isGuiInDarkMode;

    public bool IsGuiInDarkMode
    {
      get
      {
        return _isGuiInDarkMode;
      }
      set
      {
        if (!(_isGuiInDarkMode == value))
        {
          _isGuiInDarkMode = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGuiInDarkMode)));
        }
      }
    }

    private bool _isInAudioMode;

    public bool IsInAudioMode
    {
      get
      {
        return _isInAudioMode;
      }
      set
      {
        if (!(_isInAudioMode == value))
        {
          _isInAudioMode = value;
          Settings.BookSettings.IsInAudioMode = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInAudioMode)));
        }
      }
    }

    #endregion

    public void LoadSettings()
    {
      // Load the settings

      var appPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      appPath = System.IO.Path.Combine(appPath, "LediReader");
      var appSettingsFileName = System.IO.Path.Combine(appPath, "Settings.xml");


      using (var str = new FileStream(appSettingsFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        var xmlSettings = new System.Xml.XmlReaderSettings() { CloseInput = true, IgnoreWhitespace = true };
        using (var tr = System.Xml.XmlReader.Create(str, xmlSettings))
        {
          tr.MoveToContent();
          Settings.LoadXml(tr);
        }
      }
    }

    public void UpdateDictionaryFileNames(IEnumerable<string> dictionaryFileNames)
    {
      // Update dictionaries currently in use
      Settings.DictionarySettings.DictionaryFileNames.Clear();
      Settings.DictionarySettings.DictionaryFileNames.AddRange(dictionaryFileNames);
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
            Settings.SaveXml(tw);
          }
        }
      }
      catch (Exception ex)
      {

      }
    }

    public (HtmlToFlowDocument.Dom.FlowDocument Document, Dictionary<string, string> FontDictionary) ReopenEbook()
    {
      if (string.IsNullOrEmpty(Settings.BookSettings.BookFileName))
        return (null, null);

      var document = OpenEbook(Settings.BookSettings.BookFileName);

      return document;
    }

    public (HtmlToFlowDocument.Dom.FlowDocument Document, Dictionary<string, string> FontDictionary) OpenEbook(string fileName)
    {

      var epubBook = EpubReader.ReadBook(fileName);
      _bookContent = epubBook.Content;

      Dictionary<string, EpubTextContentFile> htmlFiles = _bookContent.Html;
      Dictionary<string, EpubTextContentFile> cssFiles = _bookContent.Css;
      var readingOrder = epubBook.ReadingOrder;

      // ----------------- handle fonts ------------------------------
      var fontDictionary = new Dictionary<string, string>(); // Key is the font name, value is the absolute path to the font file
      var fontPath = Path.Combine(_instanceStorageService.InstanceStoragePath, "Fonts");
      Directory.CreateDirectory(fontPath);

      foreach (var entry in _bookContent.Fonts)
      {
        var fontName = entry.Key;
        var bytes = entry.Value;
        var fontFileName = Path.GetFileName(entry.Value.FileName);
        fontFileName = Path.Combine(fontPath, fontFileName);
        using (var stream = new FileStream(fontFileName, FileMode.Create, FileAccess.Write, FileShare.None))
        {
          var byteArray = bytes.Content;
          stream.Write(byteArray, 0, byteArray.Length);
        }
        fontDictionary.Add(fontName, fontFileName);
      }

      // -------------------------------------------------------------

      string GetStyleSheet(string name, string htmlFileNameReferencedFrom)
      {
        EpubTextContentFile cssFile;
        // calculate absolute name with reference to htmlFileNameReferencedFrom
        var absoluteName = HtmlToFlowDocument.CssStylesheets.GetAbsoluteFileNameForFileRelativeToHtmlFile(name, htmlFileNameReferencedFrom);
        if (cssFiles.TryGetValue(absoluteName, out cssFile))
          return cssFile.Content;

        // if this could not resolve the name, then try to go to parent directories
        while (htmlFileNameReferencedFrom.Contains("/"))
        {
          var idx = htmlFileNameReferencedFrom.LastIndexOf("/");
          htmlFileNameReferencedFrom = htmlFileNameReferencedFrom.Substring(0, idx - 1);
          absoluteName = HtmlToFlowDocument.CssStylesheets.GetAbsoluteFileNameForFileRelativeToHtmlFile(name, htmlFileNameReferencedFrom);
          if (cssFiles.TryGetValue(absoluteName, out cssFile))
            return cssFile.Content;
        }

        // if this was not successful, then try it with the name alone
        if (cssFiles.TryGetValue(name, out cssFile))
          return cssFile.Content;

        return null;
        // throw new ArgumentException($"CssFile {name} was not found!", nameof(name));
      }

      // Entire HTML content of the book
      var converter = new HtmlToFlowDocument.Converter() { AttachSourceAsTags = true };
      var flowDocument = new HtmlToFlowDocument.Dom.FlowDocument();
      foreach (EpubTextContentFile htmlFile in readingOrder)
      {
        string htmlContent = htmlFile.Content;
        var textElement = converter.ConvertXHtml(htmlContent, false, GetStyleSheet, htmlFile.FileName); // create sections
        flowDocument.AppendChild(textElement); // and add them to the flow document
      }
      Settings.BookSettings.BookFileName = fileName;
      return (flowDocument, fontDictionary);
    }

    #region Image provider
    private ImageSource _imageProvider;


    public ImageSource ImageProvider
    {
      get
      {
        return _imageProvider;
      }
    }



    public class ImageSource
    {
      private MainWindowController _contentManager;

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
