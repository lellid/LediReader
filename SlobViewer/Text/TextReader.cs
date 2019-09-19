// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer.Text
{
  /// <summary>
  /// Reads a text file as can be found at the TU Chemnitz (see <see href="http://dict.tu-chemnitz.de/"/>)

  /// </summary>
  public class TextReader
  {
    /// <summary>
    /// The full file name of the text file.
    /// </summary>
    string _fileName;

    static readonly char[] _partSeparator = new char[] { '|' };
    static readonly char[] _subPartSeparator = new char[] { ';' };

    /// <summary>Initializes a new instance of the <see cref="TextReader"/> class.</summary>
    /// <param name="fileName">Full file name of the text file.</param>
    public TextReader(string fileName)
    {
      _fileName = fileName;
    }

    /// <summary>
    /// Reads from the text file given in the constructor of this class and generates a dictionary.
    /// </summary>
    /// <returns>A dictionary of key-value pairs.</returns>
    public Dictionary<string, string> Read()
    {
      var dictionary = new Dictionary<string, string>();

      var keyList = new List<string>();

      using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
      {

        string line;
        using (var xr = new StreamReader(stream))
        {
          while (null != (line = xr.ReadLine()))
          {
            if (line.Length == 0 || line[0] == '#')
              continue;

            // a line is separated by two colons in the part of the one language and the part with the other language
            // inside every part there may be additional separations by |   (hopefully the same number in both language parts)

            var idx = line.IndexOf("::");

            if (idx < 0)
              continue;

            var part1 = line.Substring(0, idx);
            var part2 = line.Substring(idx + 2);

            var parts1 = part1.Split(_partSeparator, StringSplitOptions.None);
            var parts2 = part2.Split(_partSeparator, StringSplitOptions.None);



            for (int i = 0; i < parts2.Length; ++i)
            {
              var keyt = parts2[i].Trim();
              var value = parts1[i].Trim();

              var keyParts = keyt.Split(_subPartSeparator, StringSplitOptions.RemoveEmptyEntries);

              foreach (var keya in keyParts)
              {
                var key = keya.Trim();

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                  if (dictionary.TryGetValue(key, out var existingValue))
                  {
                    existingValue += "\r\n" + value;
                    dictionary[key] = existingValue;
                  }
                  else
                  {
                    dictionary.Add(key, value);
                  }
                }
              }


            }
          }
        }

      }
      return dictionary;
    }


  }
}
