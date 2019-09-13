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
        public int SpeechRate { get; set; } = 0;

        public int SpeechVolume { get; set; } = 100;

        public string SpeechVoice { get; set; }

        public bool IsEmphasisEnabled { get; set; }

        public int WorkingBackgroundBlackTheme { get; set; } = 0x444444;

        public int WorkingBackgroundLightTheme { get; set; } = 0xEFEFEF;


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
                tw.WriteElementString("SpeechVoice", SpeechVoice);
                tw.WriteElementString("SpeechRate", XmlConvert.ToString(SpeechRate));
                tw.WriteElementString("SpeechVolume", XmlConvert.ToString(SpeechVolume));
                tw.WriteElementString("IsEmphasisEnabled", XmlConvert.ToString(IsEmphasisEnabled));
                tw.WriteElementString("WorkingBackgroundBlackTheme", XmlConvert.ToString(WorkingBackgroundBlackTheme));
                tw.WriteElementString("WorkingBackgroundLightTheme", XmlConvert.ToString(WorkingBackgroundLightTheme));
            }

            tw.WriteEndElement(); // SpeechSettings
        }

        public void LoadXml(XmlReader tr)
        {
            var version = tr.GetAttribute("Version");
            tr.ReadStartElement("SpeechSettings");

            SpeechVoice = tr.ReadElementContentAsString("SpeechVoice", string.Empty);
            SpeechRate = tr.ReadElementContentAsInt("SpeechRate", string.Empty);
            SpeechVolume = tr.ReadElementContentAsInt("SpeechVolume", string.Empty);
            IsEmphasisEnabled = tr.ReadElementContentAsBoolean("IsEmphasisEnabled", string.Empty);
            WorkingBackgroundBlackTheme = tr.ReadElementContentAsInt("WorkingBackgroundBlackTheme", string.Empty);
            WorkingBackgroundLightTheme = tr.ReadElementContentAsInt("WorkingBackgroundLightTheme", string.Empty);
        }
    }

}
