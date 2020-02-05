// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlobViewer.Common;

namespace SlobViewer.StarDict
{
  public class StarDictionaryInMemory : IWordDictionary
  {
    private string _fileNameOfIfoFile;
    private FileStream _dictFile;
    private FileStream _idxFile;
    private string _version;
    private int _wordCount;
    private int _idxFileSize;
    private string _sametypesequence;

    private Dictionary<string, (uint Offset, uint Size)> _keyDictionary;


    private Dictionary<string, string> _keyValueDictionary;

    public string FileName { get => _fileNameOfIfoFile; set => _fileNameOfIfoFile = value; }

    private string[] _references;



    public StarDictionaryInMemory()
    {

    }


    public void Open(string fileNameOfIfoFile)
    {
      _fileNameOfIfoFile = fileNameOfIfoFile;

      var baseName = Path.Combine(Path.GetDirectoryName(fileNameOfIfoFile), Path.GetFileNameWithoutExtension(fileNameOfIfoFile));


      if (File.Exists(baseName + ".dict"))
        _dictFile = new FileStream(baseName + ".dict", FileMode.Open, FileAccess.Read, FileShare.Read);
      else if (File.Exists(baseName + ".dict.dz"))
        _dictFile = new FileStream(baseName + ".dict.dz", FileMode.Open, FileAccess.Read, FileShare.Read);


      if (File.Exists(baseName + ".idx"))
        _idxFile = new FileStream(baseName + ".idx", FileMode.Open, FileAccess.Read, FileShare.Read);
      else if (File.Exists(baseName + "idx.gz"))
        _idxFile = new FileStream(baseName + ".idx.gz", FileMode.Open, FileAccess.Read, FileShare.Read);

      if (null == _dictFile)
        throw new InvalidDataException($"No dictionary file found");
      if (null == _idxFile)
        throw new InvalidDataException($"No idx file found");
      ReadIfoFile(fileNameOfIfoFile);
      ReadIdxFile();
      ReadDictFile();
    }

    private void ReadIfoFile(string fileNameOfIfoFile)
    {
      string line;
      using (var srr = new StreamReader(fileNameOfIfoFile))
      {
        while (null != (line = srr.ReadLine()))
        {
          var idx = line.IndexOf("=");
          if (idx < 0)
            continue;

          var key = line.Substring(0, idx).Trim().ToLowerInvariant();
          var val = line.Substring(idx + 1, line.Length - idx - 1).Trim();

          switch (key)
          {
            case "version":
              _version = val;
              break;
            case "wordcount":
              _wordCount = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
              break;
            case "idxfilesize":
              _idxFileSize = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
              break;
            case "sametypesequence":
              _sametypesequence = val;
              break;
          }
        }
      }
    }

    private void ReadIdxFile()
    {
      byte[] buffer;

      if (_idxFile is FileStream fs && fs.Name.ToLowerInvariant().EndsWith(".gz"))
      {

        using (Stream inStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(_idxFile))
        {
          using (MemoryStream outStream = new MemoryStream())
          {
            inStream.CopyTo(outStream);
            buffer = outStream.ToArray();
          }
        }
      }
      else
      {
        buffer = new byte[_idxFile.Length];
        _idxFile.Read(buffer, 0, buffer.Length);
      }

      _idxFile.Close();
      _keyDictionary = ReadIdxBuffer(buffer);
      _references = _keyDictionary.Keys.ToArray();
      Array.Sort(_references);
    }

    private Dictionary<string, (uint Offset, uint Size)> ReadIdxBuffer(byte[] buffer)
    {
      // each buffer entry consists
      // i) of an zero terminated UTF-8 string
      // ii) of the data offset
      // iii) of the data size

      var dict = new Dictionary<string, (uint Offset, uint Size)>();

      var enc = new UTF8Encoding();

      int wordStart = 0;
      while (buffer[wordStart] < 0x20) wordStart++; // Skip chars at the beginning

      for (; wordStart < buffer.Length;)
      {
        int wordEnd;

        wordEnd = wordStart;
        while (buffer[wordEnd] != 0) wordEnd++;
        string key = enc.GetString(buffer, wordStart, wordEnd - wordStart);
        key = key.TrimStart();
        uint offset = BigEndianBitConverter.ToUInt32(buffer, wordEnd + 1);
        uint length = BigEndianBitConverter.ToUInt32(buffer, wordEnd + 5);

        dict[key] = (offset, length);

        wordStart = wordEnd + 9;
      }

      return dict;
    }

    private void ReadDictFile()
    {
      byte[] buffer;

      if (_dictFile is FileStream fs && fs.Name.ToLowerInvariant().EndsWith(".dz"))
      {
        using (Stream inStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(_dictFile))
        {
          using (MemoryStream outStream = new MemoryStream())
          {
            inStream.CopyTo(outStream);
            buffer = outStream.ToArray();
          }
        }
      }
      else
      {
        buffer = new byte[_idxFile.Length];
        _idxFile.Read(buffer, 0, buffer.Length);
      }

      var enc = new UTF8Encoding();

      _keyValueDictionary = new Dictionary<string, string>();

      foreach (var entry in _keyDictionary)
      {
        var s = enc.GetString(buffer, (int)entry.Value.Offset, (int)entry.Value.Size);
        _keyValueDictionary[entry.Key] = s;
      }

      _keyDictionary = null; // not of use anymore
    }

    public string[] GetKeys()
    {
      return _references;
    }

    public string[] GetKeys(string key, int count)
    {
      int i;
      for (i = 0; i < _references.Length; ++i)
      {
        if (0 >= string.Compare(key, _references[i]))
          break;
      }

      if (i == _references.Length)
        return null;

      var len = Math.Min(count, _references.Length - i);

      var result = new string[len];

      for (int k = 0; k < len; ++k)
        result[k] = _references[i + k];

      return result;
    }

    public bool TryGetValue(string key, out (string Content, string ContentId) value)
    {
      if (_keyValueDictionary.TryGetValue(key, out var content))
      {
        value = (content, "text/html");
        return true;
      }
      else
      {
        value = (null, null);
        return false;
      }
    }

    public (string Content, string ContentId) this[string key]
    {
      get
      {
        if (TryGetValue(key, out var value))
          return value;
        else
          return (null, null);
      }
    }
  }
}
