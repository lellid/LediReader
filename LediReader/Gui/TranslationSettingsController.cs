using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LediReader.Translation;

namespace LediReader.Gui
{
  class TranslationSettingsController : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public List<CultureInfo> DestinationLanguages { get; } = new List<CultureInfo>();


    CultureInfo _selectedDestinationLanguage;
    public CultureInfo SelectedDestinationLanguage {
      get => _selectedDestinationLanguage;
      set { if (!(_selectedDestinationLanguage == value)) { _selectedDestinationLanguage = value; OnPropertyChanged(nameof(SelectedDestinationLanguage)); } }
    }

public Array TranslationServices { get; set; }

    TranslationServiceProvider _selectedProvider;
    public TranslationServiceProvider SelectedTranslationService
    {
      get => _selectedProvider;
      set
      {
        if(!(_selectedProvider == value))
        {
          _selectedProvider = value;
          OnPropertyChanged(nameof(SelectedTranslationService));
        }
      }
    }

    public void Initialize(TranslationSettings translationSettings)
    {
      CultureInfo selectedCulture = null;
      foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
      {
        DestinationLanguages.Add(ci);
        if (ci.ThreeLetterISOLanguageName == translationSettings.DestinationLanguageThreeLetterISOLanguageName)
          selectedCulture = ci;
      }
      SelectedDestinationLanguage = selectedCulture;

      TranslationServices = Enum.GetValues(typeof(TranslationServiceProvider));
      SelectedTranslationService = translationSettings.TranslationServiceProvider;
    }

    public void Apply(TranslationSettings translationSettings)
    {
      translationSettings.DestinationLanguageThreeLetterISOLanguageName = SelectedDestinationLanguage.ThreeLetterISOLanguageName;
      translationSettings.TranslationServiceProvider = SelectedTranslationService;

    }
  }
}
