// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer.Tei
{
  /// <summary>
  /// Reads a TEI file as can be found in the FreeDict github source directory
  /// of the FREEDICT project (see <see href="https://freedict.org/"/>
  /// and <see href="https://github.com/freedict/fd-dictionaries"/>).
  /// </summary>
  public class TeiReader
  {
    /// <summary>
    /// The full file name of the TEI file.
    /// </summary>
    string _fileName;

    /// <summary>Initializes a new instance of the <see cref="TeiReader"/> class.</summary>
    /// <param name="fileName">Full file name of the TEI file.</param>
    public TeiReader(string fileName)
    {
      _fileName = fileName;
    }

    /// <summary>
    /// Reads from the TEI file given in the constructor of this class and generates a dictionary.
    /// </summary>
    /// <returns>A dictionary of key-value pairs.</returns>
    public Dictionary<string, string> Read()
    {
      var dictionary = new Dictionary<string, string>();

      var keyList = new List<string>();

      using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        var settings = new System.Xml.XmlReaderSettings()
        {
          CloseInput = false,
          IgnoreWhitespace = true,
        };

        using (var xr = System.Xml.XmlReader.Create(stream, settings))
        {
          xr.MoveToContent();

          xr.ReadStartElement(); // TEI

          xr.ReadToNextSibling("text"); // Skip TEI and teiheader

          xr.ReadStartElement(); // text

          xr.ReadStartElement(); // body

          while (xr.Name == "entry")
          {
            xr.ReadStartElement("entry");
            xr.ReadStartElement("form");
            var key = xr.ReadElementContentAsString("orth", xr.NamespaceURI);
            xr.ReadToFollowing("entry");
            keyList.Add(key);
          }
        }

        stream.Seek(0, SeekOrigin.Begin);
        using (var xr = System.Xml.XmlReader.Create(stream, settings))
        {
          xr.MoveToContent();

          xr.ReadStartElement(); // TEI

          xr.ReadToNextSibling("text"); // Skip TEI and teiheader

          xr.ReadStartElement(); // text

          xr.ReadStartElement(); // body

          for (int i = 0; xr.Name == "entry"; ++i)
          {
            string content = xr.ReadOuterXml();

            if (!dictionary.ContainsKey(keyList[i]))
            {
              dictionary.Add(keyList[i], content);
            }
            else
            {
              bool resolved = false;
              var key = keyList[i];
              var presentContent = dictionary[key];
              var altKey = ReevaluateKey(content);

              if (altKey != key && !dictionary.ContainsKey(altKey))
              {
                dictionary.Add(altKey, content);
                resolved = true;
              }
              else
              {
                altKey = ReevaluateKey(presentContent);
                if (altKey != key && !dictionary.ContainsKey(altKey))
                {
                  dictionary[key] = content;
                  dictionary[altKey] = presentContent;
                  resolved = true;
                }
              }

              if (!resolved)
              {
                if (content.Length > presentContent.Length)
                  dictionary[key] = content;
              }
            }

          }
        }
      }
      return dictionary;
    }

    /// <summary>
    /// Reevaluates the key of a TEI entry. This is sometimes neccessary as some TEI files do not have unique keys. The call
    /// evaluates the TEI entry and tries to generate a key from the content.
    /// </summary>
    /// <param name="content">The content of the TEI entry.</param>
    /// <returns>An alternative key derived from the TEI content.</returns>
    private string ReevaluateKey(string content)
    {
      string key;
      string hint = null;

      var settings = new System.Xml.XmlReaderSettings()
      {
        CloseInput = false,
        IgnoreWhitespace = true,

      };

      using (var xr = System.Xml.XmlReader.Create(new StringReader(content)))
      {
        xr.MoveToContent();
        xr.ReadStartElement("entry");
        xr.ReadStartElement("form");
        key = xr.ReadElementContentAsString("orth", xr.NamespaceURI);
        xr.ReadToFollowing("usg");
        if (xr.Name == "usg")
        {
          hint = xr.ReadElementContentAsString("usg", xr.NamespaceURI);
        }
      }

      return hint == null ? key : key + ", " + hint;
    }
  }
}
