using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LediReader.Translation
{
  /// <summary>
  /// The service provider for translation of sentences.
  /// </summary>
  public enum TranslationServiceProvider
  {
    /// <summary>
    /// The google translator.
    /// </summary>
    Google,


    /// <summary>
    /// The DeepL translator.
    /// </summary>
    DeepL,
  }


  public class TranslationSettings
  {
    public string DestinationLanguageThreeLetterISOLanguageName { get; set; } = "eng";

    public TranslationServiceProvider TranslationServiceProvider { get; set; } = TranslationServiceProvider.Google;


    public TranslationSettings()
    {
    }

    public TranslationSettings(XmlReader tr)
    {
      LoadXml(tr);
    }

    public void SaveXml(XmlWriter tw)
    {
      tw.WriteStartElement("TranslationSettings");
      tw.WriteAttributeString("Version", "1");
      {
        tw.WriteElementString("DestinationLanguage", DestinationLanguageThreeLetterISOLanguageName);

        tw.WriteElementString("ServiceProvider", TranslationServiceProvider.ToString());
      }

      tw.WriteEndElement(); // Settings
    }

    public void LoadXml(XmlReader tr)
    {

      var version = tr.GetAttribute("Version");
      tr.ReadStartElement("TranslationSettings");

      DestinationLanguageThreeLetterISOLanguageName = tr.ReadElementContentAsString("DestinationLanguage", string.Empty);
      var serviceProviderString = tr.ReadElementContentAsString("ServiceProvider", string.Empty);

      if (Enum.TryParse<TranslationServiceProvider>(serviceProviderString, out var translationServiceProvider))
        TranslationServiceProvider = translationServiceProvider;
      else
        TranslationServiceProvider = TranslationServiceProvider.Google;

      tr.ReadEndElement(); // BookSettings
    }
  }
}
