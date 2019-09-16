using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LediReader.Speech
{
    public class SpeechSettings
    {
        public string SpeechCulture { get; set; }

        public string SpeechVoice { get; set; }

        public int SpeechRate { get; set; } = 0;

        public int SpeechVolume { get; set; } = 100;


        public bool IsEmphasisEnabled { get; set; }

        /// <summary>
        /// Gets or sets the working background color for the dark mode (RGBA format).
        /// </summary>
        /// <value>
        /// The working background color dark mode (RGBA format).
        /// </value>
        public uint WorkingBackgroundColorDarkMode { get; set; } = 0x202020FF;

        /// <summary>
        /// Gets or sets the working background color for light mode (RGBA format).
        /// </summary>
        /// <value>
        /// The working background color light mode (RGBA format).
        /// </value>
        public uint WorkingBackgroundColorLightMode { get; set; } = 0xEFEFEFFF;

        public SpeechSettings()
        {
        }

        public SpeechSettings(XmlReader tr)
        {
            LoadXml(tr);
        }

        public void SaveXml(XmlWriter tw)
        {
            tw.WriteStartElement("SpeechSettings");
            tw.WriteAttributeString("Version", "1");
            {
                tw.WriteElementString("SpeechCulture", SpeechCulture);
                tw.WriteElementString("SpeechVoice", SpeechVoice);
                tw.WriteElementString("SpeechRate", XmlConvert.ToString(SpeechRate));
                tw.WriteElementString("SpeechVolume", XmlConvert.ToString(SpeechVolume));
                tw.WriteElementString("IsEmphasisEnabled", XmlConvert.ToString(IsEmphasisEnabled));
                tw.WriteElementString("WorkingBackgroundBlackTheme", XmlConvert.ToString(WorkingBackgroundColorDarkMode));
                tw.WriteElementString("WorkingBackgroundLightTheme", XmlConvert.ToString(WorkingBackgroundColorLightMode));
            }

            tw.WriteEndElement(); // SpeechSettings
        }

        public void LoadXml(XmlReader tr)
        {
            var version = tr.GetAttribute("Version");
            tr.ReadStartElement("SpeechSettings");

            SpeechCulture = tr.ReadElementContentAsString("SpeechCulture", string.Empty);
            SpeechVoice = tr.ReadElementContentAsString("SpeechVoice", string.Empty);
            SpeechRate = tr.ReadElementContentAsInt("SpeechRate", string.Empty);
            SpeechVolume = tr.ReadElementContentAsInt("SpeechVolume", string.Empty);
            IsEmphasisEnabled = tr.ReadElementContentAsBoolean("IsEmphasisEnabled", string.Empty);
            WorkingBackgroundColorDarkMode = XmlConvert.ToUInt32(tr.ReadElementContentAsString("WorkingBackgroundBlackTheme", string.Empty));
            WorkingBackgroundColorLightMode = XmlConvert.ToUInt32(tr.ReadElementContentAsString("WorkingBackgroundLightTheme", string.Empty));
        }
    }

}
