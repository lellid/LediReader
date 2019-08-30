// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using SlobViewer.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SlobViewer.Slob
{
    public class SlobReaderWriter
    {
        private readonly string _fileName;
        private System.IO.FileStream _stream;
        private readonly byte[] _buffer = new byte[65536];

        public const long SupposedMagic = 0x212d31534c4f421f;

        public const ulong SupposedVersionHi = 4862287655031097909UL;
        public const ulong SupposedVersionLo = 11718617693102973101UL;


        /// <summary>
        /// Initializes a new instance of the <see cref="SlobReaderWriter"/> class.
        /// </summary>
        /// <param name="fileName">Full file name of the SLOB file (either to read or to write).</param>
        public SlobReaderWriter(string fileName)
        {
            _fileName = fileName;
        }

        /// <summary>
        /// Reads from a SLOB file, and returns the read dictionary.
        /// </summary>
        /// <returns>The read dictionary.</returns>
        public ISlobDictionary Read()
        {

            // for format details see
            // https://github.com/itkach/slob

            using (_stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ulong magic = BigEndianBitConverter.ToUInt64(_stream, _buffer);
                if (magic != SupposedMagic)
                {
                    throw new InvalidOperationException("Magic number did not match!");
                }

                ulong uuidHi = BigEndianBitConverter.ToUInt64(_stream, _buffer);
                ulong uuidLo = BigEndianBitConverter.ToUInt64(_stream, _buffer);

                /*
                if (uuidHi != SupposedVersionHi || uuidLo != SupposedVersionLo)
                {
                  throw new InvalidOperationException("This version is not supported");
                }
                */
                var encoding = System.Text.UTF8Encoding.UTF8;
                string encodingAsString = BigEndianBitConverter.ReadTinyText(_stream, encoding, _buffer);
                switch (encodingAsString.ToLowerInvariant())
                {
                    case "utf-7":
                        encoding = System.Text.UTF7Encoding.UTF7;
                        break;
                    case "utf-8":
                        encoding = System.Text.UTF8Encoding.UTF8;
                        break;
                    case "utf-32":
                        encoding = System.Text.UTF32Encoding.UTF32;
                        break;
                    case "ascii":
                        encoding = System.Text.ASCIIEncoding.ASCII;
                        break;
                    default:
                        throw new NotImplementedException($"Encoding >>{encodingAsString}<< is not yet implemented!");
                }



                string compression = BigEndianBitConverter.ReadTinyText(_stream, encoding, _buffer);


                // char-sized sequence of tags
                //
                int tagCount = _stream.ReadByte();
                Dictionary<string, string> tagDict = new Dictionary<string, string>();

                for (int i = 0; i < tagCount; ++i)
                {
                    string key = BigEndianBitConverter.ReadTinyText(_stream, encoding, _buffer);
                    string value = BigEndianBitConverter.ReadTinyText(_stream, encoding, _buffer);
                    value = value.TrimEnd('\0');
                    tagDict.Add(key, value);
                }

                // char-sized sequence of content types
                //

                int contentTypeCount = _stream.ReadByte();
                string[] contentTypes = new string[contentTypeCount];
                for (int i = 0; i < contentTypeCount; ++i)
                {
                    string contentType = BigEndianBitConverter.ReadText(_stream, encoding, _buffer);
                    contentTypes[i] = contentType;
                }

                // Blobcount
                uint blobCount = BigEndianBitConverter.ToUInt32(_stream, _buffer);

                // Store offset
                ulong storeOffset = BigEndianBitConverter.ToUInt64(_stream, _buffer);

                // Size
                ulong size = BigEndianBitConverter.ToUInt64(_stream, _buffer);

                // list of long-positioned refs
                uint refCount = BigEndianBitConverter.ToUInt32(_stream, _buffer);
                ulong[] references = new ulong[refCount];
                for (int i = 0; i < refCount; ++i)
                {
                    ulong offset = BigEndianBitConverter.ToUInt64(_stream, _buffer);
                    references[i] = offset;
                }

                // read-in the keywords
                long refOffset = _stream.Position;
                Reference[] refItems = new Reference[refCount];
                for (int i = 0; i < refCount; ++i)
                {
                    _stream.Seek(refOffset + (long)references[i], SeekOrigin.Begin);

                    string key = BigEndianBitConverter.ReadText(_stream, encoding, _buffer);
                    uint binIndex = BigEndianBitConverter.ToUInt32(_stream, _buffer);
                    ushort itemIndex = BigEndianBitConverter.ToUInt16(_stream, _buffer);
                    string fragment = BigEndianBitConverter.ReadTinyText(_stream, encoding, _buffer);
                    refItems[i] = new Reference(key, binIndex, itemIndex, fragment);
                }


                // list of long-positioned store items
                _stream.Seek((long)storeOffset, SeekOrigin.Begin);
                uint storeCount = BigEndianBitConverter.ToUInt32(_stream, _buffer);
                ulong[] storeOffsets = new ulong[storeCount];
                for (int i = 0; i < storeCount; ++i)
                {
                    storeOffsets[i] = BigEndianBitConverter.ToUInt64(_stream, _buffer);
                }

                long storeOffset2 = _stream.Position;

                // Test store offsets
                for (int i = 0; i < storeCount; ++i)
                {
                    if (((long)storeOffsets[i] + storeOffset2) >= _stream.Length)
                        throw new System.IO.EndOfStreamException("StoreOffset behind end of file");
                }

                if (_stream.Length > 128) // For sizes > 128 MB, we return a file based slob dictionary
                {
                    var streamCopy = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var dictionary = new SlobDictionaryFileBased(streamCopy, encoding, compression, tagDict, contentTypes, refItems, storeOffsets.Select(o => new StoreItemFileBased((long)o + storeOffset2)).ToArray());
                    return dictionary;
                }
                else // For sizes that are relatively small we can make an in-memory dictionary
                {
                    // Read-in the blobs
                    StoreItemInMemory[] storeItems = new StoreItemInMemory[storeCount];

                    for (int i = 0; i < storeCount; ++i)
                    {
                        _stream.Seek(storeOffset2 + (long)storeOffsets[i], SeekOrigin.Begin);
                        uint count = BigEndianBitConverter.ToUInt32(_stream, _buffer);
                        byte[] contentIds = new byte[count];
                        for (int k = 0; k < count; ++k)
                        {
                            contentIds[k] = (byte)_stream.ReadByte();
                        }

                        uint contentLen = BigEndianBitConverter.ToUInt32(_stream, _buffer);
                        byte[] contentBytes = new byte[contentLen];
                        _stream.Read(contentBytes, 0, (int)contentLen);
                        storeItems[i] = new StoreItemInMemory(contentIds, contentBytes);
                    }

                    return new SlobDictionaryInMemory(encoding, compression, tagDict, contentTypes, refItems, storeItems);
                }
            }
        }

        /// <summary>
        /// Writes the specified dictionary as a slob file. Text is encoded as UTF8, and compression is zlib.
        /// </summary>
        /// <param name="dict">The dictionary to write.</param>
        /// <param name="mimeContentType">MIME content type of the values of the dictionary.</param>
        public void Write(Dictionary<string, string> dict, string mimeContentType)
        {
            // for format details see
            // https://github.com/itkach/slob


            KeyValuePair<string, string>[] listDict = dict.ToArray();
            CompareInfo myComp_enUS = new CultureInfo("en-US", false).CompareInfo;
            SortKey[] sortKeys = listDict.Select(x => myComp_enUS.GetSortKey(x.Key)).ToArray();
            Array.Sort(sortKeys, listDict, new UnicodeStringSorter());

            using (_stream = new FileStream(_fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                System.Text.Encoding encoding = System.Text.UTF8Encoding.UTF8;

                BigEndianBitConverter.WriteUInt64(SupposedMagic, _stream);

                BigEndianBitConverter.WriteUInt64(SupposedVersionHi, _stream);

                BigEndianBitConverter.WriteUInt64(SupposedVersionLo, _stream);

                BigEndianBitConverter.WriteTinyText("utf-8", _stream, encoding);

                BigEndianBitConverter.WriteTinyText("zlib", _stream, encoding);

                // Tag count
                _stream.WriteByte(0);

                // char-sized sequence of content types
                _stream.WriteByte(1); // there is only one content type here
                BigEndianBitConverter.WriteText(mimeContentType, _stream, encoding);



                // Blobcount
                long posBlobCount = _stream.Position;
                BigEndianBitConverter.WriteUInt32(0, _stream); // PlaceHolder for BlobCount

                // Store offset
                long posStoreOffset = _stream.Position;
                BigEndianBitConverter.WriteUInt64(0, _stream);

                // Size
                long posSize = _stream.Position;
                BigEndianBitConverter.WriteUInt64(0, _stream);

                // list of long-positioned refs
                BigEndianBitConverter.WriteUInt32((uint)listDict.Length, _stream);
                long posRefTablePositions = _stream.Position;



                long posRefTableBegin = _stream.Position + 8 * listDict.Length;
                _stream.Seek(posRefTableBegin, SeekOrigin.Begin);

                int i = -1;
                int binIndex = 0;
                int itemIndex = 0;
                int contentLength = 0;
                List<int> listOfEntryCounts = new List<int>(); // for every store item, this list contains the number of elements stored into it

                foreach (KeyValuePair<string, string> entry in listDict)
                {
                    ++i;
                    // Store current stream position in refTable
                    {
                        long currentPos = _stream.Position;
                        _stream.Seek(posRefTablePositions + i * 8, SeekOrigin.Begin);
                        BigEndianBitConverter.WriteUInt64((ulong)(currentPos - posRefTableBegin), _stream);
                        _stream.Seek(currentPos, SeekOrigin.Begin);
                    }

                    BigEndianBitConverter.WriteText(entry.Key, _stream, encoding);

                    BigEndianBitConverter.WriteUInt32((uint)binIndex, _stream);
                    BigEndianBitConverter.WriteUInt16((ushort)itemIndex, _stream);
                    BigEndianBitConverter.WriteTinyText(string.Empty, _stream, encoding);


                    ++itemIndex;
                    contentLength += entry.Value.Length;
                    if (itemIndex >= 32768 || contentLength > 320 * 1024) // limit one store item to 32767 entries or a maximum length of 320 kB
                    {
                        listOfEntryCounts.Add(itemIndex);
                        contentLength = 0;
                        itemIndex = 0;
                        ++binIndex;
                    }
                }
                if (itemIndex != 0)
                {
                    listOfEntryCounts.Add(itemIndex);
                }

                // Write the blob count and store offset
                {
                    long currentPos = _stream.Position;
                    _stream.Seek(posBlobCount, SeekOrigin.Begin);
                    BigEndianBitConverter.WriteUInt32((uint)listOfEntryCounts.Count, _stream); // Blob-Count

                    // Write the store offset
                    BigEndianBitConverter.WriteUInt64((ulong)currentPos, _stream);

                    _stream.Seek(currentPos, SeekOrigin.Begin);
                }

                // list of long-positioned store items
                BigEndianBitConverter.WriteUInt32((uint)listOfEntryCounts.Count, _stream); // Store count

                long posStoreOffsetTable = _stream.Position;
                _stream.Seek(posStoreOffsetTable + 8 * listOfEntryCounts.Count, SeekOrigin.Begin);
                long posStoreBegin = _stream.Position;

                int itemIndexOffset = 0;
                for (binIndex = 0; binIndex < listOfEntryCounts.Count; ++binIndex)
                {
                    {
                        long currentPos = _stream.Position;
                        _stream.Seek(posStoreOffsetTable + 8 * binIndex, SeekOrigin.Begin);
                        BigEndianBitConverter.WriteUInt64((ulong)(currentPos - posStoreBegin), _stream);
                        _stream.Seek(currentPos, SeekOrigin.Begin);
                    }

                    BigEndianBitConverter.WriteUInt32((uint)listOfEntryCounts[binIndex], _stream);
                    for (int j = 0; j < listOfEntryCounts[binIndex]; ++j)
                    {
                        _stream.WriteByte(0); // Table with content ids
                    }

                    long posContentLength = _stream.Position;
                    BigEndianBitConverter.WriteUInt32(0, _stream); // Placeholder for content length
                                                                   // compressStream = new System.IO.Compression.DeflateStream(_stream, System.IO.Compression.CompressionLevel.Optimal, true);
                    using (ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream compressStream = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream(_stream, new ICSharpCode.SharpZipLib.Zip.Compression.Deflater(), 1024 * 1024) { IsStreamOwner = false })
                    {

                        // now write the content

                        // First list of item offsets (without count)
                        int itemPositionOffset = 0;
                        for (int k = 0; k < listOfEntryCounts[binIndex]; ++k)
                        {
                            BigEndianBitConverter.WriteUInt32((uint)itemPositionOffset, compressStream);
                            itemPositionOffset += 4 + encoding.GetByteCount(listDict[itemIndexOffset + k].Value);
                        }

                        // now the content itself
                        for (int k = 0; k < listOfEntryCounts[binIndex]; ++k)
                        {

                            BigEndianBitConverter.WriteBigText(listDict[itemIndexOffset + k].Value, compressStream, encoding);
                        }

                        itemIndexOffset += listOfEntryCounts[binIndex];

                        compressStream.Flush();
                        compressStream.Close();
                    }

                    {
                        // Write content length
                        long currentPosition = _stream.Position;
                        _stream.Seek(posContentLength, SeekOrigin.Begin);
                        BigEndianBitConverter.WriteUInt32((uint)(currentPosition - posContentLength - 4), _stream);
                        _stream.Seek(currentPosition, SeekOrigin.Begin);
                    }



                }

            }
        }
    }


}
