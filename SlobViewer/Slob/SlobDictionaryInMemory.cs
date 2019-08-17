using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer.Slob
{
  /// <summary>
  /// Represents a dictionary retrieved from a SLOB file.
  /// </summary>
  public class SlobDictionaryInMemory : ISlobDictionary
  {
    public string FileName { get; set; }

    /// <summary>
    /// The encoding of the keys and the content.
    /// </summary>
    System.Text.Encoding _encoding;

    /// <summary>
    /// The compression method of the content.
    /// </summary>
    string _compression;

    /// <summary>
    /// The dictionary of tags of this SLOB file. The tags represent metadata for the SLOB file.
    /// </summary>
    Dictionary<string, string> _tags;

    /// <summary>
    /// The types of content. The <see cref="StoreItemInMemory"/>s contain a table which stores for each content item an index into this list of content types,
    /// so that each content item has a designated content type.
    /// </summary>
    string[] _contentTypes;

    /// <summary>
    /// The list of keys of this dictionary.
    /// </summary>
    Reference[] _references;
    /// <summary>
    /// The store items, i.e. the list of values. Each store item contains multiple values, in order to have a better compression factor of the content.
    /// </summary>
    StoreItemInMemory[] _storeItems;

    byte[] _buffer = new byte[256];

    /// <summary>
    /// Initializes a new instance of the <see cref="SlobDictionaryInMemory"/> class.
    /// </summary>
    /// <param name="encoding">The encoding of the content.</param>
    /// <param name="compression">The compression type of the content.</param>
    /// <param name="tags">The tags (metadata of this dictionary).</param>
    /// <param name="contentTypes">The content types.</param>
    /// <param name="references">The references (keys of the dictionary).</param>
    /// <param name="storeItems">The store items (each store item contains multiple content items).</param>
    public SlobDictionaryInMemory(
        System.Text.Encoding encoding,
        string compression,
        Dictionary<string, string> tags,
        string[] contentTypes,
        Reference[] references,
        StoreItemInMemory[] storeItems)
    {
      _encoding = encoding;
      _compression = compression;
      _tags = tags;
      _contentTypes = contentTypes;
      _references = references;
      _storeItems = storeItems;
    }

    /// <summary>Gets all keys of this dictionary.</summary>
    /// <returns></returns>
    public string[] GetKeys()
    {
      return _references.Select(x => x.Key).ToArray();
    }

    /// <summary>
    /// Gets a number of <paramref name="count"/> keys including the key given in <paramref name="key"/> (if found).
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="count">The number of keys following the given key.</param>
    /// <returns>A array of keys, including the key given (if it is found in the dictionary). The size of the array is equal to or less than <paramref name="count"/>.</returns>
    public string[] GetKeys(string key, int count)
    {
      int i;
      for (i = 0; i < _references.Length; ++i)
      {
        if (0 >= string.Compare(key, _references[i].Key))
          break;
      }

      if (i == _references.Length)
        return null;

      var len = Math.Min(count, _references.Length - i);

      var result = new string[len];

      for (int k = 0; k < len; ++k)
        result[k] = _references[i + k].Key;

      return result;

    }

    /// <summary>
    /// Try to get the content for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key for which the content should be retrieved..</param>
    /// <param name="value">If successfull, contains the retrieved content and the content identification.</param>
    /// <returns>True if the content with the specified key was found; otherwise, false.</returns>
    public bool TryGetValue(string key, out (string Content, string ContentId) value)
    {
      int i;
      for (i = 0; i < _references.Length; ++i)
      {
        if (0 == string.Compare(key, _references[i].Key))
          break;
      }

      if (i == _references.Length)
      {
        value = (null, null);
        return false;
      }

      var blobIndex = _references[i].BinIndex;
      var itemIndex = _references[i].ItemIndex;

      var storeItem = _storeItems[blobIndex];

      var item = storeItem.GetAt(itemIndex, _encoding, _compression, _buffer);

      value = (item.Content, _contentTypes[item.ContentId]);
      return true;
    }

    /// <summary>
    /// Gets the <see cref="System.ValueTuple{System.String, System.String}"/> with the specified key.
    /// </summary>
    /// <value>
    /// The value <see cref="System.ValueTuple{System.String, System.String}"/> for the specified key.
    /// </value>
    /// <param name="key">The key.</param>
    /// <returns>The value <see cref="System.ValueTuple{System.String, System.String}"/> for the specified key.</returns>
    public (string Content, string ContentId) this[string key]
    {
      get
      {
        TryGetValue(key, out var value);
        return value;
      }
    }
  }
}
