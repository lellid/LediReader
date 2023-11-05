// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using SlobViewer.Slob;

namespace SlobViewer.Gui
{
  /// <summary>
  /// User controlled actions for SlobViewer.
  /// </summary>
  public static class GuiActions
  {
    /// <summary>
    /// Shows the file open dialog, and then imports a file coming from the FreeDict initiative (see <see href="https://github.com/freedict"/>.
    /// Then a file save dialog is presented to store the imported data in the Slob format.
    /// </summary>
    /// <param name="controller">The dictionary controller.</param>
    /// <param name="mainWindow">The main window.</param>
    public static void ImportTeiFile(DictionaryController controller, Window mainWindow)
    {
      var entryDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      var dir = new System.IO.DirectoryInfo(entryDir + @"\Content\");

      var dlg = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "TEI files|*.tei" +
                      "|All Files|*.*",
        Multiselect = false,
        InitialDirectory = dir.FullName,
        Title = "Import from a TEI file"
      };




      if (true == dlg.ShowDialog(mainWindow))
      {
        var teiReader = new Tei.TeiReader(dlg.FileName);
        var teiDictionary = teiReader.Read();


        var saveDlg = new Microsoft.Win32.SaveFileDialog
        {
          Filter = "SLOB files|*.slob" +
                    "|All Files|*.*",

          InitialDirectory = dir.FullName,

          Title = "Save dictionary as .slob file"
        };

        if (true == saveDlg.ShowDialog(mainWindow))
        {
          var slobWriter = new SlobReaderWriter(saveDlg.FileName);
          slobWriter.Write(teiDictionary, "application/tei+xml");

          controller.LoadDictionary(saveDlg.FileName);
        }
      }
    }


    /// <summary>
    /// Shows the file open dialog, and then imports a plain text file coming from the BEOLINGUS dictionary of TU Chemnitz (see <see href="https://dict.tu-chemnitz.de/doc/faq.de.html"/>.
    /// Then a file save dialog is presented to store the imported data in the Slob format.
    /// </summary>
    /// <param name="controller">The dictionary controller.</param>
    /// <param name="mainWindow">The main window.</param>
    public static void ImportTUChemnitzFile(DictionaryController controller, Window mainWindow)
    {
      var entryDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      var dir = new System.IO.DirectoryInfo(entryDir + @"\Content\");

      var dlg = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "Text files|*.txt" +
                      "|All Files|*.*",
        Multiselect = false,
        InitialDirectory = dir.FullName,
        Title = "Import from a text file"
      };




      if (true == dlg.ShowDialog(mainWindow))
      {
        var teiReader = new Text.TextReader(dlg.FileName);
        var dictionary = teiReader.Read();


        var saveDlg = new Microsoft.Win32.SaveFileDialog
        {
          Filter = "SLOB files|*.slob" +
                    "|All Files|*.*",

          InitialDirectory = dir.FullName,

          Title = "Save dictionary as .slob file"
        };

        if (true == saveDlg.ShowDialog(mainWindow))
        {
          var slobWriter = new SlobReaderWriter(saveDlg.FileName);
          slobWriter.Write(dictionary, "text/plain");

          controller.LoadDictionary(saveDlg.FileName);
        }
      }
    }

    /// <summary>
    /// Shows the file open dialog, and then imports a file coming from the Kaikki initiative (see <see href="https://kaikki.org"/>.
    /// This are .json files.
    /// Then a file save dialog is presented to store the imported data in the Slob format.
    /// </summary>
    /// <param name="controller">The dictionary controller.</param>
    /// <param name="mainWindow">The main window.</param>
    public static void ImportKaikkiFile(DictionaryController controller, Window mainWindow)
    {
      var entryDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      var dir = new System.IO.DirectoryInfo(entryDir + @"\Content\");

      var dlg = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "Json files|*.json" +
                      "|All Files|*.*",
        Multiselect = false,
        InitialDirectory = dir.FullName,
        Title = "Import a Json file from Kaikki.org"
      };

      if (true == dlg.ShowDialog(mainWindow))
      {
        var reader = new Kaikki.KaikkiReader(dlg.FileName);
        var jsonDictionary = reader.Read();
        var domNodes = new Kaikki.DomBuilder().BuildDom(jsonDictionary, destroyDictionary: true);
        jsonDictionary = null; // no longer needed, we need to save memory
        System.GC.Collect();
        var xhtmlConverter = new Kaikki.XHtmlWriter();
        var xhtmlDictionary = new Dictionary<string, string>();
        foreach (var node in domNodes)
        {
          xhtmlDictionary[node.Name] = xhtmlConverter.GetXHtml(node, true);
        }

        var saveDlg = new Microsoft.Win32.SaveFileDialog
        {
          Filter = "SLOB files|*.slob" +
                    "|All Files|*.*",

          InitialDirectory = dir.FullName,

          Title = "Save dictionary as .slob file"
        };

        if (true == saveDlg.ShowDialog(mainWindow))
        {
          var slobWriter = new SlobReaderWriter(saveDlg.FileName);
          slobWriter.Write(xhtmlDictionary, "text/xhtml");

          controller.LoadDictionary(saveDlg.FileName);
        }
      }
    }

    /// <summary>
    /// Shows the file open dialog, and then imports a file in .slob format. Such files can be downloaded from various sources, e.g. from <see href="https://github.com/itkach/slob/wiki/Dictionaries"/>
    /// Then a file save dialog is presented to store the imported data in the Slob format.
    /// </summary>
    /// <param name="controller">The dictionary controller.</param>
    /// <param name="mainWindow">The main window.</param>
    public static void OpenSlobFile(DictionaryController controller, Window mainWindow)
    {
      var entryDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      var dir = new System.IO.DirectoryInfo(entryDir + @"\Content\");

      var dlg = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "SLOB files|*.slob" +
                      "|All Files|*.*",
        Multiselect = false,
        InitialDirectory = dir.FullName,
      };


      if (true == dlg.ShowDialog(mainWindow))
      {
        controller.LoadDictionary(dlg.FileName);
      }
    }

    /// <summary>
    /// Shows the file open dialog, and then imports a file in StarDict format. Such files can be downloaded from various sources.
    /// </summary>
    /// <param name="controller">The dictionary controller.</param>
    /// <param name="mainWindow">The main window.</param>
    public static void OpenStarDictFile(DictionaryController controller, Window mainWindow)
    {
      var entryDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      var dir = new System.IO.DirectoryInfo(entryDir + @"\Content\");

      var dlg = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "StarDict dictionary files|*.ifo" +
                      "|All Files|*.*",
        Multiselect = false,
        InitialDirectory = dir.FullName,
      };

      if (true == dlg.ShowDialog(mainWindow))
      {
        controller.LoadDictionary(dlg.FileName);
      }
    }

    /// <summary>
    /// Updates the unload submenus. This function should be called every time a new dictionary is loaded, because for every dictionary that
    /// is currently loaded, a menu item is created that allows to unload that dictionary.
    /// </summary>
    /// <param name="controller">The dictionary controller.</param>
    /// <param name="guiUnloadMenuItem">The unload submenu item.</param>
    public static void UpdateUnloadSubmenus(DictionaryController controller, MenuItem guiUnloadMenuItem)
    {
      guiUnloadMenuItem.Items.Clear();

      foreach (var dic in controller.Dictionaries)
      {
        var m = new MenuItem() { Header = dic.FileName, Tag = dic.FileName };
        m.Click += (sender, e) => { EhUnloadDictionary(controller, guiUnloadMenuItem, sender); };
        guiUnloadMenuItem.Items.Add(m);
      }
    }

    /// <summary>
    /// Unload a dictionary. The dictionary name is specified by the tag of the menu item <paramref name="guiUnloadMenuItem"/>.
    /// </summary>
    /// <param name="controller">The dictionary controller.</param>
    /// <param name="guiUnloadMenuItem">The sub menu item that contains all unload menu items. After unloading the dictionary, the menu items of this submenu will be updated.</param>
    /// <param name="sender">The menu item that triggered this action. The tag of this menu item must contain the file name of the dictionary file to unload.</param>
    private static void EhUnloadDictionary(DictionaryController controller, MenuItem guiUnloadMenuItem, object sender)
    {
      if (sender is MenuItem m)
      {
        var fileName = (string)m.Tag;

        for (int i = controller.Dictionaries.Count - 1; i >= 0; --i)
        {
          if (controller.Dictionaries[i].FileName == fileName)
          {
            controller.Dictionaries.RemoveAt(i);

            UpdateUnloadSubmenus(controller, guiUnloadMenuItem);
            controller.CollectAndSortKeys();
            break;
          }
        }
      }
    }
  }
}
