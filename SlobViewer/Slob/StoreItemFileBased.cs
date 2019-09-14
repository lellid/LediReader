// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.IO;
using System.Text;
using SlobViewer.Common;

namespace SlobViewer.Slob
{
    /// <summary>
    /// Part of a <see cref="SlobDictionaryFileBased"/>.
    /// A <see cref="StoreItemFileBased"/> holds the position where to find the contents in the file. After reading the SLOB file,
    /// the content is stored in compressed form. If the contents is accessed, it is decompressed.
    /// </summary>
    public class StoreItemFileBased : StoreItemBase
    {
        long _filePosition;


        public StoreItemFileBased(long filePosition)
        {
            _filePosition = filePosition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreItemFileBased"/> class.
        /// </summary>
        /// <param name="contentIds">The array of content IDs.</param>
        /// <param name="compressedContent">Compressed content as retrieved from the SLOB file.</param>
        public StoreItemFileBased(byte[] contentIds, byte[] compressedContent)
        {
            _contentIds = contentIds;
            _compressedContent = compressedContent;
        }

        /// <summary>
        /// Get the content item with index <paramref name="idx"/>.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <returns>A tuple, consisting of the content and content id at index <paramref name="idx"/>.</returns>
        public (string Content, int ContentId) GetAt(int idx, Stream stream, Encoding encoding, string compression, byte[] buffer)
        {
            ReadCompressedContentFromStream(stream, buffer);

            // StoreCompressedContentForDebugging();

            if (null != _compressedContent)
            {
                DecompressContent(encoding, compression, buffer);

                _compressedContent = null;
            }

            return (_content[idx], _contentIds[idx]);
        }


        private void StoreCompressedContentForDebugging()
        {
            using (var s = new FileStream("C:\\TEMP\\CompressedContent.XXX", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                s.Write(_compressedContent, 0, _compressedContent.Length);
            }
        }



        private void ReadCompressedContentFromStream(Stream stream, byte[] buffer)
        {
            stream.Seek(_filePosition, SeekOrigin.Begin);
            stream.Read(buffer, 0, 4);
            uint count = BigEndianBitConverter.ToUInt32(buffer, 0);
            _contentIds = new byte[count];
            for (int k = 0; k < count; ++k)
            {
                _contentIds[k] = (byte)stream.ReadByte();
            }

            stream.Read(buffer, 0, 4);
            uint contentLen = BigEndianBitConverter.ToUInt32(buffer, 0);
            _compressedContent = new byte[contentLen];
            stream.Read(_compressedContent, 0, (int)contentLen);
        }
    }
}
