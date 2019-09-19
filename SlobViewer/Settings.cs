// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SlobViewer
{
  public class Settings
  {
    public List<string> DictionaryFileNames { get; private set; }

    public bool IsInDarkMode { get; set; }

    public Settings()
    {
      DictionaryFileNames = new List<string>();
    }

    public Settings(XmlReader tr)
    {
      LoadXml(tr);
    }

    public void SaveXml(XmlWriter tw)
    {
      tw.WriteStartElement("DictionarySettings");
      tw.WriteAttributeString("Version", "1");

      {
        tw.WriteStartElement("Dictionaries");
        tw.WriteAttributeString("Count", XmlConvert.ToString(DictionaryFileNames.Count));
        {
          foreach (var fileName in DictionaryFileNames)
          {
            tw.WriteStartElement("Dictionary");
            tw.WriteAttributeString("AbsolutePath", fileName);
            tw.WriteAttributeString("RelativePath", PathResolver.GetPathRelativeToEntryAssembly(fileName));
            tw.WriteEndElement();
          }
        }
        tw.WriteEndElement();//Dictionaries

        tw.WriteElementString("BlackTheme", XmlConvert.ToString(IsInDarkMode));
      }

      tw.WriteEndElement(); // DictionarySettings
    }

    public void LoadXml(XmlReader tr)
    {
      DictionaryFileNames = DictionaryFileNames ?? new List<string>();
      DictionaryFileNames.Clear();

      var version = tr.GetAttribute("Version");
      tr.ReadStartElement("DictionarySettings");
      {
        var dictCount = XmlConvert.ToInt32(tr.GetAttribute("Count"));
        tr.ReadStartElement("Dictionaries");
        {
          for (int i = 0; i < dictCount; ++i)
          {
            var absolutePath = tr.GetAttribute("AbsolutePath");
            var relativePath = tr.GetAttribute("RelativePath");
            tr.ReadStartElement("Dictionary"); // dictionary is empty - so no ReadEndElement
            var resolvedPath = PathResolver.ResolvePathRelativeToEntryAssembly(absolutePath, relativePath);
            if (!string.IsNullOrEmpty(resolvedPath))
              DictionaryFileNames.Add(resolvedPath);
          }
        }
        if (dictCount > 0)
          tr.ReadEndElement(); // Dictionaries

        IsInDarkMode = tr.ReadElementContentAsBoolean("BlackTheme", string.Empty);
      }
      tr.ReadEndElement(); // DictionarySettings

    }
  }
}
