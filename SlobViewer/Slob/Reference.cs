// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlobViewer.Slob
{
  /// <summary>
  /// Stores a key of the <see cref="SlobDictionaryInMemory"/> together with the position of where to found the content belonging to that key.
  /// </summary>
  public class Reference
  {
    /// <summary>
    /// Gets the dictionary key.
    /// </summary>
    public string Key { get; }


    public string Fragment { get; }

    /// <summary>
    /// Gets the index into the array of <see cref="StoreItemInMemory"/>s.
    /// </summary>
    public uint BinIndex { get; }

    /// <summary>
    /// Gets the index of the element in the <see cref="StoreItemInMemory"/>.
    /// </summary>
    public ushort ItemIndex { get; }


    /// <summary>
    /// Initializes a new instance of the <see cref="Reference"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="binIndex">Index of the store item to find the content in.</param>
    /// <param name="itemIndex">Index of the element inside the store item.</param>
    /// <param name="fragment">The fragment.</param>
    public Reference(string key, uint binIndex, ushort itemIndex, string fragment)
    {
      Key = key;
      BinIndex = binIndex;
      ItemIndex = itemIndex;
      Fragment = fragment;
    }
  }
}
