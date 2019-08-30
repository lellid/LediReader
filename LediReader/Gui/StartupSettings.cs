using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LediReader.Gui
{
    public class StartupSettings
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public bool IsMaximized { get; set; }

        public bool IsFullScreen { get; set; }

        public StartupSettings()
        {
            IsMaximized = true;
        }


        public StartupSettings(XmlReader tr)
        {
            LoadXml(tr);
        }

        public void SaveXml(XmlWriter tw)
        {
            tw.WriteStartElement("StartupSettings");
            tw.WriteAttributeString("Version", "1");
            {
                tw.WriteElementString("IsFullScreen", XmlConvert.ToString(IsFullScreen));

                tw.WriteElementString("IsMaximized", XmlConvert.ToString(IsMaximized));

                tw.WriteElementString("Left", XmlConvert.ToString(Left));

                tw.WriteElementString("Top", XmlConvert.ToString(Top));

                tw.WriteElementString("Width", XmlConvert.ToString(Width));

                tw.WriteElementString("Height", XmlConvert.ToString(Height));


            }

            tw.WriteEndElement(); // Settings
        }

        public void LoadXml(XmlReader tr)
        {

            var version = tr.GetAttribute("Version");
            tr.ReadStartElement("StartupSettings");


            IsFullScreen = tr.ReadElementContentAsBoolean("IsFullScreen", string.Empty);
            IsMaximized = tr.ReadElementContentAsBoolean("IsMaximized", string.Empty);
            Left = tr.ReadElementContentAsDouble("Left", string.Empty);
            Top = tr.ReadElementContentAsDouble("Top", string.Empty);
            Width = tr.ReadElementContentAsDouble("Width", string.Empty);
            Height = tr.ReadElementContentAsDouble("Height", string.Empty);

            tr.ReadEndElement(); // BookSettings
        }
    }
}
