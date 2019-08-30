using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LediReader.Book
{
    public class BookSettings
    {
        /// <summary>
        /// Gets or sets the file name of the book currently open
        /// </summary>
        /// <value>
        /// The name of the book currently open.
        /// </value>
        public string BookFileName { get; set; }

        public string Bookmark { get; set; }

        public int PageNumber { get; set; } = 1;

        public double Zoom { get; set; } = 100;



        /// <summary>
        /// Gets or sets a value indicating whether the UI should have a white theme (false) or a black theme (true).
        /// </summary>
        /// <value>
        ///   <c>true</c> if UI has a black theme; otherwise, <c>false</c>.
        /// </value>
        public bool BlackTheme { get; set; }

        public double LeftAndRightMargin { get; set; } = 32;




        public BookSettings()
        {
        }

        public BookSettings(XmlReader tr)
        {
            LoadXml(tr);
        }

        public void SaveXml(XmlWriter tw)
        {
            tw.WriteStartElement("BookSettings");
            tw.WriteAttributeString("Version", "1");
            {
                tw.WriteElementString("BookFileName", BookFileName);

                tw.WriteElementString("PageNumber", XmlConvert.ToString(PageNumber));

                tw.WriteElementString("Bookmark", Bookmark);

                tw.WriteElementString("Zoom", XmlConvert.ToString(Zoom));

                tw.WriteElementString("BlackTheme", XmlConvert.ToString(BlackTheme));

                tw.WriteElementString("LeftAndRightMargin", XmlConvert.ToString(LeftAndRightMargin));
            }

            tw.WriteEndElement(); // Settings
        }

        public void LoadXml(XmlReader tr)
        {

            var version = tr.GetAttribute("Version");
            tr.ReadStartElement("BookSettings");


            BookFileName = tr.ReadElementContentAsString("BookFileName", string.Empty);
            PageNumber = tr.ReadElementContentAsInt("PageNumber", string.Empty);
            Bookmark = tr.ReadElementContentAsString("Bookmark", string.Empty);
            Zoom = tr.ReadElementContentAsDouble("Zoom", string.Empty);
            BlackTheme = tr.ReadElementContentAsBoolean("BlackTheme", string.Empty);
            LeftAndRightMargin = tr.ReadElementContentAsDouble("LeftAndRightMargin", string.Empty);

            tr.ReadEndElement(); // BookSettings
        }
    }


}
