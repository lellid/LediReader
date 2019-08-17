using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LediReader
{
    public class Settings
    {
        /// <summary>
        /// Gets or sets the file name of the book currently open
        /// </summary>
        /// <value>
        /// The name of the book currently open.
        /// </value>
        public string BookFileName { get; set; }

        public string Bookmark { get; set; }

        /// <summary>
        /// Gets the list of file names of the dictionaries to open
        /// </summary>
        /// <value>
        /// The file names of the dictionaries.
        /// </value>
        public List<string> DictionaryFileNames { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the UI should have a white theme (false) or a black theme (true).
        /// </summary>
        /// <value>
        ///   <c>true</c> if UI has a black theme; otherwise, <c>false</c>.
        /// </value>
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
                tw.WriteElementString("BookFileName", BookFileName);

                tw.WriteElementString("Bookmark", Bookmark);

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

            BookFileName = tr.ReadElementContentAsString("BookFileName", string.Empty);
            Bookmark = tr.ReadElementContentAsString("Bookmark", string.Empty);

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
