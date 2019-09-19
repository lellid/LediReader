// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LediReader
{
  public static class AppThemeSelector
  {
    public static void ApplyTheme(Uri[] dictionaryUris)
    {
      ApplyTheme(Application.Current.Resources, dictionaryUris);
    }

    public static void ApplyTheme(FrameworkElement element, Uri[] dictionaryUris)
    {
      ApplyTheme(element.Resources, dictionaryUris);
    }

    public static void ApplyTheme(ResourceDictionary targetResourceDictionary, Uri[] dictionaryUris)
    {
      try
      {
        var newThemeDictionaries = new HashSet<ThemeResourceDictionary>();
        if (dictionaryUris != null)
        {
          int index = 0;
          foreach (var uri in dictionaryUris)
          {
            var themeDictionary = new ThemeResourceDictionary();
            newThemeDictionaries.Add(themeDictionary);
            themeDictionary.Source = uri;
            // add the new dictionary to the collection of merged dictionaries of the target object
            targetResourceDictionary.MergedDictionaries.Insert(index, themeDictionary);
            ++index;
          }
        }

        // find if the target element already has a theme applied
        var existingDictionaries = targetResourceDictionary.MergedDictionaries.OfType<ThemeResourceDictionary>().ToList();

        // remove the existing dictionaries
        foreach (ThemeResourceDictionary themeDictionary in existingDictionaries)
        {
          if (!newThemeDictionaries.Contains(themeDictionary))
          {
            targetResourceDictionary.MergedDictionaries.Remove(themeDictionary);
          }
        }
      }
      finally { }
    }

  }

  public class ThemeResourceDictionary : ResourceDictionary
  {
  }
}
