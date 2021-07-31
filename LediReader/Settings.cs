// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LediReader
{
  public class Settings
  {
    public Gui.StartupSettings StartupSettings { get; set; }

    public Book.BookSettings BookSettings { get; set; }

    public SlobViewer.Settings DictionarySettings { get; set; }

    public Speech.SpeechSettings SpeechSettings { get; set; }

    /// <summary>
    /// Gets or sets the settings for translation of entire sentences and paragraphs).
    /// </summary>
    /// <value>
    /// The translation settings.
    /// </value>
    public Translation.TranslationSettings TranslationSettings { get; set; }



    public Settings()
    {
      StartupSettings = new Gui.StartupSettings();
      BookSettings = new Book.BookSettings();
      DictionarySettings = new SlobViewer.Settings();
      SpeechSettings = new Speech.SpeechSettings();
      TranslationSettings = new Translation.TranslationSettings();
    }

    public Settings(XmlReader tr)
    {
      LoadXml(tr);
    }

    public void SaveXml(XmlWriter tw)
    {
      tw.WriteStartElement("Settings");
      tw.WriteAttributeString("Version", "2");

      StartupSettings.SaveXml(tw);
      BookSettings.SaveXml(tw);
      DictionarySettings.SaveXml(tw);
      SpeechSettings.SaveXml(tw);
      TranslationSettings.SaveXml(tw); // included in Version>=2

      tw.WriteEndElement(); // Settings
    }

    public void LoadXml(XmlReader tr)
    {

      var version = XmlConvert.ToInt32(tr.GetAttribute("Version"));
      tr.ReadStartElement("Settings");

      StartupSettings = new Gui.StartupSettings(tr);
      BookSettings = new Book.BookSettings(tr);
      DictionarySettings = new SlobViewer.Settings(tr);
      SpeechSettings = new Speech.SpeechSettings(tr);

      // Version 2
      if (version > 1)
        TranslationSettings = new Translation.TranslationSettings(tr);

      tr.ReadEndElement(); // Settings

    }
  }

}
