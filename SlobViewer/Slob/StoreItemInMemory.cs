// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Text;
using SlobViewer.Common;

namespace SlobViewer.Slob
{
  /// <summary>
  /// Part of a <see cref="SlobDictionaryInMemory"/>. A <see cref="StoreItemInMemory"/> holds multiple dictionary values. After reading the SLOB file,
  /// the content is stored in compressed form. If the contents is accessed, it is decompressed.
  /// </summary>
  public class StoreItemInMemory : StoreItemBase
  {

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreItemInMemory"/> class.
    /// </summary>
    /// <param name="contentIds">The array of content IDs.</param>
    /// <param name="compressedContent">Compressed content as retrieved from the SLOB file.</param>
    public StoreItemInMemory(byte[] contentIds, byte[] compressedContent)
    {
      _contentIds = contentIds;
      _compressedContent = compressedContent;
    }

    /// <summary>
    /// Get the content item with index <paramref name="idx"/>.
    /// </summary>
    /// <param name="idx">The index.</param>
    /// <returns>A tuple, consisting of the content and content id at index <paramref name="idx"/>.</returns>
    public (string Content, int ContentId) GetAt(int idx, Encoding encoding, string compression, byte[] buffer)
    {
      if (null != _compressedContent)
      {
        DecompressContent(encoding, compression, buffer);

        _compressedContent = null;
      }

      return (_content[idx], _contentIds[idx]);
    }


  }
}
