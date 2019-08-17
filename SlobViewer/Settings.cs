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

    public bool BlackTheme { get; set; }

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
      tw.WriteStartElement("Settings");
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

        tw.WriteElementString("BlackTheme", XmlConvert.ToString(BlackTheme));
      }

      tw.WriteEndElement(); // Settings
    }

    public void LoadXml(XmlReader tr)
    {
      DictionaryFileNames = DictionaryFileNames ?? new List<string>();
      DictionaryFileNames.Clear();

      var version = tr.GetAttribute("Version");
      tr.ReadStartElement("Settings");
      var dictCount = XmlConvert.ToInt32(tr.GetAttribute("Count"));
      tr.ReadStartElement("Dictionaries");

      for (int i = 0; i < dictCount; ++i)
      {
        var absolutePath = tr.GetAttribute("AbsolutePath");
        var relativePath = tr.GetAttribute("RelativePath");
        tr.ReadStartElement("Dictionary");

        var resolvedPath = PathResolver.ResolvePathRelativeToEntryAssembly(absolutePath, relativePath);
        if (!string.IsNullOrEmpty(resolvedPath))
          DictionaryFileNames.Add(resolvedPath);
      }
      tr.ReadEndElement(); // Dictionaries

      BlackTheme = tr.ReadElementContentAsBoolean("BlackTheme", string.Empty);

    }
  }
}
